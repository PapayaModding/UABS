using UABS.Data;

namespace UABS.ViewModel
{
    public sealed class ImagePreviewViewModel : AssetPreviewViewModel
    {
        public string ImagePath { get; }

        public ImagePreviewViewModel(string imagePath)
            : base(AssetPreviewType.Image2D)
        {
            ImagePath = imagePath;
        }
    }
}