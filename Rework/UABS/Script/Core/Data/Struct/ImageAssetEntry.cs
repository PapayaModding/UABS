using UABS.Wrapper;

namespace UABS.Data
{
    public class ImageAssetEntry : AssetEntry
    {
        public IImageResource? Image { get; }
        public ImageRect Rect;
    }
}