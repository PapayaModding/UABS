using System.IO;
using UnityEngine;
using AssetsTools.NET.Extra;
using TMPro;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.UI;

namespace UABS.Assets.Script.EventListener
{
    public class DisplayInfoPanel : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        [SerializeField]
        private TMP_InputField _nameField;

        [SerializeField]
        private TMP_InputField _pathIDField;

        [SerializeField]
        private TMP_InputField _fileIDField;

        [SerializeField]
        private TMP_InputField _sizeField;

        [SerializeField]
        private TMP_InputField _pathField;

        [SerializeField]
        private MemoField _memoField;

        private AppEnvironment _appEnvironment;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is AssetDisplayInfoEvent adie)
            {
                if (adie.EntryInfo == null)
                    return;

                _nameField.text = adie.EntryInfo.assetEntryInfo.name;
                _pathIDField.text = adie.EntryInfo.assetEntryInfo.pathID.ToString();
                // _fileIDField.text = assetTextInfoEvent.Info.fileID.ToString();
                // _sizeField.text = $"{assetTextInfoEvent.Info.compressedSize} ({assetTextInfoEvent.Info.uncompressedSize})";
                // _pathField.text = assetTextInfoEvent.Info.path;
                // ! Lazy loading extra information about the asset.
                AssetExtraInfo extraInfo = _appEnvironment.AssetReader.ReadExtraInfoFromAsset(adie.EntryInfo.parsedAsset);
                _fileIDField.text = extraInfo.fileID.ToString();
                _sizeField.text = $"{extraInfo.compressedSize} ({extraInfo.uncompressedSize})";
                _pathField.text = !string.IsNullOrWhiteSpace(adie.EntryInfo.realBundlePath) ? adie.EntryInfo.realBundlePath : extraInfo.path;
                _memoField.GetMemoAndSet(_pathField.text, _nameField.text, adie.EntryInfo.assetEntryInfo.classID);
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
                    _memoField.SetTextToEmpty();
                }
            }
            else if (e is BundleReadEvent)
            {
                _nameField.text = "";
                _pathIDField.text = "";
                _fileIDField.text = "";
                _sizeField.text = "";
                _pathField.text = "";
                _memoField.SetTextToEmpty();
            }
            else if (e is FolderRead4DeriveEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _nameField.text = "";
                    _pathIDField.text = "";
                    _fileIDField.text = "";
                    _sizeField.text = "";
                    _pathField.text = "";
                    _memoField.SetTextToEmpty();
                }
            }
            else if (e is BundleRead4DeriveEvent)
            {
                _nameField.text = "";
                _pathIDField.text = "";
                _fileIDField.text = "";
                _sizeField.text = "";
                _pathField.text = "";
                _memoField.SetTextToEmpty();
            }
        }
    }
}