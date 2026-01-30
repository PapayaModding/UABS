using System;
using System.Collections.Generic;
using AssetsTools.NET.Extra;
using UABS.Data;

namespace UABS.App
{
    public class FileWindow
    {
        private readonly AssetsManager _assetsManager;
        private IReadOnlyList<AssetEntry>? Assets { get; set; }
        // When open any openable file
        public event Action<List<AssetEntry>>? OnNewBundleOpened;

        public FileWindow(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public void OpenNewBundle(string path)
        {
            // Decompose the bundle file in path to AssetEntries
            var result = BundleReader.ReadFromPath(path, _assetsManager);
            List<AssetsFileInstance>? assetsFileInstances = result.Item1;
            Assets = result.Item2;

            if (Assets is List<AssetEntry> foundAssets)
                OnNewBundleOpened?.Invoke(foundAssets);
        }
    }
}