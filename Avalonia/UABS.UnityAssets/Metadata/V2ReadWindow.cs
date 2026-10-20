namespace UABS.UnityAssets.Metadata;

/// <summary>A read-only, seekable, non-owning window; forbids reads outside this logical file/asset.</summary>
internal sealed class V2ReadWindow(Stream source, long start, long length, CancellationToken token) : Stream
{
    private long _position;
    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer)
    {
        token.ThrowIfCancellationRequested();
        source.Position = checked(start + _position);
        int read = source.Read(buffer[..(int)Math.Min(buffer.Length, length - _position)]);
        _position += read;
        return read;
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        token.ThrowIfCancellationRequested();
        long next = checked((origin switch { SeekOrigin.Begin => 0, SeekOrigin.Current => _position,
            SeekOrigin.End => length, _ => throw new ArgumentOutOfRangeException(nameof(origin)) }) + offset);
        if (next < 0 || next > length) throw new InvalidDataException("Metadata seek outside its declared byte range.");
        return _position = next;
    }
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
