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
    public class LoadEntries2ScrollFolderView : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        [SerializeField]
        private GameObject _content;

        [SerializeField]
        private Scrollbar _scrollbarRef;

        [SerializeField]
        private GameObject _entryPrefab;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        private List<EntryFolderInfoView> _currEntryInfoViews = new();

        public void ClearAndLoadFolder(List<FolderViewInfo> folderViewInfos)
        {
            ClearContentChildren();

            _currEntryInfoViews = new();
            for (int i = 0; i < folderViewInfos.Count; i++)
            {
                GameObject entryObj = CreateEntry();
                entryObj.transform.SetParent(_content.transform, worldPositionStays: false);
                _currEntryInfoViews.Add(entryObj.GetComponentInChildren<EntryFolderInfoView>());
            }

            for (int i = 0; i < folderViewInfos.Count; i++)
            {
                FolderViewInfo folderViewInfo = folderViewInfos[i];
                EntryFolderInfoView entryInfoView = _currEntryInfoViews[i];
                entryInfoView.dispatcher = _appEnvironment.Dispatcher;
                entryInfoView.AssignStuff(i);
                entryInfoView.Render(folderViewInfo);
            }
        }

        private GameObject CreateEntry()
        {
            return Instantiate(_entryPrefab);
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
            if (e is FolderViewInfosEvent fvie)
            {
                ClearAndLoadFolder(fvie.FoldViewInfos);
            }
        }

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }
    }
}