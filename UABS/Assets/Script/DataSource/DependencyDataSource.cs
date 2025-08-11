using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Reader.Search;
using UABS.Assets.Script.Writer.SystemCache;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.DataSource
{
    public class DependencyDataSource : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        private BundleFileInstance _currBunInst;
        private ReadDependencyInfo _readDependencyInfo;
        private CopyDeriveToSysFolder _copyDeriveToSysFolder = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _readDependencyInfo = new(AppEnvironment.AssetsManager, AppEnvironment.Wrapper.JsonSerializer);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is AssetsReadEvent bre)
            {
                _currBunInst = bre.Bundle;
                // Debug.Log($"Dependency data source: Received Bundle from {bre.FilePath}");
            }
            else if (e is AssetsRead4DeriveEvent br4d)
            {
                _currBunInst = br4d.Bundle;
            }
            else if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    _currBunInst = null;
                }
            }
            else if (e is FolderRead4DeriveEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _currBunInst = null;
                }
            }
            else if (e is RequestDependencyEvent rde)
            {
                if (_currBunInst == null)
                {
                    Debug.LogWarning($"This is called earlier than BundleReadEvent, inspect a bundle first.");
                    return;
                }
                List<DeriveInfo> dependencyInfos = _readDependencyInfo.ReadInfoFor(_currBunInst, rde.ReadFromCachePath);
                Debug.Log("DEPENDENCIES:");
                foreach (DeriveInfo dependencyInfo in dependencyInfos)
                {
                    Debug.Log($"{dependencyInfo.name}, {dependencyInfo.cabCode}, {dependencyInfo.path}");
                }

                List<string> dependencyPaths = dependencyInfos.Select(x => x.path).ToList();
                string previewFolderPath = _copyDeriveToSysFolder.CopyFromPaths(dependencyPaths, PredefinedPaths.ExternalSystemDependencyCache);

                AppEnvironment.Dispatcher.Dispatch(new FolderRead4DeriveEvent(previewFolderPath, dependencyInfos, "")
                {
                    from="DependencyDataSource"
                });
            }
        }
    }
}