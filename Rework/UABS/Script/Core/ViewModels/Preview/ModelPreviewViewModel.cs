using UABS.Data;

namespace UABS.ViewModel
{
    public class ModelPreviewViewModel : AssetPreviewViewModel
    {
        public string ModelPath { get; }

        public ModelPreviewViewModel(string modelPath)
            : base(AssetPreviewType.Model3D)
        {
            ModelPath = modelPath;
        }
    }
}