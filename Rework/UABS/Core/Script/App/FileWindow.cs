using System;
using System.Collections.Generic;
using AssetsTools.NET.Extra;
using UABS.Data;
using UABS.Service;
using UABS.Util;

namespace UABS.App
{
    public class FileWindow
    {
        private readonly AssetsManager _assetsManager;
        private IReadOnlyList<AssetsFileInstance>? AssetsInsts { get; set; }
        private IReadOnlyList<AssetEntry>? Assets { get; set; }
        // When open any openable file
        public event Action<List<AssetEntry>>? OnNewBundleOpened;

        public FileWindow(AssetsManagerService assetsManagerService)
        {
            _assetsManager = assetsManagerService.AssetsManager;

            // ! Load classdata.tpk
            var assembly = typeof(ClassDataLoader).Assembly;
            using var stream = assembly.GetManifestResourceStream(
                "Core.Script.Data.ClassData.classdata.tpk");
            if (stream == null)
                Log.Error("classdata.tpk not found", "FileWindow.cs");

            _assetsManager.LoadClassPackage(stream);
        }

        public void OpenNewBundle(string path)
        {
            // Decompose the bundle file in path to AssetEntries
            var result = BundleReader.ReadFromPath(path, _assetsManager);
            AssetsInsts = result.Item1;
            Assets = result.Item2;

            if (Assets is List<AssetEntry> foundAssets)
                OnNewBundleOpened?.Invoke(foundAssets);
        }
    }
}