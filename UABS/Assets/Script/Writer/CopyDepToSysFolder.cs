using System.Collections.Generic;
using System.IO;

namespace UABS.Assets.Script.Writer.Dependency
{
    public class CopyDeriveToSysFolder
    {
        public string CopyFromPaths(List<string> bundlePaths, string systemCacheFolder)
        {
            string deriveSysFolderPath = Path.Combine(systemCacheFolder, GetPreviewFolderName(systemCacheFolder));
            if (!Directory.Exists(deriveSysFolderPath))
            {
                Directory.CreateDirectory(deriveSysFolderPath);
            }

            foreach (string bundlePath in bundlePaths)
            {
                if (!IsPathValidBundle(bundlePath))
                    continue;

                string fileName = Path.GetFileName(bundlePath);
                string targetFile = Path.Combine(deriveSysFolderPath, fileName);
                File.Copy(bundlePath, targetFile, true);
                // UnityEngine.Debug.Log($"Copied {bundlePath} to dependency preview folder in UABS system folder.");
            }

            return deriveSysFolderPath;
        }

        private string GetPreviewFolderName(string systemCacheFolder)
        {
            int counter = 0;
            string sysPath = systemCacheFolder;
            string baseName = $"{counter}";
            string searchName = Path.Combine(sysPath, baseName);
            while (Directory.Exists(searchName))
            {
                counter++;
                baseName = $"{counter}";
                searchName = Path.Combine(sysPath, $"{baseName}");
            }
            return $"{baseName}";
        }

        private bool IsPathValidBundle(string path)
        {
            string pathFolder = Path.GetDirectoryName(path);
            return Directory.Exists(pathFolder) && (Path.GetFileName(path).EndsWith(".bundle") || Path.GetFileName(path).EndsWith(".ab"));
        }
    }
}