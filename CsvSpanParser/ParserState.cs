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
        QuotedFieldOpenQuote,
        QuotedFieldClosingQuote,
        QuotedFieldText,
        QuotedFieldEscape,
        QuotedFieldWithoutClosingQuote,
        UnexpectedToken,
        QuotedFieldTrailingWhiteSpace,
        UnquotedFieldTrailingWhiteSpace,
    }
}