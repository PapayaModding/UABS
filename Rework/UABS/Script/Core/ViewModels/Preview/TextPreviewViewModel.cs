using UABS.Data;

namespace UABS.ViewModel
{
    public sealed class TextPreviewViewModel : AssetPreviewViewModel
    {
        public string TextContent { get; }

        public TextPreviewViewModel(string textContent)
            : base(AssetPreviewType.Text)
        {
            TextContent = textContent;
        }
    }
}