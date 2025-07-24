using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.Dispatcher;

namespace UABS.Assets.Script.View.BundleView
{
    public class TextureView : MonoBehaviour
    {
        [SerializeField]
        private RawImage _rawImage;
        [SerializeField]
        private TextMeshProUGUI _indexText;
        [SerializeField]
        private TextMeshProUGUI _sizeText;
        public EventDispatcher dispatcher;

        public void Render(Texture2D texture)
        {
            _rawImage.texture = texture;
            if (texture != null)
                texture.filterMode = FilterMode.Point;
        }

        public void AssignIndexText(string text)
        {
            _indexText.text = text;
        }

        public void AssignSizeText(string text)
        {
            _sizeText.text = text;
        }
    }
}