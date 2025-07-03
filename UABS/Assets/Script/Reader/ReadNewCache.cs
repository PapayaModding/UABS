using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Wrapper.Json;

namespace UABS.Assets.Script.Reader
{
    public class ReadNewCache
    {
        public class SpriteInfo
        {
            public string Name { get; set; }
            public long PathId { get; set; }
        }

        public class Bundle
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string CabCode { get; set; }
            public List<SpriteInfo> SpriteInfos { get; set; }
        }

        private AssetsManager _assetsManager;
        private IJsonSerializer _jsonSerializer;

        public ReadNewCache(AssetsManager assetsManager, IJsonSerializer jsonSerializer)
        {
            _assetsManager = assetsManager;
            _jsonSerializer = jsonSerializer;
        }

        public List<CacheInfo> BuildCache(string targetFolder, string cacheFolder)
        {
            List<CacheInfo> result = new();
            List<string> paths = SurfFoldersUnderAllDir(targetFolder);
            foreach (string path in paths)
            {
                string basePath = path.Replace(targetFolder, "");
                string newPath = cacheFolder + "\\" + basePath;
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
                    Bundle bundle = new()
                    {
                        Path = fileInPath,
                        Name = Path.GetFileName(fileInPath),
                        CabCode = ReadCABCode(filePath),

                        // --- Sprites ---
                        // Debug.Log(fileInPath);
                        SpriteInfos = GetSpriteInfoInBundle(filePath)
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
                if (file.EndsWith(".bundle") || file.EndsWith(".ab"))
                    result.Add(file);
            }

            return result;
        }

        private List<SpriteInfo> GetSpriteInfoInBundle(string bundlePath)
        {
            List<SpriteInfo> result = new();
            BundleFileInstance bunInst = _assetsManager.LoadBundleFile(bundlePath, true);
            AssetsFileInstance fileInst = _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);

            AssetsFile afile = fileInst.file;
            List<AssetFileInfo> spriteAssets = afile.GetAssetsOfType(AssetClassID.Sprite);

            if (spriteAssets.Count == 0)
            {
                Debug.Log("No Sprite found.");
                return null;
            }

            foreach (AssetFileInfo targetAsset in spriteAssets)
            {
                AssetTypeValueField spriteBase = _assetsManager.GetBaseField(fileInst, targetAsset);
                long pathId = long.Parse(targetAsset.PathId.ToString());
                string name = spriteBase["m_Name"].AsString;
                result.Add(new() {Name=name, PathId=pathId});
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
    }
}