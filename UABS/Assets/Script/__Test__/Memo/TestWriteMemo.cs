using System.IO;
using UnityEngine;
using UABS.Assets.Script.__Test__.Memo;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Reader.UserPackage;
using UABS.Assets.Script.Writer.UserPackage;

namespace UABS.Assets.Script.__Test__
{
    public class TestWriteMemo : MonoBehaviour
    {
        private void Start()
        {
            Test();
        }

        private void Test()
        {
            // Need a clean build
            string PACKAGE_PATH = Path.Combine(PredefinedTestPaths.TestResPath, "Memo/TestWriteMemo/UserPackage");
            string package_meta = PACKAGE_PATH + ".meta";
            HelperMemo.SafeDeleteDirectory(PACKAGE_PATH);
            HelperMemo.SafeDeleteFile(package_meta);

            string CleanBuild_Path = Path.Combine(PredefinedTestPaths.TestResPath, "Memo/TestWriteMemo/DoNotOverride");
            string TestMemoWriter_Path = Path.Combine(PredefinedTestPaths.TestResPath, "Memo/TestWriteMemo");
            HelperMemo.CopyDirectory(CleanBuild_Path, TestMemoWriter_Path, () =>
            {
                // Test
                AppEnvironment appEnvironment = new();
                MemoWriter memoWriter = new(appEnvironment);
                string BundlePath = "\\\\?\\D:\\Git\\UABS\\UABS\\Assets\\TestResources\\Depend\\TestFindDependency\\GameData\\graphiceffecttextureseparatelygroup_assets_assets\\sprites\\uniteffect_0.spriteatlas_66b2db9fb94b5bda5b7794c6ed82cf3f.bundle";
                string assetName = "octopus_tentacles_59";
                string memo = "章鱼";
                memoWriter.WriteMemo(PACKAGE_PATH, BundlePath, assetName, memo);

                MemoReader memoReader = new(appEnvironment);
                string readingResult = memoReader.ReadAssetMemo(PACKAGE_PATH, BundlePath, assetName);
                if (readingResult == memo)
                    Debug.Log("[✔] Memo Writer Test Succeed.");
                else
                Debug.Log("[✘] Memo Writer Test Failed.");
            });
        }
    }
}