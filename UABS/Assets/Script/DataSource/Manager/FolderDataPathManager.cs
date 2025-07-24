using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader.BundlesRead;

namespace UABS.Assets.Script.DataSource.Manager
{
    public class FolderDataPathManager : IAppEventListener
    {
        public Action<List<string>> SetPathsCallBack;
        public Func<List<string>> GetPathsCallBack;
        private List<string> Paths => GetPathsCallBack != null ? GetPathsCallBack() : new();

        private readonly ReadFolderContent _readFolderContent;
        private readonly AppEnvironment _appEnvironment;

        public FolderDataPathManager(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _readFolderContent = new(_appEnvironment.AssetsManager);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is FolderReadEvent fre)
            {
                if (PathUtils.IsSystemCacheFolder(fre.FolderPath))
                    return;

                if (Directory.Exists(fre.FolderPath))
                {
                    List<FolderViewInfo> allReadable = _readFolderContent.ReadAllReadable(fre.FolderPath);
                    RecordPath(fre.FolderPath, "fre");
                    _appEnvironment.Dispatcher.Dispatch(new FolderViewInfosEvent(allReadable));
                }
                else
                {
                    Debug.Log($"{fre.FolderPath} is not a folder, attempt to read it as a bundle.");
                    _appEnvironment.BundleReader.ReadBundle(fre.FolderPath);
                }
                _appEnvironment.Dispatcher.Dispatch(new ControlSearchGoBackEvent(true));
            }
            else if (e is BundleReadEvent bre)
            {
                if (PathUtils.IsSystemCacheFolder(bre.FilePath))
                    return;

                RecordPath(bre.FilePath, "bre");
                _appEnvironment.Dispatcher.Dispatch(new ControlSearchGoBackEvent(true));
            }
            else if (e is BundleRead4DeriveEvent br4d)
            {
                RecordPath(br4d.FilePath, "br4d");
                _appEnvironment.Dispatcher.Dispatch(new ControlSearchGoBackEvent(true));
            }
        }

        public void RecordPath(string newPath, string from)
        {
            // * Must avoid adding path that is the same as the last
            if (Paths.Count > 0 && PathUtils.ArePathsEqual(newPath, GetLastRecordedPath()))
                return;

            AddToPaths(newPath);
            Debug.Log($"RECORD SIZE: {Paths.Count}");
            // PrintRecordedPaths();
        }

        private void AddToPaths(string newPath)
        {
            List<string> copy = new(Paths)
            {
                newPath
            };
            SetPathsCallBack(copy);
        }

        public string GetBackDirectory()
        {
            if (Paths.Count == 1)
            {
                // We want to return the Directory name
                string newDir = Path.GetDirectoryName(Paths[0]);
                RemoveLastPath();
                return newDir;
            }
            else
            {
                RemoveLastPath();
                return GetLastRecordedPath();
            }
        }

        private void RemoveLastPath()
        {
            List<string> copy = new(Paths);
            copy.RemoveAt(copy.Count - 1);
            SetPathsCallBack(copy);
        }

        private string GetLastRecordedPath()
        {
            return Paths[^1];
        }

        private void PrintRecordedPaths()
        {
            string result = "Currently recorded paths: \n";
            foreach (string path in Paths)
            {
                result += $"\t - {path}\n";
            }
            Debug.Log(result);
        }
    }
}