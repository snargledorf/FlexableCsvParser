namespace FlexableCsvParser
{
    public enum IncompleteRecordHandling
    {
        ThrowException,
        FillInWithEmpty,
        FillInWithNull,
        TruncateRecord
    }
}