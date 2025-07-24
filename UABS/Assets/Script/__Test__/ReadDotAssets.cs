using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.__Test__
{
    public class ReadDotAssets : MonoBehaviour
    {
        public const string TestAssetsPath = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\resources.assets";

        private void Start()
        {
            Test();
        }

        private void Test()
        {
            AppEnvironment appEnvironment = new();
            AssetsManager assetsManager = appEnvironment.AssetsManager;
            assetsManager.LoadClassPackage(PredefinedPaths.ClassDataPath);
            AssetsFileInstance assetsInst = appEnvironment.AssetsManager.LoadAssetsFile(TestAssetsPath, true);
            string uVer = assetsInst.file.Metadata.UnityVersion;
            assetsManager.LoadClassDatabaseFromPackage(uVer);
            List<AssetFileInfo> assetFileInfos = assetsInst.file.AssetInfos;
            // foreach (AssetFileInfo assetFileInfo in assetFileInfos)
            // {
            //     var baseField = assetsManager.GetBaseField(assetsInst, assetFileInfo);
            // }
            AssetFileInfo assetFileInfo = assetFileInfos[0];
            var baseField = assetsManager.GetBaseField(assetsInst, assetFileInfo);
            Debug.Log(GetAssetTypeValueFieldString(baseField));
        }
        
        // ! For debug
        public static string GetAssetTypeValueFieldString(AssetTypeValueField field, int indentLevel = 0)
        {
            if (field == null) return "<null>";

            StringBuilder sb = new();
            string indent = new(' ', indentLevel * 2);

            // Field name and type
            sb.Append(indent);
            sb.Append(field.FieldName);
            sb.Append(" (");
            sb.Append(field.TypeName);
            sb.Append(")");

            // Field value (if any)
            if (field.Value != null)
            {
                sb.Append(" : ");
                sb.Append(field.Value);
            }
            sb.AppendLine();

            // Recursively append children
            if (field.Children != null)
            {
                foreach (var child in field.Children)
                {
                    sb.Append(GetAssetTypeValueFieldString(child, indentLevel + 1));
                }
            }

            return sb.ToString();
        }
    }
}