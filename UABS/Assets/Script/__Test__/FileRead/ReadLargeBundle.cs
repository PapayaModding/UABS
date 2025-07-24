using System;
using System.Threading.Tasks;
using UnityEngine;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Streams;

namespace UABS.Assets.Script.__Test__.FileRead
{
    public class ReadLargeBundle : MonoBehaviour
    {
        private AppEnvironment _appEnvironment;
        // public const string TestBundlePath = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\StreamingAssets\aa\StandaloneWindows64\bodygroup_assets_all_2d25edfe2a44d351d4079093e6d8239b.bundle";
        public const string TestBundlePath = @"\\?\D:\元气骑士\assets\AssetBundles\common.ab";

        private void Start()
        {
            Debug.Log($"Reading {TestBundlePath}");
            _appEnvironment = new();

            Task.Run(() =>
            {
                Debug.Log("Start ReadBundle");
                var start = DateTime.UtcNow;

                BundleFileInstance bunInst = ReadBundleNoTypeTree(TestBundlePath);
                Debug.Log($"Expected total size: {GetUncompressedBundleSize(bunInst)}");
                
                AssetBundleFile bundle = bunInst.file;
                ProgressStreamCallback callback = new();
                ProgressStream bundleStream = new(callback);
                bundle.Unpack(new AssetsFileWriter(bundleStream));
                bundleStream.Position = 0;
                AssetBundleFile newBundle = new();
                newBundle.Read(new AssetsFileReader(bundleStream));
                bundle.Close();
                bunInst.file = newBundle;
                Debug.Log($"Written total size: {callback.TotalBytesWritten}");
                Debug.Log($"Read total size: {callback.TotalBytesRead}");

                var end = DateTime.UtcNow;
                Debug.Log($"ReadBundle took {(end - start).TotalMilliseconds} ms");
            });
        }

        public long GetUncompressedBundleSize(BundleFileInstance bundleInst)
        {
            if (bundleInst?.file == null)
                return 0;

            AssetBundleFile bundleFile = bundleInst.file;
            long totalSize = 0;

            foreach (var block in bundleFile.BlockAndDirInfo.BlockInfos)
            {
                totalSize += block.DecompressedSize;
            }

            return totalSize;
        }

        public BundleFileInstance ReadBundleNoTypeTree(string path)
        {
            BundleFileInstance bunInst = _appEnvironment.AssetsManager.LoadBundleFile(@path, false);
            // _appEnvironment.Dispatcher.Dispatch(new BundleReadEvent(bunInst, path));
            return bunInst;
        }
    }
}