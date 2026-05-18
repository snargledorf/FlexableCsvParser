namespace FlexableCsvParser
{
    internal enum ParserState
    {
        StartOfRecord,
        UnquotedFieldText,
        EndOfField,
        EndOfRecord,
        EndOfReader,
        LeadingWhiteSpace,
        LeadingEscape,
        QuotedFieldOpenQuote,
        QuotedFieldClosingQuote,
        QuotedFieldText,
        QuotedFieldEscape,
        QuotedFieldWithoutClosingQuote,
        UnexpectedToken,
        QuotedFieldLeadingWhiteSpace,
        QuotedFieldTrailingWhiteSpace,
        QuotedFieldClosingQuoteTrailingWhiteSpace,
        UnquotedFieldTrailingWhiteSpace,
        EscapeAfterLeadingEscape,
        QuoteAfterLeadingEscape,
    }
}