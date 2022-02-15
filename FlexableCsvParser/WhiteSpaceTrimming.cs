namespace FlexableCsvParser
{
    [Flags]
    public enum WhiteSpaceTrimming
    {
        None = 0,
        Leading = 1,
        Trailing = 2,
        Both = Leading | Trailing,
    }
}