namespace CsvSpanParser
{
    public enum TokenType
    {
        EndOfReader,
        Field,
        EndOfRecord,
        Quote,
        Escape,
        Text,
        WhiteSpace,
    }
}