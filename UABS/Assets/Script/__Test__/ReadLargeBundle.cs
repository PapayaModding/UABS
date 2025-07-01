using System.Collections.Generic;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UnityEngine;
using System.Diagnostics;
using UABS.Assets.Script.DataStruct;


namespace UABS.Assets.Script.__Test__
{
    public class ReadLargeBundle : MonoBehaviour
    {
        private AppEnvironment _appEnvironment;
        private ReadTextInfoFromBundle _readTextInfoFromBundle;
        private BundleReader _bundleReader;
        public const string TestBundlePath = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\StreamingAssets\aa\StandaloneWindows64\bodygroup_assets_all_2d25edfe2a44d351d4079093e6d8239b.bundle";

        private void Start()
        {
            _appEnvironment = new();
            _bundleReader = new(_appEnvironment);
            _readTextInfoFromBundle = new(_appEnvironment.AssetsManager);
            BundleFileInstance bunInst = _bundleReader.ReadBundle(TestBundlePath);

            // 96930
            Stopwatch stopwatch = Stopwatch.StartNew();
            // List<AssetTextInfo> assetsTextInfo = _readTextInfoFromBundle.ReadAllBasicInfo(bunInst);
            stopwatch.Stop();  // Stop timer
                               // UnityEngine.Debug.Log($"Reading everything took {stopwatch.ElapsedMilliseconds} ms");

            // 6739
            // stopwatch = Stopwatch.StartNew();
            // List<(string, long)> necessary = _readTextInfoFromBundle.ReadNecessaryOnly(bunInst);
            // stopwatch.Stop();  // Stop timer
            // UnityEngine.Debug.Log($"Reading everything took {stopwatch.ElapsedMilliseconds} ms");

            // 32
            stopwatch = Stopwatch.StartNew();
            List<ParsedAsset> parsedAssets = _readTextInfoFromBundle.ReadAssetOnly(bunInst);
            stopwatch.Stop();
            UnityEngine.Debug.Log($"Reading everything took {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}