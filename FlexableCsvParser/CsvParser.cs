using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using FastState;

namespace FlexableCsvParser
{
    public class CsvParser
    {
        private readonly TextReader reader;
        private readonly CsvParserConfig config;
        private readonly ITokenizer tokenizer;
        private readonly bool trimLeadingWhiteSpace;
        private readonly bool trimTrailingWhiteSpace;

        private StateMachine<ParserState, TokenType> parserStateMachine;

        private readonly string quote;
        private readonly int escapeLength;

        private Memory<char> readBuffer = new char[4096];
        private int readBufferLength;

        private bool endOfReader;
        private int startOfRecordIndex;

        private int currentFieldStartIndex;
        private int escapedQuoteCount;

        private int fieldLength;
        private int leadingWhiteSpaceLength;
        private int possibleTrailingWhiteSpaceLength;

        private List<RecordFieldInfo> currentRecordFields;
        private int expectedRecordFieldCount;

        private StringPool stringPool;
        private int recordBufferObserved;

        public CsvParser(TextReader reader, int recordLength)
            : this(reader, recordLength, CsvParserConfig.Default)
        {
        }

        public CsvParser(TextReader reader, int recordLength, CsvParserConfig config)
            : this(reader, recordLength, config, Tokenizer.For(config.Delimiters))
        {
        }

        public CsvParser(TextReader reader, int recordLength, CsvParserConfig config, ITokenizer tokenizer)
        {
            if (recordLength <= 0)
                throw new ArgumentOutOfRangeException("Record length required");

            this.reader = reader;
            this.config = config;
            this.tokenizer = tokenizer;

            trimLeadingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Leading);
            trimTrailingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Trailing);

            parserStateMachine = CsvParserStateMachineFactory.BuildParserStateMachine();

            expectedRecordFieldCount = recordLength;

            currentRecordFields = new List<RecordFieldInfo>(expectedRecordFieldCount);

            quote = config.Delimiters.Quote;
            escapeLength = config.Delimiters.Escape.Length;

            stringPool = new(config.StringCacheMaxLength);
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
                int bufferLength = Math.Min(record.Length, expectedRecordFieldCount);
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
            if (fieldIndex >= expectedRecordFieldCount)
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

            ref var fieldInfo = ref CollectionsMarshal.AsSpan(currentRecordFields)[fieldIndex];
            if (fieldInfo.Length == 0)
                return string.Empty;

            ReadOnlySpan<char> recordBuffer = readBuffer.Span[startOfRecordIndex..readBufferLength];

            ReadOnlySpan<char> fieldSpan = recordBuffer.Slice(fieldInfo.StartIndex, fieldInfo.Length);

            if (fieldInfo.EscapedQuoteCount == 0)
                return stringPool.GetString(fieldSpan);

            int quoteEscapeLengthDiff = (escapeLength * fieldInfo.EscapedQuoteCount) - (quote.Length * fieldInfo.EscapedQuoteCount);

            int fieldUpdatedLength = fieldInfo.Length - quoteEscapeLengthDiff;

            char[] strBuffer = ArrayPool<char>.Shared.Rent(fieldUpdatedLength);
            try
            {
                Span<char> strBufferSpan = strBuffer.AsSpan()[..fieldUpdatedLength];
                ReadOnlySpan<char> resultSpan = strBufferSpan;
                while (!fieldSpan.IsEmpty)
                {
                    if (!tokenizer.TryParseToken(fieldSpan, true, out TokenType type, out int tokenLength))
                        throw new Exception("Unable to parse field token");

                    if (type == TokenType.Escape)
                    {
                        quote.CopyTo(strBufferSpan);
                        strBufferSpan = strBufferSpan[quote.Length..];
                    }
                    else
                    {
                        fieldSpan[..tokenLength].CopyTo(strBufferSpan);
                        strBufferSpan = strBufferSpan[tokenLength..];
                    }

                    fieldSpan = fieldSpan[tokenLength..];
                }

                return stringPool.GetString(resultSpan);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(strBuffer);
            }
        }

        public bool Read()
        {
            var state = ParserState.Start;

            currentRecordFields.Clear();

            startOfRecordIndex += recordBufferObserved;
            recordBufferObserved = 0;

            currentFieldStartIndex = 0;

            do
            {
                ReadOnlySpan<char> recordBuffer = readBuffer.Span[startOfRecordIndex..readBufferLength];
                if (recordBuffer.IsEmpty)
                    continue;

                if (recordBuffer.StartsWith("1205319"))
                    System.Threading.Thread.Sleep(0);

                ParserState previousState;

                ReadOnlySpan<char> tokenBuffer = recordBuffer[recordBufferObserved..];
                while (tokenizer.TryParseToken(tokenBuffer, endOfReader, out TokenType type, out int length))
                {
                    tokenBuffer = tokenBuffer[length..];

                    recordBufferObserved = recordBuffer.Length - tokenBuffer.Length;

                    previousState = state;

                    if (parserStateMachine.TryTransition(state, type, out ParserState newState))
                    {
                        state = newState;
                        switch (state)
                        {
                            case ParserState.UnquotedFieldText:
                            case ParserState.QuotedFieldText:
                                fieldLength += length + leadingWhiteSpaceLength + possibleTrailingWhiteSpaceLength;
                                leadingWhiteSpaceLength = possibleTrailingWhiteSpaceLength = 0;
                                break;

                            case ParserState.EndOfField:
                                AddCurrentField();
                                currentFieldStartIndex = recordBuffer.Length - tokenBuffer.Length;
                                break;

                            case ParserState.EndOfRecord:
                                CheckRecord();
                                currentFieldStartIndex = recordBuffer.Length - tokenBuffer.Length;
                                return true;

                            case ParserState.EscapeAfterLeadingEscape:
                                escapedQuoteCount++;
                                fieldLength += escapeLength;
                                break;

                            case ParserState.QuotedFieldEscape:
                                escapedQuoteCount++;
                                fieldLength += length + leadingWhiteSpaceLength + possibleTrailingWhiteSpaceLength;
                                leadingWhiteSpaceLength = possibleTrailingWhiteSpaceLength = 0;
                                break;

                            case ParserState.QuotedFieldLeadingWhiteSpace:
                            case ParserState.LeadingWhiteSpace:
                                // Only store the leading whitespace if there is a possiblity we might need it
                                // IE. If trim leading isn't enabled
                                if (trimLeadingWhiteSpace)
                                    currentFieldStartIndex += length;
                                else
                                    leadingWhiteSpaceLength = length;
                                break;

                            case ParserState.QuotedFieldTrailingWhiteSpace:
                            case ParserState.UnquotedFieldTrailingWhiteSpace:
                                // Only store the trailing whitespace if trailing whitespace trimming is enabled
                                // This is so if we do end up with more field text, we can append the whitespace
                                // since that means it isn't trailing
                                if (trimTrailingWhiteSpace)
                                    possibleTrailingWhiteSpaceLength = length;
                                else
                                    fieldLength += length;

                                break;

                            case ParserState.UnexpectedToken:
                                throw new InvalidDataException($"Unexpected token: State = {previousState}, Buffer = {tokenBuffer}");

                            case ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace:
                            case ParserState.QuotedFieldClosingQuote:
                                // NoOp
                                break;

                            case ParserState.LeadingEscape:
                                currentFieldStartIndex += leadingWhiteSpaceLength + quote.Length; // TODO Handle escapes that aren't "" (Ex. &quote;)
                                leadingWhiteSpaceLength = 0;
                                break;

                            case ParserState.QuoteAfterLeadingEscape:
                                escapedQuoteCount++;
                                fieldLength += escapeLength;
                                break;

                            default:
                                currentFieldStartIndex += length + leadingWhiteSpaceLength;
                                leadingWhiteSpaceLength = 0;
                                break;
                        }
                    }
                    else
                    {
                        switch (state)
                        {
                            case ParserState.QuotedFieldText:
                            case ParserState.UnquotedFieldText:
                                fieldLength += length;
                                break;

                            case ParserState.EscapeAfterLeadingEscape: // I don't think this will ever be hit???
                                escapedQuoteCount++;
                                fieldLength += length;
                                break;

                            default:
                                throw new InvalidDataException($"Unexpected state: State = {state}, Buffer = {tokenBuffer}");
                        }
                    }
                }
            } while (CheckBuffer());

            if (state == ParserState.Start)
                return false;

            bool missingClosingQuote = state == ParserState.QuotedFieldText;
            if (!missingClosingQuote && parserStateMachine.TryGetDefaultForState(state, out ParserState defaultState))
                missingClosingQuote = defaultState == ParserState.QuotedFieldText;

            if (missingClosingQuote)
                throw new Exception($"Final quoted field did not have a closing quote: State = {state}, Buffer = {readBuffer}");

            CheckRecord();
            return true;
        }

        private bool CheckBuffer()
        {
            if (!endOfReader)
            {
                if (startOfRecordIndex != 0)
                {
                    this.readBuffer.Span[startOfRecordIndex..readBufferLength].CopyTo(this.readBuffer.Span);

                    readBufferLength -= startOfRecordIndex;

                    startOfRecordIndex = 0;
                }
                else if (readBufferLength == this.readBuffer.Length)
                {
                    int newLength = this.readBuffer.Length * 2;

                    // Check for overflow
                    if (newLength < this.readBuffer.Length)
                        throw new OutOfMemoryException();

                    var oldBuffer = this.readBuffer;
                    this.readBuffer = new char[newLength];

                    oldBuffer.CopyTo(this.readBuffer);
                }

                Span<char> readBuffer = this.readBuffer.Span[readBufferLength..];
                int charsRead = reader.Read(readBuffer);
                readBufferLength += charsRead;

                endOfReader = charsRead < readBuffer.Length && reader.Peek() == -1;
            }

            int currentReadIndex = startOfRecordIndex + recordBufferObserved;

            return currentReadIndex != readBufferLength;
        }

        private void CheckRecord()
        {
            AddCurrentField();

            if (FieldCount < expectedRecordFieldCount && config.IncompleteRecordHandling == IncompleteRecordHandling.ThrowException)
                throw new InvalidDataException($"Record is incomplete"); // TODO output partial record
        }

        private void AddCurrentField()
        {
            fieldLength += leadingWhiteSpaceLength;

            var fieldInfo = new RecordFieldInfo(currentFieldStartIndex, fieldLength, escapedQuoteCount);
            currentRecordFields.Add(fieldInfo);

            currentFieldStartIndex += fieldLength;
            fieldLength = escapedQuoteCount = leadingWhiteSpaceLength = possibleTrailingWhiteSpaceLength = 0;
        }
    }
}
