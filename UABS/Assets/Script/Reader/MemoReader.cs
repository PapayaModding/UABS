using System.Collections.Generic;
using System.IO;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Reader
{
    public class MemoReader
    {
        private AppEnvironment _appEnvironment;

        public MemoReader(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public string ReadAssetMemo(string cachePath, string bundlePath, string assetName)
        {
            string[] jsonFiles = Directory.GetFiles(cachePath, "*.json", SearchOption.AllDirectories);
            (string targetJson, int targetIndex) = SearchTargetJson(jsonFiles, bundlePath);
            if (string.IsNullOrEmpty(targetJson))
            {
                UnityEngine.Debug.LogWarning($"Failed to read memo in {bundlePath}. Couldn't find path in the currently selected cache.");
                return null;
            }
            string targetJsonContent = File.ReadAllText(targetJson);
            List<IJsonObject> arr = _appEnvironment.Wrapper.JsonSerializer.DeserializeToArray(targetJsonContent);
            IJsonObject targetItem = arr[targetIndex];
            List<IJsonObject> assetInfos = targetItem.GetArray("AssetInfos");
            if (assetInfos == null || assetInfos.Count == 0)
            {
                UnityEngine.Debug.LogWarning($"Found bundle path {bundlePath} but failed to find the target asset name.");
                return null;
            }
            int indexOfAssetInfo = FindIndexOfAssetInfoWithTargetName(assetInfos, assetName);
            if (indexOfAssetInfo == -1)
            {
                UnityEngine.Debug.LogWarning($"Found bundle path {bundlePath} but failed to find the target asset name.");
                return null;
            }
            return assetInfos[indexOfAssetInfo].GetString("Memo");
        }

        private int FindIndexOfAssetInfoWithTargetName(List<IJsonObject> assetInfos, string targetName)
        {
            for (int i = 0; i < assetInfos.Count; i++)
            {
                IJsonObject assetInfo = assetInfos[i];
                if (assetInfo.GetString("Name") == targetName)
                    return i;
            }
            return -1;
        }

        private (string, int) SearchTargetJson(string[] jsonFiles, string bundlePath)
        {
            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string jsonFile = jsonFiles[i];
                string json = File.ReadAllText(jsonFile);
                List<IJsonObject> arr = _appEnvironment.Wrapper.JsonSerializer.DeserializeToArray(json);
                for (int j = 0; j < arr.Count; j++)
                {
                    IJsonObject item = arr[j];
                    // UnityEngine.Debug.Log(item.GetString("Path"));
                    if (IsPathSame(item.GetString("Path"), bundlePath))
                        return (jsonFile, j);
                }
            }
            return (null, -1);
        }

        private bool IsPathSame(string path1, string path2)
        {
            return PathUtils.ArePathsEqual(path1, path2);
        }
    }
}