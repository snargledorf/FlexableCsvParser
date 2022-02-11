namespace CsvSpanParser
{
    public enum TokenType
    {
        EndOfReader,
        FieldDelimiter,
        EndOfRecord,
        Quote,
        Escape,
        Text,
        WhiteSpace,
    }
}