using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;

namespace FlexableCsvParser;

internal class ReadBuffer(int initialBufferSize) : IDisposable
{
    private char[] _buffer = new char[initialBufferSize];

    private int _index;
    private int _length;
    private bool _endOfReader;

    public bool EndOfReader => _endOfReader;

    public ReadOnlySpan<char> Chars => _buffer.AsSpan(_index, _length);

    public int Length => _length;

    public bool Read(TextReader reader)
    {
        if (_endOfReader)
            return false;
        
        do
        {
            Span<char> readBuffer = _buffer.AsSpan(_index + _length);
            if (readBuffer.Length == 0)
            {
                CheckBuffer();
                readBuffer = _buffer.AsSpan(_length);
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
    }

    private void CheckBuffer()
    {
        if (_length == _buffer.Length)
        {
            char[] oldBuffer = _buffer;
                
            int newBufferLength = _buffer.Length < (int.MaxValue / 2) ? _buffer.Length * 2 : int.MaxValue;
            var newBuffer = new char[newBufferLength];
                
            oldBuffer.AsSpan(0, _length).CopyTo(newBuffer);
                
            _buffer = newBuffer;
        }
        else if (_index > 0)
        {
            _buffer.AsSpan(_index, _length).CopyTo(_buffer);
            _index = 0;
        }
    }

    public void Dispose()
    {
        if (_buffer is null)
            return;
        
        char[] toReturn = _buffer;
        _buffer = null!;
        
        ArrayPool<char>.Shared.Return(toReturn, true);
    }
}