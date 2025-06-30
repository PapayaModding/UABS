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

        private FolderDataPathManager _pathManager;
        private List<List<DependencyInfo>> _recordDependencyInfosList = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _pathManager = new(_appEnvironment);
            _appEnvironment.Dispatcher.Register(_pathManager);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is FolderRead4DependencyEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _recordDependencyInfosList.Add(fr4d.DependencyInfos);
                }
            }
            else if (e is GoBackEvent)
            {
                string backDir = _pathManager.GetBackDirectory();
                if (_recordDependencyInfosList.Count == 0)
                {
                    _appEnvironment.Dispatcher.Dispatch(new FolderReadEvent(backDir));
                }
                else
                {
                    _appEnvironment.Dispatcher.Dispatch(new FolderRead4DependencyEvent(backDir, _recordDependencyInfosList[^1]));
                    _recordDependencyInfosList.RemoveAt(_recordDependencyInfosList.Count - 1);
                }
            }
        }
    }
}