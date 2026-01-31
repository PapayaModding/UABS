namespace UABS.Data
{
    public sealed class AssetPreviewInfo
    {
        public AssetPreviewType Type { get; }
        public string AssetPath { get; }
        public long Size { get; }

        public AssetPreviewInfo(
            AssetPreviewType type,
            string assetPath,
            long size)
        {
            Type = type;
            AssetPath = assetPath;
            Size = size;
        }
    }
}