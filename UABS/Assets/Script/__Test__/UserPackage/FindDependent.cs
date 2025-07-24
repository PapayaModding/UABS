using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.Reader.BundlesRead;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.__Test__.UserPackage
{
    public class FindDependent : MonoBehaviour
    {
        private void Start()
        {
            Test();
        }

        private void Test()
        {
            string PACKAGE_PATH = Path.Combine(PredefinedPaths.ExternalUserPackages, "战魂铭人2.11.0.3max");
            AppEnvironment appEnvironment = new();
            DependentReader dependentReader = new(appEnvironment.Wrapper.JsonSerializer);
            List<string> dependentPaths = dependentReader.FindDependentPaths("CAB-fd2b4437a0d855001a014467f13d723b", PACKAGE_PATH);
            foreach (string path in dependentPaths)
            {
                Debug.Log(path);
            }
            Debug.Log("Search done");
        }
    }
}