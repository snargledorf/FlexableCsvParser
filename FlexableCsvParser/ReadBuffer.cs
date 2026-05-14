using System;
using System.Diagnostics;
using System.IO;

namespace FlexableCsvParser;

internal class ReadBuffer(int initialBufferSize)
{
    private Memory<char> _buffer = new char[initialBufferSize];

    private int _index;
    private int _length;
    private bool _endOfReader;

    public bool EndOfReader => _endOfReader;

    public ReadOnlyMemory<char> Chars => _buffer.Slice(_index, _length);

    public int Length => _length;

    public bool Read(TextReader reader)
    {
        if (_endOfReader)
            return false;
        
        do
        {
            Span<char> bufferSpan = _buffer.Span;
            
            int startIndex = _index + _length;
            Span<char> readBuffer = bufferSpan[startIndex..];
            
            if (readBuffer.Length == 0)
            {
                CheckBuffer();
                readBuffer = _buffer.Span[_length..];
            }
            
            int charsRead = reader.Read(readBuffer);
            if (charsRead == 0)
            {
                _endOfReader = true;
                return _length > 0;
            }

            _length += charsRead;

        } while (_length < _buffer.Length);

        return _length > 0;
    }

    public void AdvanceBuffer(int charsConsumed)
    {
        Debug.Assert(charsConsumed <= _length);
        
        _length -= charsConsumed;
        _index += charsConsumed;
        
        _buffer.Span.Slice(_index, _length).CopyTo(_buffer.Span);
        _index = 0;
    }

    private void CheckBuffer()
    {
        if (_length == _buffer.Length)
        {
            Memory<char> oldBuffer = _buffer;
                
            int newBufferLength = _buffer.Length < (int.MaxValue / 2) ? _buffer.Length * 2 : int.MaxValue;
            var newBuffer = new char[newBufferLength];
                
            oldBuffer.Span[.._length].CopyTo(newBuffer);
                
            _buffer = newBuffer;
        }
        else if (_index > 0)
        {
            _buffer.Span.Slice(_index, _length).CopyTo(_buffer.Span);
            _index = 0;
        }
    }
}