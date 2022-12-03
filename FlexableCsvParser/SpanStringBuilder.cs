using System;
using System.Reflection.Emit;

namespace FlexableCsvParser
{
    internal class MemoryStringBuilder
    {
        private Memory<char> buffer = new char[4096];

        public int Length { get; private set; }

        public ReadOnlySpan<char> Span
        {
            get
            {
                return buffer.Span[..Length];
            }
        }

        public void Append(ReadOnlySpan<char> chars)
        {
            EnsureCapacity(chars.Length);

            chars.CopyTo(buffer.Span[Length..]);
            Length += chars.Length;
        }

        private void EnsureCapacity(int charCount)
        {
            if (buffer.Length - Length < charCount)
            {
                int newBlockLength = Math.Max(charCount, Math.Min(buffer.Length, 0x7FFFFFC7));
                int newLength = buffer.Length + newBlockLength;

                // Check for overflow
                if (newLength < newBlockLength)
                    throw new OutOfMemoryException();

                var oldBuffer = buffer;
                buffer = new char[newLength];
                oldBuffer.CopyTo(buffer);
            }
        }

        public void Append(MemoryStringBuilder builder)
        {
            Append(builder.Span);
        }

        public void Clear()
        {
            Length = 0;
        }

        public override string ToString()
        {
            return Span.ToString();
        }
    }
}