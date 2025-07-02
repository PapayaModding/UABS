using System.Collections.Generic;
using UABS.Assets.Script.DataSource.Manager;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Misc;
using UnityEngine;

namespace UABS.Assets.Script.DataSource
{
    public class AssetsDataSource : MonoBehaviour, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        public List<ParsedAssetAndEntry> _entryInfos;
        private AssetsDataExportManager _exportManager;
        private AssetsDataSortManager _sortManager;
        private AssetsOpenBundleManager _openBundleManager;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _exportManager = new(_appEnvironment.AssetsManager)
            {
                EntryInfosCallBack = () => _entryInfos
            };
            _appEnvironment.Dispatcher.Register(_exportManager);

            _sortManager = new(_appEnvironment.Dispatcher)
            {
                GetEntryInfosCallBack = () => _entryInfos,
                SetEntryInfosCallBack = val => _entryInfos = val
            };
            _appEnvironment.Dispatcher.Register(_sortManager);

            _openBundleManager = new(_appEnvironment)
            {
                GetEntryInfosCallBack = () => _entryInfos,
                SetEntryInfosCallBack = val => _entryInfos = val
            };
            _appEnvironment.Dispatcher.Register(_openBundleManager);
        }
    }
}