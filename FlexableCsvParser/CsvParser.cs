using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using FastState;

namespace FlexableCsvParser
{
    public class CsvParser
    {
        private readonly CsvParserConfig config;
        private readonly ITokenizer tokenizer;
        private readonly bool trimLeadingWhiteSpace;
        private readonly bool trimTrailingWhiteSpace;

        private StateMachine<ParserState, TokenType> parserStateMachine;

        private readonly string quote;

        private MemoryStringBuilder currentRecordBuffer = new MemoryStringBuilder();

        private List<RecordFieldInfo> currentRecordFields;

        private int expectedRecordLength;

        private readonly MemoryStringBuilder leadingWhiteSpaceValue = new MemoryStringBuilder();
        private readonly MemoryStringBuilder possibleTrailingWhiteSpaceValue = new MemoryStringBuilder();

        private readonly MemoryStringBuilder fieldBuilder = new MemoryStringBuilder();

        public CsvParser(TextReader reader, int recordLength)
            : this(reader, recordLength, CsvParserConfig.Default)
        {
        }

        public CsvParser(TextReader reader, int recordLength, CsvParserConfig config)
            : this(Tokenizer.For(config.Delimiters, reader), recordLength, config)
        {
        }

        public CsvParser(ITokenizer tokenizer, int recordLength, CsvParserConfig config)
        {
            if (recordLength <= 0)
                throw new ArgumentOutOfRangeException("Record length required");

            this.config = config;
            this.tokenizer = tokenizer;

            trimLeadingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Leading);
            trimTrailingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Trailing);

            parserStateMachine = CsvParserStateMachineFactory.BuildParserStateMachine();

            expectedRecordLength = recordLength;

            currentRecordFields = new List<RecordFieldInfo>(expectedRecordLength);

            quote = config.Delimiters.Quote;
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
                int bufferLength = Math.Min(record.Length, expectedRecordLength);
                if (bufferLength > FieldCount && config.IncompleteRecordHandling == IncompleteRecordHandling.TruncateRecord)
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

        public int FieldCount => currentRecordFields.Count;

        public string GetString(int fieldIndex)
        {
            if (fieldIndex >= expectedRecordLength)
                throw new ArgumentOutOfRangeException(nameof(fieldIndex));

            // TODO Cache values

            if (fieldIndex >= FieldCount)
            {
                switch (config.IncompleteRecordHandling)
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

            var fieldInfo = currentRecordFields[fieldIndex];
            return currentRecordBuffer.Span.Slice(fieldInfo.StartIndex, fieldInfo.Length).ToString();
        }

        public bool Read()
        {
            var state = ParserState.Start;

            currentRecordFields.Clear();
            currentRecordBuffer.Clear();

            ParserState previousState;
            while (tokenizer.Read())
            {
                previousState = state;
                if (parserStateMachine.TryTransition(state, tokenizer.TokenType, out ParserState newState))
                {
                    state = newState;
                    switch (state)
                    {
                        case ParserState.UnquotedFieldText:
                        case ParserState.QuotedFieldText:
                            AppendLeadingWhiteSpace();
                            AppendTrailingWhiteSpace();
                            fieldBuilder.Append(tokenizer.TokenValue);
                            break;

                        case ParserState.EndOfField:
                            AddCurrentField();
                            break;

                        case ParserState.EndOfRecord:
                            CheckRecord();
                            return true;

                        case ParserState.EscapeAfterLeadingEscape:
                        case ParserState.QuotedFieldEscape:
                            AppendLeadingWhiteSpace();
                            AppendTrailingWhiteSpace();
                            fieldBuilder.Append(quote.AsSpan());
                            break;

                        case ParserState.QuotedFieldLeadingWhiteSpace:
                        case ParserState.LeadingWhiteSpace:
                            // Only store the leading whitespace if there is a possiblity we might need it
                            // IE. If trim leading isn't enabled
                            if (!trimLeadingWhiteSpace)
                                leadingWhiteSpaceValue.Append(tokenizer.TokenValue);
                            break;

                        case ParserState.QuotedFieldTrailingWhiteSpace:
                        case ParserState.UnquotedFieldTrailingWhiteSpace:
                            // Only store the trailing whitespace if trailing whitespace trimming is enabled
                            // This is so if we do end up with more field text, we can append the whitespace
                            // since that means it isn't trailing
                            if (trimTrailingWhiteSpace)
                                possibleTrailingWhiteSpaceValue.Append(tokenizer.TokenValue);
                            else
                                fieldBuilder.Append(tokenizer.TokenValue);
                            break;

                        case ParserState.UnexpectedToken:
                            throw new InvalidDataException($"Unexpected token: State = {previousState}, Token = {tokenizer.TokenValue.ToString()}, Buffer = {fieldBuilder}");

                        default:
                            ClearLeadingWhiteSpace();
                            break;
                    }
                }
                else
                {
                    switch (state)
                    {
                        case ParserState.QuotedFieldText:
                        case ParserState.UnquotedFieldText:
                            fieldBuilder.Append(tokenizer.TokenValue);
                            break;

                        case ParserState.EscapeAfterLeadingEscape: // I don't think this will ever be hit???
                            fieldBuilder.Append(quote.AsSpan());
                            break;

                        default:
                            throw new InvalidDataException($"Unexpected state: State = {state}, Token = {tokenizer.TokenValue.ToString()}, Buffer = {fieldBuilder}");
                    }
                }
            }

            if (state == ParserState.Start)
                return false;

            bool missingClosingQuote = state == ParserState.QuotedFieldText;
            if (!missingClosingQuote && parserStateMachine.TryGetDefaultForState(state, out ParserState defaultState))
                missingClosingQuote = defaultState == ParserState.QuotedFieldText;

            if (missingClosingQuote)
                throw new Exception($"Final quoted field did not have a closing quote: State = {state}, Buffer = {fieldBuilder}");

            CheckRecord();
            return true;
        }

        private void CheckRecord()
        {
            AddCurrentField();

            if (FieldCount < expectedRecordLength && config.IncompleteRecordHandling == IncompleteRecordHandling.ThrowException)
                throw new InvalidDataException($"Record is incomplete"); // TODO output partial record
        }

        private void AddCurrentField()
        {
            AppendLeadingWhiteSpace();
            ClearTrailingWhiteSpace();

            var fieldInfo = new RecordFieldInfo(currentRecordBuffer.Length, fieldBuilder.Length);

            currentRecordBuffer.Append(fieldBuilder);
            fieldBuilder.Clear();

            currentRecordFields.Add(fieldInfo);
        }

        private void AppendLeadingWhiteSpace()
        {
            if (leadingWhiteSpaceValue.Length == 0)
                return;

            fieldBuilder.Append(leadingWhiteSpaceValue);
            ClearLeadingWhiteSpace();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearLeadingWhiteSpace()
        {
            leadingWhiteSpaceValue.Clear();
        }

        private void AppendTrailingWhiteSpace()
        {
            if (possibleTrailingWhiteSpaceValue.Length == 0)
                return;

            fieldBuilder.Append(possibleTrailingWhiteSpaceValue);
            ClearTrailingWhiteSpace();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearTrailingWhiteSpace()
        {
            possibleTrailingWhiteSpaceValue.Clear();
        }
    }
}
