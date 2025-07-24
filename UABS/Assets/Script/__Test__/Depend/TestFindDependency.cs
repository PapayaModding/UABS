using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Reader.BundlesRead;
using UABS.Assets.Script.Reader.Search;

namespace UABS.Assets.Script.__Test__.Depend
{
    public class TestFindDependency : MonoBehaviour
    {
        private void Start()
        {
            Test();
        }

        private void Test()
        {
            AppEnvironment appEnvironment = new();
            ReadDependencyInfo readDependencyInfo = new(appEnvironment.AssetsManager,
                                                        appEnvironment.Wrapper.JsonSerializer);
            BundleReader bundleReader = new(appEnvironment.AssetsManager, appEnvironment.Dispatcher);

            string PACKAGE_PATH = Path.Combine(PredefinedTestPaths.TestResPath, "Depend/TestFindDependency/UserPackage");
            string DATA_PATH = Path.Combine(PredefinedTestPaths.TestResPath, "Depend/TestFindDependency/GameData");
            string dependentPath = Path.Combine(DATA_PATH, "graphiceffecttextureseparatelygroup_assets_assets/sprites/uniteffect_0.spriteatlas_66b2db9fb94b5bda5b7794c6ed82cf3f.bundle");

            (BundleFileInstance bunInst, AssetsFileInstance _) = bundleReader.ReadBundle(dependentPath);
            List<DeriveInfo> deriveInfos = readDependencyInfo.ReadInfoFor(bunInst, PACKAGE_PATH, false);

            List<string> expectingDependencies = new(){
                "cab-8aec1fae84b6a213e9d9089f91bdc9c1",
                "cab-ab9abde196f8584c62c6b015421e254e",
                "cab-c0e651c84145c1ae8e19e2c9b991bec8",
                "cab-c4ab7132a743d96843c845eb90f25572"
            };
            int counter = 0;
            foreach (var deriveInfo in deriveInfos)
            {
                if (expectingDependencies.Contains(deriveInfo.cabCode))
                {
                    counter++;
                }
            }

            if (counter == expectingDependencies.Count)
                Debug.Log("[✔] Dependency Test Succeed.");
            else
                Debug.LogError("[✘] Failed to pass Dependency Test.");
        }
    }
}