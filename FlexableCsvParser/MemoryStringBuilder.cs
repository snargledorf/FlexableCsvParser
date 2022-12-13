using System;
using System.Buffers;

namespace FlexableCsvParser
{
    internal class MemoryStringBuilder
    {
        private Memory<char> buffer;

        private int length;

        public MemoryStringBuilder(int initialCapacity = 4096)
        {
            buffer = new char[initialCapacity];
        }

        public int Length => length;

        public ReadOnlySpan<char> Span
        {
            get
            {
                return buffer.Span[..Length];
            }
        }

        public void Append(in ReadOnlySpan<char> chars)
        {
            EnsureCapacity(chars.Length);

            chars.CopyTo(buffer.Span[Length..]);
            length += chars.Length;
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

        public void Clear()
        {
            length = 0;
        }

        public override string ToString()
        {
            return Span.ToString();
        }

        public static implicit operator ReadOnlySpan<char>(MemoryStringBuilder builder)
        {
            return builder.Span;
        }
    }
}