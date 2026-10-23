using System.Buffers.Binary;
using System.Text;
using AssetsTools.NET;

namespace UABS.UnityAssets.Metadata;

/// <summary>Bounded field skipping, not generic object deserialization. No payload/value trees are built.</summary>
internal static class V2MetadataEnrichment
{
    internal sealed record Field(string Name, string Type, int Size, bool Align, IReadOnlyList<Field> Children);

    internal static Field? Shape(TypeTreeType? tree)
    {
        if (tree is null || tree.Nodes.Count == 0 || tree.Nodes.Count > 16384) return null;
        int index = 0;
        Field Read(int depth)
        {
            if (depth > 64) throw new InvalidDataException("Type metadata exceeds depth budget.");
            TypeTreeNode node = tree.Nodes[index++];
            List<Field> children = [];
            while (index < tree.Nodes.Count && tree.Nodes[index].Level > node.Level) children.Add(Read(depth + 1));
            return new(node.GetNameString(tree.StringBufferBytes), node.GetTypeString(tree.StringBufferBytes),
                node.ByteSize, (node.MetaFlags & 0x4000) != 0, children);
        }
        return Read(0);
    }

    internal static Field Shape(ClassDatabaseTypeNode root, ClassDatabaseFile database)
    {
        int count = 0;
        Field Read(ClassDatabaseTypeNode node, int depth)
        {
            if (++count > 16384 || depth > 64) throw new InvalidDataException("Class metadata exceeds budget.");
            return new(database.GetString(node.FieldName), database.GetString(node.TypeName), node.ByteSize,
                (node.MetaFlag & 0x4000) != 0, node.Children.Select(child => Read(child, depth + 1)).ToArray());
        }
        return Read(root, 0);
    }

    internal static string ReadString(AssetsFileReader reader, int maximum)
    {
        int length = reader.ReadInt32();
        if (length < 0) throw new InvalidDataException("Negative metadata string length.");
        if (length > maximum) throw new MetadataBudgetException("string-bytes", length, maximum, reader.Position - 4);
        if (length > reader.BaseStream.Length - reader.Position) throw new EndOfStreamException();
        byte[] bytes = new byte[length];
        reader.BaseStream.ReadExactly(bytes);
        return new UTF8Encoding(false, true).GetString(bytes);
    }

    internal static (string? Name, V2NameStatus Status) Name(AssetsFile file, AssetFileInfo info,
        Field? shape, V2ExtractionOptions options, CancellationToken token)
    {
        if (!options.ReadNames) return (null, V2NameStatus.Unattempted);
        if (shape is null) return (null, V2NameStatus.Unattempted);
        // Only an evidence-backed leading name. Version-specific GameObject/MonoBehaviour offsets are not guessed.
        if (shape.Children.FirstOrDefault() is not { Name: "m_Name", Type: "string" })
            return (null, shape.Children.Any(f => f.Name == "m_Name") ? V2NameStatus.Unattempted : V2NameStatus.Absent);
        using V2ReadWindow window = new(file.Reader.BaseStream, info.GetAbsoluteByteOffset(file), info.ByteSize, token);
        using AssetsFileReader reader = new(window) { BigEndian = file.Header.Endianness };
        try { return (ReadString(reader, options.MaxNameBytes), V2NameStatus.Present); }
        catch (MetadataBudgetException) { return (null, V2NameStatus.Budgeted); }
        catch (Exception e) when (e is IOException or InvalidDataException or DecoderFallbackException or ArgumentException)
        { return (null, V2NameStatus.Failed); }
    }

    internal static IReadOnlyList<string>? BundleDependencies(AssetsFile file, AssetFileInfo info,
        Field? shape, V2ExtractionOptions options, CancellationToken token)
    {
        if (shape is null) return null;
        using V2ReadWindow window = new(file.Reader.BaseStream, info.GetAbsoluteByteOffset(file),
            Math.Min(info.ByteSize, options.MaxDependencyBytes), token);
        using AssetsFileReader reader = new(window) { BigEndian = file.Header.Endianness };
        int visits = 0;
        string rootField = "unknown";
        void Align(Field field) { if (field.Align) reader.Position = checked((reader.Position + 3) & ~3L); }
        void Skip(Field field, int depth)
        {
            token.ThrowIfCancellationRequested();
            if (++visits > options.MaxDependencyItems) throw new MetadataBudgetException("field-visits:" + rootField, visits, options.MaxDependencyItems, reader.Position);
            if (depth > 64) throw new MetadataBudgetException("field-depth", depth, 64, reader.Position);
            if (field.Type == "string") { _ = ReadString(reader, options.MaxDependencyBytes); reader.Position = (reader.Position + 3) & ~3L; }
            else if (field.Type == "Array")
            {
                int count = reader.ReadInt32();
                if (count < 0) throw new InvalidDataException("Negative metadata array length.");
                if (count > options.MaxDependencyItems) throw new MetadataBudgetException("array-items:" + rootField + "/" + field.Name, count, options.MaxDependencyItems, reader.Position - 4);
                if (field.Children.Count != 2) throw new InvalidDataException("Unsupported dependency array shape: expected size/data children.");
                for (int i = 0; i < count; i++) Skip(field.Children[1], depth + 1);
            }
            else if (field.Size >= 0) reader.Position = checked(reader.Position + field.Size);
            else if (field.Children.Count > 0) { foreach (Field child in field.Children) Skip(child, depth + 1); }
            else throw new InvalidDataException("Unknown variable-size dependency field.");
            Align(field);
        }
        foreach (Field field in shape.Children)
        {
            rootField = field.Name;
            if (field.Name != "m_Dependencies") { Skip(field, 0); continue; }
            Field? array = field.Type == "Array" ? field : field.Children.FirstOrDefault();
            if (array?.Type != "Array" || array.Children.Count != 2 || array.Children[1].Type != "string")
                throw new InvalidDataException("Unsupported dependency declaration layout.");
            int count = reader.ReadInt32();
            if (count < 0) throw new InvalidDataException("Negative dependency count.");
            if (count > options.MaxDependencyItems) throw new MetadataBudgetException("dependency-items", count, options.MaxDependencyItems, reader.Position - 4);
            List<string> values = new(count);
            for (int i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();
                values.Add(ReadString(reader, options.MaxDependencyBytes));
                reader.Position = (reader.Position + 3) & ~3L;
            }
            return values;
        }
        return null;
    }

    public static string GuidHex(GUID128 guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, guid.data0);
        BinaryPrimitives.WriteUInt32BigEndian(bytes[4..], guid.data1);
        BinaryPrimitives.WriteUInt32BigEndian(bytes[8..], guid.data2);
        BinaryPrimitives.WriteUInt32BigEndian(bytes[12..], guid.data3);
        return Convert.ToHexString(bytes);
    }

    internal sealed class MetadataBudgetException(string budget, long observed, long limit, long position)
        : IOException($"Metadata budget '{budget}' exceeded: observed={observed}, limit={limit}, asset-relative offset={position}. Optional enrichment incomplete; not evidence of corrupt source.");
}
