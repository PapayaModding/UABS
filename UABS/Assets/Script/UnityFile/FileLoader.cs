using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct._New;

namespace UABS.Assets.Script.UnityFile
{
    public class FileLoader
    {
        public static FileInstanceLike LoadAnyFile(AssetsManager assetsManager, string filePath)
        {
            DetectedFileType fileType = FileTypeDetector.DetectFileType(filePath);
            if (fileType == DetectedFileType.BundleFile)
            {
                return LoadBundle(assetsManager, filePath);
            }
            else if (fileType == DetectedFileType.AssetsFile)
            {
                return LoadAssets(assetsManager, filePath);
            }
            return null; // Don't care stream for now
        }

        private static FileInstanceLike LoadBundle(AssetsManager assetsManager, string filePath)
        {
            FileStream stream = File.OpenRead(filePath);
            BundleFileInstance bunInst = assetsManager.LoadBundleFile(stream);
            TryLoadClassDatabase(assetsManager, bunInst.file);
            return new(bunInst);
        }

        private static FileInstanceLike LoadAssets(AssetsManager assetsManager, string filePath)
        {
            FileStream stream = File.OpenRead(filePath);
            AssetsFileInstance assetsInst = assetsManager.LoadAssetsFile(stream);
            TryLoadClassDatabase(assetsManager, assetsInst.file);
            return new(assetsInst);
        }

        // From UABEANEXT
        private static void TryLoadClassDatabase(AssetsManager assetsManager, AssetBundleFile file)
        {
            if (assetsManager.ClassDatabase == null)
            {
                var fileVersion = file.Header.EngineVersion;
                if (fileVersion != "0.0.0")
                {
                    assetsManager.LoadClassDatabaseFromPackage(fileVersion);
                }
            }
        }

        // From UABEANEXT
        private static void TryLoadClassDatabase(AssetsManager assetsManager, AssetsFile file)
        {
            if (assetsManager.ClassDatabase == null)
            {
                var metadata = file.Metadata;
                var fileVersion = metadata.UnityVersion;
                if (fileVersion != "0.0.0")
                {
                    assetsManager.LoadClassDatabaseFromPackage(fileVersion);
                }
            }
        }
    }
}