using System.Collections.Generic;
using System.IO;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.Writer.Dependency
{
    public class CopyDepToSysFolder
    {
        public string CopyFromPaths(List<string> bundlePaths)
        {
            string depSysFolderPath = Path.Combine(PredefinedPaths.ExternalSystemDeriveCache, GetPreviewFolderName());
            if (!Directory.Exists(depSysFolderPath))
            {
                Directory.CreateDirectory(depSysFolderPath);
            }

            foreach (string bundlePath in bundlePaths)
            {
                if (!IsPathValidBundle(bundlePath))
                    continue;

                string fileName = Path.GetFileName(bundlePath);
                string targetFile = Path.Combine(depSysFolderPath, fileName);
                File.Copy(bundlePath, targetFile, true);
                UnityEngine.Debug.Log($"Copied {bundlePath} to dependency preview folder in UABS system folder.");
            }

            return depSysFolderPath;
        }

        private string GetPreviewFolderName()
        {
            int counter = 0;
            string sysPath = PredefinedPaths.ExternalSystemDeriveCache;
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