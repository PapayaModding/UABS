using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.DataStruct._New;
using UABS.Assets.Script.UnityFile;

namespace UABS.Assets.Script.Reader.BundlesRead
{
    public class AssetsReader
    {
        private readonly AssetsManager _assetsManager;

        public AssetsReader(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public (List<AssetsFileInstance>, List<ParsedAssetAndEntry>) ReadAssets(string filePath)
        {
            FileInstanceLike fileInst = NextInstance.LoadAnyFile(_assetsManager, filePath);
            if (fileInst.IsAssetsFileInstance)
            {
                AssetsFileInstance assetsInst = fileInst.AsAssetsFileInstance;
                return (new() { assetsInst }, GetAssets(assetsInst, filePath));
            }
            else if (fileInst.IsBundleFileInstance)
            {
                BundleFileInstance bunInst = fileInst.AsBundleFileInstace;
                List<AssetsFileInstance> assetsInsts = bunInst.loadedAssetsFiles;
                List<ParsedAssetAndEntry> result = new();
                foreach (AssetsFileInstance assetsInst in assetsInsts)
                {
                    result.AddRange(GetAssets(assetsInst, filePath));
                }
                return (assetsInsts, result);
            }
            else
            {
                return (null, null);
            }
        }

        private List<ParsedAssetAndEntry> GetAssets(AssetsFileInstance assetsInst, string filePath)
        {
            List<ParsedAssetAndEntry> result = new();
            NextInstance nextInstance = new(_assetsManager, assetsInst);
            var assetFileInfos = assetsInst.file.AssetInfos;
            foreach (var assetFileInfo in assetFileInfos)
            {
                (string assetName, AssetClassID typeName) = nextInstance.GetDisplayNameFast(assetFileInfo);
                ParsedAsset parsedAsset = new()
                {
                    fileInfo = assetFileInfo,
                    classID = typeName,
                    fileInst = assetsInst
                };
                AssetEntryInfo assetEntryInfo = new()
                {
                    name = assetName,
                    classID = typeName,
                    pathID = assetFileInfo.PathId
                };

                result.Add(new()
                {
                    parsedAsset = parsedAsset,
                    assetEntryInfo = assetEntryInfo,
                    originalPath = filePath,
                    isHighlighted = false
                });
            }
            
            return result;
        }

        public AssetsFileInstance ReadAssetsFileInstFromBundle(BundleFileInstance bunInst)
        {
            AssetBundleFile bundle = bunInst.file;
            if (bunInst.file.DataIsCompressed)
            {
                MemoryStream bundleStream = new();
                bundle.Unpack(new AssetsFileWriter(bundleStream));
                bundleStream.Position = 0;

                AssetBundleFile newBundle = new();
                newBundle.Read(new AssetsFileReader(bundleStream));

                bundle.Close();
                bunInst.file = newBundle;
            }

            return _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
        }

        public AssetsFileInstance ReadValidAssetsFileInst(string filePath, bool loadDep)
        {
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogWarning($"File does not exist: {filePath}");
                return null;
            }

            // Optional: check if the file has a valid Unity file signature
            try
            {
                using (var fs = File.OpenRead(filePath))
                {
                    byte[] headerBytes = new byte[20];
                    fs.Read(headerBytes, 0, headerBytes.Length);
                    string signature = Encoding.ASCII.GetString(headerBytes).TrimEnd('\0');

                    if (!(signature.StartsWith("UnityFS") || signature.StartsWith("UnityRaw") || signature.StartsWith("UnityWeb") || signature.StartsWith("SerializedFile")))
                    {
                        UnityEngine.Debug.LogWarning($"Unknown Unity file signature: {signature}");
                        // You can choose to return null here or still try to load
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to read file header: {ex.Message}");
                return null;
            }

            // Try loading the file with AssetsManager
            try
            {
                AssetsFileInstance fileInstance = _assetsManager.LoadAssetsFile(filePath, loadDep);
                if (fileInstance != null && fileInstance.file != null && fileInstance.file.Header != null)
                {
                    return fileInstance;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Assets file loaded but appears invalid.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to load assets file: {ex.Message}");
                return null;
            }
        }
    }
}