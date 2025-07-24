using System.Collections.Generic;
using UnityEngine;
using UABS.Assets.Script.DataSource.Manager;
using UABS.Assets.Script.Misc.AppCore;

namespace UABS.Assets.Script.DataSource
{
    public class FolderDataSource : MonoBehaviour, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        
        private List<string> _paths = new();
        
        private FolderDataPathManager _pathManager;
        private FolderGoBackManager _goBackManager;
        private FolderDataDeriveManager _deriveManager;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _pathManager = new(_appEnvironment);
            _appEnvironment.Dispatcher.Register(_pathManager);
            _pathManager.SetPathsCallBack = val => _paths = val;
            _pathManager.GetPathsCallBack = () => _paths;

            _goBackManager = new(_appEnvironment)
            {
                GetPaths = () => _paths
            };
            _appEnvironment.Dispatcher.Register(_goBackManager);
            _goBackManager.GetBackDirectory = _pathManager.GetBackDirectory;

            _deriveManager = new(_appEnvironment, _pathManager)
            {
                GetPaths = () => _paths
            };
            _appEnvironment.Dispatcher.Register(_deriveManager);
        }
    }
}