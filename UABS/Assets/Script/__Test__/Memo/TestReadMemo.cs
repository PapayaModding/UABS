using System.IO;
using UnityEngine;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Reader.UserPackage;
using UABS.Assets.Script.__Test__.TestUtil;
using System;

namespace UABS.Assets.Script.__Test__.Memo
{
    public class TestReadMemo : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            string PACKAGE_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "UserPackage1");

            TestHelper.TestOnCleanLabDesk(() =>
            {
                AppEnvironment appEnvironment = new();
                MemoReader memoReader = new(appEnvironment);
                string BundlePath = "\\\\?\\D:\\Git\\UABS\\UABS\\Assets\\TestResources\\__DoNotOverwrite__\\GameData\\graphiceffecttextureseparatelygroup_assets_assets\\sprites\\uniteffect_0.spriteatlas_66b2db9fb94b5bda5b7794c6ed82cf3f.bundle";
                string assetName = "hook";
                string memo = "钩爪";
                string readingResult = memoReader.ReadAssetMemo(PACKAGE_PATH, BundlePath, assetName);

                appEnvironment.AssetsManager.UnloadAll();

                if (readingResult == memo)
                    Debug.Log("[✔] Memo Reader Test Succeed.");
                else
                    Debug.Log("[✘] Memo Reader Test Failed.");
                onComplete?.Invoke();
            });
        }
    }
}