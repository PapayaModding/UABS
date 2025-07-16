using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Writer
{
    public class InheritMemoWriter
    {
        private AppEnvironment _appEnvironment;

        public InheritMemoWriter(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void InheritMemoCache(string from, string to, bool safeCopy=true)
        {
            string[] fromJsonFiles = Directory.GetFiles(from, "*.json", SearchOption.AllDirectories);
            foreach (string fromJsonFile in fromJsonFiles)
            {
                string excludePrefixPath = PathUtils.RemovePrefix(fromJsonFile, from);
                string toJsonFile = Path.Combine(to, excludePrefixPath);
                if (!File.Exists(toJsonFile))
                    continue;

                CopyTo(fromJsonFile, toJsonFile, safeCopy);
            }
        }

        private void CopyTo(string fromJsonFile, string toJsonFile, bool safeCopy)
        {
            string fromJsonContents = File.ReadAllText(fromJsonFile);
            string toJsonContents = File.ReadAllText(toJsonFile);
            List<IJsonObject> fromArr = _appEnvironment.Wrapper.JsonSerializer.DeserializeToArray(fromJsonContents);
            List<IJsonObject> toArr = _appEnvironment.Wrapper.JsonSerializer.DeserializeToArray(toJsonContents);
            string[] namesInFrom = fromArr.Select(x => GetIdentifyNameFromItemName(x.GetString("Name"))).ToArray();
            string[] namesInTo = toArr.Select(x => GetIdentifyNameFromItemName(x.GetString("Name"))).ToArray();

            List<(int, int, string)> changeInToArr = new();

            for (int i = 0; i < namesInTo.Length; i++)
            {
                // * Phase 1: the bundle name comparison
                int indexInFrom = Array.IndexOf(namesInFrom, namesInTo[i]);
                if (indexInFrom == -1)
                    continue;

                // * Phase 2: check if asset infos exist
                IJsonObject fromItem = fromArr[indexInFrom];
                IJsonObject toItem = toArr[i];

                List<IJsonObject> fromAssetInfos = GetAssetInfos(fromItem);
                if (fromAssetInfos == null || fromAssetInfos.Count == 0)
                    continue;
                List<IJsonObject> toAssetInfos = GetAssetInfos(toItem);
                if (toAssetInfos == null || toAssetInfos.Count == 0)
                    continue;

                // * Phase 3: check if 'from' asset name exists in 'to'
                List<string> fromAssetNames = fromAssetInfos.Select(x => x.GetString("Name")).ToList();
                List<string> toAssetNames = toAssetInfos.Select(x => x.GetString("Name")).ToList();

                for (int j = 0; j < fromAssetNames.Count; j++)
                {
                    string fromAssetName = fromAssetNames[j];
                    int indexInTo = toAssetNames.IndexOf(fromAssetName);
                    if (indexInTo == -1)
                        continue;

                    string fromMemo = fromAssetInfos[j].GetString("Memo");
                    string toMemo = toAssetInfos[indexInTo].GetString("Memo");

                    string memo = safeCopy ? SafeCopyMemo(fromMemo, toMemo) : OverrideCopyMemo(fromMemo);
                    if (memo != null)
                        changeInToArr.Add((i, indexInTo, memo));
                }
            }
            ApplyChangeToArr(toArr, changeInToArr);
            string jsonContent = _appEnvironment.Wrapper.JsonSerializer.Serialize(toArr, true);
            File.WriteAllText(toJsonFile, jsonContent, Encoding.UTF8);
        }

        private void ApplyChangeToArr(List<IJsonObject> toArr, List<(int, int, string)> changeInToArr)
        {
            foreach (var change in changeInToArr)
            {
                (int i, int j, string memo) = change;
                ApplyChangeByItem(toArr, i, j, memo);
            }
        }

        private void ApplyChangeByItem(List<IJsonObject> toArr, int itemIndex, int assetIndex, string memo)
        {
            IJsonObject targetItem = toArr[itemIndex];
            List<IJsonObject> assetInfos = targetItem.GetArray("AssetInfos");
            
            // Everything is good, set the new memo for asset
            assetInfos[assetIndex].SetString("Memo", memo);
            // Write back to arr
            toArr[itemIndex].SetArray("AssetInfos", assetInfos);
        }

        private string SafeCopyMemo(string fromMemo, string toMemo)
        {
            if (string.IsNullOrWhiteSpace(fromMemo))
                return null;  // Null means don't copy
            if (!string.IsNullOrWhiteSpace(toMemo))
                return null;
            return fromMemo;
        }

        // Doesn't consider if there is memo in 'to'
        private string OverrideCopyMemo(string fromMemo)
        {
            if (string.IsNullOrWhiteSpace(fromMemo))
                return null;  // Null means don't copy
            return fromMemo;
        }

        private List<IJsonObject> GetAssetInfos(IJsonObject item)
        {
            return item.GetArray("AssetInfos");
        }

        private string GetIdentifyNameFromItemName(string name)
        {
            name = Path.GetFileNameWithoutExtension(name);
            int lastUnderScore = name.LastIndexOf('_');
            return name[..lastUnderScore];
        }
    }
}