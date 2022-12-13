namespace FlexableCsvParser
{
    internal readonly struct RecordFieldInfo
    {
        public readonly int StartIndex;

        public readonly int Length;

        public readonly int EscapedQuoteCount;

        public RecordFieldInfo(int startIndex, int length, int escapedQuoteCount) : this()
        {
            StartIndex = startIndex;
            Length = length;
            this.EscapedQuoteCount = escapedQuoteCount;
        }
    }
}