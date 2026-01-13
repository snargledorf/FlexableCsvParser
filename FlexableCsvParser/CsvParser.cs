using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SwiftState;
using Tokensharp;
using Tokensharp.StateMachine;

namespace FlexableCsvParser
{
    public class CsvParser
    {
        private readonly TextReader _reader;
        private readonly CsvParserConfig _config;
        private readonly bool _trimLeadingWhiteSpace;
        private readonly bool _trimTrailingWhiteSpace;

        private State<TokenType<CsvTokens>, ParserState> _startState;

        private readonly string _quote;
        private readonly int _escapeLength;

        private Memory<char> _readBuffer = new char[4096];
        private int _readBufferLength;

        private bool _endOfReader;
        private int _startOfRecordIndex;

        private int _currentFieldStartIndex;
        private int _escapedQuoteCount;

        private int _fieldLength;
        private int _leadingWhiteSpaceLength;
        private int _possibleTrailingWhiteSpaceLength;

        private List<RecordFieldInfo> _currentRecordFields;
        private int _expectedRecordFieldCount;

        private StringPool _stringPool;
        private int _recordBufferObserved;
        
        private readonly TokenReaderStateMachine<CsvTokens> _tokenReaderStateMachine;

        public CsvParser(TextReader reader, int recordLength)
            : this(reader, recordLength, CsvParserConfig.Default)
        {
        }

        public CsvParser(TextReader reader, int recordLength, CsvParserConfig config)
        {
            if (recordLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(recordLength));

            _reader = reader;
            _config = config;
            
            _trimLeadingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Leading);
            _trimTrailingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Trailing);

            TokenConfiguration<CsvTokens> tokenConfiguration;
            if (!config.Delimiters.AreRFC4180Compliant)
            {
                tokenConfiguration = new TokenConfigurationBuilder<CsvTokens>()
                {
                    { config.Delimiters.Field, CsvTokens.FieldDelimiter },
                    { config.Delimiters.EndOfRecord, CsvTokens.EndOfRecord },
                    { config.Delimiters.Quote, CsvTokens.Quote },
                    { config.Delimiters.Escape, CsvTokens.Escape },
                }.Build();
            }
            else
            {
                tokenConfiguration = CsvTokens.Configuration;
            }
            
            _tokenReaderStateMachine = TokenReaderStateMachine<CsvTokens>.For(tokenConfiguration);
            
            _startState = CsvParserStateMachineFactory.BuildParserStateMachine();

            _expectedRecordFieldCount = recordLength;

            _currentRecordFields = new List<RecordFieldInfo>(_expectedRecordFieldCount);

            _quote = config.Delimiters.Quote;
            _escapeLength = config.Delimiters.Escape.Length;

            _stringPool = new(config.StringCacheMaxLength);
        }

        public ValueTask<int> ReadRecordAsync(Memory<string> record)
        {
            return new ValueTask<int>(Task.Factory.StartNew((state) =>
            {
                (CsvParser parser, Memory<string> r) = ((CsvParser, Memory<string>))state;
                return parser.ReadRecord(r.Span);
            }, (this, record), default, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
        }

        public int ReadRecord(Span<string> record)
        {
            if (Read())
            {
                int bufferLength = Math.Min(record.Length, _expectedRecordFieldCount);
                if (bufferLength > FieldCount && _config.IncompleteRecordHandling == IncompleteRecordHandling.TruncateRecord)
                    bufferLength = FieldCount;

                string[] buffer = ArrayPool<string>.Shared.Rent(bufferLength);
                try
                {
                    Span<string> bufferSpan = buffer[..bufferLength];
                    for (int fieldIndex = 0; fieldIndex < bufferLength; fieldIndex++)
                        bufferSpan[fieldIndex] = GetString(fieldIndex);

                    bufferSpan.CopyTo(record);

                    return bufferLength;
                }
                finally
                {
                    ArrayPool<string>.Shared.Return(buffer);
                }
            }

            return 0;
        }

        public int FieldCount => _currentRecordFields.Count;

        public string GetString(int fieldIndex)
        {
            if (fieldIndex >= _expectedRecordFieldCount)
                throw new ArgumentOutOfRangeException(nameof(fieldIndex));

            // TODO Cache values

            if (fieldIndex >= FieldCount)
            {
                switch (_config.IncompleteRecordHandling)
                {
                    case IncompleteRecordHandling.ThrowException:
                        throw new InvalidDataException($"Record is incomplete"); // TODO output partial record

                    case IncompleteRecordHandling.FillInWithEmpty:
                        return string.Empty;

                    case IncompleteRecordHandling.FillInWithNull:
                        return null;

                    case IncompleteRecordHandling.TruncateRecord:
                        throw new ArgumentOutOfRangeException(nameof(fieldIndex));

                    default:
                        throw new InvalidOperationException($"Not a valid {nameof(IncompleteRecordHandling)}");
                }
            }

            ref var fieldInfo = ref CollectionsMarshal.AsSpan(_currentRecordFields)[fieldIndex];
            if (fieldInfo.Length == 0)
                return string.Empty;

            ReadOnlySpan<char> recordBuffer = _readBuffer.Span[_startOfRecordIndex.._readBufferLength];

            ReadOnlySpan<char> fieldSpan = recordBuffer.Slice(fieldInfo.StartIndex, fieldInfo.Length);

            if (fieldInfo.EscapedQuoteCount == 0)
                return _stringPool.GetString(fieldSpan);

            int quoteEscapeLengthDiff = (_escapeLength * fieldInfo.EscapedQuoteCount) - (_quote.Length * fieldInfo.EscapedQuoteCount);

            int fieldUpdatedLength = fieldInfo.Length - quoteEscapeLengthDiff;

            char[] strBuffer = ArrayPool<char>.Shared.Rent(fieldUpdatedLength);
            try
            {
                Span<char> strBufferSpan = strBuffer.AsSpan()[..fieldUpdatedLength];
                ReadOnlySpan<char> resultSpan = strBufferSpan;
                while (!fieldSpan.IsEmpty)
                {
                    if (!Tokenizer.TryParseToken(fieldSpan, _tokenReaderStateMachine, false, out TokenType<CsvTokens>? type, out ReadOnlySpan<char> lexeme))
                        throw new Exception("Unable to parse field token");

                    if (type == CsvTokens.Escape)
                    {
                        _quote.CopyTo(strBufferSpan);
                        strBufferSpan = strBufferSpan[_quote.Length..];
                    }
                    else
                    {
                        lexeme.CopyTo(strBufferSpan);
                        strBufferSpan = strBufferSpan[lexeme.Length..];
                    }

                    fieldSpan = fieldSpan[lexeme.Length..];
                }

                return _stringPool.GetString(resultSpan);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(strBuffer);
            }
        }

        public bool Read()
        {
            State<TokenType<CsvTokens>, ParserState> currentState = _startState;

            _currentRecordFields.Clear();

            _startOfRecordIndex += _recordBufferObserved;
            _recordBufferObserved = 0;

            _currentFieldStartIndex = 0;

            do
            {
                ReadOnlySpan<char> recordBuffer = _readBuffer.Span[_startOfRecordIndex.._readBufferLength];
                if (recordBuffer.IsEmpty)
                    continue;

                State<TokenType<CsvTokens>, ParserState> previousState;

                ReadOnlySpan<char> tokenBuffer = recordBuffer[_recordBufferObserved..];
                while (Tokenizer.TryParseToken(tokenBuffer, _tokenReaderStateMachine, !_endOfReader, out TokenType<CsvTokens>? type, out ReadOnlySpan<char> lexeme))
                {
                    tokenBuffer = tokenBuffer[lexeme.Length..];

                    _recordBufferObserved = recordBuffer.Length - tokenBuffer.Length;

                    previousState = currentState;

                    if (currentState.TryTransition(type, out State<TokenType<CsvTokens>, ParserState>? newState))
                    {
                        currentState = newState;
                        switch (currentState.Id)
                        {
                            case ParserState.UnquotedFieldText:
                            case ParserState.QuotedFieldText:
                                _fieldLength += lexeme.Length + _leadingWhiteSpaceLength + _possibleTrailingWhiteSpaceLength;
                                _leadingWhiteSpaceLength = _possibleTrailingWhiteSpaceLength = 0;
                                break;

                            case ParserState.EndOfField:
                                AddCurrentField();
                                _currentFieldStartIndex = recordBuffer.Length - tokenBuffer.Length;
                                break;

                            case ParserState.EndOfRecord:
                                CheckRecord();
                                _currentFieldStartIndex = recordBuffer.Length - tokenBuffer.Length;
                                return true;

                            case ParserState.EscapeAfterLeadingEscape:
                                _escapedQuoteCount++;
                                _fieldLength += _escapeLength;
                                break;

                            case ParserState.QuotedFieldEscape:
                                _escapedQuoteCount++;
                                _fieldLength += lexeme.Length + _leadingWhiteSpaceLength + _possibleTrailingWhiteSpaceLength;
                                _leadingWhiteSpaceLength = _possibleTrailingWhiteSpaceLength = 0;
                                break;

                            case ParserState.QuotedFieldLeadingWhiteSpace:
                            case ParserState.LeadingWhiteSpace:
                                // Only store the leading whitespace if there is a possiblity we might need it
                                // IE. If trim leading isn't enabled
                                if (_trimLeadingWhiteSpace)
                                    _currentFieldStartIndex += lexeme.Length;
                                else
                                    _leadingWhiteSpaceLength = lexeme.Length;
                                break;

                            case ParserState.QuotedFieldTrailingWhiteSpace:
                            case ParserState.UnquotedFieldTrailingWhiteSpace:
                                // Only store the trailing whitespace if trailing whitespace trimming is enabled
                                // This is so if we do end up with more field text, we can append the whitespace
                                // since that means it isn't trailing
                                if (_trimTrailingWhiteSpace)
                                    _possibleTrailingWhiteSpaceLength = lexeme.Length;
                                else
                                    _fieldLength += lexeme.Length;

                                break;

                            case ParserState.UnexpectedToken:
                                throw new InvalidDataException($"Unexpected token: State = {previousState}, Buffer = {tokenBuffer}");

                            case ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace:
                            case ParserState.QuotedFieldClosingQuote:
                                // NoOp
                                break;

                            case ParserState.LeadingEscape:
                                _currentFieldStartIndex += _leadingWhiteSpaceLength + _quote.Length; // TODO Handle escapes that aren't "" (Ex. &quote;)
                                _leadingWhiteSpaceLength = 0;
                                break;

                            case ParserState.QuoteAfterLeadingEscape:
                                _escapedQuoteCount++;
                                _fieldLength += _escapeLength;
                                break;

                            default:
                                _currentFieldStartIndex += lexeme.Length + _leadingWhiteSpaceLength;
                                _leadingWhiteSpaceLength = 0;
                                break;
                        }
                    }
                    else
                    {
                        switch (currentState.Id)
                        {
                            case ParserState.QuotedFieldText:
                            case ParserState.UnquotedFieldText:
                                _fieldLength += lexeme.Length;
                                break;

                            case ParserState.EscapeAfterLeadingEscape: // I don't think this will ever be hit???
                                _escapedQuoteCount++;
                                _fieldLength += lexeme.Length;
                                break;

                            default:
                                throw new InvalidDataException($"Unexpected state: State = {currentState}, Buffer = {tokenBuffer}");
                        }
                    }
                }
            } while (CheckBuffer());

            if (currentState == _startState)
                return false;

            bool missingClosingQuote = currentState.Id == ParserState.QuotedFieldText;
            if (!missingClosingQuote && currentState.TryGetDefault(out State<TokenType<CsvTokens>, ParserState>? defaultState))
                missingClosingQuote = defaultState.Id == ParserState.QuotedFieldText;

            if (missingClosingQuote)
                throw new Exception($"Final quoted field did not have a closing quote: State = {currentState}, Buffer = {_readBuffer}");

            CheckRecord();
            return true;
        }

        private bool CheckBuffer()
        {
            if (!_endOfReader)
            {
                if (_startOfRecordIndex != 0)
                {
                    this._readBuffer.Span[_startOfRecordIndex.._readBufferLength].CopyTo(this._readBuffer.Span);

                    _readBufferLength -= _startOfRecordIndex;

                    _startOfRecordIndex = 0;
                }
                else if (_readBufferLength == this._readBuffer.Length)
                {
                    int newLength = this._readBuffer.Length * 2;

                    // Check for overflow
                    if (newLength < this._readBuffer.Length)
                        throw new OutOfMemoryException();

                    var oldBuffer = this._readBuffer;
                    this._readBuffer = new char[newLength];

                    oldBuffer.CopyTo(this._readBuffer);
                }

                Span<char> readBuffer = this._readBuffer.Span[_readBufferLength..];
                int charsRead = _reader.Read(readBuffer);
                _readBufferLength += charsRead;

                _endOfReader = charsRead < readBuffer.Length && _reader.Peek() == -1;
            }

            int currentReadIndex = _startOfRecordIndex + _recordBufferObserved;

            return currentReadIndex != _readBufferLength;
        }

        private void CheckRecord()
        {
            AddCurrentField();

            if (FieldCount < _expectedRecordFieldCount && _config.IncompleteRecordHandling == IncompleteRecordHandling.ThrowException)
                throw new InvalidDataException($"Record is incomplete"); // TODO output partial record
        }

        private void AddCurrentField()
        {
            _fieldLength += _leadingWhiteSpaceLength;

            var fieldInfo = new RecordFieldInfo(_currentFieldStartIndex, _fieldLength, _escapedQuoteCount);
            _currentRecordFields.Add(fieldInfo);

            _currentFieldStartIndex += _fieldLength;
            _fieldLength = _escapedQuoteCount = _leadingWhiteSpaceLength = _possibleTrailingWhiteSpaceLength = 0;
        }
    }
}
