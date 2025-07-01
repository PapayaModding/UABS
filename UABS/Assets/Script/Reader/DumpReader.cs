using AssetsTools.NET;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Reader
{
    public class DumpReader
    {
        private AssetsManager AssetsManager { get; }

        public DumpReader(AssetsManager am)
        {
            AssetsManager = am;
        }

        public List<DumpInfo> ReadSpriteAtlasDumps(AssetsFileInstance fileInst)
        {
            return ReadDumps(fileInst, AssetClassID.SpriteAtlas);
        }

        public List<DumpInfo> ReadSpriteDumps(AssetsFileInstance fileInst)
        {
            return ReadDumps(fileInst, AssetClassID.Sprite);
        }

        public List<DumpInfo> ReadSpriteAtlasDumps(BundleFileInstance bunInst)
        {
            return ReadDumps(bunInst, AssetClassID.SpriteAtlas);
        }

        public List<DumpInfo> ReadSpriteDumps(BundleFileInstance bunInst)
        {
            return ReadDumps(bunInst, AssetClassID.Sprite);
        }

        public List<DumpInfo> ReadTexture2DDumps(BundleFileInstance bunInst)
        {
            return ReadDumps(bunInst, AssetClassID.Texture2D);
        }

        private List<DumpInfo> ReadDumps(BundleFileInstance bunInst, AssetClassID assetType)
        {
            return ReadDumps(AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false), assetType);
        }
        
        private List<DumpInfo> ReadDumps(AssetsFileInstance fileInst, AssetClassID assetType)
        {
            List<DumpInfo> result = new();

            List<AssetFileInfo> assetInfos = fileInst.file.GetAssetsOfType(assetType);
            if (assetInfos.Count == 0)
                return result;

            foreach (var assetInfo in assetInfos)
            {
                AssetTypeValueField assetBase = AssetsManager.GetBaseField(fileInst, assetInfo);
                result.Add(new()
                {
                    dumpJson = (JObject)JsonDumper.RecurseJsonDump(assetBase, true),
                    pathID=assetInfo.PathId
                });
            }

            return result;
        }
    }
}