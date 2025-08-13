using System;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.UnityFile;
using UABS.Assets.Script.DataStruct._New;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.__Test__.UnityFile
{
    public class TestGetDisplayNameFast : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            AppEnvironment appEnvironment = new();
            string owlPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Otherworld Legends\\Otherworld Legends_Data";
            string assetsFile = Path.Combine(owlPath, "resources.assets");

            FileInstanceLike fileInst = NextInstance.LoadAnyFile(appEnvironment.AssetsManager, assetsFile);
            AssetsFileInstance assetsInst = fileInst.AsAssetsFileInstance;
            NextInstance nextInstance = new(appEnvironment.AssetsManager, assetsInst);

            var assetFileInfos = assetsInst.file.AssetInfos;
            foreach (var assetFileInfo in assetFileInfos)
            {
                (string assetName, AssetClassID typeName) = nextInstance.GetDisplayNameFast(assetFileInfo);
                Debug.Log($"{assetName}, {typeName}");
            }

            onComplete?.Invoke();
        }
    }
}