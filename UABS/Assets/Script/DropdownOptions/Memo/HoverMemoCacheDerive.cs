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
using UABS.Assets.Script.UI;

namespace UABS.Assets.Script.DropdownOptions.Memo
{
    public class HoverMemoCacheDerive : HoverArea, IAppEnvironment, IAppEventListener
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        [SerializeField]
        private GameObject _entryPrefab;

        [SerializeField]
        private RectTransform _content;

        private ReadExternalCache _readExternalCache;

        [SerializeField]
        private Button _button;

        private List<IMemoCacheScrollEntry> _memoCacheScrollEntries = new();

        private string _currSelectedShortPath = "";

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _readExternalCache = new();
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
            List<string> paths = _readExternalCache.GetCacheFoldersInExternal();
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
            IMemoCacheScrollEntry menuScrollEntry = entry.GetComponentsInChildren<MonoBehaviour>(true)
                                                .OfType<IMemoCacheScrollEntry>()
                                                .FirstOrDefault();
            menuScrollEntry.ShortPath = path;
            menuScrollEntry.AssignDispatcher(AppEnvironment.Dispatcher);
            menuScrollEntry.ManagedButton.interactable = interactable;
            menuScrollEntry.IsSelected = menuScrollEntry.ShortPath == _currSelectedShortPath;
            _memoCacheScrollEntries.Add(menuScrollEntry);

            return entry;
        }

        private void ClearContentChildren()
        {
            Transform parentTransform = _content.transform;

            _memoCacheScrollEntries.Clear();

            for (int i = parentTransform.childCount - 1; i >= 0; i--)
            {
                GameObject child = parentTransform.GetChild(i).gameObject;
                Destroy(child);
                child = null;
            }
        }

        public void OnEvent(AppEvent e)
        {
            if (e is CacheRefreshEvent)
            {
                ClearAndRecreate();
            }
            else if (e is ClickMemoCacheEvent cmce)
            {
                if (cmce.IsSelected)
                {
                    _currSelectedShortPath = cmce.ShortPath;
                }
                else
                {
                    if (_currSelectedShortPath == cmce.ShortPath)
                    {
                        _currSelectedShortPath = "";  // Deselect
                    }
                }
                foreach (IMemoCacheScrollEntry memoCacheScrollEntry in _memoCacheScrollEntries)
                {
                    if (memoCacheScrollEntry.ShortPath != _currSelectedShortPath)
                        memoCacheScrollEntry.IsSelected = false;
                }
                _appEnvironment.Dispatcher.Dispatch(new MemoCacheEvent(_currSelectedShortPath));
            }
        }
    }
}