using System;

namespace FlexableCsvParser
{
    public struct Token
    {
        public Token(TokenType type, int columnIndex, int lineIndex)
        {
            Type = type;
            ColumnIndex = columnIndex;
            LineIndex = lineIndex;
            Value = null;
        }

        public Token(TokenType type, int columnIndex, int lineIndex, string value)
            : this(type, columnIndex, lineIndex)
        {
            Value = value;
        }

        public TokenType Type { get; }
        public int ColumnIndex { get; }
        public int LineIndex { get; }
        public string Value { get; }

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
                   Value == token.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }

        public override string ToString()
        {
            string typeWithLocation = $"{Type}; Column: {ColumnIndex + 1}, Line: {LineIndex + 1}";
            return string.IsNullOrEmpty(Value)
                ? typeWithLocation
                : string.Join("; ", typeWithLocation, Value);
        }
    }
}