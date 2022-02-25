using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

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
        private readonly string field;
        private readonly string endOfRecord;

        private string[] currentRecord;
        private int currentFieldIndex;

        string leadingWhiteSpaceValue;
        private string possibleTrailingWhiteSpaceValue;

        private readonly List<string> initialRecordBuilder = new List<string>();
        private readonly StringBuilder fieldBuilder = new StringBuilder();

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

            quote = config.Delimiters.Quote;
            field = config.Delimiters.Field;
            endOfRecord = config.Delimiters.EndOfRecord;
        }

        public bool TryReadRecord(out string[] record)
        {
            var state = ParserState.Start;

            currentFieldIndex = 0;

            ParserState previousState;

            Token token;
            while ((token = tokenizer.NextToken(reader)).Type != TokenType.EndOfReader)
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
                            record = CreateRecord();
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
                                leadingWhiteSpaceValue = token.Value;
                            break;

                        case ParserState.QuotedFieldTrailingWhiteSpace:
                        case ParserState.UnquotedFieldTrailingWhiteSpace:
                            // Only store the trailing whitespace if trailing whitespace trimming is enabled
                            // This is so if we do end up with more field text, we can append the whitespace
                            // since that means it isn't trailing
                            if (trimTrailingWhiteSpace)
                                possibleTrailingWhiteSpaceValue = token.Value;
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

                        // I don't think this will ever be hit???
                        case ParserState.EscapeAfterLeadingEscape:
                            fieldBuilder.Append(quote);
                            break;

                        default:
                            throw new InvalidDataException($"Unexpected state: State = {state}, Token = {token}, Buffer = { fieldBuilder }");
                    }
                }
            }

            if (state == ParserState.Start)
            {
                record = Array.Empty<string>();
                return false;
            }

            previousState = state;
            if (state != ParserState.Start && parserStateMachine.TryGetDefaultForState(state, out ParserState defaultState))
                state = defaultState;

            if (state == ParserState.QuotedFieldText)
                throw new Exception($"Final quoted field did not have a closing quote: State = {previousState}, Buffer = {fieldBuilder}");

            record = CreateRecord();
            return true;
        }

        private string[] CreateRecord()
        {
            AddCurrentField();

            if (currentRecord.Length == 0)
            {
                currentRecord = initialRecordBuilder.ToArray();
                currentFieldIndex = currentRecord.Length;
            }

            int recordLength = currentRecord.Length;

            if (currentFieldIndex != currentRecord.Length)
            {
                switch (config.IncompleteRecordHandling)
                {
                    case IncompleteRecordHandling.ThrowException:
                        throw new InvalidDataException($"Record is incomplete: {string.Join(',', currentRecord.Take(currentFieldIndex))}");

                    case IncompleteRecordHandling.FillInWithEmpty:
                        Array.Fill(currentRecord, string.Empty, currentFieldIndex, currentRecord.Length - currentFieldIndex);
                        break;

                    case IncompleteRecordHandling.FillInWithNull:
                        Array.Fill(currentRecord, null, currentFieldIndex, currentRecord.Length - currentFieldIndex);
                        break;

                    case IncompleteRecordHandling.TruncateRecord:
                        recordLength = currentFieldIndex;
                        break;

                    default:
                        throw new InvalidOperationException($"Not a valid {nameof(IncompleteRecordHandling)}");
                }
            }

            var result = new string[recordLength];
            Array.Copy(currentRecord, result, recordLength);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCurrentField()
        {
            AppendLeadingWhiteSpace();
            ClearTrailingWhiteSpace();

            string value = fieldBuilder.ToString();
            fieldBuilder.Clear();

            if (currentRecord.Length == 0)
            {
                initialRecordBuilder.Add(value);
            }
            else
            {
                currentRecord[currentFieldIndex++] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendLeadingWhiteSpace()
        {
            if (leadingWhiteSpaceValue == null)
                return;

            fieldBuilder.Append(leadingWhiteSpaceValue);
            ClearLeadingWhiteSpace();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearLeadingWhiteSpace()
        {
            leadingWhiteSpaceValue = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendTrailingWhiteSpace()
        {
            if (possibleTrailingWhiteSpaceValue == null)
                return;

            fieldBuilder.Append(possibleTrailingWhiteSpaceValue);
            ClearTrailingWhiteSpace();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearTrailingWhiteSpace()
        {
            possibleTrailingWhiteSpaceValue = null;
        }
    }
}
