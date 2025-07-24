using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.UI.OptionPanel
{
    public class SearchPickImageButton : MonoBehaviour, IAppEnvironment
    {
        [SerializeField]
        private TMP_InputField _imagePathField;

        [SerializeField]
        private RawImage _image;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            string[] selections = _appEnvironment.Wrapper.FileBrowser.OpenFilePanel("Search sprite / texture2D image.", "", new[] { "png", "jpeg" }, false);
            if (selections.Length <= 0)
            {
                Debug.Log("Couldn't find path to File.");
            }
            else
            {
                _imagePathField.text = selections[0];
                LoadImage(selections[0]);
            }
        }

        private void LoadImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.LogError("Invalid image path: " + path);
                return;
            }

            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new(2, 2);  // Size doesn't matter, LoadImage will replace it

            if (texture.LoadImage(imageData))
            {
                _image.texture = texture;
                // _image.SetNativeSize();
            }
            else
            {
                Debug.LogError("Failed to load image: " + path);
            }
        }
    }
}