using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Wrapper.Json;
using UABS.Assets.Script.Reader.UserPackage;
using UABS.Assets.Script.Misc.Threads;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Writer.UserPackage
{
    public class WriteUserPackage
    {
        private readonly UserPackageBuildReader _packageBuilder;

        public WriteUserPackage(AssetsManager assetsManager, IJsonSerializer jsonSerializer)
        {
            _packageBuilder = new(assetsManager, jsonSerializer);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async public void CreateAndSaveNewPackage(string dataPath, string savePath, Action onDone)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Thread thread = new(() =>
            {
                DoHeavyWork(dataPath, savePath);

                // Return to main thread
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log("Work done!");
                    onDone?.Invoke();
                });
            });
            thread.Start();
        }

        private void DoHeavyWork(string dataPath, string savePath)
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

            List<UserPackageInfo> packages = _packageBuilder.ReadInfoForBuildPackage(dataPath, savePath);
            foreach (UserPackageInfo packageInfo in packages)
            {
                string path = packageInfo.path;
                string content = packageInfo.jsonContent;
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Debug.Log($"Creating path {dir}");
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(path, content);
            }

            // Validate creation by adding an extra file "Validation.txt" in save path
            File.WriteAllText(Path.Combine(savePath, "Validation.txt"), "");
        }
    }
}