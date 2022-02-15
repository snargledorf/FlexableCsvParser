using FastState;

namespace CsvSpanParser
{
    internal class CsvParserStateMachineFactory
    {
        internal static StateMachine<ParserState, TokenType> BuildParserStateMachine(CsvParserConfig config)
        {
            return new StateMachine<ParserState, TokenType>(builder =>
            {
                BuildStartOfFieldTransitions(builder);

                BuildUnquotedFieldTransitions(builder);
                BuildLeadingWhiteSpaceTransitions(builder);
                BuildQuotedFieldTransitions(builder);
            });
        }

        private static void BuildStartOfFieldTransitions(IStateMachineTransitionMapBuilder<ParserState, TokenType> builder)
        {
            BuildStartOfFieldTransitions(builder.From(ParserState.Start));
            BuildStartOfFieldTransitions(builder.From(ParserState.EndOfField));
            BuildStartOfFieldTransitions(builder.From(ParserState.EndOfRecord));
        }

        private static void BuildStartOfFieldTransitions(IStateTransitionMapBuilder<ParserState, TokenType> builder)
        {
            builder
                .When(TokenType.Text, ParserState.UnquotedFieldText)
                .When(TokenType.Quote, ParserState.QuotedFieldOpenQuote)
                .When(TokenType.WhiteSpace, ParserState.LeadingWhiteSpace)
                .When(TokenType.Field, ParserState.EndOfField)
                .When(TokenType.EndOfRecord, ParserState.EndOfRecord)
                .When(TokenType.EndOfReader, ParserState.EndOfReader)
                .When(TokenType.Escape, ParserState.LeadingEscape);
        }

        private static void BuildUnquotedFieldTransitions(IStateMachineTransitionMapBuilder<ParserState, TokenType> builder)
        {
            builder.From(ParserState.UnquotedFieldText)
                .When(TokenType.Field, ParserState.EndOfField)
                .When(TokenType.WhiteSpace, ParserState.UnquotedFieldTrailingWhiteSpace)
                .When(TokenType.EndOfRecord, ParserState.EndOfRecord);

            builder.From(ParserState.UnquotedFieldTrailingWhiteSpace)
                .When(TokenType.Text, ParserState.UnquotedFieldText)
                .When(TokenType.Field, ParserState.EndOfField)
                .When(TokenType.EndOfRecord, ParserState.EndOfRecord)
                .GotoWhen(ParserState.UnexpectedToken, TokenType.WhiteSpace, TokenType.Escape, TokenType.Quote)
                .Default(ParserState.EndOfField);
        }

        private static void BuildLeadingWhiteSpaceTransitions(IStateMachineTransitionMapBuilder<ParserState, TokenType> builder)
        {
            builder.From(ParserState.LeadingWhiteSpace)
                .When(TokenType.Text, ParserState.UnquotedFieldText)
                .When(TokenType.Quote, ParserState.QuotedFieldOpenQuote)
                .When(TokenType.Field, ParserState.EndOfField)
                .When(TokenType.EndOfRecord, ParserState.EndOfRecord)
                .When(TokenType.Escape, ParserState.LeadingEscape)
                .When(TokenType.WhiteSpace, ParserState.UnexpectedToken)
                .Default(ParserState.EndOfField);
        }

        private static void BuildQuotedFieldTransitions(IStateMachineTransitionMapBuilder<ParserState, TokenType> builder)
        {
            builder.From(ParserState.QuotedFieldOpenQuote)
                .When(TokenType.Quote, ParserState.QuotedFieldClosingQuote)
                .When(TokenType.Escape, ParserState.QuotedFieldEscape)
                .Default(ParserState.QuotedFieldText);

            builder.From(ParserState.QuotedFieldText)
                .When(TokenType.Quote, ParserState.QuotedFieldClosingQuote)
                .When(TokenType.Escape, ParserState.QuotedFieldEscape);

            builder.From(ParserState.QuotedFieldEscape)
                .When(TokenType.Quote, ParserState.QuotedFieldClosingQuote)
                .When(TokenType.Escape, ParserState.QuotedFieldEscape)
                .Default(ParserState.QuotedFieldText);

            builder.From(ParserState.QuotedFieldClosingQuote)
                .When(TokenType.WhiteSpace, ParserState.QuotedFieldTrailingWhiteSpace)
                .When(TokenType.EndOfRecord, ParserState.EndOfRecord)
                .GotoWhen(ParserState.UnexpectedToken, TokenType.Text, TokenType.Escape, TokenType.Quote)
                .Default(ParserState.EndOfField);

            builder.From(ParserState.QuotedFieldTrailingWhiteSpace)
                .When(TokenType.EndOfRecord, ParserState.EndOfRecord)
                .GotoWhen(ParserState.UnexpectedToken, TokenType.Text, TokenType.WhiteSpace, TokenType.Escape, TokenType.Quote)
                .Default(ParserState.EndOfField);

            builder.From(ParserState.LeadingEscape)
                .When(TokenType.Quote, ParserState.QuotedFieldEscape) // Handles Foo,"""Bar""",Biz
                .When(TokenType.Escape, ParserState.EscapeAfterLeadingEscape) // Handles Foo,""""" Empty quotes",Biz
                .When(TokenType.WhiteSpace, ParserState.QuotedFieldTrailingWhiteSpace) // Handles Foo,"" ,Biz
                .When(TokenType.Text, ParserState.UnexpectedToken)
                .Default(ParserState.EndOfField); // Handles Foo,"",Biz | Foo,Bar,""

            builder.From(ParserState.EscapeAfterLeadingEscape) // Handles Foo,"""""",Biz
                .When(TokenType.Escape, ParserState.EscapeAfterLeadingEscape) // Handles Foo,"""""",Biz
                .When(TokenType.Field, ParserState.EndOfField) // Handles Foo,"""""",Biz
                .When(TokenType.Quote, ParserState.QuotedFieldEscape)
                .When(TokenType.WhiteSpace, ParserState.QuotedFieldTrailingWhiteSpace) //  Foo,"""" ,Biz
                .When(TokenType.Text, ParserState.UnexpectedToken) // Foo,""""Bar,Biz
                .Default(ParserState.EndOfField); // Foo,"""" | Foo,""""""
        }
    }
}