using System;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Writer.UserPackage;
using UABS.Assets.Script.Reader.UserPackage;

namespace UABS.Assets.Script.__Test__.Memo
{
    public class TestInheritMemo : MonoBehaviour
    {
        private void Start()
        {
            Test();
        }

        private void Test()
        {
            // Need a clean build
            // 1. Remove existing UserPackage_From & UserPackage_To
            //    and their meta files
            string FROM_PATH = Path.Combine(PredefinedTestPaths.TestResPath, "Memo/TestInheritMemo/UserPackage_From");
            string TO_PATH = Path.Combine(PredefinedTestPaths.TestResPath, "Memo/TestInheritMemo/UserPackage_To");
            string from_meta = FROM_PATH + ".meta";
            string to_meta = TO_PATH + ".meta";
            if (Directory.Exists(FROM_PATH))
                SafeDeleteDirectory(FROM_PATH);
            if (Directory.Exists(TO_PATH))
                SafeDeleteDirectory(TO_PATH);
            if (File.Exists(from_meta))
                File.Delete(from_meta);
            if (File.Exists(to_meta))
                File.Delete(to_meta);

            // 2. Copy the user packages from clean build
            string CleanBuild_Path = Path.Combine(PredefinedTestPaths.TestResPath, "Memo/TestInheritMemo/DoNotOverride");
            string TestInheritMemo_Path = Path.Combine(PredefinedTestPaths.TestResPath, "Memo/TestInheritMemo");
            CopyDirectory(CleanBuild_Path, TestInheritMemo_Path, () =>
            {
                // Test
                AppEnvironment appEnvironment = new();
                MemoWriter memoWriter = new(appEnvironment);

                // 1. Write some memos in UserPackage_From
                string BundlePath = "\\\\?\\D:\\Git\\UABS\\UABS\\Assets\\TestResources\\Depend\\TestFindDependency\\GameData\\graphiceffecttextureseparatelygroup_assets_assets\\sprites\\uniteffect_0.spriteatlas_66b2db9fb94b5bda5b7794c6ed82cf3f.bundle";
                string assetName = "octopus_tentacles_59";
                string memo = "章鱼";
                memoWriter.WriteMemo(FROM_PATH, BundlePath, assetName, memo);

                // 2. Inherit the Memo
                InheritMemoWriter writer = new(appEnvironment);
                writer.InheritMemoPackage(FROM_PATH, TO_PATH, DataStruct.MemoInheritMode.Safe, false);

                // 3. Expecting to read the inherited memo in UserPackage_To
                MemoReader memoReader = new(appEnvironment);
                string readingResult = memoReader.ReadAssetMemo(TO_PATH, BundlePath, assetName);
                if (readingResult == memo)
                    Debug.Log("[✔] Inherit Memo Test Succeed.");
                else
                Debug.Log("[✘] Inherit Memo Test Failed.");
            });
        }

        private static void CopyDirectory(string sourceDir, string targetDir, Action onComplete)
        {
            if (!Directory.Exists(sourceDir))
            {
                Debug.Log("Source directory does not exist: " + sourceDir);
                return;
            }

            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir, null);
            }

            onComplete?.Invoke();
        }

        private void SafeDeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                // Remove read-only flags
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);

                Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete {path}: {ex.Message}");
            }
        }
    }
}