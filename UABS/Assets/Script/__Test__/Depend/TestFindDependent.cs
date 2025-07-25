using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.Reader.BundlesRead;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.__Test__.TestUtil;

namespace UABS.Assets.Script.__Test__.Depend
{
    public class TestFindDependent : MonoBehaviour, ITestable
    {
        public void Test(System.Action onComplete)
        {
            string PACKAGE_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "UserPackage1");
            TestHelper.TestOnCleanLabDesk(() =>
            {
                AppEnvironment appEnvironment = new();
                DependentReader dependentReader = new(appEnvironment.Wrapper.JsonSerializer);
                string DependentCAB = "CAB-c8b157fca857626dbba75589e140a72a";

                List<string> dependentPaths = dependentReader.FindDependentPaths(DependentCAB, PACKAGE_PATH);
                List<string> expectingBundleNames = new()
                {
                    "drop.psd_aabbc0f3fd250c94537b2f88c3b61a66.bundle",
                    "rockparticles.psd_0f016160800e83e164e88f9b8a8d30a8.bundle",
                    "rockparticlesblue.psd_887b5a296151bada22bc8623dfe9af97.bundle",
                    "hookarm.png_4c49053944b0c5a6a2e9ac046bc6af11.bundle"
                };

                bool HasPathEndsWith(string p)
                {
                    foreach (string name in expectingBundleNames)
                    {
                        if (p.EndsWith(name))
                            return true;
                    }
                    return false;
                }

                int counter = 0;
                foreach (string path in dependentPaths)
                {
                    if (HasPathEndsWith(path))
                        counter++;
                }

                appEnvironment.AssetsManager.UnloadAll();

                if (counter == expectingBundleNames.Count)
                    Debug.Log("[✔] Dependent Test Succeed.");
                else
                    Debug.Log("[✘] Dependent Test Failed.");
                onComplete?.Invoke();
            });
        }
    }
}