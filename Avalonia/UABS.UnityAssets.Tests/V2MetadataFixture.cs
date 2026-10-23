using System.Buffers.Binary;
using System.Text;
using AssetsTools.NET;

namespace UABS.UnityAssets.Tests;

/// <summary>Synthetic derived fixtures only; never changes the genuine Unity-produced inputs.</summary>
internal sealed class V2MetadataFixture : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("uabs-v2-extraction-").FullName;
    internal string Save(byte[] bytes, string name = "test.assets")
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }
    internal static byte[] Serialized(int count = 6, string virtualPath = "")
    {
        AssetsFile file = new();
        file.Read(File.OpenRead(TestPaths.SerializedFixture));
        try
        {
            int texture = file.Metadata.TypeTreeTypes.FindIndex(t => t.TypeId == 28);
            int unknown = file.Metadata.TypeTreeTypes.Count;
            file.Metadata.TypeTreeTypes.Add(new TypeTreeType
            {
                TypeId = 1234567, ScriptTypeIndex = ushort.MaxValue, Nodes = [], StringBufferBytes = [],
                TypeDependencies = [], TypeHash = new Hash128(new byte[16]), ScriptIdHash = new Hash128(new byte[16])
            });
            List<AssetFileInfo> rows = [];
            for (int i = 0; i < count; i++)
            {
                AssetFileInfo row = new()
                {
                    PathId = i switch { 0 => -77, 1 => long.MaxValue, 4 or 5 => 900, _ => i },
                    TypeIdOrIndex = i == 2 ? unknown : texture, TypeId = i == 2 ? 1234567 : 28,
                    ScriptTypeIndex = ushort.MaxValue
                };
                byte[] name = Encoding.UTF8.GetBytes(i == 0 ? "" : "duplicate");
                byte[] data = new byte[4 + name.Length];
                BinaryPrimitives.WriteInt32LittleEndian(data, i == 3 ? -1 : name.Length);
                name.CopyTo(data, 4);
                row.SetNewData(data);
                rows.Add(row);
            }
            file.Metadata.AssetInfos = rows;
            file.Metadata.Externals =
            [
                new() { PathName = "CAB-duplicate", OriginalPathName = "archive:/first", VirtualAssetPathName = virtualPath,
                    Type = (AssetsFileExternalType)1, Guid = new GUID128 { data0 = 0x01020304, data1 = 0x8090A0B0, data2 = 0xFFFFFFFF, data3 = 0x11223344 } },
                new() { PathName = "", OriginalPathName = "", VirtualAssetPathName = "", Guid = new GUID128() }
            ];
            using MemoryStream output = new();
            file.Write(new AssetsFileWriter(output));
            return output.ToArray();
        }
        finally { file.Close(); }
    }
    internal static byte[] Bundle(string logicalName, bool corruptSecond = false, string virtualPath = "",
        byte[]? leadingResource = null, bool unflaggedLogical = false, byte[]? serializedOverride = null)
    {
        byte[] serialized = serializedOverride ?? Serialized(virtualPath: virtualPath);
        (string Name, byte[] Bytes, uint Flags)[] entries =
        [
            ("before.resS", leadingResource ?? new byte[8], 0), (logicalName, serialized, unflaggedLogical ? 0u : 4u),
            ("gap.resource", new byte[8], 0), (logicalName, corruptSecond ? new byte[48] : serialized, unflaggedLogical ? 0u : 4u)
        ];
        using MemoryStream info = new();
        info.Write(new byte[16]);
        void U32(Stream s, uint value) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, value); s.Write(b); }
        void I64(Stream s, long value) { Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteInt64BigEndian(b, value); s.Write(b); }
        void Str(Stream s, string value) { s.Write(Encoding.UTF8.GetBytes(value)); s.WriteByte(0); }
        uint size = (uint)entries.Sum(e => e.Bytes.Length);
        U32(info, 1); U32(info, size); U32(info, size); info.Write(new byte[2]);
        U32(info, (uint)entries.Length);
        long offset = 0;
        foreach (var entry in entries)
        { I64(info, offset); I64(info, entry.Bytes.Length); U32(info, entry.Flags); Str(info, entry.Name); offset += entry.Bytes.Length; }
        using MemoryStream output = new();
        Str(output, "UnityFS"); U32(output, 6); Str(output, "5.x.x"); Str(output, "2019.4.31f1");
        long sizePosition = output.Position;
        I64(output, 0); U32(output, (uint)info.Length); U32(output, (uint)info.Length); U32(output, 0x40);
        info.Position = 0; info.CopyTo(output);
        foreach (var entry in entries) output.Write(entry.Bytes);
        long total = output.Length; output.Position = sizePosition; I64(output, total);
        return output.ToArray();
    }
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
