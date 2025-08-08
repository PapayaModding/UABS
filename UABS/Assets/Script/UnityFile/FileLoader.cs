using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct._New;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.UnityFile
{
    public class FileLoader
    {
        public static bool _hasSetMonoTempGenerators = false;

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

        // From UABEANEXT
        public static AssetTypeTemplateField GetTemplateField(AssetsManager assetsManager,
                                                                AssetsFileInstance assetsInst,
                                                                AssetFileInfo assetFileInfo,
                                                                bool skipMonoBehaviourFields = false)
        {
            AssetReadFlags readFlags = AssetReadFlags.None;
            AssetClassID assetClassID = (AssetClassID)assetFileInfo.TypeId;
            if (skipMonoBehaviourFields && assetClassID == AssetClassID.MonoBehaviour)
            {
                readFlags |= AssetReadFlags.SkipMonoBehaviourFields | AssetReadFlags.ForceFromCldb;
            }

            return assetsManager.GetTemplateBaseField(assetsInst, assetFileInfo, readFlags);
        }

        // From UABEANEXT
        public static AssetTypeValueField? GetBaseField(AssetsManager assetsManager,
                                                        AssetsFileInstance fileInst,
                                                        AssetTypeValueField pptrField)
        {
            return GetBaseField(assetsManager, fileInst, pptrField["m_FileID"].AsInt, pptrField["m_PathID"].AsLong);
        }

        // From UABEANEXT
        public static AssetTypeValueField? GetBaseField(AssetsManager assetsManager,
                                                        AssetsFileInstance assetsInst,
                                                        AssetFileInfo assetFileInfo)
        {
            return GetBaseField(assetsManager, assetsInst, assetFileInfo.PathId);
        }

        // From UABEANEXT
        public static AssetTypeValueField? GetBaseField(AssetsManager assetsManager,
                                                        AssetsFileInstance fileInst,
                                                        long pathId)
        {
            return GetBaseField(assetsManager, fileInst, 0, pathId);
        }

        // From UABEANEXT
        public static AssetTypeValueField? GetBaseField(AssetsManager assetsManager,
                                                        AssetsFileInstance fileInst,
                                                        int fileId, long pathId)
        {
            if (fileId != 0)
            {
                fileInst = fileInst.GetDependency(assetsManager, fileId - 1);
            }
            if (fileInst == null)
            {
                return null;
            }

            AssetFileInfo? info = fileInst.file.GetAssetInfo(pathId);
            if (info == null)
            {
                return null;
            }

            CheckAndSetMonoTempGenerators(assetsManager, fileInst, info);

            // negative target platform seems to indicate an editor version
            AssetReadFlags readFlags = AssetReadFlags.None;
            if ((int)fileInst.file.Metadata.TargetPlatform < 0)
            {
                readFlags |= AssetReadFlags.PreferEditor;
            }

            try
            {
                return assetsManager.GetBaseField(fileInst, info, readFlags);
            }
            catch
            {
                return null;
            }
        }

        public static void CheckAndSetMonoTempGenerators(AssetsManager assetsManager,
                                                        AssetsFileInstance fileInst,
                                                        AssetFileInfo? info)
        {
            bool isValidMono = info == null || info.TypeId == (int)AssetClassID.MonoBehaviour || info.TypeId < 0;
            if (isValidMono && !_hasSetMonoTempGenerators && !fileInst.file.Metadata.TypeTreeEnabled)
            {
                string dataDir = PathUtils.GetAssetsFileDirectory(fileInst);
                bool success = SetMonoTempGenerators(assetsManager, dataDir);
            }
        }

        private static bool SetMonoTempGenerators(AssetsManager assetsManager, string fileDir)
        {
            _hasSetMonoTempGenerators = true;
            string managedDir = Path.Combine(fileDir, "Managed");
            if (Directory.Exists(managedDir))
            {
                bool hasDll = Directory.GetFiles(managedDir, "*.dll").Length > 0;
                if (hasDll)
                {
                    assetsManager.MonoTempGenerator = new MonoCecilTempGenerator(managedDir);
                    return true;
                }
            }

            FindCpp2IlFilesResult il2cppFiles = FindCpp2IlFiles.Find(fileDir);
            if (il2cppFiles.success && true/*ConfigurationManager.Settings.UseCpp2Il*/)
            {
                assetsManager.MonoTempGenerator = new Cpp2IlTempGenerator(il2cppFiles.metaPath, il2cppFiles.asmPath);
                return true;
            }

            return false;
        }
    }
}