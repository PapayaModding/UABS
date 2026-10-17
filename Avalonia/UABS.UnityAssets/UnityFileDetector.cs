using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace UABS.UnityAssets;

public static partial class UnityFileDetector
{
    public static UnityFileKind Detect(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("The Unity asset file does not exist.", path);

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Detect(stream);
    }

    public static UnityFileKind Detect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead || !stream.CanSeek || stream.Length < 0x20)
            return UnityFileKind.Unknown;

        long originalPosition = stream.Position;
        try
        {
            Span<byte> headerBytes = stackalloc byte[7];
            stream.Position = 0;
            if (stream.Read(headerBytes) != headerBytes.Length)
                return UnityFileKind.Unknown;

            string bundleHeader = Encoding.ASCII.GetString(headerBytes);
            if (bundleHeader is "UnityFS" or "UnityRaw" or "UnityWeb")
                return UnityFileKind.Bundle;

            Span<byte> formatBytes = stackalloc byte[4];
            stream.Position = 0x08;
            if (stream.Read(formatBytes) != formatBytes.Length)
                return UnityFileKind.Unknown;

            int format = BinaryPrimitives.ReadInt32BigEndian(formatBytes);
            long versionOffset = format >= 0x16 ? 0x30 : 0x14;
            if (versionOffset >= stream.Length)
                return UnityFileKind.Unknown;

            stream.Position = versionOffset;
            StringBuilder possibleVersion = new(32);
            while (stream.Position < stream.Length && possibleVersion.Length <= 0xFF)
            {
                int value = stream.ReadByte();
                if (value <= 0)
                    break;

                possibleVersion.Append((char)value);
            }

            string version = possibleVersion.ToString();
            string invalidCharacters = ValidVersionCharacters().Replace(version, string.Empty);
            string normalizedVersion = InvalidVersionCharacters().Replace(version, string.Empty);

            return format < 0xFF && invalidCharacters.Length == 0 && normalizedVersion.Length >= 5
                ? UnityFileKind.SerializedFile
                : UnityFileKind.Unknown;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    [GeneratedRegex("[a-zA-Z0-9.\\n-]")]
    private static partial Regex ValidVersionCharacters();

    [GeneratedRegex("[^a-zA-Z0-9.\\n-]")]
    private static partial Regex InvalidVersionCharacters();
}
