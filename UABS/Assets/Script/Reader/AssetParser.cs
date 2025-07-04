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

        public (List<ParsedAsset>, AssetsFileInstance) ReadAssetOnly(BundleFileInstance bunInst)
        {
            // List<ParsedAsset> parsedAssets = new();
            // AssetsFileInstance fileInst = _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);

            // foreach (AssetClassID classId in Enum.GetValues(typeof(AssetClassID)))
            // {
            //     List<AssetFileInfo> assets = fileInst.file.GetAssetsOfType(classId);
            //     if (assets == null || assets.Count == 0)
            //         continue;

            //     foreach (AssetFileInfo assetInfo in assets)
            //     {
            //         parsedAssets.Add(new ParsedAsset
            //         {
            //             fileInfo = assetInfo,
            //             classID = classId,
            //             fileInst = fileInst
            //         });
            //     }
            // }

            // return (parsedAssets, fileInst);

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

            List<ParsedAsset> parsedAssets = new();
            AssetsFileInstance fileInst = _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);

            List<AssetFileInfo> allAssets = fileInst.file.AssetInfos;

            foreach (var assetInfo in allAssets)
            {
                parsedAssets.Add(new ParsedAsset
                {
                    fileInfo = assetInfo,
                    classID = (AssetClassID)assetInfo.TypeId,
                    fileInst = fileInst
                });
            }

            return (parsedAssets, fileInst);
        }
    }
}