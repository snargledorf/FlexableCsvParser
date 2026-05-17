using System;
using Tokensharp;

namespace FlexableCsvParser;

internal static class CsvStateMachine
{
    private const int MaxCsvTokenIndex = 6;
    private const int MatrixColumnCount = MaxCsvTokenIndex + 1;
    private const int ParserStateCount = 19;

    private static readonly int[,] TransitionMatrix = new int[ParserStateCount, MatrixColumnCount];

    static CsvStateMachine()
    {
        var matrixBuilder = new MatrixBuilder(TransitionMatrix, MatrixColumnCount);
        
        SetDefaultsToUnexpectedToken(matrixBuilder);

        ConfigureStartOfFieldTransitions(matrixBuilder);

        // UnquotedFieldText
        matrixBuilder.SetDefault(ParserState.UnquotedFieldText, ParserState.UnquotedFieldText)
            .Set(ParserState.UnquotedFieldText, CsvTokens.FieldDelimiter, ParserState.EndOfField)
            .Set(ParserState.UnquotedFieldText, CsvTokens.EndOfRecord, ParserState.EndOfRecord)
            .Set(ParserState.UnquotedFieldText, CsvTokens.WhiteSpace,
                ParserState.UnquotedFieldTrailingWhiteSpace);

        // LeadingWhiteSpace
        matrixBuilder.SetDefault(ParserState.LeadingWhiteSpace, ParserState.EndOfField)
            .Set(ParserState.LeadingWhiteSpace, CsvTokens.Text, ParserState.UnquotedFieldText)
            .Set(ParserState.LeadingWhiteSpace, CsvTokens.Quote, ParserState.QuotedFieldOpenQuote)
            .Set(ParserState.LeadingWhiteSpace, CsvTokens.FieldDelimiter, ParserState.EndOfField)
            .Set(ParserState.LeadingWhiteSpace, CsvTokens.EndOfRecord, ParserState.EndOfRecord)
            .Set(ParserState.LeadingWhiteSpace, CsvTokens.Escape, ParserState.LeadingEscape)
            .Set(ParserState.LeadingWhiteSpace, CsvTokens.WhiteSpace, ParserState.UnexpectedToken);

        // LeadingEscape
        matrixBuilder.SetDefault(ParserState.LeadingEscape, ParserState.EndOfField)
            .Set(ParserState.LeadingEscape, CsvTokens.Quote, ParserState.QuoteAfterLeadingEscape)
            .Set(ParserState.LeadingEscape, CsvTokens.Escape, ParserState.EscapeAfterLeadingEscape)
            .Set(ParserState.LeadingEscape, CsvTokens.WhiteSpace,
                ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace)
            .Set(ParserState.LeadingEscape, CsvTokens.Text, ParserState.UnexpectedToken);

        // QuotedFieldOpenQuote
        matrixBuilder.SetDefault(ParserState.QuotedFieldOpenQuote, ParserState.QuotedFieldText)
            .Set(ParserState.QuotedFieldOpenQuote, CsvTokens.Quote, ParserState.QuotedFieldClosingQuote)
            .Set(ParserState.QuotedFieldOpenQuote, CsvTokens.Escape, ParserState.QuotedFieldEscape)
            .Set(ParserState.QuotedFieldOpenQuote, CsvTokens.WhiteSpace,
                ParserState.QuotedFieldLeadingWhiteSpace);

        // QuotedFieldClosingQuote
        matrixBuilder.SetDefault(ParserState.QuotedFieldClosingQuote, ParserState.EndOfField)
            .Set(ParserState.QuotedFieldClosingQuote, CsvTokens.WhiteSpace,
                ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace)
            .Set(ParserState.QuotedFieldClosingQuote, CsvTokens.EndOfRecord, ParserState.EndOfRecord)
            .Set(ParserState.QuotedFieldClosingQuote, CsvTokens.Text, ParserState.UnexpectedToken)
            .Set(ParserState.QuotedFieldClosingQuote, CsvTokens.Quote, ParserState.UnexpectedToken)
            .Set(ParserState.QuotedFieldClosingQuote, CsvTokens.Escape, ParserState.UnexpectedToken);

        // QuotedFieldText
        matrixBuilder.SetDefault(ParserState.QuotedFieldText, ParserState.QuotedFieldText)
            .Set(ParserState.QuotedFieldText, CsvTokens.Quote, ParserState.QuotedFieldClosingQuote)
            .Set(ParserState.QuotedFieldText, CsvTokens.Escape, ParserState.QuotedFieldEscape)
            .Set(ParserState.QuotedFieldText, CsvTokens.WhiteSpace,
                ParserState.QuotedFieldTrailingWhiteSpace);

        // QuotedFieldEscape
        matrixBuilder.SetDefault(ParserState.QuotedFieldEscape, ParserState.QuotedFieldText)
            .Set(ParserState.QuotedFieldEscape, CsvTokens.Quote, ParserState.QuotedFieldClosingQuote)
            .Set(ParserState.QuotedFieldEscape, CsvTokens.Escape, ParserState.QuotedFieldEscape);

        // QuotedFieldLeadingWhiteSpace
        matrixBuilder.SetDefault(ParserState.QuotedFieldLeadingWhiteSpace, ParserState.QuotedFieldText)
            .Set(ParserState.QuotedFieldLeadingWhiteSpace, CsvTokens.Quote,
                ParserState.QuotedFieldClosingQuote)
            .Set(ParserState.QuotedFieldLeadingWhiteSpace, CsvTokens.Escape, ParserState.QuotedFieldEscape);

        // QuotedFieldTrailingWhiteSpace
        matrixBuilder.SetDefault(ParserState.QuotedFieldTrailingWhiteSpace, ParserState.QuotedFieldText)
            .Set(ParserState.QuotedFieldTrailingWhiteSpace, CsvTokens.Quote,
                ParserState.QuotedFieldClosingQuote)
            .Set(ParserState.QuotedFieldTrailingWhiteSpace, CsvTokens.Escape,
                ParserState.QuotedFieldEscape);

        // QuotedFieldClosingQuoteTrailingWhiteSpace
        matrixBuilder.SetDefault(ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace, ParserState.EndOfField)
            .Set(ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace, CsvTokens.EndOfRecord,
                ParserState.EndOfRecord)
            .Set(ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace, CsvTokens.Text,
                ParserState.UnexpectedToken)
            .Set(ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace, CsvTokens.Quote,
                ParserState.UnexpectedToken)
            .Set(ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace, CsvTokens.Escape,
                ParserState.UnexpectedToken);

        // UnquotedFieldTrailingWhiteSpace
        matrixBuilder.SetDefault(ParserState.UnquotedFieldTrailingWhiteSpace, ParserState.EndOfField)
            .Set(ParserState.UnquotedFieldTrailingWhiteSpace, CsvTokens.Text, ParserState.UnquotedFieldText)
            .Set(ParserState.UnquotedFieldTrailingWhiteSpace, CsvTokens.FieldDelimiter,
                ParserState.EndOfField)
            .Set(ParserState.UnquotedFieldTrailingWhiteSpace, CsvTokens.EndOfRecord,
                ParserState.EndOfRecord)
            .Set(ParserState.UnquotedFieldTrailingWhiteSpace, CsvTokens.WhiteSpace,
                ParserState.UnexpectedToken)
            .Set(ParserState.UnquotedFieldTrailingWhiteSpace, CsvTokens.Escape, ParserState.UnexpectedToken)
            .Set(ParserState.UnquotedFieldTrailingWhiteSpace, CsvTokens.Quote, ParserState.UnexpectedToken);

        // EscapeAfterLeadingEscape
        matrixBuilder.SetDefault(ParserState.EscapeAfterLeadingEscape, ParserState.EndOfField)
            .Set(ParserState.EscapeAfterLeadingEscape, CsvTokens.Escape,
                ParserState.EscapeAfterLeadingEscape)
            .Set(ParserState.EscapeAfterLeadingEscape, CsvTokens.Quote, ParserState.QuoteAfterLeadingEscape)
            .Set(ParserState.EscapeAfterLeadingEscape, CsvTokens.FieldDelimiter, ParserState.EndOfField)
            .Set(ParserState.EscapeAfterLeadingEscape, CsvTokens.WhiteSpace,
                ParserState.QuotedFieldClosingQuoteTrailingWhiteSpace)
            .Set(ParserState.EscapeAfterLeadingEscape, CsvTokens.Text, ParserState.UnexpectedToken);

        // QuoteAfterLeadingEscape
        matrixBuilder.SetDefault(ParserState.QuoteAfterLeadingEscape, ParserState.QuotedFieldText)
            .Set(ParserState.QuoteAfterLeadingEscape, CsvTokens.WhiteSpace,
                ParserState.QuotedFieldTrailingWhiteSpace)
            .Set(ParserState.QuoteAfterLeadingEscape, CsvTokens.Quote, ParserState.UnexpectedToken)
            .Set(ParserState.QuoteAfterLeadingEscape, CsvTokens.Escape, ParserState.UnexpectedToken);
    }

    private static void SetDefaultsToUnexpectedToken(MatrixBuilder matrixBuilder)
    {
        foreach (ParserState parserState in Enum.GetValues<ParserState>()) 
            matrixBuilder.SetDefault(parserState, ParserState.UnexpectedToken);
    }

    private static void ConfigureStartOfFieldTransitions(MatrixBuilder matrixBuilder)
    {
        ParserState[] startOfFieldStates =
            [ParserState.StartOfField, ParserState.EndOfField, ParserState.EndOfRecord];
        // StartOfField, EndOfField, EndOfRecord
        foreach (ParserState state in startOfFieldStates)
            matrixBuilder.Set(state, CsvTokens.Text, ParserState.UnquotedFieldText)
                .Set(state, CsvTokens.Quote, ParserState.QuotedFieldOpenQuote)
                .Set(state, CsvTokens.WhiteSpace, ParserState.LeadingWhiteSpace)
                .Set(state, CsvTokens.FieldDelimiter, ParserState.EndOfField)
                .Set(state, CsvTokens.EndOfRecord, ParserState.EndOfRecord)
                .Set(state, CsvTokens.Escape, ParserState.LeadingEscape);
    }

    public static ParserState Transition(ParserState state, TokenType<CsvTokens> token)
    {
        return (ParserState)TransitionMatrix[(int)state, token];
    }
}