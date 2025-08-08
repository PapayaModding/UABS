using System;
using System.IO;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.Paths;
using UABS.Assets.Script.UnityFile;
using UnityEngine;

namespace UABS.Assets.Script.__Test__.UnityFile
{
    public class TestFileTypeDetection : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            TestHelper.TestOnCleanLabDesk(() =>
            {
                string abFile = Path.Combine(PredefinedTestPaths.DNO_AssetsFolder, "aigirl.ab");
                string bundleFile = Path.Combine(PredefinedTestPaths.DNO_AssetsFolder, "seebee.bundle");
                string noExtFile = Path.Combine(PredefinedTestPaths.DNO_AssetsFolder, "no_ext");
                string emptyFile = Path.Combine(PredefinedTestPaths.DNO_AssetsFolder, "empty.txt");

                string owlPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Otherworld Legends\\Otherworld Legends_Data";
                string assetsFile = Path.Combine(owlPath, "resources.assets");
                string resSFile = Path.Combine(owlPath, "resources.assets.resS");
                string resourceFile = Path.Combine(owlPath, "resources.resource");
                string globalgamemanagersFile = Path.Combine(owlPath, "globalgamemanagers");
                string level0File = Path.Combine(owlPath, "level0");

                PrintDetectionInfo(abFile);
                PrintDetectionInfo(bundleFile);
                PrintDetectionInfo(noExtFile);
                PrintDetectionInfo(emptyFile);
                PrintDetectionInfo(assetsFile);
                PrintDetectionInfo(resSFile);
                PrintDetectionInfo(resourceFile);
                PrintDetectionInfo(globalgamemanagersFile);
                PrintDetectionInfo(level0File);
                onComplete?.Invoke();
            });
        }

        public void PrintDetectionInfo(string filePath)
        {
            if (!File.Exists(filePath))
                Debug.Log($"[✘] DNE: {filePath}");
            filePath = PathUtils.ToLongPath(filePath);
            Debug.Log($"[✔] {Path.GetFileName(filePath)}: {UnityFileDetector.DetectUnityFileType(filePath)}, exists: True");
        }
    }
}