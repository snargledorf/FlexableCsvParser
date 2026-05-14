using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance.Helpers;

namespace FlexableCsvParser
{
    public sealed class StringPool
    {
        private const int DefaultCapacity = 64;
        private const int DefaultSizeLimit = 32;
        private const int CollisionLimit = 8;

        // This is a greatly-simplified HashSet<string> that only allows additions.
        // and accepts char[] instead of string.

        // An extremely simple, and hopefully fast, hash algorithm.
        private static int GetHashCode(ReadOnlySpan<char> buffer)
        {
            return HashCode<char>.Combine(buffer);
        }

        private readonly int _stringSizeLimit;
        private int[] _buckets; // contains index into entries offset by -1. So that 0 (default) means empty bucket.
        private Entry[] _entries;

        private int _count;

        /// <summary>
        /// Creates a new StringPool instance.
        /// </summary>
        public StringPool() : this(DefaultSizeLimit) { }

        /// <summary>
        /// Creates a new StringPool instance.
        /// </summary>
        /// <param name="stringSizeLimit">The size limit beyond which strings will not be pooled.</param>
        /// <remarks>
        /// The <paramref name="stringSizeLimit"/> prevents pooling strings beyond a certain size. 
        /// Longer strings are typically less likely to be duplicated, and and carry extra cost for identifying uniqueness.
        /// </remarks>
        public StringPool(int stringSizeLimit)
        {
            _stringSizeLimit = stringSizeLimit;
            _buckets = new int[DefaultCapacity];
            _entries = new Entry[DefaultCapacity];
        }

        private int GetSize(int capacity)
        {
            int size = _entries.Length * 2;
            return Math.Max(size, capacity);
        }

        /// <summary>
        /// Gets a string containing the characters in the input buffer.
        /// </summary>
        public string GetString(ReadOnlySpan<char> buffer)
        {
            if (buffer.IsEmpty) 
                return string.Empty;
            
            if (buffer.Length > _stringSizeLimit)
                return buffer.ToString();

            Span<Entry> entries = _entries.AsSpan();
            int hashCode = GetHashCode(buffer);

            uint collisionCount = 0;
            ref int bucket = ref GetBucket(hashCode);
            int i = bucket - 1;

            while ((uint)i < (uint)entries.Length)
            {
                ref Entry e = ref entries[i];
                if (e.HashCode == hashCode && buffer.Equals(e.Str, StringComparison.Ordinal))
                {
                    e.Count++;
                    return e.Str;
                }

                i = e.Next;

                collisionCount++;
                if (collisionCount > CollisionLimit)
                {
                    // protects against malicious inputs
                    // too many collisions give up and let the caller create the string.					
                    return buffer.ToString();
                }
            }

            int count = _count;
            if (count == entries.Length)
            {
                entries = Resize().AsSpan();
                bucket = ref GetBucket(hashCode);
            }
            
            int index = count;
            _count = count + 1;

            var stringValue = buffer.ToString();

            ref Entry entry = ref entries[index];
            entry.HashCode = hashCode;
            entry.Count = 1;
            entry.Next = bucket - 1;
            entry.Str = stringValue;

            bucket = index + 1; // bucket is an int ref

            return stringValue;
        }

        private Entry[] Resize()
        {
            int newSize = GetSize(_count + 1);

            var entries = new Entry[newSize];
            Span<Entry> entriesSpan = entries.AsSpan();

            int count = _count;
            _entries.AsSpan(0, count).CopyTo(entriesSpan);

            _buckets = new int[newSize];

            for (var i = 0; i < count; i++)
            {
                ref Entry entry = ref entriesSpan[i];
                
                if (entry.Next >= -1)
                {
                    ref int bucket = ref GetBucket(entry.HashCode);
                    entry.Next = bucket - 1;
                    bucket = i + 1;
                }
            }

            _entries = entries;
            return entries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(int hashCode)
        {
            int[] buckets = _buckets;
            return ref buckets[hashCode & (buckets.Length - 1)];
        }

        private struct Entry
        {
            public int HashCode;
            public int Next;
            public int Count;
            public string Str;
        }
    }
}
