using System;
using System.IO;
using UnityEngine;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.UnityFile;
using UABS.Assets.Script.DataStruct._New;
using AssetsTools.NET.Extra;
using AssetsTools.NET;

namespace UABS.Assets.Script.__Test__.UnityFile
{
    public class TestGetDisplayNameFast : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            AppEnvironment appEnvironment = new();
            string owlPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Otherworld Legends\\Otherworld Legends_Data";
            string assetsFile = Path.Combine(owlPath, "resources.assets");

            FileInstanceLike fileInst = FileLoader.LoadAnyFile(appEnvironment.AssetsManager, assetsFile);
            AssetsFileInstance assetsInst = fileInst.AsAssetsFileInstance;
            
            var assetFileInfos = assetsInst.file.AssetInfos;
            int count = 0;
            // while (count < 100)
            // {
                AssetFileInfo assetFileInfo = assetFileInfos[count];
                (string assetName, string typeName) = AssetNameUtils.GetDisplayNameFast(
                    appEnvironment.AssetsManager,
                    assetsInst,
                    assetFileInfo
                );
            //     Debug.Log($"{assetName}, {typeName}");
            //     count++;
            // }

            onComplete?.Invoke();
        }
    }
}