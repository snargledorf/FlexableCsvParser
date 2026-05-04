using System;
using System.Buffers;
using System.Collections.Generic;

namespace FlexableCsvParser;

public sealed class FieldSequenceBuilder(ReadOnlyMemory<char> quote) : IDisposable
{
    private sealed class FieldSegment : ReadOnlySequenceSegment<char>
    {
        public void Set(ReadOnlyMemory<char> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public void SetNext(FieldSegment? next)
        {
            Next = next;
        }

        public void Reset()
        {
            Memory = default;
            Next = null;
            RunningIndex = 0;
        }
    }

    private readonly Stack<FieldSegment> _segmentPool = new();
    private readonly List<FieldSegment> _usedSegments = [];
    private FieldSegment? _first;
    private FieldSegment? _last;
    private long _totalLength;

    public bool IsEmpty => _totalLength == 0;

    public long Length => _totalLength;

    public void Append(ReadOnlyMemory<char> memory)
    {
        if (memory.IsEmpty) 
            return;

        if (!_segmentPool.TryPop(out FieldSegment? segment))
            segment = new FieldSegment();
        
        _usedSegments.Add(segment);
        segment.Set(memory, _totalLength);

        if (_first == null)
        {
            _first = _last = segment;
        }
        else
        {
            _last!.SetNext(segment);
            _last = segment;
        }

        _totalLength += memory.Length;
    }

    public void AppendQuote()
    {
        Append(quote);
    }

    public ReadOnlySequence<char> Build()
    {
        if (_first == null) 
            return ReadOnlySequence<char>.Empty;
        
        var sequence = new ReadOnlySequence<char>(_first, 0, _last!, _last!.Memory.Length);
        
        // Reset state for next field, but DON'T return segments to pool yet!
        // The built sequence still needs them.
        _first = null;
        _last = null;
        _totalLength = 0;
        
        return sequence;
    }

    public void Reset()
    {
        foreach (FieldSegment segment in _usedSegments)
        {
            segment.Reset();
            _segmentPool.Push(segment);
        }
        
        _usedSegments.Clear();
        _first = null;
        _last = null;
        _totalLength = 0;
    }

    public void Dispose()
    {
        _segmentPool.Clear();
        _usedSegments.Clear();
    }
}