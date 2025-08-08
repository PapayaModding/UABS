using System;
using System.IO;
using System.Text;
using UABS.Assets.Script.DataStruct._New;

namespace UABS.Assets.Script.UnityFile
{
    public class UnityFileDetector
    {
        public static UnityAssetContainer DetectUnityFileType(string filePath)
        {
            if (!File.Exists(filePath))
                return UnityAssetContainer.Unknown;

            byte[] header = new byte[64];
            using (var fs = File.OpenRead(filePath))
            {
                fs.Read(header, 0, header.Length);
            }

            string headStr = Encoding.ASCII.GetString(header).Trim('\0', '\r', '\n', ' ');

            // 1. UnityFS bundle
            if (headStr.StartsWith("UnityFS"))
                return UnityAssetContainer.UnityFSBundle;

            // 2. UnityRaw / UnityWeb bundle
            if (headStr.StartsWith("UnityRaw") || headStr.StartsWith("UnityWeb"))
                return UnityAssetContainer.UnityRawBundle;

            // 3. CAB container
            if (headStr.StartsWith("CAB-"))
                return UnityAssetContainer.CabContainer;

            // 4. Serialized .assets file
            // Unity serialized files have a small int version in first 4 bytes, and
            // a data offset within the first 32 bytes.
            // This is heuristic but works for most cases.
            using (var fs = File.OpenRead(filePath))
            using (var br = new BinaryReader(fs))
            {
                fs.Seek(0, SeekOrigin.Begin);
                int fileGen = br.ReadInt32();
                if (fileGen >= 6 && fileGen <= 30) // Common Unity file generation range
                {
                    return UnityAssetContainer.AssetsFile;
                }
            }

            // 5. Heuristic for ResourceStream (.resS)
            // If filename ends with .resS and doesn't match other formats
            if (filePath.EndsWith(".resS", StringComparison.OrdinalIgnoreCase))
                return UnityAssetContainer.ResourceStream;

            // 6. Heuristic for LegacyResource (.resource)
            if (filePath.EndsWith(".resource", StringComparison.OrdinalIgnoreCase))
                return UnityAssetContainer.LegacyResource;

            // 7. Unknown
            return UnityAssetContainer.Unknown;
        }
    }
}