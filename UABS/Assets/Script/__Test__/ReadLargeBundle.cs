using System.Collections.Generic;
using UnityEngine;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using AssetsTools.NET;
using System.IO;
using System;
using System.Threading.Tasks;
using AssetsTools.NET.Extra.Decompressors.LZ4;

namespace UABS.Assets.Script.__Test__
{
    public class ReadLargeBundle : MonoBehaviour
    {
        private AppEnvironment _appEnvironment;
        private BundleReader _bundleReader;
        // public const string TestBundlePath = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\StreamingAssets\aa\StandaloneWindows64\bodygroup_assets_all_2d25edfe2a44d351d4079093e6d8239b.bundle";
        public const string TestBundlePath = @"\\?\D:\元气骑士\assets\AssetBundles\common.ab";
        private volatile float _loadProgress;
        private volatile bool _isLoading;

        private void Start()
        {
            UnityEngine.Debug.Log($"Reading {TestBundlePath}");
            _appEnvironment = new();
            _bundleReader = new(_appEnvironment);
            // BundleFileInstance bunInst = _bundleReader.ReadBundle(TestBundlePath);

            // Task.Run(() =>
            // {
            //     UnityEngine.Debug.Log("Start Set 1");
            //     var start = DateTime.UtcNow;
            //     ReadBundle1(TestBundlePath);
            //     var end = DateTime.UtcNow;
            //     UnityEngine.Debug.Log($"ReadBundle1 took {(end - start).TotalMilliseconds} ms");
            // });

            Task.Run(() =>
            {
                // Debug.Log("Start Set 2");
                // var start = DateTime.UtcNow;
                // BundleFileInstance bunInst = ReadBundle2(TestBundlePath);
                // // GetBasicInfoNoTypeTree(bunInst);
                // Debug.Log($"Expected total size: {GetUncompressedBundleSize(bunInst)}");
                
                // AssetBundleFile bundle = bunInst.file;
                // ProgressStream bundleStream = new();
                // bundle.Unpack(new AssetsFileWriter(bundleStream));
                // bundleStream.Position = 0;
                // AssetBundleFile newBundle = new();
                // newBundle.Read(new AssetsFileReader(bundleStream));

                // bundle.Close();
                // bunInst.file = newBundle;

                // var end = DateTime.UtcNow;
                // Debug.Log(bundleStream.TotalBytesWritten);
                // Debug.Log(bundleStream.TotalBytesRead);
                // Debug.Log($"ReadBundle2 took {(end - start).TotalMilliseconds} ms");

                // start = DateTime.UtcNow;
                // GetBasicInfoNoTypeTree(bunInst);
                // end = DateTime.UtcNow;
                // UnityEngine.Debug.Log($"Extract info took {(end - start).TotalMilliseconds} ms");
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

        public BundleFileInstance ReadBundle1(string path)
        {
            BundleFileInstance bunInst = _appEnvironment.AssetsManager.LoadBundleFile(@path, true);
            // _appEnvironment.Dispatcher.Dispatch(new BundleReadEvent(bunInst, path));
            return bunInst;
        }

        public BundleFileInstance ReadBundle2(string path)
        {
            BundleFileInstance bunInst = _appEnvironment.AssetsManager.LoadBundleFile(@path, false);
            // _appEnvironment.Dispatcher.Dispatch(new BundleReadEvent(bunInst, path));
            return bunInst;
        }

        // public async void GetBasicInfoNoTypeTree(BundleFileInstance bunInst)
        // {
        //     var progressHandler = new Progress<double>(p =>
        //     {
        //         Debug.Log($"{p * 100:F1}%: Loading file...");
        //     });
        //     MemoryStream decompressedStream = await ReadFileWithProgressAsync(TestBundlePath, progressHandler);
        //     decompressedStream.Position = 0;
        //     Debug.Log($"Stream Length: {decompressedStream.Length}");
        //     Debug.Log($"Stream Position: {decompressedStream.Position}");

        //     // Reset position before decompressing output
        //     decompressedStream.SetLength(0);
        //     decompressedStream.Position = 0;

        //     var decompressProgress = new Progress<double>(p =>
        //     {
        //         Debug.Log($"Decompressing progress: {p:P1}");
        //     });

        //     // IMPORTANT:  
        //     // bunInst.BundleStream should be the compressed input stream,  
        //     // decompressedStream is the output stream to write decompressed data into.

        //     bunInst.BundleStream.Position = 0;  // reset input stream position if needed

        //     await DecompressBundleWithProgressAsync(bunInst.BundleStream, decompressedStream, decompressProgress);

        //     decompressedStream.Position = 0;  // reset position to read decompressed data

        //     Debug.Log($"Decompressed stream length: {decompressedStream.Length}");
        //     Debug.Log($"Bundle stream length: {bunInst.BundleStream.Length}");

        //     long totalSize = bunInst.BundleStream.Length;

        //     _isLoading = true;
        //     _loadProgress = 0f;

        //     await Task.Run(() =>
        //     {
        //         Debug.Log(_isLoading);
        //         bunInst.BundleStream.Position = 0;
        //         using var progressStream = new ProgressStream(bunInst.BundleStream, (bytesRead) =>
        //         {
        //             _loadProgress = (float)bytesRead / totalSize;
        //         });

        //         var reader = new AssetsFileReader(progressStream);
        //         var newBundle = new AssetBundleFile();
        //         newBundle.Read(reader);
        //         bunInst.file = newBundle;

        //         _appEnvironment.AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
        //     });

        //     // _isLoading = false;
        //     // Debug.Log(_isLoading);
        //     // _loadProgress = 1f;

        //     // var reader = new AssetsFileReader(decompressedStream);
        //     // AssetBundleFile newBundle = new();
        //     // newBundle.Read(reader);
        //     // bunInst.file = newBundle;
        //     // AssetsFileInstance assetsFileInstance = _appEnvironment.AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);

        //     // foreach (var assetInfo in assetsFileInstance.file.AssetInfos)
        //     // {
        //     //     long pathId = assetInfo.PathId;
        //     //     int classId = assetInfo.ClassId;
        //     //     string name = GetAssetNameFast(assetsFileInstance, assetInfo);

        //     //     Debug.Log($"Asset: {name}, PathID: {pathId}, ClassID: {classId}");
        //     // }

        //     // AssetsFileInstance assetsFileInstance = _appEnvironment.AssetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
        //     // foreach(var dirInfo in bunInst.loadedAssetsFiles.)


        //     // DecompressToMemory(bunInst);

        //     // var assetsFileInst = DecompressToMemory(bunInst);
        //     // // Fast assets file load (no TypeTree)
        //     // // var assetsFileInst =  _appEnvironment.AssetsManager.LoadAssetsFile(bunInst.file, 0, false);

        //     // // Get basic info without TypeTree
        //     // foreach (var assetInfo in assetsFileInst.file.AssetInfos)
        //     // {
        //     //     long pathId = assetInfo.PathId;
        //     //     int classId = assetInfo.ClassId;
        //     //     string name =  GetAssetNameFast(assetsFileInst, assetInfo);

        //     //     UnityEngine.Debug.Log($"Asset: {name}, PathID: {pathId}, ClassID: {classId}");
        //     // }
        // }

        // private void Update()
        // {
        //     if (_isLoading)
        //     {
        //         // progressBar.value = loadProgress;  // Assuming slider/bar value is 0-1
        //         // progressLabel.text = $"Loading: {loadProgress:P0}";
        //         Debug.Log(_loadProgress);
        //     }
        // }

        public async Task<MemoryStream> ReadFileWithProgressAsync(string path, IProgress<double> progress)
        {
            const int bufferSize = 81920;
            byte[] buffer = new byte[bufferSize];

            var memStream = new MemoryStream();

            using var fs = File.OpenRead(path);
            long totalSize = fs.Length;
            long totalRead = 0;

            int bytesRead;
            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await memStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                progress.Report((double)totalRead / totalSize);
            }

            memStream.Position = 0;
            return memStream;
        }

        public async Task DecompressBundleWithProgressAsync(Stream compressedStream, Stream outputStream, IProgress<double> progress)
        {
            const int bufferSize = 81920;  // 80 KB chunks (tweakable)
            byte[] buffer = new byte[bufferSize];

            long totalSize = compressedStream.Length;
            long totalRead = 0;

            using var decompressionStream = new Lz4DecoderStream(compressedStream);

            int bytesRead;
            while ((bytesRead = await decompressionStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, bytesRead);

                totalRead = compressedStream.Position;  // Amount of compressed data consumed

                double percent = Math.Clamp((double)totalRead / totalSize, 0, 1);
                progress.Report(percent);
            }

            await outputStream.FlushAsync();
        }

        private async Task ReadBundleWithProgressAsync(Stream bundleStream, IProgress<double> progress)
        {
            const int bufferSize = 8192;  // 8 KB per read
            byte[] buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            long totalSize = bundleStream.Length;

            int bytesRead;
            while ((bytesRead = await bundleStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;

                // Update progress (value between 0 and 1)
                double percent = (double)totalBytesRead / totalSize;
                progress.Report(percent);

                // Optionally process the buffer here
            }
        }

        private void DecompressToMemory(BundleFileInstance bundleInst)
        {
            AssetBundleFile bundle = bundleInst.file;

            // * Note: unpack bundle
            MemoryStream bundleStream = new();
            bundle.Unpack(new AssetsFileWriter(bundleStream));

            bundleStream.Position = 0;

            AssetBundleFile newBundle = new();
            newBundle.Read(new AssetsFileReader(bundleStream));

            bundle.Close();
            bundleInst.file = newBundle;

            // return _appEnvironment.AssetsManager.LoadAssetsFile(bundleStream, TestBundlePath, false);
        }

        public string GetAssetNameFast(AssetsFileInstance assetsFileInst, AssetFileInfo assetInfo)
        {
            try
            {
                var baseField = _appEnvironment.AssetsManager.GetBaseField(assetsFileInst, assetInfo, readFlags: AssetReadFlags.None);
                if (baseField != null)
                {
                    var nameField = baseField.Get("m_Name");
                    if (nameField != null)
                        return nameField.AsString ?? "Unnamed Asset";
                }
            }
            catch
            {
                // Failed to get name, return fallback
            }
            return "Unnamed Asset";
        }
    }
}