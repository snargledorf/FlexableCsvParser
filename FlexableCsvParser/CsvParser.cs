using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlexableCsvParser.StateMachine;
using Tokensharp;

namespace FlexableCsvParser
{
    public class CsvParser
    {
        private readonly TextReader _reader;
        private readonly CsvParserConfig _config;
        private readonly bool _trimLeadingWhiteSpace;
        private readonly bool _trimTrailingWhiteSpace;
        
        private static readonly IState StartState = StartOfFieldState.Instance;
        
        private readonly string _quote;
        private readonly int _escapeLength;

        private readonly ReadBuffer _recordBuffer = new(4096);

        private int _currentFieldStartIndex;
        private int _escapedQuoteCount;

        private int _fieldExaminedLength;
        private int _fieldLength;
        private int _leadingWhiteSpaceLength;
        private int _possibleTrailingWhiteSpaceLength;

        private readonly RecordFieldInfo[] _currentRecordFields;
        private readonly int _expectedRecordFieldCount;

        private readonly StringPool _stringPool;
        private int _recordBufferObserved;

        private uint _recordCount;
        private int _fieldCount;

        private readonly TokenConfiguration<CsvTokens> _tokenConfiguration;

        public int FieldCount => _fieldCount;

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

            _tokenConfiguration = config.TokenConfiguration;

            _expectedRecordFieldCount = recordLength;

            _currentRecordFields = new RecordFieldInfo[_expectedRecordFieldCount];

            _quote = config.Delimiters.Quote;
            _escapeLength = config.Delimiters.Escape.Length;

            _stringPool = new StringPool(config.StringCacheMaxLength);
        }

        public ValueTask<int> ReadRecordAsync(Memory<string> record, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(Task.Factory.StartNew((state) =>
            {
                if (state is (CsvParser parser, Memory<string> recordMemory))
                    return parser.ReadRecord(recordMemory.Span);
                
                Debug.Assert(false, "Invalid state");
                return -1;
            }, (this, record), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
        }

        public int ReadRecord(Span<string> record)
        {
            if (Read())
            {
                int bufferLength = Math.Min(record.Length, _expectedRecordFieldCount);
                if (bufferLength > _fieldCount && _config.IncompleteRecordHandling == IncompleteRecordHandling.TruncateRecord)
                    bufferLength = _fieldCount;

                for (int fieldIndex = 0; fieldIndex < bufferLength; fieldIndex++)
                    record[fieldIndex] = GetString(fieldIndex)!;

                return bufferLength;
            }

            return 0;
        }

        public string? GetString(int fieldIndex)
        {
            if (fieldIndex >= _expectedRecordFieldCount)
                throw new ArgumentOutOfRangeException(nameof(fieldIndex));

            if (fieldIndex >= _fieldCount)
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

            ref RecordFieldInfo fieldInfo = ref _currentRecordFields[fieldIndex];
            if (fieldInfo.Length == 0)
                return string.Empty;

            ReadOnlySpan<char> fieldSpan = _recordBuffer.Chars.Slice(fieldInfo.StartIndex, fieldInfo.Length);

            if (fieldInfo.EscapedQuoteCount == 0)
                return _stringPool.GetString(fieldSpan);

            int quoteEscapeLengthDiff = (_escapeLength * fieldInfo.EscapedQuoteCount) - (_quote.Length * fieldInfo.EscapedQuoteCount);

            int fieldUpdatedLength = fieldInfo.Length - quoteEscapeLengthDiff;

            char[] strBuffer = ArrayPool<char>.Shared.Rent(fieldUpdatedLength);
            try
            {
                Span<char> strBufferSpan = strBuffer.AsSpan()[..fieldUpdatedLength];
                ReadOnlySpan<char> resultSpan = strBufferSpan;
                var tokenParser = new TokenParser<CsvTokens>(_tokenConfiguration);
                while (!fieldSpan.IsEmpty)
                {
                    if (!tokenParser.TryParseToken(fieldSpan, false, out TokenType<CsvTokens>? type, out ReadOnlySpan<char> lexeme))
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
            IState currentState = StartState;

            _fieldCount = 0;
            _currentFieldStartIndex = 0;
            _fieldExaminedLength = 0;
            
            _recordBuffer.AdvanceBuffer(_recordBufferObserved);
            _recordBufferObserved = 0;

            do
            {
                int resumeLocationForTokenBuffer = _recordBufferObserved + _fieldExaminedLength;
                ReadOnlySpan<char> tokenBuffer = _recordBuffer.Chars[resumeLocationForTokenBuffer..];
                var tokenParser = new TokenParser<CsvTokens>(_tokenConfiguration);
                while (tokenParser.TryParseToken(tokenBuffer, !_recordBuffer.EndOfReader, out TokenType<CsvTokens>? type, out int lexemeLength))
                {
                    tokenBuffer = tokenBuffer[lexemeLength..];
                    _fieldExaminedLength += lexemeLength;

                    IState previousState = currentState;

                    if (currentState.TryTransition(type, out IState? newState))
                    {
                        currentState = newState;
                        switch (currentState.Id)
                        {
                            case ParserState.UnquotedFieldText:
                            case ParserState.QuotedFieldText:
                                _fieldLength += lexemeLength + _leadingWhiteSpaceLength + _possibleTrailingWhiteSpaceLength;
                                _leadingWhiteSpaceLength = _possibleTrailingWhiteSpaceLength = 0;
                                break;

                            case ParserState.EndOfField:
                                AddCurrentField();
                                break;

                            case ParserState.EndOfRecord:
                                CheckRecord();
                                return true;

                            case ParserState.EscapeAfterLeadingEscape:
                                _escapedQuoteCount++;
                                _fieldLength += _escapeLength;
                                break;

                            case ParserState.QuotedFieldEscape:
                                _escapedQuoteCount++;
                                _fieldLength += lexemeLength + _leadingWhiteSpaceLength + _possibleTrailingWhiteSpaceLength;
                                _leadingWhiteSpaceLength = _possibleTrailingWhiteSpaceLength = 0;
                                break;

                            case ParserState.QuotedFieldLeadingWhiteSpace:
                            case ParserState.LeadingWhiteSpace:
                                // Only store the leading whitespace if there is a possiblity we might need it
                                // IE. If trim leading isn't enabled
                                if (_trimLeadingWhiteSpace)
                                    _currentFieldStartIndex += lexemeLength;
                                else
                                    _leadingWhiteSpaceLength = lexemeLength;
                                break;

                            case ParserState.QuotedFieldTrailingWhiteSpace:
                            case ParserState.UnquotedFieldTrailingWhiteSpace:
                                // Only store the trailing whitespace if trailing whitespace trimming is enabled
                                // This is so if we do end up with more field text, we can append the whitespace
                                // since that means it isn't trailing
                                if (_trimTrailingWhiteSpace)
                                    _possibleTrailingWhiteSpaceLength = lexemeLength;
                                else
                                    _fieldLength += lexemeLength;

                                break;

                            case ParserState.UnexpectedToken:
                                ThrowUnexpectedTokenException(previousState, tokenBuffer);
                                break;

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
                                _currentFieldStartIndex += lexemeLength + _leadingWhiteSpaceLength;
                                _leadingWhiteSpaceLength = 0;
                                break;
                        }
                    }
                    else
                    {
                        ThrowUnexpectedTokenException(currentState, tokenBuffer);
                    }
                }
            } while (_recordBuffer.Read(_reader));

            if (_recordBuffer is { Length: 0, EndOfReader: true })
                return false;

            bool missingClosingQuote = currentState.Id == ParserState.QuotedFieldText;
            if (!missingClosingQuote && currentState.TryGetDefault(out IState? defaultState))
                missingClosingQuote = defaultState.Id == ParserState.QuotedFieldText;

            if (missingClosingQuote)
                throw new Exception($"Final quoted field did not have a closing quote: State = {currentState}, Buffer = {_recordBuffer}");

            CheckRecord();
            return true;
        }

        private void ThrowUnexpectedTokenException(IState state, ReadOnlySpan<char> tokenBuffer)
        {
            throw new InvalidDataException($"Unexpected token: State = {state}, Buffer = {tokenBuffer}, Record count = {_recordCount}");
        }

        private void CheckRecord()
        {
            AddCurrentField();

            if (_fieldCount < _expectedRecordFieldCount && _config.IncompleteRecordHandling == IncompleteRecordHandling.ThrowException)
                throw new InvalidDataException($"Record is incomplete: {string.Join(", ", Enumerable.Range(0, _fieldCount).Select(GetString))}");
            
            _recordCount++;
        }

        private void AddCurrentField()
        {
            if (_fieldCount == _expectedRecordFieldCount)
                throw new InvalidDataException($"Record is too long: {string.Join(", ", Enumerable.Range(0, _fieldCount).Select(GetString))}");
            
            _fieldLength += _leadingWhiteSpaceLength;
            
            ref RecordFieldInfo fieldInfo = ref _currentRecordFields[_fieldCount++];
            fieldInfo = new RecordFieldInfo(_currentFieldStartIndex, _fieldLength, _escapedQuoteCount);

            _recordBufferObserved += _fieldExaminedLength;
            _currentFieldStartIndex = _recordBufferObserved;

            _fieldLength = _escapedQuoteCount =
                _leadingWhiteSpaceLength = _possibleTrailingWhiteSpaceLength = _fieldExaminedLength = 0;
        }
    }
}
