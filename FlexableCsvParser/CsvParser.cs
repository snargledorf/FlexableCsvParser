using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance.Buffers;
using Tokensharp;

namespace FlexableCsvParser
{
    public class CsvParser
    {
        private const int InitialRecordBufferSize = 4096;
        
        private readonly TextReader _reader;
        private readonly CsvParserConfig _config;
        private readonly bool _trimLeadingWhiteSpace;
        private readonly bool _trimTrailingWhiteSpace;
        
        private readonly string _quote;
        private readonly int _escapeLength;

        private readonly ReadBuffer _recordBuffer = new(InitialRecordBufferSize);

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

        private readonly TokenParserState<CsvTokens> _tokenParserState;

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

            _tokenParserState = new TokenParserState<CsvTokens>(config.TokenConfiguration);

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

            ReadOnlySpan<char> fieldSpan = _recordBuffer.Chars.Span.Slice(fieldInfo.StartIndex, fieldInfo.Length);

            if (fieldInfo.EscapedQuoteCount == 0)
                return _stringPool.GetString(fieldSpan);

            int quoteEscapeLengthDiff = (_escapeLength * fieldInfo.EscapedQuoteCount) - (_quote.Length * fieldInfo.EscapedQuoteCount);

            int fieldUpdatedLength = fieldInfo.Length - quoteEscapeLengthDiff;

            using SpanOwner<char> strBufferOwner = SpanOwner<char>.Allocate(fieldUpdatedLength);
            
            Span<char> currentPositionSpan = strBufferOwner.Span;
            ReadOnlySpan<char> quoteSpan = _quote.AsSpan();
            
            var tokenParser = new TokenParser<CsvTokens>(fieldSpan, _tokenParserState);
            while (tokenParser.Read())
            {
                if (tokenParser.TokenType == CsvTokens.Escape)
                {
                    quoteSpan.CopyTo(currentPositionSpan);
                    currentPositionSpan = currentPositionSpan[quoteSpan.Length..];
                }
                else
                {
                    tokenParser.Lexeme.CopyTo(currentPositionSpan);
                    currentPositionSpan = currentPositionSpan[tokenParser.Lexeme.Length..];
                }
            }
                
            if (tokenParser.CharsConsumed != fieldSpan.Length)
                throw new Exception("Unable to parse field token");

            return _stringPool.GetString(strBufferOwner.Span);
        }

        public bool Read()
        {
            var currentState = ParserState.StartOfRecord;

            _fieldCount = 0;
            _currentFieldStartIndex = 0;
            _fieldExaminedLength = 0;
            
            _recordBuffer.AdvanceBuffer(_recordBufferObserved);
            _recordBufferObserved = 0;

            do
            {
                int resumeLocationForTokenBuffer = _recordBufferObserved + _fieldExaminedLength;
                ReadOnlyMemory<char> tokenBuffer = _recordBuffer.Chars[resumeLocationForTokenBuffer..];
                var tokenParser = new TokenParser<CsvTokens>(tokenBuffer.Span, !_recordBuffer.EndOfReader, _tokenParserState);
                while (tokenParser.Read())
                {
                    _fieldExaminedLength += tokenParser.Lexeme.Length;

                    ParserState previousState = currentState;
                    currentState = CsvStateMachine.Transition(currentState, tokenParser.TokenType);

                    switch (currentState)
                    {
                        case ParserState.UnquotedFieldText:
                        case ParserState.QuotedFieldText:
                            _fieldLength += tokenParser.Lexeme.Length + _leadingWhiteSpaceLength + _possibleTrailingWhiteSpaceLength;
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
                            _fieldLength += tokenParser.Lexeme.Length + _leadingWhiteSpaceLength + _possibleTrailingWhiteSpaceLength;
                            _leadingWhiteSpaceLength = _possibleTrailingWhiteSpaceLength = 0;
                            break;

                        case ParserState.QuotedFieldLeadingWhiteSpace:
                        case ParserState.LeadingWhiteSpace:
                            // Only store the leading whitespace if there is a possiblity we might need it
                            // IE. If trim leading isn't enabled
                            if (_trimLeadingWhiteSpace)
                                _currentFieldStartIndex += tokenParser.Lexeme.Length;
                            else
                                _leadingWhiteSpaceLength = tokenParser.Lexeme.Length;
                            break;

                        case ParserState.QuotedFieldTrailingWhiteSpace:
                        case ParserState.UnquotedFieldTrailingWhiteSpace:
                            // Only store the trailing whitespace if trailing whitespace trimming is enabled
                            // This is so if we do end up with more field text, we can append the whitespace
                            // since that means it isn't trailing
                            if (_trimTrailingWhiteSpace)
                                _possibleTrailingWhiteSpaceLength = tokenParser.Lexeme.Length;
                            else
                                _fieldLength += tokenParser.Lexeme.Length;

                            break;

                        case ParserState.UnexpectedToken:
                            throw new InvalidDataException($"Unexpected token: State = {previousState}, Buffer = {tokenBuffer}, Record count = {_recordCount}");

                        case ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace:
                        case ParserState.QuotedFieldClosingQuote:
                            // NoOp
                            break;

                        case ParserState.LeadingEscape:
                            _currentFieldStartIndex += _leadingWhiteSpaceLength + _quote.Length;
                            _leadingWhiteSpaceLength = 0;
                            break;

                        case ParserState.QuoteAfterLeadingEscape:
                            _escapedQuoteCount++;
                            _fieldLength += _escapeLength;
                            break;

                        default:
                            _currentFieldStartIndex += tokenParser.Lexeme.Length + _leadingWhiteSpaceLength;
                            _leadingWhiteSpaceLength = 0;
                            break;
                    }
                }
            } while (_recordBuffer.Read(_reader));

            if (_recordBuffer is { Length: 0, EndOfReader: true })
                return false;

            bool missingClosingQuote = CheckIfCurrentStateIndicatesMissingClosingQuote(currentState);
            if (missingClosingQuote)
                throw new Exception($"Final quoted field did not have a closing quote: State = {currentState}, Buffer = {_recordBuffer}");

            CheckRecord();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckIfCurrentStateIndicatesMissingClosingQuote(ParserState currentState)
        {
            return currentState is
                ParserState.QuotedFieldText or 
                ParserState.QuotedFieldOpenQuote or
                ParserState.QuotedFieldEscape or
                ParserState.QuotedFieldLeadingWhiteSpace or
                ParserState.QuotedFieldTrailingWhiteSpace or
                ParserState.QuoteAfterLeadingEscape;
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
