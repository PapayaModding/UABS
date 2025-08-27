using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;
using UABS.Assets.Script.Misc.Threads;
using UABS.Assets.Script.Wrapper.Json;
using UnityEngine;
using static UABS.Assets.Script.Reader.UserPackage.UserPackageBuildReader;

namespace UABS.Assets.Script.__Test__.TextAsset
{
    public class ReadAllTextAssets : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            TestHelper.TestOnCleanLabDesk(() =>
            {
                AppEnvironment appEnvironment = new();
                string[] targetPaths = appEnvironment.Wrapper.FileBrowser.OpenFolderPanel("Target", "", false);
                if (targetPaths.Length > 0)
                {
                    string targetPath = PathUtils.ToLongPath(targetPaths[0]);
                    if (!Directory.Exists(targetPath))
                    {
                        Debug.Log("Target doesn't exist");
                        return;
                    }

                    string[] storagePaths = appEnvironment.Wrapper.FileBrowser.OpenFolderPanel("Storage", "", false);
                    if (storagePaths.Length > 0)
                    {
                        string storagePath = PathUtils.ToLongPath(storagePaths[0]);
                        if (!Directory.Exists(storagePath))
                        {
                            Debug.Log("Storage doesn't exist");
                            return;
                        }

                        Thread thread = new(() =>
                        {
                            DoHeavyWork(targetPath, storagePath, appEnvironment.AssetsManager, appEnvironment.Wrapper.JsonSerializer);

                            // Return to main thread
                            UnityMainThreadDispatcher.Enqueue(() =>
                                {
                                    Debug.Log("Work done!");
                                });
                            });
                        thread.Start();
                        }
                    }

                onComplete?.Invoke();
            });
        }

        private void DoHeavyWork(string dataPath, string savePath, 
                                    AssetsManager assetsManager,
                                    IJsonSerializer jsonSerializer)
        {
            if (string.IsNullOrWhiteSpace(dataPath.Replace(@"\\?\", "")))
            {
                Debug.LogWarning("Failed to read Game Data Folder. Abort.");
                return;
            }

            if (string.IsNullOrWhiteSpace(savePath.Replace(@"\\?\", "")))
            {
                Debug.LogWarning("Failed to read New Save Folder. Abort.");
                return;
            }

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            List<UserPackageInfo> packages = ReadInfoForBuildPackage(dataPath, savePath, assetsManager, jsonSerializer);
            foreach (UserPackageInfo packageInfo in packages)
            {
                string path = packageInfo.path;
                string content = packageInfo.jsonContent;
                if (content != "[]")
                {
                    string dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                    {
                        Debug.Log($"Creating path {dir}");
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(path, content);
                }
            }

            // Validate creation by adding an extra file "Validation.txt" in save path
            File.WriteAllText(Path.Combine(savePath, "Validation.txt"), "");
        }

        public List<UserPackageInfo> ReadInfoForBuildPackage(string targetFolder, string packageFolder,
                                                            AssetsManager assetsManager,
                                                            IJsonSerializer jsonSerializer)
        {
            List<UserPackageInfo> result = new();
            List<string> paths = SurfFoldersUnderAllDir(targetFolder);
            foreach (string path in paths)
            {
                string basePath = path.Replace(targetFolder, "");
                string newPath = packageFolder + "\\" + basePath;
                // if (!Directory.Exists(newPath))
                // {
                //     Debug.Log($"Creating path {newPath}");
                //     Directory.CreateDirectory(newPath);
                // }
                List<Bundle> writeToIndex = new();
                List<string> filesInPath = SurfFilesUnderTopDir(path);
                // Create Bundle objects
                foreach (string fileInPath in filesInPath)
                {
                    string filePath = @$"{fileInPath}";
                    BundleFileInstance bunInst = assetsManager.LoadBundleFile(fileInPath, true);
                    AssetsFileInstance fileInst = assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
                    List<AssetInfo> textAssets = GetAssetInfoOfTextAssets(fileInst, assetsManager);
                    if (textAssets.Count > 0)
                    {
                        Bundle bundle = new()
                        {
                            Path = fileInPath,
                            Name = Path.GetFileName(fileInPath),
                            CabCode = ReadCABCode(filePath),

                            // --- Sprites ---
                            // Debug.Log(fileInPath);
                            AssetInfos = textAssets,
                        };
                        writeToIndex.Add(bundle);
                    }
                }

                string indexFile = newPath + "\\index.json";
                result.Add(new()
                {
                    path = indexFile,
                    // jsonContent = JsonConvert.SerializeObject(writeToIndex, Formatting.Indented)
                    jsonContent = jsonSerializer.Serialize(writeToIndex, true)
                });
            }
            return result;
        }

        private List<string> SurfFoldersUnderAllDir(string directory)
        {
            List<string> result = new();
            string[] allFolders = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);

            foreach (string folder in allFolders)
            {
                result.Add(folder);
            }
            result.Add(directory);

            return result;
        }

        private List<string> SurfFilesUnderTopDir(string directory)
        {
            List<string> result = new();
            string[] allFiles = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string file in allFiles)
            {
                if (file.EndsWith(".bundle") || file.EndsWith(".ab") || file.EndsWith(".assets"))
                    result.Add(file);
            }

            return result;
        }

        private List<AssetInfo> GetAssetInfoOfTextAssets(AssetsFileInstance fileInst, AssetsManager assetsManager)
        {
            // var allAssetClassIDs = Enum.GetValues(typeof(AssetClassID)).Cast<AssetClassID>().ToArray();
            // UnityEngine.Debug.Log(allAssetClassIDs.Length);
            // return GetAssetInfoInBundle(fileInst, new[] { AssetClassID.Sprite, AssetClassID.Texture2D });
            return GetAssetInfoInBundle(fileInst, new[] { AssetClassID.TextAsset }, assetsManager);
        }

        private List<AssetInfo> GetAssetInfoInBundle(AssetsFileInstance fileInst, AssetClassID[] classIDs, AssetsManager assetsManager)
        {
            List<AssetInfo> result = new();
            AssetsFile afile = fileInst.file;

            // !!! Testing purpose only, remember to change back

            // foreach (AssetClassID classID in Enum.GetValues(typeof(AssetClassID)))
            foreach (AssetClassID classID in classIDs)
            {
                List<AssetFileInfo> assets = afile.GetAssetsOfType(classID);
                if (assets.Count == 0)
                {
                    UnityEngine.Debug.Log($"No {classID} found.");
                    continue;
                }

                foreach (AssetFileInfo asset in assets)
                {
                    AssetTypeValueField baseField = assetsManager.GetBaseField(fileInst, asset);

                    string assetName = "Unnamed asset";

                    if (baseField != null)
                    {
                        var nameField = baseField.Get("m_Name");
                        if (nameField != null && !nameField.IsDummy)
                        {
                            try
                            {
                                assetName = nameField.AsString;
                            }
                            catch
                            {
                                UnityEngine.Debug.Log(GetAssetTypeValueFieldString(nameField));
                            }
                        }
                    }
                    result.Add(new()
                    {
                        Name = assetName,
                        PathId = asset.PathId
                    });
                }
            }

            return result;
        }
    }
}