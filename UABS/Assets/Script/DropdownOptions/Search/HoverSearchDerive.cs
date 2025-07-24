using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.UI.OptionPanel;

namespace UABS.Assets.Script.DropdownOptions.Search
{
    public class HoverSearchDerive : HoverArea, IAppEnvironment, IAppEventListener
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        [SerializeField]
        private GameObject _entryPrefab;

        [SerializeField]
        private RectTransform _content;

        private ReadUserPackage _readUserPackage;

        [SerializeField]
        private Button _button;

        private HashSet<string> _includedPaths = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _readUserPackage = new();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            _button.targetGraphic.color = _button.colors.selectedColor;
            ClearAndRecreate();
        }

        private void ClearAndRecreate()
        {
            ClearContentChildren();
            // Search paths and create prefabs
            List<string> paths = _readUserPackage.GetPackagesInExternal();
            foreach (string path in paths)
            {
                string validationFilePath = Path.Combine(path, "Validation.txt");
                validationFilePath = validationFilePath.Replace("/", Path.DirectorySeparatorChar.ToString());
                GameObject entry = CreateScrollEntry(Path.GetFileName(path), File.Exists(validationFilePath));
                entry.GetComponent<RectTransform>().SetParent(_content.transform, worldPositionStays: false);
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            _button.targetGraphic.color = _button.colors.normalColor;
        }

        private GameObject CreateScrollEntry(string path, bool interactable)
        {
            GameObject entry = Instantiate(_entryPrefab);
            ISearchScrollEntry menuScrollEntry = entry.GetComponentsInChildren<MonoBehaviour>(true)
                                                .OfType<ISearchScrollEntry>()
                                                .FirstOrDefault();
            menuScrollEntry.ShortPath = path;
            menuScrollEntry.AssignDispatcher(AppEnvironment.Dispatcher);
            menuScrollEntry.ManagedButton.interactable = interactable;
            menuScrollEntry.IsIncluded = _includedPaths.Contains(menuScrollEntry.ShortPath);
            return entry;
        }

        private void ClearContentChildren()
        {
            Transform parentTransform = _content.transform;

            for (int i = parentTransform.childCount - 1; i >= 0; i--)
            {
                GameObject child = parentTransform.GetChild(i).gameObject;
                Destroy(child);
                child = null;
            }
        }

        public void OnEvent(AppEvent e)
        {
            if (e is PackageRefreshEvent)
            {
                ClearAndRecreate();
            }
            else if (e is ClickSearchEvent csce)
            {
                if (csce.IsIncluded)
                {
                    _includedPaths.Add(csce.ShortPath);
                }
                else
                {
                    if (_includedPaths.Contains(csce.ShortPath))
                    {
                        _includedPaths.Remove(csce.ShortPath);
                    }
                }
                // Send the hashset to somewhere else
                _appEnvironment.Dispatcher.Dispatch(new SearchBundleEvent(_includedPaths));
            }
        }
    }
}