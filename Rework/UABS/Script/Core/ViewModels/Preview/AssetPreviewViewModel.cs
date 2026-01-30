using UABS.Data;
using UABS.Misc;

namespace UABS.ViewModel
{
    public abstract class AssetPreviewViewModel : ObservableObject
    {
        public AssetPreviewType PreviewType { get; }

        protected AssetPreviewViewModel(AssetPreviewType type)
        {
            PreviewType = type;
        }
    }
}