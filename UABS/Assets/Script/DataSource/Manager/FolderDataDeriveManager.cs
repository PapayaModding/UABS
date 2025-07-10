using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class FolderDataDeriveManager : IAppEventListener
    {
        private readonly ReadFolderContent _readFolderContent;
        private readonly AppEnvironment _appEnvironment;
        private readonly FolderDataPathManager _folderDataPathManager;

        public FolderDataDeriveManager(AppEnvironment appEnvironment, FolderDataPathManager folderDataPathManager)
        {
            _appEnvironment = appEnvironment;
            _readFolderContent = new(_appEnvironment.AssetsManager);
            _folderDataPathManager = folderDataPathManager;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is FolderRead4DeriveEvent fr4d)
            {
                // Debug.Log($"Fr4d came from {fr4d.from}");
                if (Directory.Exists(fr4d.FolderPath))
                {
                    List<FolderViewInfo> allReadable = _readFolderContent.ReadAllReadable(fr4d.FolderPath);
                    List<DeriveInfo> deriveInfos = fr4d.DeriveInfos;
                    foreach (FolderViewInfo readable in allReadable)
                    {
                        foreach (DeriveInfo deriveInfo in deriveInfos)
                        {
                            if (readable.name == deriveInfo.name)
                            {
                                readable.overrideDerivePath = deriveInfo.path;
                                continue;
                            }
                        }
                    }
                    _folderDataPathManager.RecordPath(fr4d.FolderPath, "fr4d");
                    _appEnvironment.Dispatcher.Dispatch(new FolderViewInfosEvent(allReadable));
                }
                else
                {
                    Debug.Log($"{fr4d.FolderPath} is not a folder, attempt to read it as a bundle.");
                    _appEnvironment.BundleReader.ReadBundle4Derive(fr4d.FolderPath, fr4d.OverrideBundlePath);
                }
            }
        }
    }
}