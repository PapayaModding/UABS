using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Writer
{
    public class WriteCache
    {
        private ReadNewCache _readNewCache;

        public WriteCache(AssetsManager assetsManager, IJsonSerializer jsonSerializer)
        {
            _readNewCache = new(assetsManager, jsonSerializer);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async public void CreateAndSaveNewCache(string dataPath, string savePath, Action onDone)
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

            List<CacheInfo> cache = _readNewCache.BuildCache(dataPath, savePath);
            foreach (CacheInfo cacheInfo in cache)
            {
                string path = cacheInfo.path;
                string content = cacheInfo.jsonContent;
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