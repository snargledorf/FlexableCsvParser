using System.Runtime.CompilerServices;
using System.Text;
using CsvSpanParser.StateMachine;

namespace CsvSpanParser
{
    public class CsvParser
    {
        private readonly TextReader reader;
        private CsvParserConfig config;

        private ITokenizer tokenizer;

        private StateMachine<ParserState, TokenType> parserStateMachine;

        private readonly List<string> recordBuilder = new();
        private readonly StringBuilder fieldBuilder = new();

        string? leadingWhiteSpaceValue = null;

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
        }

        public bool TryReadRecord(out string[] record)
        {
            var state = ParserState.Start;
            
            recordBuilder.Clear();
            fieldBuilder.Clear();

            ParserState previousState;

            Token token;
            while ((token = tokenizer.NextToken(reader)).Type != TokenType.EndOfReader)
            {
                previousState = state;
                if (parserStateMachine.TryTransition(state, token.Type, out state))
                {
                    switch (state)
                    {
                        case ParserState.UnquotedFieldText:
                            AppendLeadingWhiteSpace();
                            fieldBuilder.Append(token.Value);
                            break;

                        case ParserState.EndOfField:
                            AppendLeadingWhiteSpace();
                            AddCurrentField();
                            break;

                        case ParserState.EndOfRecord:
                            AppendLeadingWhiteSpace();
                            AddCurrentField();
                            record = recordBuilder.ToArray();
                            return record.Length > 0;

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

            AddCurrentField();
            record = recordBuilder.ToArray();
            return record.Length > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCurrentField()
        {
            recordBuilder.Add(fieldBuilder.ToString());
            fieldBuilder.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendLeadingWhiteSpace()
        {
            if (leadingWhiteSpaceValue != null)
            {
                fieldBuilder.Append(leadingWhiteSpaceValue);
                ClearLeadingWhiteSpace();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearLeadingWhiteSpace()
        {
            leadingWhiteSpaceValue = null;
        }
    }
}
