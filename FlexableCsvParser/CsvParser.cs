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
        private readonly TextReader reader;
        private readonly CsvParserConfig config;
        private readonly bool trimLeadingWhiteSpace;
        private readonly bool trimTrailingWhiteSpace;

        private State<TokenType<CsvTokens>, ParserState> startState;

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
        
        private readonly TokenReaderStateMachine<CsvTokens> _tokenReaderStateMachine;

        public CsvParser(TextReader reader, int recordLength)
            : this(reader, recordLength, CsvParserConfig.Default)
        {
        }

        public CsvParser(TextReader reader, int recordLength, CsvParserConfig config)
        {
            if (recordLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(recordLength));

            this.reader = reader;
            this.config = config;
            
            trimLeadingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Leading);
            trimTrailingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Trailing);

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
            
            startState = CsvParserStateMachineFactory.BuildParserStateMachine();

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
                    if (!Tokenizer.TryParseToken(fieldSpan, _tokenReaderStateMachine, false, out TokenType<CsvTokens>? type, out ReadOnlySpan<char> lexeme))
                        throw new Exception("Unable to parse field token");

                    if (type == CsvTokens.Escape)
                    {
                        quote.CopyTo(strBufferSpan);
                        strBufferSpan = strBufferSpan[quote.Length..];
                    }
                    else
                    {
                        lexeme.CopyTo(strBufferSpan);
                        strBufferSpan = strBufferSpan[lexeme.Length..];
                    }

                    fieldSpan = fieldSpan[lexeme.Length..];
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
            State<TokenType<CsvTokens>, ParserState> currentState = startState;

            currentRecordFields.Clear();

            startOfRecordIndex += recordBufferObserved;
            recordBufferObserved = 0;

            currentFieldStartIndex = 0;

            do
            {
                ReadOnlySpan<char> recordBuffer = readBuffer.Span[startOfRecordIndex..readBufferLength];
                if (recordBuffer.IsEmpty)
                    continue;

                State<TokenType<CsvTokens>, ParserState> previousState;

                ReadOnlySpan<char> tokenBuffer = recordBuffer[recordBufferObserved..];
                while (Tokenizer.TryParseToken(tokenBuffer, _tokenReaderStateMachine, !endOfReader, out TokenType<CsvTokens>? type, out ReadOnlySpan<char> lexeme))
                {
                    tokenBuffer = tokenBuffer[lexeme.Length..];

                    recordBufferObserved = recordBuffer.Length - tokenBuffer.Length;

                    previousState = currentState;

                    if (currentState.TryTransition(type, out State<TokenType<CsvTokens>, ParserState>? newState))
                    {
                        currentState = newState;
                        switch (currentState.Id)
                        {
                            case ParserState.UnquotedFieldText:
                            case ParserState.QuotedFieldText:
                                fieldLength += lexeme.Length + leadingWhiteSpaceLength + possibleTrailingWhiteSpaceLength;
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
                                fieldLength += lexeme.Length + leadingWhiteSpaceLength + possibleTrailingWhiteSpaceLength;
                                leadingWhiteSpaceLength = possibleTrailingWhiteSpaceLength = 0;
                                break;

                            case ParserState.QuotedFieldLeadingWhiteSpace:
                            case ParserState.LeadingWhiteSpace:
                                // Only store the leading whitespace if there is a possiblity we might need it
                                // IE. If trim leading isn't enabled
                                if (trimLeadingWhiteSpace)
                                    currentFieldStartIndex += lexeme.Length;
                                else
                                    leadingWhiteSpaceLength = lexeme.Length;
                                break;

                            case ParserState.QuotedFieldTrailingWhiteSpace:
                            case ParserState.UnquotedFieldTrailingWhiteSpace:
                                // Only store the trailing whitespace if trailing whitespace trimming is enabled
                                // This is so if we do end up with more field text, we can append the whitespace
                                // since that means it isn't trailing
                                if (trimTrailingWhiteSpace)
                                    possibleTrailingWhiteSpaceLength = lexeme.Length;
                                else
                                    fieldLength += lexeme.Length;

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
                                currentFieldStartIndex += lexeme.Length + leadingWhiteSpaceLength;
                                leadingWhiteSpaceLength = 0;
                                break;
                        }
                    }
                    else
                    {
                        switch (currentState.Id)
                        {
                            case ParserState.QuotedFieldText:
                            case ParserState.UnquotedFieldText:
                                fieldLength += lexeme.Length;
                                break;

                            case ParserState.EscapeAfterLeadingEscape: // I don't think this will ever be hit???
                                escapedQuoteCount++;
                                fieldLength += lexeme.Length;
                                break;

                            default:
                                throw new InvalidDataException($"Unexpected state: State = {currentState}, Buffer = {tokenBuffer}");
                        }
                    }
                }
            } while (CheckBuffer());

            if (currentState == startState)
                return false;

            bool missingClosingQuote = currentState.Id == ParserState.QuotedFieldText;
            if (!missingClosingQuote && currentState.TryGetDefault(out State<TokenType<CsvTokens>, ParserState>? defaultState))
                missingClosingQuote = defaultState.Id == ParserState.QuotedFieldText;

            if (missingClosingQuote)
                throw new Exception($"Final quoted field did not have a closing quote: State = {currentState}, Buffer = {readBuffer}");

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
