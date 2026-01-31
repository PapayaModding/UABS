using System.Collections.Generic;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UABS.Data
{
    public static class BundleReader
    {
        public static (List<AssetsFileInstance>?, List<AssetEntry>?) ReadFromPath(string path, AssetsManager assetsManager)
        {
            FileInstanceLike fileInst = NextInstance.LoadAnyFile(assetsManager, path);
            if (fileInst.IsAssetsFileInstance)
            {
                AssetsFileInstance assetsInst = fileInst.AsAssetsFileInstance!;
                return (new(){assetsInst}, GetAssets(path, assetsManager, assetsInst));
            }
            else if (fileInst.IsBundleFileInstance)
            {
                BundleFileInstance bunInst = fileInst.AsBundleFileInstace!;
                List<AssetsFileInstance> assetsInsts = bunInst.loadedAssetsFiles;
                List<AssetEntry> result = new();
                foreach (AssetsFileInstance assetsInst in assetsInsts)
                {
                    result.AddRange(GetAssets(path, assetsManager, assetsInst));
                }
                return (assetsInsts, result);
            }
            return (null, null);
        }

        private static List<AssetEntry> GetAssets(string path, AssetsManager assetsManager, AssetsFileInstance assetsInst)
        {
            List<AssetEntry> result = new();
            NextInstance nextInstance = new(assetsManager, assetsInst);
            IList<AssetFileInfo> assetFileInfos = assetsInst.file.AssetInfos;
            foreach (var assetFileInfo in assetFileInfos)
            {
                (string assetName, AssetClassID classID) = nextInstance.GetDisplayNameFast(assetFileInfo);
                AssetEntry assetEntry = new()
                {
                    Name = assetName,
                    ClassID = new(classID),
                    PathID = assetFileInfo.PathId,
                    AssetFileInfo = assetFileInfo,
                    AssetsInst = assetsInst
                };
                result.Add(assetEntry);
            }

            return result;
        }
    }
}