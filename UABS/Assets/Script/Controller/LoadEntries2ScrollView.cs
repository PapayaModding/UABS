using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        private int _maxNumOfEntryPerPage = 13;
        private List<EntryInfoView> _entryPool = new();
        private List<ParsedAssetAndEntry> _renderEntryInfos;

        private float _itemHeight = 80f;
        [SerializeField]
        private float _paddingTop = -35f;
        [SerializeField]
        private float _paddingLeft = -10f;

        public void OnScroll()
        {
            float scrollY = GetScrollAxisY();
            int startIndex = GetStartIndex(scrollY, _renderEntryInfos.Count, _maxNumOfEntryPerPage);

            OnScroll(startIndex);
        }

        public void OnScroll(int startIndex)
        {
            startIndex = (int) Mathf.Clamp(startIndex, 0, _renderEntryInfos.Count - _maxNumOfEntryPerPage);

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
                _entryPool[i].Render(_renderEntryInfos[dataIndex]);
                _entryPool[i].SetPosition(new Vector2(_paddingLeft, -dataIndex * _itemHeight + _paddingTop));
            }
        }

        public void Refresh()
        {
            OnScroll();
        }

        public void Refresh(int startIndex)
        {
            OnScroll(startIndex);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is OnAssetsDataChangeEvent dce)
            {
                _renderEntryInfos = dce.RenderEntryInfos;
                _content.sizeDelta = new Vector2(
                    _content.sizeDelta.x,
                        _renderEntryInfos.Count * _itemHeight
                );
                Refresh();
                StartCoroutine(CallAfterDelay(0.3f, () => Refresh()));
            }
            else if (e is AssetMultiSelectionEvent amse)
            {
                // ! Bug-fix: from first entry jump to last entry or last to first
                if (amse.StartIndex == 0 || amse.StartIndex == _renderEntryInfos.Count - 1)
                {
                    float newScrollbarValue = 1 - amse.StartIndex / (float)(_renderEntryInfos.Count - 1);
                    _scrollbarRef.value = newScrollbarValue;
                }
                Debug.Log($"Start index; {amse.StartIndex}");
                Refresh(amse.StartIndex);
                // StartCoroutine(CallAfterDelay(0.3f, () => Refresh(amse.StartIndex)));
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
            return Mathf.Clamp(Mathf.CeilToInt(scrollY * maxStart), 0, maxStart);
        }

        private IEnumerator CallAfterDelay(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }
    }
}