using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
namespace FlexableCsvParser
{
    public abstract class Tokenizer : ITokenizer
    {
        public static readonly Tokenizer RFC4180 = new RFC4180Tokenizer();
        public static readonly Tokenizer Default = RFC4180;

        protected readonly string FieldValue;
        protected readonly string EndOfRecordValue;
        protected readonly string QuoteValue;
        protected readonly string EscapeValue;

        protected Tokenizer(Delimiters delimiters)
        {
            Delimiters = delimiters;

            FieldValue = delimiters.Field;
            EndOfRecordValue = delimiters.EndOfRecord;
            QuoteValue = delimiters.Quote;
            EscapeValue = delimiters.Escape;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Token CreateToken(in TokenType type, in int columnIndex, in int lineIndex, in ReadOnlySpan<char> buffer)
        {
            return new Token(type, columnIndex, lineIndex, new string(buffer));
        }

        public static Tokenizer For(Delimiters delimiters)
        {
            if (delimiters.IsRFC4180Compliant)
                return RFC4180;

            return new FlexableTokenizer(delimiters);
        }
    }
}