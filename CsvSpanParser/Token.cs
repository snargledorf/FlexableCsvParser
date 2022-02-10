namespace CsvSpanParser
{
    public struct Token
    {
        internal static readonly Token EndOfReader = new(TokenType.EndOfReader);
        internal static readonly Token FieldDelimiter = new(TokenType.FieldDelimiter);
        internal static readonly Token EndOfRecord = new(TokenType.RecordDelimiter);
        internal static readonly Token Quote = new(TokenType.QuoteDelimiter);
        internal static readonly Token Escape = new(TokenType.EscapeDelimiter);

        public Token(TokenType type)
        {
            Type = type;
            Value = null;
        }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        public TokenType Type { get; }
        public string? Value { get; }
    }
}