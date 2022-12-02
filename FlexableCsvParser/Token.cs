using System;

namespace FlexableCsvParser
{
    public readonly struct Token
    {
        public Token(in TokenType type, in int columnIndex, in int lineIndex, in ReadOnlyMemory<char> value)
        {
            Type = type;
            ColumnIndex = columnIndex;
            LineIndex = lineIndex;
            Value = value;
        }

        public readonly TokenType Type;
        public readonly int ColumnIndex;
        public readonly int LineIndex;
        public readonly ReadOnlyMemory<char> Value;

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
            return HashCode.Combine(Type, ColumnIndex, LineIndex, Value);
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