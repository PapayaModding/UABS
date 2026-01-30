using UABS.Data;

namespace UABS.ViewModel
{
    public sealed class AudioPreviewViewModel : AssetPreviewViewModel
    {
        public string AudioPath { get; }

        public AudioPreviewViewModel(string audioPath)
            : base(AssetPreviewType.Audio)
        {
            AudioPath = audioPath;
        }
    }
}