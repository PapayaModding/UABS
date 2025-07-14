using System.Collections.Generic;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Reader
{
    /// <summary>
    /// * Obtain assets of different kinds from a bundle
    /// </summary>
    public class AssetParser
    {
        private AssetsManager _assetsManager;

        public AssetParser(AssetsManager am)
        {
            _assetsManager = am;
        }

        public (List<ParsedAsset>, AssetsFileInstance) ReadAssetOnly(AssetsFileInstance assetsInst)
        {
            List<ParsedAsset> parsedAssets = new();
            List<AssetFileInfo> allAssets = assetsInst.file.AssetInfos;

            foreach (var assetInfo in allAssets)
            {
                parsedAssets.Add(new ParsedAsset
                {
                    fileInfo = assetInfo,
                    classID = (AssetClassID)assetInfo.TypeId,
                    fileInst = assetsInst
                });
            }

            return (parsedAssets, assetsInst);
        }
    }
}