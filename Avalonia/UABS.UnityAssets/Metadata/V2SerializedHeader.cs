using System.Buffers.Binary;

namespace UABS.UnityAssets.Metadata;

/// <summary>Bounded header evidence before handing a candidate to the vendored metadata parser.
/// Not a metadata parser, extension filter, or claim of support for all structurally plausible versions.</summary>
internal sealed record V2SerializedHeader(uint Version, long MetadataSize, long FileSize, long DataOffset, byte Endian, int Bytes)
{
    public static V2SerializedHeader Read(Stream source, long offset, long size, CancellationToken token)
    {
        if (offset < 0 || size < 0 || offset > source.Length || size > source.Length - offset)
            throw new InvalidDataException("Logical-file range exceeds the decompressed stream.");
        using V2ReadWindow window = new(source, offset, size, token);
        Span<byte> bytes = stackalloc byte[48];
        window.ReadExactly(bytes[..20]);
        uint version = BinaryPrimitives.ReadUInt32BigEndian(bytes[8..]);
        long metadata = BinaryPrimitives.ReadUInt32BigEndian(bytes), length = BinaryPrimitives.ReadUInt32BigEndian(bytes[4..]);
        long data = BinaryPrimitives.ReadUInt32BigEndian(bytes[12..]);
        if (version >= 22)
        {
            window.ReadExactly(bytes[20..]);
            metadata = BinaryPrimitives.ReadUInt32BigEndian(bytes[20..]);
            length = BinaryPrimitives.ReadInt64BigEndian(bytes[24..]);
            data = BinaryPrimitives.ReadInt64BigEndian(bytes[32..]);
        }
        return new(version, metadata, length, data, bytes[16], version >= 22 ? 48 : 20);
    }
    public bool Plausible(long logicalSize) => Version >= 5 && Endian <= 1 && MetadataSize > 0 && MetadataSize <= FileSize
        && FileSize >= Bytes && FileSize <= logicalSize && DataOffset >= Bytes && DataOffset <= FileSize;
    public static bool IsCandidate(Stream source, long offset, long size, CancellationToken token)
    {
        if (size < 20) return false;
        try { return Read(source, offset, size, token).Plausible(size); }
        catch (EndOfStreamException) { return false; } // Unflagged short resource: not a confirmed serialized table.
    }
    public string Evidence => $"version={Version}; endian={Endian}; metadataSize={MetadataSize}; fileSize={FileSize}; dataOffset={DataOffset}";
}
