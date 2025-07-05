using System;
using System.Collections.Generic;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Reader
{
    public class ReadFolderContent
    {
        private AssetsManager _assetsManager;

        public ReadFolderContent(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public List<FolderViewInfo> ReadAllReadable(string directory)
        {
            List<FolderViewInfo> result = new();
            result.AddRange(ReadAllTopDirectory(directory));
            result.AddRange(ReadAllTopBundle(directory));
            return result;
        }

        public List<FolderViewInfo> ReadAllTopDirectory(string directory)
        {
            List<FolderViewInfo> result = new();
            string[] allFolders = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

            foreach (string folder in allFolders)
            {
                result.Add(new()
                {
                    name = Path.GetFileName(folder),
                    path = folder,
                    type = FolderViewType.Folder
                });
            }
            return result;
        }

        public List<FolderViewInfo> ReadAllTopBundle(string directory)
        {
            List<FolderViewInfo> result = new();
            string[] allFiles = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string file in allFiles)
            {
                if (IsExtension(file, ".bundle") || IsExtension(file, ".ab"))
                    result.Add(new()
                    {
                        name = Path.GetFileName(file),
                        path = file,
                        type = FolderViewType.Bundle,
                        size = GetBundleSize(file)
                    });
            }

            return result;
        }

        private long GetBundleSize(string filePath)
        {
            BundleFileInstance bunInst = ReadBundleNoTypeTree(filePath);
            return GetUncompressedBundleSize(bunInst);
        }

        private BundleFileInstance ReadBundleNoTypeTree(string path)
        {
            BundleFileInstance bunInst = _assetsManager.LoadBundleFile(@path, false);
            // _appEnvironment.Dispatcher.Dispatch(new BundleReadEvent(bunInst, path));
            return bunInst;
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

        private static bool IsExtension(string filePath, string expectedExtension)
        {
            // Compares extensions case-insensitively
            return string.Equals(Path.GetExtension(filePath), expectedExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}