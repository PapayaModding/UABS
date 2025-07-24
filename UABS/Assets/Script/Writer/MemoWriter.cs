using System.Collections.Generic;
using System.IO;
using System.Text;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Writer
{
    public class MemoWriter
    {
        private AppEnvironment _appEnvironment;

        public MemoWriter(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void WriteMemo(string packagePath, string bundlePath, string assetName, string memo)
        {
            string[] jsonFiles = Directory.GetFiles(packagePath, "*.json", SearchOption.AllDirectories);
            (string targetJson, int targetIndex) = SearchTargetJson(jsonFiles, bundlePath);
            if (string.IsNullOrEmpty(targetJson))
            {
                UnityEngine.Debug.LogWarning($"Failed to write memo in {bundlePath}. Couldn't find path in the currently selected package.");
                return;
            }
            string targetJsonContent = File.ReadAllText(targetJson);
            List<IJsonObject> arr = _appEnvironment.Wrapper.JsonSerializer.DeserializeToArray(targetJsonContent);
            IJsonObject targetItem = arr[targetIndex];

            List<IJsonObject> assetInfos = targetItem.GetArray("AssetInfos");
            if (assetInfos == null || assetInfos.Count == 0)
            {
                UnityEngine.Debug.LogWarning($"Found bundle path {bundlePath} but failed to find the target asset name.");
                return;
            }
            int indexOfAssetInfo = FindIndexOfAssetInfoWithTargetName(assetInfos, assetName);
            if (indexOfAssetInfo == -1)
            {
                UnityEngine.Debug.LogWarning($"Found bundle path {bundlePath} but failed to find the target asset name.");
                return;
            }
            // Everything is good, set the new memo for asset
            assetInfos[indexOfAssetInfo].SetString("Memo", memo);
            // Write back to arr
            arr[targetIndex].SetArray("AssetInfos", assetInfos);

            // Write content to target json path
            string jsonContent = _appEnvironment.Wrapper.JsonSerializer.Serialize(arr, true);
            File.WriteAllText(targetJson, jsonContent, Encoding.UTF8);

            // UnityEngine.Debug.Log($"Success: read {memo} from {targetJson}");
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

        // * Look for the json file that contains item with the same path 
        // * as provided bundle path.
        // * Return the json file path & index of item that found 
        // * found the target in json.
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