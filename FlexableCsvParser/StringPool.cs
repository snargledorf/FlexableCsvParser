using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
        private static uint GetHashCode(ReadOnlySpan<char> buffer)
        {
            uint hash = 0;
            foreach (char c in buffer)
                hash = hash * 31 + c;

            return hash;
        }

        private IEnumerable<(string str, int count)> GetUsage()
        {
            for (var i = 0; i < _buckets.Length; i++)
            {
                int b = _buckets[i];
                if (b != 0)
                {
                    int idx = b - 1;
                    while ((uint)idx < _entries.Length)
                    {
                        Entry e = _entries[idx];
                        yield return (e.Str, e.Count);
                        idx = e.Next;
                    }
                }
            }
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
            int size = GetSize(DefaultCapacity);
            _stringSizeLimit = stringSizeLimit;
            _buckets = new int[size];
            _entries = new Entry[size];
        }

        private static int GetSize(int capacity)
        {
            int size = DefaultCapacity;
            while (size < capacity)
                size = size * 2;
            return size;
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

            Entry[] entries = _entries;
            uint hashCode = GetHashCode(buffer);

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
                entries = Resize();
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

            int count = _count;
            Array.Copy(_entries, entries, count);

            _buckets = new int[newSize];

            for (var i = 0; i < count; i++)
            {
                if (entries[i].Next >= -1)
                {
                    ref int bucket = ref GetBucket(entries[i].HashCode);
                    entries[i].Next = bucket - 1;
                    bucket = i + 1;
                }
            }

            _entries = entries;
            return entries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(uint hashCode)
        {
            int[] buckets = _buckets;
            return ref buckets[hashCode & ((uint)buckets.Length - 1)];
        }

        private struct Entry
        {
            public uint HashCode;
            public int Next;
            public int Count;
            public string Str;
        }
    }
}
