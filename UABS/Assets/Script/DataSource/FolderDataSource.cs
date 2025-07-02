using System.Collections.Generic;
using System.IO;
using UABS.Assets.Script.DataSource.Manager;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UnityEngine;

namespace UABS.Assets.Script.DataSource
{
    public class FolderDataSource : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        
        private List<string> _paths = new();
        
        private FolderDataPathManager _pathManager;
        private FolderGoBackManager _goBackManager;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _pathManager = new(_appEnvironment);
            _appEnvironment.Dispatcher.Register(_pathManager);
            _pathManager.SetPathsCallBack = val => _paths = val;
            _pathManager.GetPathsCallBack = () => _paths;

            _goBackManager = new(_appEnvironment);
            _appEnvironment.Dispatcher.Register(_goBackManager);
            _goBackManager.GetBackDirectory = _pathManager.GetBackDirectory;
        }

        public void OnEvent(AppEvent e)
        {
            
        }
    }
}