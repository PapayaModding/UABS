using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public (List<ParsedAsset>, AssetsFileInstance) ReadAssetOnly(BundleFileInstance bunInst)
        {
            List<ParsedAsset> parsedAssets = new();
            AssetsFileInstance fileInst = _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
            foreach (AssetClassID classId in Enum.GetValues(typeof(AssetClassID)))
            {
                List<AssetFileInfo> assets = fileInst.file.GetAssetsOfType(classId);
                parsedAssets.AddRange(assets.Select(x => new ParsedAsset()
                {
                    fileInfo = x,
                    classID = classId,
                    fileInst = fileInst
                }).ToList());
            }
            // _assetsManager.UnloadBundleFile(bunInst);
            return (parsedAssets, fileInst);
        }
    }
}