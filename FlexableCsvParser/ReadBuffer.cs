using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FlexableCsvParser;

internal class ReadBuffer(int initialBufferSize) : IDisposable
{
    private char[] _buffer = ArrayPool<char>.Shared.Rent(initialBufferSize);

    private int _index;
    private int _length;
    private bool _endOfReader;

    public bool EndOfReader => _endOfReader;

    public ReadOnlySpan<char> Chars => _buffer.AsSpan(_index, _length);

    public int Length => _length;

    public void Read(TextReader reader)
    {
        if (_endOfReader)
            return;
        
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
                return;
            }

            _length += charsRead;

        } while (_length < _buffer.Length);
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
                
            int newMinBufferLength = _buffer.Length < (int.MaxValue / 2) ? _buffer.Length * 2 : int.MaxValue;
            char[] newBuffer = ArrayPool<char>.Shared.Rent(newMinBufferLength);
                
            oldBuffer.AsSpan(0, _length).CopyTo(newBuffer);
                
            _buffer = newBuffer;
                
            ArrayPool<char>.Shared.Return(oldBuffer);
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