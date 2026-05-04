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
    public class CsvParser : IDisposable
    {
        private const int InitialRecordBufferSize = 4096;
        
        private readonly TextReader _reader;
        private readonly CsvParserConfig _config;
        private readonly bool _trimLeadingWhiteSpace;
        private readonly bool _trimTrailingWhiteSpace;
        
        private static readonly IState StartState = StartOfFieldState.Instance;

        private readonly ReadBuffer _recordBuffer = new(InitialRecordBufferSize);

        private int _fieldExaminedLength;
        private ReadOnlyMemory<char> _leadingWhiteSpace;
        private ReadOnlyMemory<char> _possibleTrailingWhiteSpace;

        private readonly ReadOnlySequence<char>[] _currentRecordFields;
        private readonly int _expectedRecordFieldCount;

        private readonly StringPool _stringPool;
        private readonly FieldSequenceBuilder _fieldBuilder;
        private int _recordBufferObserved;

        private uint _recordCount;
        private int _fieldCount;

        private char[] _getStringBuffer;

        private TokenParserState<CsvTokens> _tokenParserState;

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

            _expectedRecordFieldCount = recordLength;

            _currentRecordFields = new ReadOnlySequence<char>[_expectedRecordFieldCount];

            _stringPool = new StringPool(config.StringCacheMaxLength);
            _fieldBuilder = new FieldSequenceBuilder(config.Delimiters.Quote.AsMemory());
            _tokenParserState = new TokenParserState<CsvTokens>(config.TokenConfiguration);
            _getStringBuffer = new char[InitialRecordBufferSize];
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

            ref ReadOnlySequence<char> fieldInfo = ref _currentRecordFields[fieldIndex];
            if (fieldInfo.IsEmpty)
                return string.Empty;

            if (fieldInfo.IsSingleSegment)
                return _stringPool.GetString(fieldInfo.FirstSpan);

            var length = (int)fieldInfo.Length;
            
            if (_getStringBuffer.Length < length)
            {
                int newSize = _getStringBuffer.Length * 2;
                if ((uint)newSize > Array.MaxLength) 
                    newSize = Math.Max(length, Array.MaxLength);
                
                Array.Resize(ref _getStringBuffer, newSize);
            }
            
            fieldInfo.CopyTo(_getStringBuffer);
            return _stringPool.GetString(_getStringBuffer.AsSpan()[..length]);
        }

        public bool Read()
        {
            IState? currentState = StartState;

            _fieldCount = 0;
            _fieldExaminedLength = 0;
            _fieldBuilder.Reset();

            _recordBuffer.AdvanceBuffer(_recordBufferObserved);
            _recordBufferObserved = 0;

            do
            {
                int resumeLocationForTokenBuffer = _recordBufferObserved + _fieldExaminedLength;
                ReadOnlyMemory<char> tokenBuffer = _recordBuffer.Chars[resumeLocationForTokenBuffer..];
                var tokenParser = new TokenParser<CsvTokens>(tokenBuffer.Span, !_recordBuffer.EndOfReader, _tokenParserState);
                while (tokenParser.Read())
                {
                    int lexemeLength = tokenParser.Lexeme.Length;
                    
                    _fieldExaminedLength += lexemeLength;

                    IState previousState = currentState;

                    if (currentState.TryTransition(tokenParser.TokenType, out currentState))
                    {
                        switch (currentState.Id)
                        {
                            case ParserState.UnquotedFieldText:
                            case ParserState.QuotedFieldText:
                                if (!_leadingWhiteSpace.IsEmpty)
                                {
                                    _fieldBuilder.Append(_leadingWhiteSpace);
                                    _leadingWhiteSpace = default;
                                }
                                if (!_possibleTrailingWhiteSpace.IsEmpty)
                                {
                                    _fieldBuilder.Append(_possibleTrailingWhiteSpace);
                                    _possibleTrailingWhiteSpace = default;
                                }
                    
                                ReadOnlyMemory<char> tokenMemory = SliceTokenMemory(tokenBuffer, tokenParser, lexemeLength);
                                _fieldBuilder.Append(tokenMemory);
                                break;

                            case ParserState.EndOfField:
                                AddCurrentField();
                                break;

                            case ParserState.EndOfRecord:
                                CheckRecord();
                                return true;

                            case ParserState.EscapeAfterLeadingEscape:
                            case ParserState.QuotedFieldEscape:
                            case ParserState.QuoteAfterLeadingEscape:
                                if (!_leadingWhiteSpace.IsEmpty)
                                {
                                    _fieldBuilder.Append(_leadingWhiteSpace);
                                    _leadingWhiteSpace = default;
                                }
                                if (!_possibleTrailingWhiteSpace.IsEmpty)
                                {
                                    _fieldBuilder.Append(_possibleTrailingWhiteSpace);
                                    _possibleTrailingWhiteSpace = default;
                                }
                                _fieldBuilder.AppendQuote();
                                break;

                            case ParserState.QuotedFieldLeadingWhiteSpace:
                            case ParserState.LeadingWhiteSpace:
                                if (!_trimLeadingWhiteSpace)
                                    _leadingWhiteSpace = SliceTokenMemory(tokenBuffer, tokenParser, lexemeLength);
                                break;

                            case ParserState.QuotedFieldTrailingWhiteSpace:
                            case ParserState.UnquotedFieldTrailingWhiteSpace:
                                ReadOnlyMemory<char> possibleTrailingWhiteSpace = SliceTokenMemory(tokenBuffer, tokenParser, lexemeLength);
                                if (_trimTrailingWhiteSpace)
                                    _possibleTrailingWhiteSpace = possibleTrailingWhiteSpace;
                                else
                                    _fieldBuilder.Append(possibleTrailingWhiteSpace);

                                break;

                            case ParserState.UnexpectedToken:
                                throw new InvalidDataException($"Unexpected token: State = {previousState}, Buffer = {tokenBuffer}, Record count = {_recordCount}");

                            case ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace:
                            case ParserState.QuotedFieldClosingQuote:
                                // NoOp
                                break;

                            case ParserState.LeadingEscape:
                                _leadingWhiteSpace = default;
                                break;

                            default:
                                _leadingWhiteSpace = default;
                                break;
                        }
                    }
                    else
                    {
                        throw new InvalidDataException($"Unexpected token: State = {previousState}, Buffer = {tokenBuffer}, Record count = {_recordCount}");
                    }
                }

                _tokenParserState = tokenParser.CurrentState;
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

            ReadOnlyMemory<char> SliceTokenMemory(ReadOnlyMemory<char> tokenBuffer, TokenParser<CsvTokens> tokenParser, int lexemeLength)
            {
                return tokenBuffer.Slice(tokenParser.StartOfLexemeIndex, lexemeLength);
            }
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
            
            // Handles empty unquoted fields
            if (!_leadingWhiteSpace.IsEmpty)
            {
                if (!_fieldBuilder.IsEmpty)
                    throw new InvalidOperationException(
                        "Invalid state: Leading whitespace is not empty and field build is not empty");

                if (!_trimLeadingWhiteSpace && !_trimTrailingWhiteSpace)
                {
                    _fieldBuilder.Append(_leadingWhiteSpace);
                    _leadingWhiteSpace = default;
                }
            }

            _currentRecordFields[_fieldCount++] = _fieldBuilder.Build();

            _recordBufferObserved += _fieldExaminedLength;
            _fieldExaminedLength = 0;
            _possibleTrailingWhiteSpace = default;
        }

        public void Dispose()
        {
            _fieldBuilder.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }
}
