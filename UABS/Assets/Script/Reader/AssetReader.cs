using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Reader
{
    /// <summary>
    /// * Reads information from ParsedAsset
    /// </summary>
    public class AssetReader
    {
        private Dictionary<long, string> _currMonoScriptNames;
        private AssetsManager _assetsManager;

        public AssetReader(AssetsManager am)
        {
            _assetsManager = am;
        }

        public void MakeMonoScriptNameTable(AssetsFileInstance fileInst)
        {
            _currMonoScriptNames = new();
            List<AssetFileInfo> monoScriptAssets = fileInst.file.GetAssetsOfType(AssetClassID.MonoScript);
            foreach (AssetFileInfo monoScriptAsset in monoScriptAssets)
            {
                AssetTypeValueField baseField = _assetsManager.GetBaseField(fileInst, monoScriptAsset);
                _currMonoScriptNames[monoScriptAsset.PathId] = baseField["m_Name"].AsString;
            }
        }

        public AssetEntryInfo ReadEntryInfoFromAsset(ParsedAsset parsedAsset)
        {
            AssetTypeValueField baseField = _assetsManager.GetBaseField(parsedAsset.fileInst, parsedAsset.fileInfo);
            string name = "";
            if (parsedAsset.classID == AssetClassID.MonoBehaviour)
            {
                name = _currMonoScriptNames[baseField["m_Script"]["m_PathID"].AsLong];
            }
            else
            {
                var _name = baseField.Get("m_Name");
                if (_name != null && !_name.IsDummy)
                {
                    name = _name.AsString;
                }
            }
            if (name == "")
                name = "Unnamed asset";
            return new()
            {
                pathID = parsedAsset.fileInfo.PathId,
                name = name,
                classID = parsedAsset.classID
            };
        }

        public AssetExtraInfo ReadExtraInfoFromAsset(ParsedAsset parsedAsset)
        {
            AssetsFileInstance fileInst = parsedAsset.fileInst;
            AssetFileInfo fileInfo = parsedAsset.fileInfo;
            AssetClassID classID = parsedAsset.classID;

            int StableStringHash(string input)
            {
                using var sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToInt32(hash, 0); // deterministic but may collide
            }

            uint GetAssetDataSize()
            {
                if (classID == AssetClassID.Texture2D)
                {
                    try
                    {
                        AssetTypeValueField baseField = _assetsManager.GetBaseField(fileInst, fileInfo);
                        if (baseField == null) return (uint)fileInfo.ByteSize;

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
                            return (uint)streamSize;
                        }

                        // Inline image data
                        byte[] imageBytes = baseField["image data"].AsByteArray;
                        return (uint)(imageBytes?.Length ?? 0);
                    }
                    catch
                    {
                        return (uint)fileInfo.ByteSize;
                    }
                }
                else
                {
                    // Fallback: use serialized byte size
                    return (uint)fileInfo.ByteSize;
                }
            }

            return new()
            {
                path = fileInst.path,
                fileID = fileInst.parentBundle != null ? StableStringHash(fileInst.parentBundle.name) : 0,
                uncompressedSize = GetAssetDataSize(),
                compressedSize = fileInfo.ByteSize
            };
        }
    }
}