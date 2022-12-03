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
        public static Tokenizer CreateRFC4180Tokenizer() => new RFC4180Tokenizer();
        public static Tokenizer CreateDefaultTokenizer() => CreateRFC4180Tokenizer();

        private Memory<char> readBuffer = new char[4096];

        private int readBufferIndex;
        private int readBufferLength;

        private int tokenStartIndex;

        private bool endOfReader;

        protected Tokenizer(Delimiters delimiters)
        {
            Delimiters = delimiters;
        }

        public Delimiters Delimiters { get; }

        private int tokenLength;

        public TokenType TokenType { get; private set; }

        public ReadOnlySpan<char> TokenValue
        {
            get
            {
                int endIndex = tokenStartIndex + tokenLength;
                return readBuffer[tokenStartIndex..endIndex].Span;
            }
        }

        public int TokenLineNumber { get; private set; }

        public int TokenColumnNumber { get; private set; }

        public static Tokenizer For(Delimiters delimiters)
        {
            if (delimiters.IsRFC4180Compliant)
                return CreateRFC4180Tokenizer();

            return new FlexableTokenizer(delimiters);
        }

        public virtual IEnumerable<ITokenizer> EnumerateTokens(TextReader reader)
        {
            while (TryGetNextToken(reader))
                yield return this;
        }

        public bool TryGetNextToken(TextReader reader)
        {
            tokenStartIndex += tokenLength;
            tokenLength = 0;
            readBufferIndex = tokenStartIndex;

            do
            {
                Span<char> buffer = readBuffer[readBufferIndex..readBufferLength].Span;
                if (TryParseToken(buffer, endOfReader, out TokenType type, out int columnIndex, out int lineIndex, out int tokenLength))
                {
                    TokenType = type;
                    TokenColumnNumber = columnIndex;
                    TokenLineNumber = lineIndex;
                    this.tokenLength = tokenLength;

                    return true;
                }
                else
                {
                    readBufferIndex += buffer.Length;
                }
            }
            while (Read(reader));

            return false;
        }

        protected abstract bool TryParseToken(ReadOnlySpan<char> buffer, bool endOfReader, out TokenType type, out int columnIndex, out int lineIndex, out int charCount);

        private bool Read(TextReader reader)
        {
            if (!endOfReader)
                return FillBuffer(reader);

            return readBufferIndex != readBufferLength;
        }

        private bool FillBuffer(TextReader reader)
        {
            if (endOfReader)
                return false;

            if (tokenStartIndex != 0)
            {
                // Move the start of the current token to the start of the buffer
                readBuffer[tokenStartIndex..].CopyTo(readBuffer);

                // Adjust the current buffer length
                readBufferLength -= tokenStartIndex;

                // Shift the current read index
                readBufferIndex -= tokenStartIndex;

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

            Span<char> buffer = readBuffer[readBufferLength..].Span;
            int charsRead = reader.Read(buffer);

            readBufferLength += charsRead;

            endOfReader = charsRead < buffer.Length && reader.Peek() == -1;

            return readBufferIndex != readBufferLength;
        }
    }
}