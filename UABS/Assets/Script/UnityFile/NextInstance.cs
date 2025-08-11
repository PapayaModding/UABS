using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct._New;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.UnityFile
{
    // For performance improvement 
    // Mostly from UABEANext, I modified a small potion
    // * GetDisplayName
    // * LoadAnyFile
    public class NextInstance
    {
        // For very fast string - classId conversion
        private static readonly Dictionary<string, AssetClassID> _classIdMap =
            Enum.GetValues(typeof(AssetClassID))
            .Cast<AssetClassID>()
            .ToDictionary(
                id => id.ToString(),
                id => id,
                StringComparer.Ordinal
        );
        
        // Just need to be set once for the (only) assets manager in the app
        private static bool _hasSetMonoTempGenerators = false;
        public static bool HasSetMonoTempGenerators => _hasSetMonoTempGenerators;
        private readonly AssetsManager _assetsManager;
        private readonly AssetsFileInstance _assetsInst;

        public NextInstance(AssetsManager assetsManager, AssetsFileInstance assetsInst)
        {
            _assetsManager = assetsManager;
            _assetsInst = assetsInst;
        }

        public (string, AssetClassID) GetDisplayNameFast(AssetFileInfo assetFileInfo)
        {
            string assetName;
            string typeName;
            bool usePrefix = true;

            ClassDatabaseFile cldb = _assetsManager.ClassDatabase;
            AssetsFile file = _assetsInst.file;
            AssetsFileReader reader = assetFileInfo.IsReplacerPreviewable ?
                                    new AssetsFileReader(assetFileInfo.Replacer.GetPreviewStream()) :
                                    _assetsInst.file.Reader;
            long filePosition = assetFileInfo.IsReplacerPreviewable ?
                                0 : assetFileInfo.GetAbsoluteByteOffset(file);
            int classId = assetFileInfo.TypeId;
            ushort monoId = assetFileInfo.ScriptTypeIndex;
            // long pathId = assetFileInfo.PathId;

            if (file.Metadata.TypeTreeEnabled)
            {
                TypeTreeType ttType;
                if (classId == 0x72 || classId < 0)
                    ttType = file.Metadata.FindTypeTreeTypeByScriptIndex(monoId);
                else
                    ttType = file.Metadata.FindTypeTreeTypeByID(classId);

                if (ttType != null && ttType.Nodes.Count > 0)
                {
                    typeName = ttType.Nodes[0].GetTypeString(ttType.StringBufferBytes);
                    if (ttType.Nodes.Count > 1 && ttType.Nodes[1].GetNameString(ttType.StringBufferBytes) == "m_Name")
                    {
                        reader.Position = filePosition;
                        assetName = reader.ReadCountStringInt32();
                        if (assetName == "")
                            assetName = null;

                        return (assetName, Str2ClassID(typeName));
                    }
                    else if (typeName == "GameObject")
                    {
                        reader.Position = filePosition;
                        int size = reader.ReadInt32();
                        int componentSize = file.Header.Version > 0x10 ? 0x0c : 0x10;
                        reader.Position += size * componentSize;
                        reader.Position += 0x04;
                        assetName = reader.ReadCountStringInt32();
                        if (usePrefix)
                            assetName = $"GameObject {assetName}";

                        return (assetName, Str2ClassID(typeName));
                    }
                    else if (typeName == "MonoBehaviour")
                    {
                        reader.Position = filePosition;
                        reader.Position += 0x1c;
                        assetName = reader.ReadCountStringInt32();
                        if (assetName == "")
                        {
                            assetName = GetMonoBehaviourNameFast(assetFileInfo,
                                                                reader,
                                                                filePosition);
                            if (assetName == "")
                                assetName = null;
                        }
                        return (assetName, Str2ClassID(typeName));
                    }
                    assetName = null;
                    return (assetName, Str2ClassID(typeName));
                }
            }

            ClassDatabaseType type = cldb?.FindAssetClassByID(classId);
            if (type == null || cldb == null)
            {
                typeName = $"0x{classId:X8}";
                assetName = null;
                return (assetName, Str2ClassID(typeName));
            }

            typeName = cldb.GetString(type.Name);
            List<ClassDatabaseTypeNode> cldbNodes = type.GetPreferredNode(false).Children;

            if (cldbNodes.Count == 0)
            {
                assetName = null;
                return (assetName, Str2ClassID(typeName));
            }

            if (cldbNodes.Count > 1 && cldb.GetString(cldbNodes[0].FieldName) == "m_Name")
            {
                reader.Position = filePosition;
                assetName = reader.ReadCountStringInt32();
                if (assetName == "")
                    assetName = null;

                return (assetName, Str2ClassID(typeName));
            }
            else if (typeName == "GameObject")
            {
                reader.Position = filePosition;
                int size = reader.ReadInt32();
                int componentSize = file.Header.Version > 0x10 ? 0x0c : 0x10;
                reader.Position += size * componentSize;
                reader.Position += 0x04;
                assetName = reader.ReadCountStringInt32();
                if (usePrefix)
                    assetName = $"GameObject {assetName}";

                return (assetName, Str2ClassID(typeName));
            }
            else if (typeName == "MonoBehaviour")
            {
                reader.Position = filePosition;
                reader.Position += 0x1c;
                assetName = reader.ReadCountStringInt32();
                if (assetName == "")
                {
                    assetName = GetMonoBehaviourNameFast(assetFileInfo,
                                                        reader,
                                                        filePosition);
                    if (assetName == "")
                        assetName = null;
                }
                return (assetName, Str2ClassID(typeName));
            }
            assetName = null;
            return (assetName, Str2ClassID(typeName));
        }

        private AssetClassID Str2ClassID(string s)
        {
            return _classIdMap.TryGetValue(s, out var id) ? id : AssetClassID.@void;
        }

        private string GetMonoBehaviourNameFast(AssetFileInfo assetFileInfo,
                                                AssetsFileReader reader,
                                                long filePosition)
        {
            AssetClassID assetClassID = (AssetClassID)assetFileInfo.TypeId;
            if (assetClassID != AssetClassID.MonoBehaviour && assetFileInfo.TypeId >= 0)
                return "";

            AssetTypeTemplateField monoTemp = GetTemplateField(_assetsManager, _assetsInst, assetFileInfo, true);

            int nameIndex = -1;
            var children = monoTemp.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].Name == "m_Script")
                {
                    nameIndex = i;
                    break;
                }
            }
            if (nameIndex != -1)
            {
                monoTemp.Children.RemoveRange(nameIndex + 1, monoTemp.Children.Count - (nameIndex + 1));
            }

            AssetTypeValueField monoBf = monoTemp.MakeValue(reader, filePosition);
            AssetTypeValueField scriptBaseField = GetBaseField(_assetsManager,
                                                                        _assetsInst,
                                                                        monoBf["m_Script"]);
            if (scriptBaseField == null)
                return "";

            if (scriptBaseField["m_ClassName"].IsDummy)
                return "";

            return scriptBaseField["m_ClassName"].AsString;
        }

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
            // FileStream stream = File.OpenRead(filePath);
            // BundleFileInstance bunInst = assetsManager.LoadBundleFile(stream);
            // TryLoadClassDatabase(assetsManager, bunInst.file);
            // return new(bunInst);
            FileStream stream = File.OpenRead(filePath);
            BundleFileInstance bunInst = assetsManager.LoadBundleFile(stream);
            TryLoadClassDatabase(assetsManager, bunInst.file);

            // Load all .assets files in the bundle
            var dirInfos = bunInst.file.BlockAndDirInfo.DirectoryInfos;

            for (int i = 0; i < dirInfos.Count; i++)
            {
                var dirInfo = dirInfos[i];
                if (dirInfo.Name.EndsWith(".assets", StringComparison.OrdinalIgnoreCase) ||
                    dirInfo.Name.StartsWith("CAB-", StringComparison.OrdinalIgnoreCase))
                {
                    assetsManager.LoadAssetsFileFromBundle(bunInst, i, false);
                }
            }

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
        public AssetTypeTemplateField GetTemplateField(AssetsManager assetsManager,
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
        public AssetTypeValueField GetBaseField(AssetsManager assetsManager,
                                                        AssetsFileInstance fileInst,
                                                        AssetTypeValueField pptrField)
        {
            return GetBaseField(assetsManager, fileInst, pptrField["m_FileID"].AsInt, pptrField["m_PathID"].AsLong);
        }

        // From UABEANEXT
        public AssetTypeValueField GetBaseField(AssetsManager assetsManager,
                                                        AssetsFileInstance assetsInst,
                                                        AssetFileInfo assetFileInfo)
        {
            return GetBaseField(assetsManager, assetsInst, assetFileInfo.PathId);
        }

        // From UABEANEXT
        public AssetTypeValueField GetBaseField(AssetsManager assetsManager,
                                                        AssetsFileInstance fileInst,
                                                        long pathId)
        {
            return GetBaseField(assetsManager, fileInst, 0, pathId);
        }

        // From UABEANEXT
        public AssetTypeValueField GetBaseField(AssetsManager assetsManager,
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

            AssetFileInfo info = fileInst.file.GetAssetInfo(pathId);
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

        public void CheckAndSetMonoTempGenerators(AssetsManager assetsManager,
                                                        AssetsFileInstance fileInst,
                                                        AssetFileInfo info)
        {
            bool isValidMono = info == null || info.TypeId == (int)AssetClassID.MonoBehaviour || info.TypeId < 0;
            if (isValidMono && !_hasSetMonoTempGenerators && !fileInst.file.Metadata.TypeTreeEnabled)
            {
                string dataDir = PathUtils.GetAssetsFileDirectory(fileInst);
                SetMonoTempGenerators(assetsManager, dataDir);
            }
        }

        private bool SetMonoTempGenerators(AssetsManager assetsManager, string fileDir)
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