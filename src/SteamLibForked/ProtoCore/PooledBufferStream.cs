using System.Buffers;

namespace SteamLib.ProtoCore;

/// <summary>
/// A non-allocating, forward-only stream backed by an ArrayPool buffer for high-performance protobuf serialization.
/// </summary>
internal sealed class PooledBufferStream(int initialCapacity = 256) : Stream
{
    private byte[]? _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    private int _length;

    public ReadOnlySpan<byte> WrittenSpan => _buffer != null ? _buffer.AsSpan(0, _length) : ReadOnlySpan<byte>.Empty;
    public ReadOnlyMemory<byte> WrittenMemory => _buffer != null ? _buffer.AsMemory(0, _length) : ReadOnlyMemory<byte>.Empty;

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => _buffer != null;
    public override long Length => _length;

    public override long Position
    {
        get => _length;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_buffer == null, this);
        ArgumentNullException.ThrowIfNull(buffer);
        EnsureCapacity(_length + count);
        Buffer.BlockCopy(buffer, offset, _buffer, _length, count);
        _length += count;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_buffer == null, this);
        EnsureCapacity(_length + buffer.Length);
        buffer.CopyTo(_buffer.AsSpan(_length));
        _length += buffer.Length;
    }

    public override void WriteByte(byte value)
    {
        ObjectDisposedException.ThrowIf(_buffer == null, this);
        EnsureCapacity(_length + 1);
        _buffer[_length++] = value;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _buffer!.Length) return;

        var newCapacity = Math.Max(requiredCapacity, _buffer.Length * 2);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _buffer = newBuffer;
    }

    protected override void Dispose(bool disposing)
    {
        if (_buffer != null)
        {
            ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
            _buffer = null;
        }
        base.Dispose(disposing);
    }
}
