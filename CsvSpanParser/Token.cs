﻿namespace FlexableCsvParser
{
    public struct Token
    {
        public static readonly Token EndOfReader = new(TokenType.EndOfReader);
        public static readonly Token FieldDelimiter = new(TokenType.Field);
        public static readonly Token EndOfRecord = new(TokenType.EndOfRecord);
        public static readonly Token Quote = new(TokenType.Quote);
        public static readonly Token Escape = new(TokenType.Escape);

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

        public static bool operator ==(Token left, Token right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Token left, Token right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return obj is Token token &&
                   Type == token.Type &&
                   Value == token.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Value)
                ? Type.ToString()
                : string.Join(" : ", Type, Value);
        }
    }
}