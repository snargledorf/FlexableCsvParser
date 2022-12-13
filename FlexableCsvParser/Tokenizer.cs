using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexableCsvParser
{
    public abstract class Tokenizer : ITokenizer
    {
        protected Tokenizer(Delimiters delimiters)
        {
            Delimiters = delimiters;
        }

        public Delimiters Delimiters { get; }

        public static Tokenizer For(Delimiters delimiters)
        {
            return delimiters.AreRFC4180Compliant ? new RFC4180Tokenizer() : new FlexableTokenizer(delimiters);
        }

        public abstract bool TryParseToken(ReadOnlySpan<char> buffer, bool endOfReader, out TokenType type, out int tokenLength);
    }
}