using System.Runtime.CompilerServices;
using System.Text;

using FastState;

namespace CsvSpanParser
{
    public class CsvParser
    {
        private readonly TextReader reader;
        private CsvParserConfig config;

        private ITokenizer tokenizer;

        private StateMachine<ParserState, TokenType> parserStateMachine;

        private readonly List<string> initialRecordBuilder = new();
        private readonly StringBuilder fieldBuilder = new();

        private string[] currentRecord;
        private int currentFieldIndex;

        string? leadingWhiteSpaceValue;

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

            parserStateMachine = CsvParserStateMachineFactory.BuildParserStateMachine(config);

            currentRecord = new string[config.RecordLength];
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
                            AppendLeadingWhiteSpace();
                            fieldBuilder.Append(token.Value);
                            break;

                        case ParserState.EndOfField:
                            AddCurrentField();
                            break;

                        case ParserState.EndOfRecord:
                            record = CreateRecord();
                            return true;

                        case ParserState.QuotedFieldText:
                            fieldBuilder.Append(token.Value);
                            break;

                        case ParserState.EscapeAfterLeadingEscape:
                        case ParserState.QuotedFieldEscape:
                            fieldBuilder.Append(config.Delimiters.Quote);
                            break;

                        case ParserState.LeadingWhiteSpace:
                            leadingWhiteSpaceValue = token.Value;
                            break;

                        case ParserState.UnquotedFieldTrailingWhiteSpace:
                            // Possible todo in the future: Store trailing whitespace in the event trimming is turned on but we may have more text (IE. A space)
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
                            switch (token.Type)
                            {
                                case TokenType.Text:
                                case TokenType.WhiteSpace:
                                    fieldBuilder.Append(token.Value);
                                    break;
                                case TokenType.Field:
                                    fieldBuilder.Append(config.Delimiters.Field);
                                    break;
                                case TokenType.EndOfRecord:
                                    fieldBuilder.Append(config.Delimiters.EndOfRecord);
                                    break;

                                default:
                                    throw new InvalidDataException($"Unexpected token: State = {state}, Token = {token}, Buffer = {fieldBuilder}");
                            }
                            break;
                        case ParserState.EscapeAfterLeadingEscape:
                            fieldBuilder.Append(config.Delimiters.EndOfRecord);
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
            if (parserStateMachine.TryGetDefaultForState(state, out ParserState defaultState))
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
    }
}
