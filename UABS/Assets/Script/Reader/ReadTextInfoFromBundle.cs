using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.Reader
{
    public class ReadTextInfoFromBundle
    {
        private AssetsManager AssetsManager { get; }

        public ReadTextInfoFromBundle(AssetsManager am)
        {
            AssetsManager = am;
        }

        public List<AssetTextInfo> ReadAllBasicInfo(BundleFileInstance bunInst)
        {
            List<AssetTextInfo> result = new();
            // result.AddRange(ReadSpritesBasicInfo(bunInst));
            // result.AddRange(ReadTexture2DBasicInfo(bunInst));
            AssetsFileInstance fileInst = AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
            foreach (AssetClassID classId in Enum.GetValues(typeof(AssetClassID)))
            {
                result.AddRange(ReadBasicInfoOf(classId, fileInst, AssetsManager));
            }

            return result;
        }

        public List<ParsedAsset> ReadAssetOnly(BundleFileInstance bunInst)
        {
            List<ParsedAsset> result = new();
            AssetsFileInstance fileInst = AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
            foreach (AssetClassID classId in Enum.GetValues(typeof(AssetClassID)))
            {
                List<AssetFileInfo> assets = fileInst.file.GetAssetsOfType(classId);
                result.AddRange(assets.Select(x => new ParsedAsset()
                {
                    fileInfo = x,
                    assetType = classId,
                    fileInst = fileInst
                }).ToList());
            }
            return result;
        }

        private static List<AssetTextInfo> ReadBasicInfoOf(AssetClassID assetType,
                                                                AssetsFileInstance fileInst,
                                                                AssetsManager am)
        {
            int StableStringHash(string input)
            {
                using var sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToInt32(hash, 0); // deterministic but may collide
            }

            int GetAssetDataSize(AssetsManager am, AssetsFileInstance fileInst, AssetFileInfo assetInfo)
            {
                if (assetType == AssetClassID.Texture2D)
                {
                    try
                    {
                        AssetTypeValueField baseField = am.GetBaseField(fileInst, assetInfo);
                        if (baseField == null) return (int)assetInfo.ByteSize;

                        // Texture2D specific data
                        int width = baseField["m_Width"].AsInt;
                        int height = baseField["m_Height"].AsInt;
                        int format = baseField["m_TextureFormat"].AsInt;

                        var streamData = baseField["m_StreamData"];
                        uint streamSize = streamData["size"].AsUInt;
                        string streamPath = streamData["path"].AsString;

                        if (streamSize > 0 && !string.IsNullOrEmpty(streamPath))
                        {
                            // Data is stored in external .resS file
                            return (int)streamSize;
                        }

                        // Inline image data
                        byte[] imageBytes = baseField["image data"].AsByteArray;
                        return imageBytes?.Length ?? 0;
                    }
                    catch
                    {
                        return (int)assetInfo.ByteSize;
                    }
                }
                else
                {
                    // Fallback: use serialized byte size
                    return (int)assetInfo.ByteSize;
                }
            }

            bool HasField(AssetTypeValueField parent, string fieldName)
            {
                return parent.Children.Any(child => child.FieldName == fieldName);
            }

            List<AssetTextInfo> result = new();
            List<AssetFileInfo> assets = fileInst.file.GetAssetsOfType(assetType);

            List<AssetFileInfo> monoScriptAssets = fileInst.file.GetAssetsOfType(AssetClassID.MonoScript);
            foreach (AssetFileInfo asset in assets)
            {
                long pathId = asset.PathId;
                int fileId = fileInst.parentBundle != null
                                ? StableStringHash(fileInst.parentBundle.name)
                                : 0; // or any way to uniquely ID the file
                string filePath = fileInst.path;
                long size = GetAssetDataSize(am, fileInst, asset);

                // To get the asset name:
                AssetTypeValueField baseField = am.GetBaseField(fileInst, asset);
                string name = HasField(baseField, "m_Name") ? baseField["m_Name"].AsString : "Unnamed asset";

                if (assetType == AssetClassID.MonoBehaviour)  // MonoBehaviour inherits name from its MonoScript
                {
                    long searchingPathID = baseField["m_Script"]["m_PathID"].AsLong;
                    foreach (AssetFileInfo compared in monoScriptAssets)
                    {
                        AssetTypeValueField comparedBaseField = am.GetBaseField(fileInst, compared);
                        long comparePathID = compared.PathId;
                        if (comparePathID == searchingPathID)
                        {
                            name = comparedBaseField["m_Name"].AsString;
                        }
                    }
                }

                if (name == "")
                    name = "Unnamed asset";

                result.Add(new()
                {
                    name = name,
                    pathID = pathId,
                    fileID = fileId,
                    path = filePath,
                    compressedSize = asset.ByteSize,
                    uncompressedSize = size,
                    type = assetType
                });
            }

            return result;
        }

        public static string GetAssetTypeValueFieldString(AssetTypeValueField field, int indentLevel = 0)
        {
            if (field == null) return "<null>";

            StringBuilder sb = new StringBuilder();
            string indent = new string(' ', indentLevel * 2);

            // Field name and type
            sb.Append(indent);
            sb.Append(field.FieldName);
            sb.Append(" (");
            sb.Append(field.TypeName);
            sb.Append(")");

            // Field value (if any)
            if (field.Value != null)
            {
                sb.Append(" : ");
                sb.Append(field.Value);
            }
            sb.AppendLine();

            // Recursively append children
            if (field.Children != null)
            {
                foreach (var child in field.Children)
                {
                    sb.Append(GetAssetTypeValueFieldString(child, indentLevel + 1));
                }
            }

            return sb.ToString();
        }
    }
}
