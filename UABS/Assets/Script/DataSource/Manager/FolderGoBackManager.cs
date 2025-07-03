using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class FolderGoBackManager : IAppEventListener
    {
        public Func<string> GetBackDirectory;
        private AppEnvironment _appEnvironment = null;
        private List<List<DependencyInfo>> _recordDependencyInfosList = new();

        public FolderGoBackManager(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
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
                Debug.Log("Unloaded everything from assets manager.");
                _appEnvironment.AssetsManager.UnloadAll();
                
                string backDir = GetBackDirectory();
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