using System.IO;
using UnityEngine;
using TMPro;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.UI
{
    public class MemoField : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        [SerializeField]
        private TMP_InputField _field;

        [SerializeField]
        private TextMeshProUGUI _fieldDescription;

        private string _memoCacheShortPath = "";

        private AppEnvironment _appEnvironment;
        public AppEnvironment AppEnvironment => _appEnvironment;

        private MemoReader _memoReader;

        [SerializeField]
        private Color _descriptionEnableColor;

        [SerializeField]
        private Color _descriptionDisableColor;

        private string _storedBundlePath = "";
        private string _storedAssetName = "";
        private AssetClassID _storedClassID = AssetClassID.@void;

        private void Start()
        {
            DisableField();
        }

        public void OnEvent(AppEvent e)
        {
            if (e is MemoCacheEvent mce)
            {
                _memoCacheShortPath = mce.SelectedShortPath;
                Debug.Log($"Set memo cache short path to [{_memoCacheShortPath}]");

                if (_memoCacheShortPath != "")
                {
                    if (_storedBundlePath != "" && _storedAssetName != "")
                    {
                        GetMemoAndSet(_storedBundlePath, _storedAssetName, _storedClassID);
                        Debug.Log($"Set memo with {_storedBundlePath} ---- {_storedAssetName}");
                    }
                }
                else
                {
                    DisableField();
                    SetTextToEmpty();
                }
            }
            else if (e is BundleReadEvent)
            {
                _storedBundlePath = "";
                _storedAssetName = "";
            }
        }

        public void ManualEnterMemo(string text)
        {

        }

        // Read from cache
        public void GetMemoAndSet(string bundlePath, string name, AssetClassID classID)
        {
            _storedBundlePath = bundlePath;
            _storedAssetName = name;
            _storedClassID = classID;

            if (!ShouldUseMemoField(classID))
            {
                DisableField();
                return;
            }
            else
            {
                EnableField();
            }
            
            if (!_field.interactable)
                return;

            string memo = GetRecordedMemo(Path.GetDirectoryName(bundlePath), name);
            _field.text = memo;
        }

        public void SetTextToEmpty()
        {
            _field.text = "";
        }

        private string GetRecordedMemo(string bundlePath, string name)
        {
            string cachePath = GetMemoCacheCompletePath();
            return _memoReader.ReadAssetMemo(cachePath, bundlePath, name);
        }

        private string GetMemoCacheCompletePath()
        {
            return Path.Combine(PredefinedPaths.ExternalCache, _memoCacheShortPath);
        }

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _memoReader = new(_appEnvironment);
        }

        public void EnableField()
        {
            _field.interactable = true;
            _fieldDescription.color = _descriptionEnableColor;
        }

        public void DisableField()
        {
            _field.interactable = false;
            _fieldDescription.color = _descriptionDisableColor;
        }
        
        private bool ShouldUseMemoField(AssetClassID classID)
        {
            return _memoCacheShortPath != "" &&
                    (classID == AssetClassID.Texture2D ||
                    classID == AssetClassID.Sprite);
        }
    }
}