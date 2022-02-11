using System.Runtime.CompilerServices;
using System.Text;
using CsvSpanParser.StateMachine;

namespace CsvSpanParser
{
    public class CsvParser
    {
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
        {
            this.config = config;

            tokenizer = CsvParserTokenizerFactory.GetTokenizerForConfig(config.ToTokenizerConfig(), reader);

            parserStateMachine = CsvParserStateMachineFactory.BuildParserStateMachine(config);
        }

        public bool TryReadRecord(out string[] record)
        {
            var state = ParserState.Start;
            
            recordBuilder.Clear();
            fieldBuilder.Clear();

            ParserState previousState;

            Token token;
            while ((token = tokenizer.ReadToken()).Type != TokenType.EndOfReader)
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

                        case ParserState.QuotedFieldEscape:
                            fieldBuilder.Append(config.Quote);
                            break;

                        case ParserState.LeadingWhiteSpace:
                            leadingWhiteSpaceValue = token.Value;
                            break;

                        case ParserState.UnquotedFieldTrailingWhiteSpace:
                            // Possible todo in the future: Store trailing whitespace in the event trimming is turned on but we may have more text (IE. A space)
                            fieldBuilder.Append(token.Value);
                            break;

                        case ParserState.UnexpectedToken:
                            throw new InvalidOperationException($"Unexpected token: State = {previousState}, Token = {token}");

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
                                case TokenType.FieldDelimiter:
                                    fieldBuilder.Append(config.FieldDelimiter);
                                    break;
                                case TokenType.EndOfRecord:
                                    fieldBuilder.Append(config.EndOfRecord);
                                    break;

                                default:
                                    throw new InvalidOperationException($"Unexpected token: State = {state}, Token = {token}");
                            }
                            break;

                        default:
                            throw new InvalidOperationException($"Unexpected state: Current State = {state}, Last Token = {token}");
                    }
                }
            }

            if (state == ParserState.Start)
            {
                record = Array.Empty<string>();
                return false;
            }

            if (parserStateMachine.TryGetDefaultForState(state, out ParserState defaultState))
                state = defaultState;

            switch (state)
            {
                case ParserState.QuotedFieldText:
                    throw new Exception("Final quoted field did not have a closing quote");
            }

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
