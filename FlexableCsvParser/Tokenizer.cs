using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace FlexableCsvParser
{
    public abstract class Tokenizer : ITokenizer
    {
        public static readonly Tokenizer RFC4180 = new RFC4180Tokenizer();
        public static readonly Tokenizer Default = RFC4180;

        protected Tokenizer(Delimiters delimiters)
        {
            Delimiters = delimiters;
        }

        public Delimiters Delimiters { get; }

        public virtual IEnumerable<Token> EnumerateTokens(TextReader reader)
        {
            Token token;
            while ((token = NextToken(reader)).Type != TokenType.EndOfReader)
                yield return token;

            yield return token;
        }

        public abstract Token NextToken(TextReader reader);

        protected static Token CreateToken(in TokenType type, ref StringBuilder valueBuilder, in ReadOnlySpan<char> buffer)
        {
            var token = new Token(type, valueBuilder?.Append(buffer).ToString() ?? new string(buffer));
            valueBuilder = null;
            return token;
        }

        public static Tokenizer For(Delimiters delimiters)
        {
            if (delimiters.IsRFC4180Compliant)
                return RFC4180;

            return new FlexableTokenizer(delimiters);
        }
    }
}