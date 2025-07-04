using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.View;

namespace UABS.Assets.Script.Controller
{
    public class LoadEntries2ScrollView : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        [SerializeField]
        private RectTransform _content;

        [SerializeField]
        private Scrollbar _scrollbarRef;

        [SerializeField]
        private GameObject _entryPrefab;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        [SerializeField]
        private int _maxNumOfEntryPerPage = 15;
        private List<EntryInfoView> _entryPool = new();
        // private List<AssetTextInfo> _currAssetsTextInfo;
        private List<ParsedAssetAndEntry> _currEntryInfos;
        private List<ParsedAssetAndEntry> _renderEntryInfos;

        private float _itemHeight = 80f;
        [SerializeField]
        private float _paddingTop = -35f;
        [SerializeField]
        private float _paddingLeft = -10f;

        private Dictionary<AssetClassID, bool> _isClassIDFiltered = new();

        private HashSet<long> _highlighted = new();

        public void OnScroll()
        {
            float scrollY = GetScrollAxisY();
            int startIndex = GetStartIndex(scrollY, _renderEntryInfos.Count, _maxNumOfEntryPerPage);

            for (int i = 0; i < _maxNumOfEntryPerPage; i++)
            {
                int dataIndex = startIndex + i;
                if (dataIndex >= _renderEntryInfos.Count)
                {
                    _entryPool[i].Hide();
                    continue;
                }

                _entryPool[i].Show();
                _entryPool[i].AssignStuff(dataIndex, _renderEntryInfos.Count, _scrollbarRef);
                _entryPool[i].Render(_renderEntryInfos[dataIndex], _highlighted.Contains(_renderEntryInfos[dataIndex].assetEntryInfo.pathID));
                _entryPool[i].SetPosition(new Vector2(_paddingLeft, -dataIndex * _itemHeight + _paddingTop));
            }
        }

        private List<ParsedAssetAndEntry> FilterEntryInfoByType(List<ParsedAssetAndEntry> entryInfos)
        {
            return entryInfos.Where(x => !_isClassIDFiltered.ContainsKey(x.assetEntryInfo.classID) ||
                                            (_isClassIDFiltered.ContainsKey(x.assetEntryInfo.classID) &&
                                            !_isClassIDFiltered[x.assetEntryInfo.classID])).ToList();
        }

        public void Refresh()
        {
            OnScroll();
        }

        public void OnEvent(AppEvent e)
        {
            if (e is SortEntryEvent see)
            {
                _renderEntryInfos = FilterEntryInfoByType(see.EntryInfos);
                _content.sizeDelta = new Vector2(
                    _content.sizeDelta.x,
                        _renderEntryInfos.Count * _itemHeight
                );
                Refresh();
                StartCoroutine(CallAfterDelay(0.3f, () => Refresh()));
            }
            else if (e is GoBundleViewEvent gbve)
            {
                _currEntryInfos = gbve.EntryInfos;
                _renderEntryInfos = _currEntryInfos;
                _content.sizeDelta = new Vector2(
                    _content.sizeDelta.x,
                        _renderEntryInfos.Count * _itemHeight
                );
                Refresh();

                // Some bundles might contain a lot assets, which can take time to load.
                // Try refresh again after a short period of time.
                StartCoroutine(CallAfterDelay(0.3f, () => Refresh()));
            }
            else if (e is AssetMultiSelectionEvent amse)
            {
                _highlighted = amse.SelectedPathIDs;
                Refresh();
            }
            else if (e is FilterTypeEvent fte)
            {
                _isClassIDFiltered = fte.IsClassIDFiltered;
                _renderEntryInfos = FilterEntryInfoByType(_currEntryInfos);
                _content.sizeDelta = new Vector2(
                    _content.sizeDelta.x,
                        _renderEntryInfos.Count * _itemHeight
                );
                Refresh();
                StartCoroutine(CallAfterDelay(0.3f, () => Refresh()));
            }
        }

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            // Initialize pool
            for (int i = 0; i < _maxNumOfEntryPerPage; i++)
            {
                GameObject entryObj = Instantiate(_entryPrefab);
                entryObj.transform.SetParent(_content.transform, worldPositionStays: false);
                EntryInfoView entryInfoView = entryObj.GetComponentInChildren<EntryInfoView>();
                entryInfoView.dispatcher = _appEnvironment.Dispatcher;
                _appEnvironment.Dispatcher.Register(entryInfoView);
                _entryPool.Add(entryInfoView);
            }
            _itemHeight = _entryPrefab.GetComponent<RectTransform>().rect.height;
        }

        private float GetScrollAxisY()
        {
            return 1f - _scrollbarRef.value;
        }

        private int GetStartIndex(float scrollY, int totalItems, int visibleCount)
        {
            int maxStart = Mathf.Max(0, totalItems - visibleCount);
            return Mathf.Clamp(Mathf.FloorToInt(scrollY * maxStart), 0, maxStart);
        }

        private IEnumerator CallAfterDelay(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }
    }
}