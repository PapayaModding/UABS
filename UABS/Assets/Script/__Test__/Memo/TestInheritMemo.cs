using System.IO;
using UnityEngine;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Writer.UserPackage;
using UABS.Assets.Script.Reader.UserPackage;
using System;

namespace UABS.Assets.Script.__Test__.Memo
{
    public class TestInheritMemo : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            string FROM_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "UserPackage1");
            string TO_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "UserPackage2");

            TestHelper.TestOnCleanLabDesk(() =>
            {
                // Test
                AppEnvironment appEnvironment = new();
                MemoWriter memoWriter = new(appEnvironment);

                // 1. Write some memos in UserPackage_From
                string BundlePath = "\\\\?\\D:\\Git\\UABS\\UABS\\Assets\\TestResources\\__DoNotOverwrite__\\GameData\\graphiceffecttextureseparatelygroup_assets_assets\\sprites\\uniteffect_0.spriteatlas_66b2db9fb94b5bda5b7794c6ed82cf3f.bundle";
                string assetName = "octopus_tentacles_59";
                string memo = "章鱼";
                memoWriter.WriteMemo(FROM_PATH, BundlePath, assetName, memo);

                // 2. Inherit the Memo
                InheritMemoWriter writer = new(appEnvironment);
                writer.InheritMemoPackage(FROM_PATH, TO_PATH, DataStruct.MemoInheritMode.Safe, false);

                // 3. Expecting to read the inherited memo in UserPackage_To
                MemoReader memoReader = new(appEnvironment);
                string readingResult = memoReader.ReadAssetMemo(TO_PATH, BundlePath, assetName);
                
                appEnvironment.AssetsManager.UnloadAll();

                if (readingResult == memo)
                    Debug.Log("[✔] Inherit Memo Test Succeed.");
                else
                    Debug.Log("[✘] Inherit Memo Test Failed.");
                onComplete?.Invoke();
            });
        }
    }
}