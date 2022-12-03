namespace FlexableCsvParser
{
    internal readonly struct RecordFieldInfo
    {
        public readonly int StartIndex;

        public RecordFieldInfo(int startIndex, int length) : this()
        {
            StartIndex = startIndex;
            Length = length;
        }

        public readonly int Length;
    }
}