using System;

namespace FlexableCsvParser
{
    public struct Token
    {
        public Token(TokenType type, int columnIndex, int lineIndex, ReadOnlyMemory<char> value)
        {
            Type = type;
            ColumnIndex = columnIndex;
            LineIndex = lineIndex;
            Value = value;
        }

        public TokenType Type { get; }
        public int ColumnIndex { get; }
        public int LineIndex { get; }
        public ReadOnlyMemory<char> Value { get; }

        public static bool operator ==(Token left, Token right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Token left, Token right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Token token &&
                   Type == token.Type &&
                   ColumnIndex == token.ColumnIndex &&
                   LineIndex == token.LineIndex &&
                   Value.Span.SequenceEqual(token.Value.Span);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }

        public override string ToString()
        {
            string typeWithLocation = $"{Type}; Column: {ColumnIndex + 1}, Line: {LineIndex + 1}";
            return Value.IsEmpty
                ? typeWithLocation
                : string.Join("; ", typeWithLocation, Value);
        }
    }
}