using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;

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
            if (e is FolderRead4DependencyEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    List<FolderViewInfo> allReadable = _readFolderContent.ReadAllReadable(fr4d.FolderPath);
                    List<DependencyInfo> dependencyInfos = fr4d.DependencyInfos;
                    // _recordDependencyInfosList.Add(dependencyInfos);
                    foreach (FolderViewInfo readable in allReadable)
                    {
                        foreach (DependencyInfo dependencyInfo in dependencyInfos)
                        {
                            if (readable.name == dependencyInfo.name)
                            {
                                readable.RealPath = dependencyInfo.path;
                                continue;
                            }
                        }
                    }
                    _appEnvironment.Dispatcher.Dispatch(new FolderViewInfosEvent(allReadable));
                }
                else
                {
                    Debug.Log($"{fr4d.FolderPath} is not a folder, attempt to read it as a bundle.");
                    BundleReader bundleReader = new(_appEnvironment);
                    bundleReader.ReadBundle4Dependency(fr4d.FolderPath, fr4d.OverrideBundlePath);
                }
                RecordPath(fr4d.FolderPath);
            }
            else if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    List<FolderViewInfo> allReadable = _readFolderContent.ReadAllReadable(fre.FolderPath);
                    _appEnvironment.Dispatcher.Dispatch(new FolderViewInfosEvent(allReadable));
                }
                else
                {
                    Debug.Log($"{fre.FolderPath} is not a folder, attempt to read it as a bundle.");
                    BundleReader bundleReader = new(_appEnvironment);
                    bundleReader.ReadBundle(fre.FolderPath);
                }
                RecordPath(fre.FolderPath);
            }
            else if (e is BundleReadEvent bre)
            {
                RecordPath(bre.FilePath);
            }
            else if (e is BundleRead4DependencyEvent br4d)
            {
                RecordPath(br4d.FilePath);
            }
        }

        private void RecordPath(string newPath)
        {
            if (Paths.Count == 0)
            {
                AddToPaths(newPath);
                return;
            }

            string last = GetLastRecordedPath();
            if (newPath == last)
            {
                // Avoid adding the same last path (prevents go back loop)
                return;
            }

            if (IsPathPrefix(last, newPath) || IsPathPrefix(newPath, last))
            {
                ReplaceLastPath(newPath);
            }
            else
            {
                AddToPaths(newPath);
            }
            // PrintRecordedPaths();
        }

        public string GetBackDirectory()
        {
            if (Paths.Count == 1) // This is the main path
            {
                return Path.GetDirectoryName(GetMainPath());
            }

            // Must have sub-paths (such as dependency view)
            // ! Side-Effect: Remove if sub-path cannot be go back further
            string last = GetLastRecordedPath();
            // Debug.Log($"Last path: {last}");
            if (ShouldPathBeRemoved(last))
            {
                // Debug.Log($"REMOVED {_recordPaths[^1]}");
                RemoveLastPath();
                return GetLastRecordedPath();
            }
            else
            {
                return Path.GetDirectoryName(last);
            }
        }

        private bool ShouldPathBeRemoved(string subPath)
        {
            if (Paths.Count == 1)
                return false;
            return ArePathsEqual(Path.GetDirectoryName(subPath), PredefinedPaths.ExternalSystemDepCache);
        }

        private bool ArePathsEqual(string pathA, string pathB)
        {
            string fullA = Path.GetFullPath(pathA).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullB = Path.GetFullPath(pathB).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // Windows paths are case-insensitive
            return string.Equals(fullA, fullB, System.StringComparison.OrdinalIgnoreCase);
#else
            // macOS/Linux paths are case-sensitive
            return string.Equals(fullA, fullB, System.StringComparison.Ordinal);
#endif
        }

        private string GetMainPath()
        {
            return Paths[0];
        }

        private string GetLastRecordedPath()
        {
            return Paths[^1];
        }

        private bool IsPathPrefix(string basePath, string targetPath)
        {
            string fullBase = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullTarget = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return fullTarget.StartsWith(fullBase + Path.DirectorySeparatorChar);
        }

        private void AddToPaths(string newPath)
        {
            List<string> copy = new(Paths)
            {
                newPath
            };
            SetPathsCallBack(copy);
        }

        private void RemoveLastPath()
        {
            List<string> copy = new(Paths);
            copy.RemoveAt(copy.Count - 1);
            SetPathsCallBack(copy);
        }

        private void ReplaceLastPath(string newPath)
        {
            List<string> copy = new(Paths);
            copy[^1] = newPath;
            SetPathsCallBack(copy);
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