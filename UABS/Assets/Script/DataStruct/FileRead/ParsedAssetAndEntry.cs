namespace UABS.Assets.Script.DataStruct
{
    /// <summary>
    /// * Used for asset operations
    /// </summary>
    public class ParsedAssetAndEntry
    {
        public ParsedAsset parsedAsset;
        public AssetEntryInfo assetEntryInfo;
        public string originalPath;  // In case bundle was cached, need to reference its true location
        public bool isHighlighted;
    }
}