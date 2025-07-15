using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AssetsTools.NET;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Wrapper.Json;
using UnityEngine;

namespace UABS.Assets.Script.Reader
{
    public class FindDeriveInfoInCache
    {
        private IJsonSerializer _jsonSerializer;

        public FindDeriveInfoInCache(IJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public DeriveInfo? FindInCacheByCabCode(string cachePath, string cabCode)
        {
            string[] jsonFiles = Directory.GetFiles(cachePath, "*.json", SearchOption.AllDirectories);
            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    List<IJsonObject> arr = _jsonSerializer.DeserializeToArray(json);
                    foreach (var item in arr)
                    {
                        if (CompareCabCode(item.GetString("CabCode"), cabCode))
                        {
                            return new()
                            {
                                name = item.GetString("Name"),
                                path = item.GetString("Path"),
                                cabCode = cabCode
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read {filePath}: {ex.Message}");
                }
            }
            return null;
        }

        private bool CompareCabCode(string cab1, string cab2)
        {
            string cab1Low = cab1.ToLower();
            string cab2Low = cab2.ToLower();
            return cab1Low.StartsWith(cab2Low) || cab2Low.StartsWith(cab1Low);
        }

        public List<DeriveInfo> FindInCacheBySearchOptions(string cachePath,
                                                            List<string> sKeys,
                                                            List<string> eKeys,
                                                            bool exactMatch=false,
                                                            bool searchMemo=false)
        {
            List<DeriveInfo> result = new();
            string[] jsonFiles = Directory.GetFiles(cachePath, "*.json", SearchOption.AllDirectories);
            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    List<IJsonObject> arr = _jsonSerializer.DeserializeToArray(json);
                    foreach (var item in arr)
                    {
                        if (exactMatch)
                        {
                            if (ShouldInclude(item, sKeys[0]))
                            {
                                result.Add(new()
                                {
                                    name = item.GetString("Name"),
                                    path = item.GetString("Path"),
                                    cabCode = item.GetString("CabCode")
                                });
                            }
                        }
                        else
                        {
                            if (ShouldInclude(item, sKeys, eKeys, searchMemo))
                            {
                                result.Add(new()
                                {
                                    name = item.GetString("Name"),
                                    path = item.GetString("Path"),
                                    cabCode = item.GetString("CabCode")
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read {filePath}: {ex.Message}");
                }
            }
            return result;
        }

        // * The search options:
        // eKeys have higher priority; if a name contains eKeys, even if it contains sKeys it won't be included
        // searching based on OR rather than AND
        // searching bundle name & assets name in bundle (but will return bundle in the end, a.k.a name, path, cabCode)
        private bool ShouldInclude(IJsonObject item, List<string> sKeys, List<string> eKeys, bool searchMemo)
        {
            string bundleName = item.GetString("Name");
            if (PassSearchOptions(bundleName, sKeys, eKeys)) return true;

            string assetInfosString = item.GetString("AssetInfos");
            if (string.IsNullOrWhiteSpace(assetInfosString)) return false;

            List<IJsonObject> assetInfos = item.GetArray("AssetInfos");
            if (assetInfos == null || assetInfos.Count == 0)
                return false;

            foreach (IJsonObject assetInfo in assetInfos)
            {
                string spriteName = assetInfo.GetString("Name");
                if (PassSearchOptions(spriteName, sKeys, eKeys)) return true;
                if (searchMemo)
                {
                    string memo = assetInfo.GetString("Memo");
                    if (PassSearchOptions(memo, sKeys, eKeys)) return true;
                }
            }
            return false;
        }

        private bool ShouldInclude(IJsonObject item, string sKey)
        {
            string bundleName = item.GetString("Name");
            if (bundleName == sKey) return true;

            string assetInfosString = item.GetString("AssetInfos");
            if (string.IsNullOrWhiteSpace(assetInfosString)) return false;

            List<IJsonObject> assetInfos = item.GetArray("AssetInfos");
            if (assetInfos == null || assetInfos.Count == 0)
                return false;

            foreach (IJsonObject assetInfo in assetInfos)
            {
                string spriteName = assetInfo.GetString("Name");
                if (spriteName == sKey) return true;
            }
            return false;
        }

        private bool PassSearchOptions(string s, List<string> sKeys, List<string> eKeys)
        {
            foreach (string eKey in eKeys)
            {
                if (ContainsIgnoreCase(s, eKey)) return false;
            }
            foreach (string sKey in sKeys)
            {
                if (ContainsIgnoreCase(s, sKey)) return true;
            }
            return false;
        }

        // check if a contains b, case-insensitive
        private bool ContainsIgnoreCase(string a, string b)
        {
            if (a == null || b == null)
                return false;

            return a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ! For debug
        public static string GetAssetTypeValueFieldString(AssetTypeValueField field, int indentLevel = 0)
        {
            if (field == null) return "<null>";

            StringBuilder sb = new();
            string indent = new(' ', indentLevel * 2);

            // Field name and type
            sb.Append(indent);
            sb.Append(field.FieldName);
            sb.Append(" (");
            sb.Append(field.TypeName);
            sb.Append(")");

            // Field value (if any)
            if (field.Value != null)
            {
                sb.Append(" : ");
                sb.Append(field.Value);
            }
            sb.AppendLine();

            // Recursively append children
            if (field.Children != null)
            {
                foreach (var child in field.Children)
                {
                    sb.Append(GetAssetTypeValueFieldString(child, indentLevel + 1));
                }
            }

            return sb.ToString();
        }
    }
}