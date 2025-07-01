using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.DataStruct
{
    public struct ParsedAsset
    {
        public AssetFileInfo fileInfo;
        public AssetClassID assetType;
        public AssetsFileInstance fileInst;
    }
}