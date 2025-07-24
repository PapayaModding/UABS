using System.Collections;
using System.IO;
using UnityEngine;
using AssetsTools.NET.Extra;
using TMPro;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.Writer;

namespace UABS.Assets.Script.UI
{
    public class MemoField : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        [SerializeField]
        private TMP_InputField _field;

        [SerializeField]
        private TextMeshProUGUI _fieldDescription;

        private string _memoPackageShortPath = "";

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

        // For writer
        private MemoWriter _memoWriter;
        private Coroutine _debounceCoroutine;
        [SerializeField]
        private float _debounceDelay = 0.4f;

        private void Start()
        {
            DisableField();
            _field.onValueChanged.AddListener(input => ManualEnterMemo(input));
        }

        public void OnEvent(AppEvent e)
        {
            if (e is MemoPackageEvent mce)
            {
                _memoPackageShortPath = mce.SelectedShortPath;
                Debug.Log($"Set memo package short path to [{_memoPackageShortPath}]");

                if (_memoPackageShortPath != "")
                {
                    if (_storedBundlePath != "" && _storedAssetName != "")
                    {
                        GetMemoAndSet(_storedBundlePath, _storedAssetName, _storedClassID);
                        // Debug.Log($"Set memo with {_storedBundlePath} ---- {_storedAssetName}");
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

        private void ManualEnterMemo(string text)
        {
            Debounce(() =>
            {
                string memoPackageShortPath = _memoPackageShortPath;
                string storedBundlePath = _storedBundlePath;
                string storedAssetName = _storedAssetName;
                HandleDebouncedInput(memoPackageShortPath, storedBundlePath, storedAssetName, text);
            });
        }

        private void Debounce(System.Action action)
        {
            if (_debounceCoroutine != null)
            {
                StopCoroutine(_debounceCoroutine);
            }
            _debounceCoroutine = StartCoroutine(DebounceRoutine(action));
        }

        private IEnumerator DebounceRoutine(System.Action action)
        {
            yield return new WaitForSeconds(_debounceDelay);
            action?.Invoke();
        }

        private void HandleDebouncedInput(string memoPackageShortPath,
                                            string storedBundlePath,
                                            string storedAssetName,
                                            string text)
        {
            if (!_field.isFocused)
                return;
            
            _memoWriter.WriteMemo(Path.Combine(PredefinedPaths.ExternalUserPackages, memoPackageShortPath),
                                    storedBundlePath,
                                    storedAssetName,
                                    text);
        }

        // Read from package
        public void GetMemoAndSet(string bundlePath, string name, AssetClassID classID)
        {
            _storedBundlePath = bundlePath;
            _storedAssetName = name;
            _storedClassID = classID;

            if (!ShouldUseMemoField(classID))
            {
                DisableField();
                SetTextToEmpty();
                return;
            }
            else
            {
                EnableField();
            }

            if (!_field.interactable)
                return;

            string memo = GetRecordedMemo(bundlePath, name);
            _field.text = memo;
        }

        public void SetTextToEmpty()
        {
            _field.text = "";
        }

        private string GetRecordedMemo(string bundlePath, string name)
        {
            string packagePath = GetMemoPackageCompletePath();
            string result = _memoReader.ReadAssetMemo(packagePath, bundlePath, name);
            return !string.IsNullOrEmpty(result) ? result : "";
        }

        private string GetMemoPackageCompletePath()
        {
            return Path.Combine(PredefinedPaths.ExternalUserPackages, _memoPackageShortPath);
        }

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _memoReader = new(_appEnvironment);
            _memoWriter = new(_appEnvironment);
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
        
        // ! Memo only work for specific assets
        private bool ShouldUseMemoField(AssetClassID classID)
        {
            return _memoPackageShortPath != "" &&
                    (classID == AssetClassID.Texture2D ||
                    classID == AssetClassID.Sprite);
        }
    }
}