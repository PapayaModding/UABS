using System.Collections.Generic;
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

            }
            return (null, null);
        }

        
    }
}