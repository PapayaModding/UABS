using UABS.Data;

namespace UABS.ViewModel
{
    public sealed class UnknownPreviewViewModel : AssetPreviewViewModel
    {
        public string Reason { get; }

        public UnknownPreviewViewModel(string reason)
            : base(AssetPreviewType.None)
        {
            Reason = reason;
        }
    }
}