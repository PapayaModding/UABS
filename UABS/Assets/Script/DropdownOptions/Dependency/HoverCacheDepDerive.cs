using System.Collections.Generic;
using System.IO;
using System.Linq;
using UABS.Assets.Script.DropdownOptions.Dependency;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UABS.Assets.Script.EventListener;

namespace UABS.Assets.Script.DropdownOptions
{
    public class HoverCacheDepDerive : HoverArea, IAppEnvironment, IAppEventListener
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        [SerializeField]
        private GameObject _entryPrefab;

        [SerializeField]
        private RectTransform _content;

        private ReadExternalCache _readExternalCache;

        [SerializeField]
        private Color _hoverColor;

        [SerializeField]
        private Color _normalColor;

        [SerializeField]
        private Image _bgImage;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _readExternalCache = new();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            _bgImage.color = _hoverColor;
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
            _bgImage.color = _normalColor;
        }

        private GameObject CreateScrollEntry(string path, bool interactable)
        {
            GameObject entry = Instantiate(_entryPrefab);
            IDepCacheScrollEntry menuScrollEntry = entry.GetComponentsInChildren<MonoBehaviour>(true)
                                                .OfType<IDepCacheScrollEntry>()
                                                .FirstOrDefault();
            menuScrollEntry.ShortPath = path;
            menuScrollEntry.AssignDispatcher(AppEnvironment.Dispatcher);
            menuScrollEntry.ManagedButton.interactable = interactable;
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
            if (e is CacheRefreshEvent)
            {
                ClearAndRecreate();
            }
        }
    }
}