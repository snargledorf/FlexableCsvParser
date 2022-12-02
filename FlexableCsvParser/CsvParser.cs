using System;
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
        private readonly TextReader reader;
        private readonly CsvParserConfig config;
        private readonly ITokenizer tokenizer;
        private readonly bool trimLeadingWhiteSpace;
        private readonly bool trimTrailingWhiteSpace;

        private StateMachine<ParserState, TokenType> parserStateMachine;

        private readonly ReadOnlyMemory<char> quote;

        private Memory<string> currentRecord;
        private int currentFieldIndex;

        private int expectedRecordLength;
        private bool currentRecordInitialized;
        private int recordLength;

        private readonly MemoryStringBuilder leadingWhiteSpaceValue = new MemoryStringBuilder();
        private readonly MemoryStringBuilder possibleTrailingWhiteSpaceValue = new MemoryStringBuilder();
        private readonly List<string> initialRecordBuilder = new List<string>();
        
        private readonly MemoryStringBuilder fieldBuilder = new MemoryStringBuilder();

        public CsvParser(TextReader reader)
            : this(reader, CsvParserConfig.Default)
        {
        }

        public CsvParser(TextReader reader, CsvParserConfig config)
            : this(reader, config, Tokenizer.For(config.Delimiters))
        {
        }

        public CsvParser(TextReader reader, CsvParserConfig config, ITokenizer tokenizer)
        {
            this.reader = reader;
            this.config = config;
            this.tokenizer = tokenizer;

            trimLeadingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Leading);
            trimTrailingWhiteSpace = config.WhiteSpaceTrimming.HasFlag(WhiteSpaceTrimming.Trailing);

            parserStateMachine = CsvParserStateMachineFactory.BuildParserStateMachine();

            currentRecord = new string[config.RecordLength];

            expectedRecordLength = recordLength = currentRecord.Length;
            currentRecordInitialized = expectedRecordLength != 0;

            quote = config.Delimiters.Quote.AsMemory();
        }

        public async IAsyncEnumerable<ReadOnlyMemory<string>> EnumerateRecords()
        {
            while (await ReadAsync().ConfigureAwait(false))
                yield return currentRecord;
        }

        public async ValueTask<bool> ReadRecordAsync(Memory<string> record)
        {
            if (await ReadAsync().ConfigureAwait(false))
            {
                currentRecord[..recordLength].CopyTo(record);
                return true;
            }

            return false;
        }

        public async ValueTask<(bool Success, ReadOnlyMemory<string> Record)> ReadRecordAsync()
        {
            if (await ReadAsync().ConfigureAwait(false))
            {
                Memory<string> record = new string[recordLength];
                currentRecord[..recordLength].CopyTo(record);
                return (true, record);
            }

            return (false, ReadOnlyMemory<string>.Empty);
        }

        private async ValueTask<bool> ReadAsync()
        {
            var state = ParserState.Start;

            currentFieldIndex = 0;

            ParserState previousState;

            Token token = await tokenizer.NextTokenAsync(reader).ConfigureAwait(false);
            while (token.Type != TokenType.EndOfReader)
            {
                previousState = state;
                if (parserStateMachine.TryTransition(state, token.Type, out ParserState newState))
                {
                    state = newState;
                    switch (state)
                    {
                        case ParserState.UnquotedFieldText:
                        case ParserState.QuotedFieldText:
                            AppendLeadingWhiteSpace();
                            AppendTrailingWhiteSpace();
                            fieldBuilder.Append(token.Value);
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
                            fieldBuilder.Append(quote);
                            break;

                        case ParserState.QuotedFieldLeadingWhiteSpace:
                        case ParserState.LeadingWhiteSpace:
                            // Only store the leading whitespace if there is a possiblity we might need it
                            // IE. If trim leading isn't enabled
                            if (!trimLeadingWhiteSpace)
                                leadingWhiteSpaceValue.Append(token.Value);
                            break;

                        case ParserState.QuotedFieldTrailingWhiteSpace:
                        case ParserState.UnquotedFieldTrailingWhiteSpace:
                            // Only store the trailing whitespace if trailing whitespace trimming is enabled
                            // This is so if we do end up with more field text, we can append the whitespace
                            // since that means it isn't trailing
                            if (trimTrailingWhiteSpace)
                                possibleTrailingWhiteSpaceValue.Append(token.Value);
                            else
                                fieldBuilder.Append(token.Value);
                            break;

                        case ParserState.UnexpectedToken:
                            throw new InvalidDataException($"Unexpected token: State = {previousState}, Token = {token}, Buffer = {fieldBuilder}");

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
                            fieldBuilder.Append(token.Value);
                            break;

                        case ParserState.EscapeAfterLeadingEscape: // I don't think this will ever be hit???
                            fieldBuilder.Append(quote);
                            break;

                        default:
                            throw new InvalidDataException($"Unexpected state: State = {state}, Token = {token}, Buffer = {fieldBuilder}");
                    }
                }

                token = await tokenizer.NextTokenAsync(reader).ConfigureAwait(false);
            }

            if (state == ParserState.Start)
                return false;

            if (state == ParserState.QuotedFieldText ||
                (parserStateMachine.TryGetDefaultForState(state, out ParserState defaultState) && defaultState == ParserState.QuotedFieldText))
                throw new Exception($"Final quoted field did not have a closing quote: State = {state}, Buffer = {fieldBuilder}");

            CheckRecord();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRecord()
        {
            AddCurrentField();

            if (!currentRecordInitialized)
            {
                currentRecord = initialRecordBuilder.ToArray();
                currentFieldIndex = expectedRecordLength = recordLength = currentRecord.Length;
                currentRecordInitialized = true;
            }
            else if (currentFieldIndex != expectedRecordLength)
            {
                switch(config.IncompleteRecordHandling)
                {
                    case IncompleteRecordHandling.ThrowException:
                        throw new InvalidDataException($"Record is incomplete: {string.Join(',', currentRecord[..currentFieldIndex].ToArray())}");

                    case IncompleteRecordHandling.FillInWithEmpty:
                        currentRecord[currentFieldIndex..].Span.Fill(string.Empty);
                        break;

                    case IncompleteRecordHandling.FillInWithNull:
                        currentRecord[currentFieldIndex..].Span.Fill(null);
                        break;

                    case IncompleteRecordHandling.TruncateRecord:
                        recordLength = currentFieldIndex;
                        break;

                    default:
                        throw new InvalidOperationException($"Not a valid {nameof(IncompleteRecordHandling)}");
                }
            }
            else if (config.IncompleteRecordHandling == IncompleteRecordHandling.TruncateRecord)
            {
                recordLength = expectedRecordLength;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCurrentField()
        {
            AppendLeadingWhiteSpace();
            ClearTrailingWhiteSpace();

            string value = fieldBuilder.ToString();
            fieldBuilder.Clear();

            if (!currentRecordInitialized)
            {
                initialRecordBuilder.Add(value);
            }
            else
            {
                currentRecord.Span[currentFieldIndex++] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
