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
        private Memory<char> readBuffer = new char[4096];

        private int readBufferIndex;
        private int readBufferLength;

        private int tokenStartIndex;

        private bool endOfReader;

        private int tokenLength;
        private int tokenLineNumber;
        private int tokenColumnNumber;
        private TokenType tokenType;
        private readonly TextReader reader;

        protected Tokenizer(TextReader reader, Delimiters delimiters)
        {
            this.reader = reader;
            Delimiters = delimiters;
        }

        public Delimiters Delimiters { get; }

        public TokenType TokenType => tokenType;

        public ReadOnlySpan<char> TokenValue
        {
            get
            {
                int endIndex = tokenStartIndex + tokenLength;
                return readBuffer[tokenStartIndex..endIndex].Span;
            }
        }

        public int TokenLineNumber => tokenLineNumber;

        public int TokenColumnNumber => tokenColumnNumber;

        public static Tokenizer For(Delimiters delimiters, TextReader reader)
        {
            return delimiters.AreRFC4180Compliant ? new RFC4180Tokenizer(reader) : new FlexableTokenizer(reader, delimiters);
        }

        public virtual IEnumerable<ITokenizer> EnumerateTokens()
        {
            while (Read())
                yield return this;
        }

        public bool Read()
        {
            // Move the start of the token
            tokenStartIndex += tokenLength;
            tokenLength = 0;

            readBufferIndex = tokenStartIndex;

            do
            {
                ReadOnlySpan<char> buffer = readBuffer.Span[readBufferIndex..readBufferLength];
                while (!buffer.IsEmpty)
                {
                    if (TryParseToken(buffer, endOfReader, out TokenType type, out int columnIndex, out int lineIndex, out int tokenLength))
                    {
                        tokenType = type;
                        tokenColumnNumber = columnIndex;
                        tokenLineNumber = lineIndex;
                        this.tokenLength = tokenLength;

                        return true;
                    }
                    else
                    {
                        readBufferIndex += buffer.Length;
                    }

                    buffer = readBuffer.Span[readBufferIndex..readBufferLength];
                }
            }
            while (FillBuffer());

            return false;
        }

        protected abstract bool TryParseToken(ReadOnlySpan<char> buffer, bool endOfReader, out TokenType type, out int columnIndex, out int lineIndex, out int charCount);

        private bool FillBuffer()
        {
            if (!endOfReader)
            {
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

                Span<char> buffer = readBuffer.Span[readBufferLength..];
                int charsRead = reader.Read(buffer);

                readBufferLength += charsRead;

                endOfReader = charsRead < buffer.Length && reader.Peek() == -1;
            }

            return readBufferIndex != readBufferLength;
        }
    }
}