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
        public static readonly Tokenizer RFC4180 = new RFC4180Tokenizer();
        public static readonly Tokenizer Default = RFC4180;

        protected readonly ReadOnlyMemory<char> FieldValue;
        protected readonly ReadOnlyMemory<char> EndOfRecordValue;
        protected readonly ReadOnlyMemory<char> QuoteValue;
        protected readonly ReadOnlyMemory<char> EscapeValue;

        private Memory<char> readBuffer = new char[4096];

        private int readBufferIndex;
        private int readBufferLength;

        private int tokenStartIndex;

        protected Tokenizer(Delimiters delimiters)
        {
            Delimiters = delimiters;

            FieldValue = delimiters.Field.AsMemory();
            EndOfRecordValue = delimiters.EndOfRecord.AsMemory();
            QuoteValue = delimiters.Quote.AsMemory();
            EscapeValue = delimiters.Escape.AsMemory();
        }

        public Delimiters Delimiters { get; }

        public static Tokenizer For(Delimiters delimiters)
        {
            if (delimiters.IsRFC4180Compliant)
                return RFC4180;

            return new FlexableTokenizer(delimiters);
        }

        public virtual async IAsyncEnumerable<Token> EnumerateTokensAsync(TextReader reader)
        {
            Token token;
            while ((token = await NextTokenAsync(reader)).Type != TokenType.EndOfReader)
                yield return token;

            yield return token;
        }

        public abstract ValueTask<Token> NextTokenAsync(TextReader reader);

        protected Token CreateToken(in TokenType type, in int columnIndex, in int lineIndex)
        {
            ReadOnlyMemory<char> value = readBuffer[tokenStartIndex..readBufferIndex];

            tokenStartIndex = readBufferIndex;

            return new Token(type, columnIndex, lineIndex, value);
        }

        public bool EndOfBuffer => readBufferIndex == readBufferLength;

        protected char CurrentChar => readBuffer.Span[readBufferIndex];

        protected void MoveToNextChar()
        {
            readBufferIndex++;
        }

        protected async ValueTask FillBufferAsync(TextReader reader)
        {
            if (tokenStartIndex != 0)
            {
                // Move the start of the current token to the start of the buffer
                readBuffer[tokenStartIndex..].CopyTo(readBuffer);

                // Shift the current read index
                readBufferIndex -= tokenStartIndex;

                // Adjust the current buffer length
                readBufferLength -= tokenStartIndex;

                tokenStartIndex = 0;
            }
            else if (readBufferLength != 0)
            {
                int newBufferSize = readBuffer.Length * 2;
                if (newBufferSize < readBuffer.Length)
                    throw new OutOfMemoryException();

                var oldBuffer = readBuffer;
                readBuffer = new char[newBufferSize];
                oldBuffer.CopyTo(readBuffer);
            }

            readBufferLength += await reader.ReadAsync(readBuffer[readBufferLength..]).ConfigureAwait(false);
        }
    }
}