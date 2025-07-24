using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Reader.BundlesRead
{
    public class DependentReader
    {
        private readonly IJsonSerializer _jsonSerializer;

        public DependentReader(IJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public List<string> FindDependentPaths(string dependencyCabCode, string fromCache)
        {
            List<string> result = new();
            string[] jsonFiles = Directory.GetFiles(fromCache, "*.json", SearchOption.AllDirectories);
            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    List<IJsonObject> arr = _jsonSerializer.DeserializeToArray(json);
                    foreach (var item in arr)
                    {
                        List<string> dependencies = item.GetStringArray("Dependencies");
                        foreach (string cabCode in dependencies)
                        {
                            if (IsEquivalent(cabCode, dependencyCabCode))
                            {
                                result.Add(item.GetString("Path"));
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

        private bool IsEquivalent(string code1, string code2)
        {
            code1 = code1.ToLower();
            code2 = code2.ToLower();
            return code1.StartsWith(code2) || code2.StartsWith(code1);
        }
    }
}