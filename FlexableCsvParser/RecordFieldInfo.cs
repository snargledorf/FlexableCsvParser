namespace FlexableCsvParser
{
    internal readonly record struct RecordFieldInfo(int StartIndex, int Length, int EscapedQuoteCount);
}