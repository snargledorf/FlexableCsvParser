namespace CsvSpanParser
{
    public enum TokenType
    {
        EndOfReader,
        FieldDelimiter,
        RecordDelimiter,
        QuoteDelimiter,
        EscapeDelimiter,
        Text,
        WhiteSpace,
    }
}