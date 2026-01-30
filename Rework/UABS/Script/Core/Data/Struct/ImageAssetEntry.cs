using UABS.Wrapper;

namespace UABS.Data
{
    public sealed class ImageAssetEntry : AssetEntry
    {
        public IImageResource? Image { get; }
        public ImageRect Rect;
    }
}