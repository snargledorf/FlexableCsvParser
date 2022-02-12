namespace CsvSpanParser
{
    internal enum ParserState
    {
        Start,
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
        QuotedFieldTrailingWhiteSpace,
        UnquotedFieldTrailingWhiteSpace,
        EscapeAfterLeadingEscape,
    }
}