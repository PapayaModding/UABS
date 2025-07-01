namespace UABS.Assets.Script.DataStruct
{
    /// <summary>
    /// * Used for asset operations
    /// </summary>
    public struct ParsedAssetAndEntry
    {
        public ParsedAsset parsedAsset;
        public AssetEntryInfo assetEntryInfo;
        public string realBundlePath;  // In case bundle was cached, need to reference its true location
    }
}