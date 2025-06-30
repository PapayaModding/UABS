using System.Collections.Generic;
using System.IO;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UnityEngine;

namespace UABS.Assets.Script.DataSource
{
    public class FolderDataSource : MonoBehaviour, IAppEventListener, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        private ReadFolderContent _readFolderContent = new();
        private List<string> _recordPaths = new();
        private List<List<DependencyInfo>> _recordDependencyInfosList = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void OnEvent(AppEvent e)
        {
            if (e is FolderRead4DependencyEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    List<FolderViewInfo> allReadable = _readFolderContent.ReadAllReadable(fr4d.FolderPath);
                    List<DependencyInfo> dependencyInfos = fr4d.DependencyInfos;
                    _recordDependencyInfosList.Add(dependencyInfos);
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
                    AppEnvironment.Dispatcher.Dispatch(new FolderViewInfosEvent(allReadable));
                }
                else
                {
                    Debug.Log($"{fr4d.FolderPath} is not a folder, attempt to read it as a bundle.");
                    BundleReader bundleReader = new(AppEnvironment);
                    bundleReader.ReadBundle4Dependency(fr4d.FolderPath, fr4d.OverrideBundlePath);
                }
                RecordPath(fr4d.FolderPath);
            }
            else if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    List<FolderViewInfo> allReadable = _readFolderContent.ReadAllReadable(fre.FolderPath);
                    AppEnvironment.Dispatcher.Dispatch(new FolderViewInfosEvent(allReadable));
                }
                else
                {
                    Debug.Log($"{fre.FolderPath} is not a folder, attempt to read it as a bundle.");
                    BundleReader bundleReader = new(AppEnvironment);
                    bundleReader.ReadBundle(fre.FolderPath);
                }
                RecordPath(fre.FolderPath);
            }
            else if (e is GoBackEvent)
            {
                string backDir = GetBackDirectory();
                if (_recordDependencyInfosList.Count == 0)
                {
                    AppEnvironment.Dispatcher.Dispatch(new FolderReadEvent(backDir));
                }
                else
                {
                    AppEnvironment.Dispatcher.Dispatch(new FolderRead4DependencyEvent(backDir, _recordDependencyInfosList[^1]));
                    _recordDependencyInfosList.RemoveAt(_recordDependencyInfosList.Count - 1);
                }
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
            if (_recordPaths.Count == 0)
            {
                _recordPaths.Add(newPath);
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
                    _recordPaths[^1] = newPath;
                }
                else
                {
                    _recordPaths.Add(newPath);
                }
            // PrintRecordedPaths();
        }

        private string GetBackDirectory()
        {
            if (_recordPaths.Count == 1) // This is the main path
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
                _recordPaths.RemoveAt(_recordPaths.Count - 1);
                return GetLastRecordedPath();
            }
            else
            {
                return Path.GetDirectoryName(last);
            }
        }

        private bool ShouldPathBeRemoved(string subPath)
        {
            if (_recordPaths.Count == 1)
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
            return _recordPaths[0];
        }

        private string GetLastRecordedPath()
        {
            return _recordPaths[^1];
        }

        private void PrintRecordedPaths()
        {
            string result = "Currently recorded paths: \n";
            foreach (string path in _recordPaths)
            {
                result += $"\t - {path}\n";
            }
            Debug.Log(result);
        }
        
        private bool IsPathPrefix(string basePath, string targetPath)
        {
            string fullBase = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullTarget = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return fullTarget.StartsWith(fullBase + Path.DirectorySeparatorChar);
        }
    }
}