using UnityEngine;
using UABS.Assets.Script.Event;
using TMPro;
using System.IO;

namespace UABS.Assets.Script.EventListener
{
    public class DisplayInfoPanel : MonoBehaviour, IAppEventListener
    {
        [SerializeField]
        private TMP_InputField  _nameField;

        [SerializeField]
        private TMP_InputField  _pathIDField;

        [SerializeField]
        private TMP_InputField  _fileIDField;

        [SerializeField]
        private TMP_InputField  _sizeField;

        [SerializeField]
        private TMP_InputField  _pathField;

        public void OnEvent(AppEvent e)
        {
            if (e is AssetTextInfoEvent assetTextInfoEvent)
            {
                _nameField.text = assetTextInfoEvent.Info.name;
                _pathIDField.text = assetTextInfoEvent.Info.pathID.ToString();
                _fileIDField.text = assetTextInfoEvent.Info.fileID.ToString();
                _sizeField.text = $"{assetTextInfoEvent.Info.compressedSize} ({assetTextInfoEvent.Info.uncompressedSize})";
                _pathField.text = assetTextInfoEvent.Info.path;
            }
            else if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    _nameField.text = "";
                    _pathIDField.text = "";
                    _fileIDField.text = "";
                    _sizeField.text = "";
                    _pathField.text = "";
                }
            }
            else if (e is BundleReadEvent)
            {
                _nameField.text = "";
                _pathIDField.text = "";
                _fileIDField.text = "";
                _sizeField.text = "";
                _pathField.text = "";
            }
            else if (e is FolderReadEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _nameField.text = "";
                    _pathIDField.text = "";
                    _fileIDField.text = "";
                    _sizeField.text = "";
                    _pathField.text = "";
                }
            }
            else if (e is BundleRead4DependencyEvent br4d)
            {
                _nameField.text = "";
                _pathIDField.text = "";
                _fileIDField.text = "";
                _sizeField.text = "";
                _pathField.text = "";
            }
        }
    }
}