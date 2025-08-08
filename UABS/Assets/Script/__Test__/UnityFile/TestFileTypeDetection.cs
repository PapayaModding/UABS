using System;
using System.IO;
using UABS.Assets.Script.__Test__.TestUtil;
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
                onComplete?.Invoke();
            });
        }
    }
}