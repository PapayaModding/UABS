using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.View.BundleView;

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

        public void OnScroll(int focusIndex)
        {
            if (focusIndex < 0) // Don't change focus
            {
                OnScroll();
                return;
            }

            focusIndex = Mathf.Clamp(focusIndex, 0, Mathf.Max(_renderEntryInfos.Count - _maxNumOfEntryPerPage, 0));

            for (int i = 0; i < _maxNumOfEntryPerPage; i++)
            {
                int dataIndex = focusIndex + i;
                if (dataIndex >= _renderEntryInfos.Count)
                {
                    _entryPool[i].Hide();
                    continue;
                }

                _entryPool[i].Show();
                _entryPool[i].AssignStuff(dataIndex, _renderEntryInfos.Count);
                _entryPool[i].Render(_renderEntryInfos[dataIndex]);
                _entryPool[i].SetPosition(new Vector2(_paddingLeft, -dataIndex * _itemHeight + _paddingTop));
            }
        }

        public void Refresh(int focusIndex)
        {
            OnScroll(focusIndex);
            OnScroll();
        }

        public void OnEvent(AppEvent e)
        {
            if (e is AssetsRenderEvent dce)
            {
                _renderEntryInfos = dce.RenderEntryInfos;
                _content.sizeDelta = new Vector2(
                    _content.sizeDelta.x,
                        _renderEntryInfos.Count * _itemHeight
                );
                if (dce.FocusIndex != -1)
                {
                    JumpByIndex(dce.FocusIndex);
                    FindEntryWithIndex(dce.FocusIndex).TriggerEvent();
                }
                Refresh(dce.FocusIndex);
                // StartCoroutine(CallAfterDelay(0.3f, () => Refresh(dce.FocusIndex)));
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

        private void JumpByIndex(int jumpToIndex)
        {
            if (_renderEntryInfos.Count <= 1)
            {
                _scrollbarRef.value = 1f;
                return;
            }

            jumpToIndex = Mathf.Clamp(jumpToIndex, 0, _renderEntryInfos.Count - 1);
            float newScrollbarValue = 1f - jumpToIndex / (float)(_renderEntryInfos.Count - 1);
            newScrollbarValue = Mathf.Clamp01(newScrollbarValue);
            _scrollbarRef.value = newScrollbarValue;
        }

        private EntryInfoView FindEntryWithIndex(int index)
        {
            foreach (EntryInfoView entryInfoView in _entryPool)
            {
                if (entryInfoView.Index == index)
                {
                    return entryInfoView;
                }
            }
            return null;
        }
    }
}