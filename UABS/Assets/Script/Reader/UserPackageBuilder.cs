using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Reader
{
    public class UserPackageBuilder
    {
        public class AssetInfo
        {
            public string Name { get; set; }
            public long PathId { get; set; }
        }

        public class Bundle
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string CabCode { get; set; }
            public List<AssetInfo> AssetInfos { get; set; }
            public List<string> Dependencies { get; set; }
        }

        private AssetsManager _assetsManager;
        private IJsonSerializer _jsonSerializer;

        public UserPackageBuilder(AssetsManager assetsManager, IJsonSerializer jsonSerializer)
        {
            _assetsManager = assetsManager;
            _jsonSerializer = jsonSerializer;
        }

        public List<UserPackageInfo> BuildPackage(string targetFolder, string packageFolder)
        {
            List<UserPackageInfo> result = new();
            List<string> paths = SurfFoldersUnderAllDir(targetFolder);
            foreach (string path in paths)
            {
                string basePath = path.Replace(targetFolder, "");
                string newPath = packageFolder + "\\" + basePath;
                // if (!Directory.Exists(newPath))
                // {
                //     Debug.Log($"Creating path {newPath}");
                //     Directory.CreateDirectory(newPath);
                // }
                List<Bundle> writeToIndex = new();
                List<string> filesInPath = SurfFilesUnderTopDir(path);
                // Create Bundle objects
                foreach (string fileInPath in filesInPath)
                {
                    string filePath = @$"{fileInPath}";
                    BundleFileInstance bunInst = _assetsManager.LoadBundleFile(fileInPath, true);
                    AssetsFileInstance fileInst = _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
                    Bundle bundle = new()
                    {
                        Path = fileInPath,
                        Name = Path.GetFileName(fileInPath),
                        CabCode = ReadCABCode(filePath),

                        // --- Sprites ---
                        // Debug.Log(fileInPath);
                        AssetInfos = GetAssetInfoOfImageAssets(fileInst),
                        Dependencies = GetDependencies(fileInst)
                    };
                    writeToIndex.Add(bundle);
                }

                string indexFile = newPath + "\\index.json";
                result.Add(new()
                {
                    path = indexFile,
                    // jsonContent = JsonConvert.SerializeObject(writeToIndex, Formatting.Indented)
                    jsonContent = _jsonSerializer.Serialize(writeToIndex, true)
                });
            }
            return result;
        }

        private List<string> GetDependencies(AssetsFileInstance fileInst)
        {
            List<string> result = new();
            List<AssetFileInfo> assets = fileInst.file.GetAssetsOfType(AssetClassID.AssetBundle);
            AssetFileInfo assetBundleInfo = assets[0];
            // Get the base field of the AssetBundle
            AssetTypeValueField baseField = _assetsManager.GetBaseField(fileInst, assetBundleInfo);

            // Navigate to m_Dependencies array
            AssetTypeValueField dependenciesArray = baseField["m_Dependencies"][0];
            int depCount = dependenciesArray.Children.Count;
            for (int i = 0; i < depCount; i++)
            {
                string dependencyCabCode = dependenciesArray[i].Value.AsString;
                result.Add(dependencyCabCode);
            }
            return result;
        }

        private List<string> SurfFoldersUnderAllDir(string directory)
        {
            List<string> result = new();
            string[] allFolders = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);

            foreach (string folder in allFolders)
            {
                result.Add(folder);
            }
            result.Add(directory);

            return result;
        }

        private List<string> SurfFilesUnderTopDir(string directory)
        {
            List<string> result = new();
            string[] allFiles = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string file in allFiles)
            {
                if (file.EndsWith(".bundle") || file.EndsWith(".ab") || file.EndsWith(".assets"))
                    result.Add(file);
            }

            return result;
        }

        private List<AssetInfo> GetAssetInfoOfImageAssets(AssetsFileInstance fileInst)
        {
            // var allAssetClassIDs = Enum.GetValues(typeof(AssetClassID)).Cast<AssetClassID>().ToArray();
            // UnityEngine.Debug.Log(allAssetClassIDs.Length);
            return GetAssetInfoInBundle(fileInst, new[] { AssetClassID.Sprite, AssetClassID.Texture2D });
            // return GetAssetInfoInBundle(fileInst, allAssetClassIDs);
        }

        private List<AssetInfo> GetAssetInfoInBundle(AssetsFileInstance fileInst, AssetClassID[] classIDs)
        {
            List<AssetInfo> result = new();
            AssetsFile afile = fileInst.file;

            // !!! Testing purpose only, remember to change back

            // foreach (AssetClassID classID in Enum.GetValues(typeof(AssetClassID)))
            foreach (AssetClassID classID in classIDs)
            {
                List<AssetFileInfo> assets = afile.GetAssetsOfType(classID);
                if (assets.Count == 0)
                {
                    UnityEngine.Debug.Log($"No {classID} found.");
                    continue;
                }

                foreach (AssetFileInfo asset in assets)
                {
                    AssetTypeValueField baseField = _assetsManager.GetBaseField(fileInst, asset);

                    string assetName = "Unnamed asset";

                    if (baseField != null)
                    {
                        var nameField = baseField.Get("m_Name");
                        if (nameField != null && !nameField.IsDummy)
                        {
                            try
                            {
                                assetName = nameField.AsString;
                            }
                            catch
                            {
                                UnityEngine.Debug.Log(GetAssetTypeValueFieldString(nameField));
                            }
                        }
                    }
                    result.Add(new()
                    {
                        Name = assetName,
                        PathId = asset.PathId
                    });
                }
            }

            return result;
        }

        private List<AssetInfo> GetAssetInfoInBundle(string bundlePath)
        {
            List<AssetInfo> result = new();
            BundleFileInstance bunInst = _assetsManager.LoadBundleFile(bundlePath, true);
            AssetsFileInstance fileInst = _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);

            List<AssetFileInfo> allAssets = fileInst.file.AssetInfos;
            foreach (var assetInfo in allAssets)
            {
                AssetTypeValueField baseField = _assetsManager.GetBaseField(fileInst, assetInfo);

                string assetName = "Unnamed asset";

                if (baseField != null)
                {
                    var nameField = baseField.Get("m_Name");
                    if (nameField != null && !nameField.IsDummy)
                    {
                        try
                        {
                            assetName = nameField.AsString;
                        }
                        catch
                        {
                            UnityEngine.Debug.Log(GetAssetTypeValueFieldString(nameField));
                        }
                    }
                }
                result.Add(new()
                {
                    Name = assetName,
                    PathId = assetInfo.PathId
                });
            }
            return result;
        }

        public static string ReadCABCode(string bundlePath)
        {
            using var fs = new FileStream(bundlePath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            byte[] buffer = new byte[fs.Length];
            br.Read(buffer, 0, buffer.Length);

            byte[] cabPrefix = Encoding.ASCII.GetBytes("CAB-");

            for (int i = 0; i <= buffer.Length - cabPrefix.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < cabPrefix.Length; j++)
                {
                    if (buffer[i + j] != cabPrefix[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    int start = i;
                    int end = start;

                    // Read until whitespace or non-printable ASCII
                    while (end < buffer.Length)
                    {
                        byte b = buffer[end];
                        if (b < 0x21 || b > 0x7E) // non-printable ASCII
                            break;
                        end++;
                    }

                    string cab = Encoding.ASCII.GetString(buffer, start, end - start - 1);
                    return cab;
                }
            }

            return "";
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