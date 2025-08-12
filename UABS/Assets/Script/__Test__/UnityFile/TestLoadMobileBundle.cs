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
    public class TestLoadMobileBundle : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            DateTime start = DateTime.Now;

            AppEnvironment appEnvironment = new();
            appEnvironment.AssetsManager.UseRefTypeManagerCache = true;
            appEnvironment.AssetsManager.UseTemplateFieldCache = true;
            appEnvironment.AssetsManager.UseQuickLookup = true;
            
            string skPath = "D:\\Soul_Knight\\元气骑士7.4.0.11\\split_UnityDataAssetPack\\assets\\AssetBundles";
            // string assetsFile = Path.Combine(skPath, "aram_guide.ab");
            string assetsFile = Path.Combine(skPath, "common.ab");

            FileInstanceLike fileInst = NextInstance.LoadAnyFile(appEnvironment.AssetsManager, assetsFile);
            BundleFileInstance bunInst = fileInst.AsBundleFileInstace;
            // NextInstance nextInstance = new(appEnvironment.AssetsManager, bunInst);

            // var assetFileInfos = bunInst.file.AssetInfos;
            // foreach (var assetFileInfo in assetFileInfos)
            // {
            //     (string assetName, AssetClassID typeName) = nextInstance.GetDisplayNameFast(assetFileInfo);
            //     UnityEngine.Debug.Log($"{assetName}, {typeName}");
            // }

            UnityEngine.Debug.Log($"Elapsed time: {(DateTime.Now - start).TotalSeconds} seconds");

            onComplete?.Invoke();
        }
    }
}