using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tokensharp;

internal struct ReadBuffer(int initialBufferSize) : IDisposable
{
    private char[] _buffer = ArrayPool<char>.Shared.Rent(initialBufferSize);
    
    private int _length;
    private bool _endOfReader;

    public readonly bool EndOfReader => _endOfReader;

    public readonly ReadOnlySpan<char> Chars => _buffer.AsSpan(0, _length);

    public int Length => _length;

    public readonly async ValueTask<ReadBuffer> ReadAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        ReadBuffer readBuffer = this;

        do
        {
            int charsRead = await reader.ReadAsync(readBuffer._buffer.AsMemory(readBuffer._length), cancellationToken)
                .ConfigureAwait(false);
            
            if (charsRead == 0)
            {
                readBuffer._endOfReader = true;
                break;
            }
            
            readBuffer._length += charsRead;
            
        } while (readBuffer._length < readBuffer._buffer.Length);

        return readBuffer;
    }

    public void Read(TextReader reader)
    {
        do
        {
            int charsRead = reader.Read(_buffer.AsSpan(_length));
            if (charsRead == 0)
            {
                _endOfReader = true;
                break;
            }

            _length += charsRead;

        } while (_length < _buffer.Length);
    }

    public void AdvanceBuffer(int charsConsumed)
    {
        Debug.Assert(charsConsumed <= Length);
        
        _length -= charsConsumed;

        if (!_endOfReader)
        {
            if ((uint)_length > ((uint)_buffer.Length / 2))
            {
                char[] oldBuffer = _buffer;
                
                int newMinBufferLength = _buffer.Length < (int.MaxValue / 2) ? _buffer.Length * 2 : int.MaxValue;
                char[] newBuffer = ArrayPool<char>.Shared.Rent(newMinBufferLength);
                
                oldBuffer.AsSpan(charsConsumed).CopyTo(newBuffer.AsSpan(0, _length));
                
                _buffer = newBuffer;
                
                ArrayPool<char>.Shared.Return(oldBuffer, true);
            }
            else if (_length > 0)
            {
                _buffer.AsSpan(charsConsumed, _length).CopyTo(_buffer.AsSpan(0, _length));
            }
        }
        else if (_length > 0)
        {
            _buffer.AsSpan(charsConsumed, _length).CopyTo(_buffer.AsSpan(0, _length));
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