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

        protected readonly ReadOnlyMemory<char> FieldValue;
        protected readonly ReadOnlyMemory<char> EndOfRecordValue;
        protected readonly ReadOnlyMemory<char> QuoteValue;
        protected readonly ReadOnlyMemory<char> EscapeValue;

        private Memory<char> readBuffer = new char[4096];

        private int readBufferIndex;
        private int readBufferLength;

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
                return CreateRFC4180Tokenizer();

            return new FlexableTokenizer(delimiters);
        }

        public virtual async IAsyncEnumerable<Token> EnumerateTokensAsync(TextReader reader)
        {
            while (await ReadAsync(reader).ConfigureAwait(false))
            {
                while (TryGetNextToken(out Token token))
                {
                    yield return token;
                }
            }
        }

        public async ValueTask<bool> ReadAsync(TextReader reader)
        {
            if (!EndOfReader)
                return await FillBufferAsync(reader).ConfigureAwait(false);

            return readBufferIndex != readBufferLength;
        }

        public bool TryGetNextToken(out Token token)
        {
            if (TryParseToken(readBuffer[readBufferIndex..readBufferLength].Span, out TokenType type, out int columnIndex, out int lineIndex, out int charCount))
            {
                ReadOnlyMemory<char> value = readBuffer[readBufferIndex..readBufferLength][..charCount];
                token = new Token(type, columnIndex, lineIndex, value);

                readBufferIndex += charCount;
                return true;
            }

            token = default;
            return false;
        }

        protected abstract bool TryParseToken(in ReadOnlySpan<char> buffer, out TokenType type, out int columnIndex, out int lineIndex, out int charCount);

        public bool EndOfReader { get; private set; }

        private async ValueTask<bool> FillBufferAsync(TextReader reader)
        {
            if (EndOfReader)
                return false;

            if (readBufferIndex != 0)
            {
                // Move the start of the current token to the start of the buffer
                readBuffer[readBufferIndex..].CopyTo(readBuffer);

                // Adjust the current buffer length
                readBufferLength -= readBufferIndex;

                // Shift the current read index
                readBufferIndex = 0;
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

            Memory<char> buffer = readBuffer[readBufferLength..];
            int charsRead = await reader.ReadAsync(buffer).ConfigureAwait(false);

            readBufferLength += charsRead;

            EndOfReader = charsRead < buffer.Length && reader.Peek() == -1;

            return readBufferIndex != readBufferLength;
        }
    }
}