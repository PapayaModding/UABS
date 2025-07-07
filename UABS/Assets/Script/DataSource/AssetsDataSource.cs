using System.Collections.Generic;
using UnityEngine;
using UABS.Assets.Script.DataSource.Manager;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Event;

namespace UABS.Assets.Script.DataSource
{
    public class AssetsDataSource : MonoBehaviour, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        public List<ParsedAssetAndEntry> _entryInfos;
        private AssetsDataExportManager _exportManager;
        private AssetsDataSortManager _sortManager;
        private AssetsDataFilterManager _filterManager;
        private AssetsOpenBundleManager _openBundleManager;
        private AssetsDataSelectionManager _selectionManager;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _exportManager = new(_appEnvironment)
            {
                EntryInfosCallBack = () => _entryInfos
            };
            _appEnvironment.Dispatcher.Register(_exportManager);

            _sortManager = new()
            {
                GetEntryInfosCallBack = () => _entryInfos,
                SetEntryInfosCallBack = val =>
                {
                    _entryInfos = val;
                    appEnvironment.Dispatcher.Dispatch(new AssetsRenderEvent(_entryInfos, -1));
                }
            };
            _appEnvironment.Dispatcher.Register(_sortManager);

            _openBundleManager = new(_appEnvironment)
            {
                GetEntryInfosCallBack = () => _entryInfos,
                SetEntryInfosCallBack = val =>
                {
                    _entryInfos = val;
                    appEnvironment.Dispatcher.Dispatch(new AssetsRenderEvent(_entryInfos, 0));
                }
            };
            _appEnvironment.Dispatcher.Register(_openBundleManager);

            _filterManager = new()
            {
                GetEntryInfosCallBack = () => _entryInfos,
                SetEntryInfosCallBack = val =>
                {
                    _entryInfos = val;
                    appEnvironment.Dispatcher.Dispatch(new AssetsRenderEvent(_entryInfos, -1));
                }
            };
            _appEnvironment.Dispatcher.Register(_filterManager);

            _selectionManager = new(_appEnvironment.Dispatcher)
            {
                GetEntryInfosCallBack = () => _entryInfos,
            };
            _appEnvironment.Dispatcher.Register(_selectionManager);
        }


        public void Prev()
        {
            _selectionManager.Prev();
        }

        public void Next()
        {
            _selectionManager.Next();
        }
    }
}