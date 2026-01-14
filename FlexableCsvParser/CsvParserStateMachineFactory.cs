using System.Collections.Generic;
using SwiftState;
using Tokensharp;

namespace FlexableCsvParser
{
    internal static class CsvParserStateMachineFactory
    {
        public static State<TokenType<CsvTokens>, ParserState> StartState => field ??= BuildParserStateMachine();

        private static State<TokenType<CsvTokens>, ParserState> BuildParserStateMachine()
        {
            var startStateBuilder = new StateBuilder<TokenType<CsvTokens>, ParserState>(ParserState.Start);
            
            BuildStartOfFieldTransitions(startStateBuilder);
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> endOfFieldBuilder = startStateBuilder.GetBuilderForState(ParserState.EndOfField);
            BuildStartOfFieldTransitions(endOfFieldBuilder);
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> endOfRecordBuilder = startStateBuilder.GetBuilderForState(ParserState.EndOfRecord);
            BuildStartOfFieldTransitions(endOfRecordBuilder);

            BuildUnquotedFieldTransitions(startStateBuilder);
            BuildLeadingWhiteSpaceTransitions(startStateBuilder);
            BuildQuotedFieldTransitions(startStateBuilder);
            
            return startStateBuilder.Build();
        }

        private static void BuildStartOfFieldTransitions(IStateBuilder<TokenType<CsvTokens>, ParserState> builder)
        {
            builder.GotoWhen(ParserState.UnquotedFieldText, CsvTokens.Text, CsvTokens.Number);
            builder.When(CsvTokens.Quote, ParserState.QuotedFieldOpenQuote);
            builder.When(CsvTokens.WhiteSpace, ParserState.LeadingWhiteSpace);
            builder.When(CsvTokens.FieldDelimiter, ParserState.EndOfField);
            builder.When(CsvTokens.EndOfRecord, ParserState.EndOfRecord);
            //.When(TokenType.EndOfReader, ParserState.EndOfReader)
            builder.When(CsvTokens.Escape, ParserState.LeadingEscape);
        }

        private static void BuildUnquotedFieldTransitions(IStateBuilder<TokenType<CsvTokens>, ParserState> builder)
        {
            IStateBuilder<TokenType<CsvTokens>, ParserState> unquotedFieldTextBuilder = builder.GetBuilderForState(ParserState.UnquotedFieldText);
            unquotedFieldTextBuilder.When(CsvTokens.FieldDelimiter, ParserState.EndOfField);
            unquotedFieldTextBuilder.When(CsvTokens.EndOfRecord, ParserState.EndOfRecord);

            IStateBuilder<TokenType<CsvTokens>, ParserState> unquotedFieldTrailingWhiteSpaceBuilder = unquotedFieldTextBuilder.When(CsvTokens.WhiteSpace, ParserState.UnquotedFieldTrailingWhiteSpace);

            unquotedFieldTrailingWhiteSpaceBuilder.GotoWhen(ParserState.UnquotedFieldText, CsvTokens.Text, CsvTokens.Number);
            unquotedFieldTrailingWhiteSpaceBuilder.When(CsvTokens.FieldDelimiter, ParserState.EndOfField);
            unquotedFieldTrailingWhiteSpaceBuilder.When(CsvTokens.EndOfRecord, ParserState.EndOfRecord);
            unquotedFieldTrailingWhiteSpaceBuilder.GotoWhen(ParserState.UnexpectedToken, CsvTokens.WhiteSpace, CsvTokens.Escape, CsvTokens.Quote);
            unquotedFieldTrailingWhiteSpaceBuilder.Default(ParserState.EndOfField);
        }

        private static void BuildLeadingWhiteSpaceTransitions(IStateBuilder<TokenType<CsvTokens>, ParserState> builder)
        {
            IStateBuilder<TokenType<CsvTokens>, ParserState> leadingWhiteSpaceBuilder = builder.GetBuilderForState(ParserState.LeadingWhiteSpace);
            
            leadingWhiteSpaceBuilder.GotoWhen(ParserState.UnquotedFieldText, CsvTokens.Text, CsvTokens.Number);
            leadingWhiteSpaceBuilder.When(CsvTokens.Quote, ParserState.QuotedFieldOpenQuote);
            leadingWhiteSpaceBuilder.When(CsvTokens.FieldDelimiter, ParserState.EndOfField);
            leadingWhiteSpaceBuilder.When(CsvTokens.EndOfRecord, ParserState.EndOfRecord);
            leadingWhiteSpaceBuilder.When(CsvTokens.Escape, ParserState.LeadingEscape);
            leadingWhiteSpaceBuilder.When(CsvTokens.WhiteSpace, ParserState.UnexpectedToken);
            leadingWhiteSpaceBuilder.Default(ParserState.EndOfField);
        }

        private static void BuildQuotedFieldTransitions(IStateBuilder<TokenType<CsvTokens>, ParserState> builder)
        {
            IStateBuilder<TokenType<CsvTokens>, ParserState> quotedFieldOpenQuoteBuilder =
                builder.GetBuilderForState(ParserState.QuotedFieldOpenQuote);
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> leadingEscapeBuilder =
                builder.GetBuilderForState(ParserState.LeadingEscape);

            IStateBuilder<TokenType<CsvTokens>, ParserState> quotedFieldClosingQuoteBuilder =
                quotedFieldOpenQuoteBuilder.When(CsvTokens.Quote, ParserState.QuotedFieldClosingQuote);
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> quotedFieldEscapeBuilder =
                quotedFieldOpenQuoteBuilder.When(CsvTokens.Escape, ParserState.QuotedFieldEscape);
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> quotedFieldLeadingWhiteSpaceBuilder =
                quotedFieldOpenQuoteBuilder.When(CsvTokens.WhiteSpace, ParserState.QuotedFieldLeadingWhiteSpace);
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> quotedFieldTextBuilder =
                quotedFieldOpenQuoteBuilder.Default(ParserState.QuotedFieldText);

            quotedFieldTextBuilder.When(CsvTokens.Quote, ParserState.QuotedFieldClosingQuote);
            quotedFieldTextBuilder.When(CsvTokens.Escape, ParserState.QuotedFieldEscape);
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> quotedFieldTrailingWhiteSpaceBuilder =
                quotedFieldTextBuilder.When(CsvTokens.WhiteSpace, ParserState.QuotedFieldTrailingWhiteSpace);

            quotedFieldEscapeBuilder.When(CsvTokens.Quote, ParserState.QuotedFieldClosingQuote);
            quotedFieldEscapeBuilder.When(CsvTokens.Escape, ParserState.QuotedFieldEscape);
            quotedFieldEscapeBuilder.Default(ParserState.QuotedFieldText);

            quotedFieldLeadingWhiteSpaceBuilder.When(CsvTokens.Quote, ParserState.QuotedFieldClosingQuote);
            quotedFieldLeadingWhiteSpaceBuilder.When(CsvTokens.Escape, ParserState.QuotedFieldEscape);
            quotedFieldLeadingWhiteSpaceBuilder.Default(ParserState.QuotedFieldText);

            quotedFieldTrailingWhiteSpaceBuilder.When(CsvTokens.Quote, ParserState.QuotedFieldClosingQuote);
            quotedFieldTrailingWhiteSpaceBuilder.When(CsvTokens.Escape, ParserState.QuotedFieldEscape);
            quotedFieldTrailingWhiteSpaceBuilder.Default(ParserState.QuotedFieldText);

            IStateBuilder<TokenType<CsvTokens>, ParserState> quotedFieldClosingQuoteTrailingWhiteSpaceBuilder =
                quotedFieldClosingQuoteBuilder.When(CsvTokens.WhiteSpace,
                    ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace);
            
            quotedFieldClosingQuoteBuilder.When(CsvTokens.EndOfRecord, ParserState.EndOfRecord);
            quotedFieldClosingQuoteBuilder.GotoWhen(ParserState.UnexpectedToken, CsvTokens.Text, CsvTokens.Escape,
                CsvTokens.Quote);
            quotedFieldClosingQuoteBuilder.Default(ParserState.EndOfField);

            quotedFieldClosingQuoteTrailingWhiteSpaceBuilder.When(CsvTokens.EndOfRecord, ParserState.EndOfRecord);
            quotedFieldClosingQuoteTrailingWhiteSpaceBuilder.GotoWhen(ParserState.UnexpectedToken, CsvTokens.Text,
                CsvTokens.WhiteSpace, CsvTokens.Escape, CsvTokens.Quote);
            quotedFieldClosingQuoteTrailingWhiteSpaceBuilder.Default(ParserState.EndOfField);

            leadingEscapeBuilder.When(CsvTokens.Quote,
                ParserState
                    .QuoteAfterLeadingEscape); // Handles Foo,"""Bar""",Biz | Foo,""" Bar""",Biz | Foo,""", Bar""",Biz
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> escapeAfterLeadingEscapeBuilder =
                leadingEscapeBuilder.When(CsvTokens.Escape,
                    ParserState.EscapeAfterLeadingEscape); // Handles Foo,""""" Empty quotes",Biz
            
            leadingEscapeBuilder.When(CsvTokens.WhiteSpace,
                ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace); // Handles Foo,"" ,Biz
            leadingEscapeBuilder.When(CsvTokens.Text, ParserState.UnexpectedToken);
            leadingEscapeBuilder.Default(ParserState.EndOfField); // Handles Foo,"",Biz | Foo,Bar,""

            escapeAfterLeadingEscapeBuilder.When(CsvTokens.Escape,
                ParserState.EscapeAfterLeadingEscape); // Handles Foo,"""""",Biz
            escapeAfterLeadingEscapeBuilder.When(CsvTokens.FieldDelimiter,
                ParserState.EndOfField); // Handles Foo,"""""",Biz
            
            IStateBuilder<TokenType<CsvTokens>, ParserState> quoteAfterLeadingEscapeBuilder =
                escapeAfterLeadingEscapeBuilder.When(CsvTokens.Quote, ParserState.QuoteAfterLeadingEscape);
            
            escapeAfterLeadingEscapeBuilder.When(CsvTokens.WhiteSpace,
                ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace); //  Foo,"""" ,Biz
            escapeAfterLeadingEscapeBuilder.When(CsvTokens.Text, ParserState.UnexpectedToken); // Foo,""""Bar,Biz
            escapeAfterLeadingEscapeBuilder.Default(ParserState.EndOfField); // Foo,"""" | Foo,""""""

            quoteAfterLeadingEscapeBuilder.When(CsvTokens.WhiteSpace,
                ParserState.QuotedFieldTrailingWhiteSpace); // Handles Foo,"" ,Biz
            quoteAfterLeadingEscapeBuilder.GotoWhen(ParserState.UnexpectedToken, CsvTokens.Quote, CsvTokens.Escape);
            quoteAfterLeadingEscapeBuilder.Default(ParserState.QuotedFieldText);
        }
    }
}