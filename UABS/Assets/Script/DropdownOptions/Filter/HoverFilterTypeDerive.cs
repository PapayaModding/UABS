using System.Collections.Generic;
using System.Linq;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.DropdownOptions.Filter;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.LocalController;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UABS.Assets.Script.Controller
{
    public class HoverFilterTypeDerive : HoverDropdown, IAppEnvironment, IAppEventListener
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        [SerializeField]
        private GameObject _entryPrefab;

        [SerializeField]
        private RectTransform _content;

        [SerializeField]
        private Color _hoverColor;

        [SerializeField]
        private Color _normalColor;

        [SerializeField]
        private Image _bgImage;

        private List<AssetClassID> _currClassIDs = new();

        private Dictionary<AssetClassID, bool> _isClassIDFiltered = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is GoBundleViewEvent gbve)
            {
                // For creating the entries
                List<ParsedAssetAndEntry> entryInfos = gbve.EntryInfos;
                _currClassIDs = GetAssetClassIDsFrom(entryInfos);
                _isClassIDFiltered.Clear();
                ClearAndRecreate();
            }
            else if (e is ClickFilterTypeEvent cfte)
            {
                _isClassIDFiltered[cfte.ClassID] = cfte.IsFiltered;
                // Send the dictionary to somewhere else
                _appEnvironment.Dispatcher.Dispatch(new FilterTypeEvent(_isClassIDFiltered));
            }
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

            foreach (AssetClassID classID in _currClassIDs)
            {
                bool isFiltered = _isClassIDFiltered.ContainsKey(classID) && _isClassIDFiltered[classID];
                GameObject entry = CreateScrollEntry(classID, isFiltered);
                entry.GetComponent<RectTransform>().SetParent(_content.transform, worldPositionStays: false);
            }
        }

        private GameObject CreateScrollEntry(AssetClassID classID, bool isFiltered)
        {
            GameObject entry = Instantiate(_entryPrefab);
            IFilterTypeScrollEntry menuScrollEntry = entry.GetComponentsInChildren<MonoBehaviour>(true)
                                                .OfType<IFilterTypeScrollEntry>()
                                                .FirstOrDefault();
            menuScrollEntry.ClassID = classID;
            menuScrollEntry.IsFiltered = isFiltered;
            menuScrollEntry.AssignDispatcher(_appEnvironment.Dispatcher);

            return entry;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            _bgImage.color = _normalColor;
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

        private List<AssetClassID> GetAssetClassIDsFrom(List<ParsedAssetAndEntry> entryInfos)
        {
            List<AssetClassID> result = new();
            foreach (ParsedAssetAndEntry entryInfo in entryInfos)
            {
                AssetClassID entryClassID = entryInfo.assetEntryInfo.classID;
                if (!result.Contains(entryClassID))
                {
                    result.Add(entryClassID);
                }
            }

            return result;
        }
    }
}