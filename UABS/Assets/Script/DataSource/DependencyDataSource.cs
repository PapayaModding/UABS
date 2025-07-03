using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.Writer.Dependency;

namespace UABS.Assets.Script.DataSource
{
    public class DependencyDataSource : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        private BundleFileInstance _currBunInst;
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        private ReadDependencyInfo _readDependencyInfo;
        private CopyDepToSysFolder _copyDepToSysFolder = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _readDependencyInfo = new(AppEnvironment.AssetsManager, AppEnvironment.Wrapper.JsonSerializer);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is BundleReadEvent bre)
            {
                _currBunInst = bre.Bundle;
                // Debug.Log($"Dependency data source: Received Bundle from {bre.FilePath}");
            }
            else if (e is BundleRead4DependencyEvent br4d)
            {
                _currBunInst = br4d.Bundle;
            }
            else if (e is DependencyRequestEvent dre)
            {
                if (_currBunInst == null)
                {
                    Debug.LogWarning($"This is called earlier than BundleReadEvent, inspect a bundle first.");
                    return;
                }
                List<DependencyInfo> dependencyInfos = _readDependencyInfo.ReadInfoFor(_currBunInst, dre.ReadFromCachePath);
                Debug.Log("DEPENDENCIES:");
                foreach (DependencyInfo dependencyInfo in dependencyInfos)
                {
                    Debug.Log($"{dependencyInfo.name}, {dependencyInfo.cabCode}, {dependencyInfo.path}");
                }

                List<string> dependencyPaths = dependencyInfos.Select(x => x.path).ToList();
                string previewFolderPath = _copyDepToSysFolder.CopyFromPaths(dependencyPaths);

                AppEnvironment.Dispatcher.Dispatch(new FolderRead4DependencyEvent(previewFolderPath, dependencyInfos));
            }
            else if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    _currBunInst = null;
                }
            }
            else if (e is FolderRead4DependencyEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _currBunInst = null;
                }
            }
        }
    }
}