using System.Collections.Generic;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.UnityFile
{

    // From UABEANEXT
    public class AssetNameUtils
    {
        public static (string, string) GetDisplayNameFast(AssetsManager assetsManager,
                                                AssetsFileInstance assetsInst,
                                                AssetFileInfo assetFileInfo)
        {
            string assetName;
            string typeName;
            bool usePrefix = true;

            ClassDatabaseFile? cldb = assetsManager.ClassDatabase;
            AssetsFile file = assetsInst.file;
            AssetsFileReader reader = assetFileInfo.IsReplacerPreviewable ?
                                    new AssetsFileReader(assetFileInfo.Replacer.GetPreviewStream()) :
                                    assetsInst.file.Reader;
            long filePosition = assetFileInfo.IsReplacerPreviewable ?
                                0 : assetFileInfo.GetAbsoluteByteOffset(file);
            int classId = assetFileInfo.TypeId;
            ushort monoId = assetFileInfo.ScriptTypeIndex;
            long pathId = assetFileInfo.PathId;

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

                        return (assetName, typeName);
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

                        return (assetName, typeName);
                    }
                    else if (typeName == "MonoBehaviour")
                    {
                        reader.Position = filePosition;
                        reader.Position += 0x1c;
                        assetName = reader.ReadCountStringInt32();
                        if (assetName == "")
                        {
                            assetName = GetMonoBehaviourNameFast(assetsManager,
                                                                assetsInst,
                                                                assetFileInfo,
                                                                reader,
                                                                filePosition);
                            if (assetName == "")
                                assetName = null;
                        }
                        return (assetName, typeName);
                    }
                    assetName = null;
                    return (assetName, typeName);
                }
            }

            ClassDatabaseType? type = cldb?.FindAssetClassByID(classId);
            if (type == null || cldb == null)
            {
                typeName = $"0x{classId:X8}";
                assetName = null;
                return (assetName, typeName);
            }

            typeName = cldb.GetString(type.Name);
            List<ClassDatabaseTypeNode> cldbNodes = type.GetPreferredNode(false).Children;

            if (cldbNodes.Count == 0)
            {
                assetName = null;
                return (assetName, typeName);
            }

            if (cldbNodes.Count > 1 && cldb.GetString(cldbNodes[0].FieldName) == "m_Name")
            {
                reader.Position = filePosition;
                assetName = reader.ReadCountStringInt32();
                if (assetName == "")
                    assetName = null;

                return (assetName, typeName);
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

                return (assetName, typeName);
            }
            else if (typeName == "MonoBehaviour")
            {
                reader.Position = filePosition;
                reader.Position += 0x1c;
                assetName = reader.ReadCountStringInt32();
                if (assetName == "")
                {
                    assetName = GetMonoBehaviourNameFast(assetsManager,
                                                        assetsInst,
                                                        assetFileInfo,
                                                        reader,
                                                        filePosition);
                    if (assetName == "")
                        assetName = null;
                }
                return (assetName, typeName);
            }
            assetName = null;
            return (assetName, typeName);
        }

        private static string GetMonoBehaviourNameFast(AssetsManager assetsManager,
                                                        AssetsFileInstance assetsInst,
                                                        AssetFileInfo assetFileInfo,
                                                        AssetsFileReader reader,
                                                        long filePosition)
        {
            AssetClassID assetClassID = (AssetClassID)assetFileInfo.TypeId;
            if (assetClassID != AssetClassID.MonoBehaviour && assetFileInfo.TypeId >= 0)
                return "";

            AssetTypeTemplateField monoTemp = FileLoader.GetTemplateField(assetsManager, assetsInst, assetFileInfo, true);
            // TODO: Speed up
            int nameIndex = monoTemp.Children.FindIndex(monoTemp => monoTemp.Name == "m_Script");
            if (nameIndex != -1)
            {
                monoTemp.Children.RemoveRange(nameIndex + 1, monoTemp.Children.Count - (nameIndex + 1));
            }

            AssetTypeValueField monoBf = monoTemp.MakeValue(reader, filePosition);
            AssetTypeValueField? scriptBaseField = FileLoader.GetBaseField(assetsManager,
                                                                        assetsInst,
                                                                        assetFileInfo);
            if (scriptBaseField == null)
                return "";

            if (scriptBaseField["m_ClassName"].IsDummy)
                return "";

            return scriptBaseField["m_ClassName"].AsString;
        }
    }
}