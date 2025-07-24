using System.Collections.Generic;
using UnityEngine;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Reader.Search
{
    public class ReadDependencyInfo
    {
        private readonly AssetsManager _assetsManager;
        private readonly FindDeriveInfoInPackage _findDeriveInfoInCache;

        public ReadDependencyInfo(AssetsManager assetsManager, IJsonSerializer jsonSerializer)
        {
            _assetsManager = assetsManager;
            _findDeriveInfoInCache = new(jsonSerializer);
        }

        public List<DeriveInfo> ReadInfoFor(BundleFileInstance bunInst, string fromPackage, bool vocal=true)
        {
            List<DeriveInfo> result = new();
            AssetsFileInstance fileInst = _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
            List<AssetFileInfo> assets = fileInst.file.GetAssetsOfType(AssetClassID.AssetBundle);
            AssetFileInfo assetBundleInfo = assets[0];
            // Get the base field of the AssetBundle
            AssetTypeValueField baseField = _assetsManager.GetBaseField(fileInst, assetBundleInfo);

            // Navigate to m_Dependencies array
            AssetTypeValueField dependenciesArray = baseField["m_Dependencies"][0];

            int depCount = dependenciesArray.Children.Count;
            if (vocal)
                Debug.Log($"Number of dependencies: {depCount}");
            for (int i = 0; i < depCount; i++)
            {
                string dependencyCabCode = dependenciesArray[i].Value.AsString;
                if (vocal)
                    Debug.Log($"Dependency {i}: {dependencyCabCode}");

                DeriveInfo? _dependencyInfo = _findDeriveInfoInCache.FindInPackageByCabCode(fromPackage, dependencyCabCode);
                if (_dependencyInfo != null)
                {
                    DeriveInfo dependencyInfo = (DeriveInfo)_dependencyInfo;
                    result.Add(dependencyInfo);
                }
                else
                {
                    Debug.Log($"Could not find path to dependency for {dependencyCabCode}.");
                }
            }

            return result;
        }
    }
}