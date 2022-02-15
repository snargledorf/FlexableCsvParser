namespace CsvSpanParser
{
    public enum IncompleteRecordHandling
    {
        ThrowException,
        FillInWithEmpty,
        FillInWithNull,
        TruncateRecord
    }
}