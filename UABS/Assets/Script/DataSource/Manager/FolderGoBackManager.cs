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
        private List<List<DeriveInfo>> _recordDeriveInfosList = new();

        public FolderGoBackManager(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is FolderRead4DeriveEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _recordDeriveInfosList.Add(fr4d.DeriveInfos);
                }
            }
            else if (e is GoBackEvent)
            {
                Debug.Log("Unloaded everything from assets manager.");
                _appEnvironment.AssetsManager.UnloadAll();

                string backDir = GetBackDirectory();
                if (_recordDeriveInfosList.Count == 0)
                {
                    _appEnvironment.Dispatcher.Dispatch(new FolderReadEvent(backDir));
                }
                else
                {
                    _appEnvironment.Dispatcher.Dispatch(new FolderRead4DeriveEvent(backDir, _recordDeriveInfosList[^1], "")
                                                                                    {
                                                                                        from="FolderGoBackManager"
                                                                                    });
                    _recordDeriveInfosList.RemoveAt(_recordDeriveInfosList.Count - 1);
                }
            }
        }

        private DeriveType GetLastPathDeriveType(string lastPath)
        {
            if (PathUtils.PathStartsWith(lastPath, PredefinedPaths.ExternalSystemDependenceCache))
            {
                return DeriveType.Dependency;
            }
            else if (PathUtils.PathStartsWith(lastPath, PredefinedPaths.ExternalSystemSearchCache))
            {
                return DeriveType.Search;
            }
            else
            {
                return DeriveType.None;
            }
        }
    }
}