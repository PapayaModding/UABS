using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Reader.BundlesRead;

namespace UABS.Assets.Script.__Test__.UABEA
{
    public class TestImportDump : MonoBehaviour, ITestable
    {
        public void Test(Action onComplete)
        {
            TestHelper.TestOnCleanLabDesk(() =>
            {
                AppEnvironment appEnvironment = new();
                BundleReader bundleReader = new(appEnvironment.AssetsManager, appEnvironment.Dispatcher);
                string DATA_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "GameData");
                string bundlePath = Path.Combine(DATA_PATH, "spriteassetgroup_assets_assets/needdynamicloadresources/spritereference/unit_hero_gangdan.asset_266134690b1c6daffbecb67815ff8868.bundle");

                (BundleFileInstance _, AssetsFileInstance assetInst) = bundleReader.ReadBundle(bundlePath);
                long firstSpritePathID = -452988096852721839;
                AssetFileInfo firstSpriteInfo = GetAssetInfoByPathId(assetInst, firstSpritePathID);

                string DUMP_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "Dump");
                string firstSpriteDumpPath = Path.Combine(DUMP_PATH, "spriteassetgroup_assets_assets/needdynamicloadresources/spritereference/unit_hero_gangdan_0-CAB-7570f5ae7807c50c425af095d0113220--452988096852721839.json");
                SingleImportJsonDump(appEnvironment.AssetsManager, firstSpriteInfo, assetInst, firstSpriteDumpPath, bundlePath);

                appEnvironment.AssetsManager.UnloadAll();
                onComplete?.Invoke();
            });
        }

        private AssetFileInfo GetAssetInfoByPathId(AssetsFileInstance fileInst, long pathId)
        {
            foreach (AssetFileInfo info in fileInst.file.AssetInfos)
            {
                if (info.PathId == pathId)
                    return info;
            }
            return null;
        }

        private void SingleImportJsonDump(AssetsManager am,
                                            AssetFileInfo info,
                                            AssetsFileInstance assetInst,
                                            string replaceFilePath,
                                            string bundlePath)
        {
            using (FileStream fs = File.OpenRead(replaceFilePath))
            using (StreamReader sr = new(fs))
            {
                AssetImportExport importer = new();
                byte[] bytes = null;

                AssetTypeTemplateField tempField = am.GetTemplateBaseField(assetInst,
                                                                            assetInst.file.Reader,
                                                                            info.AbsoluteByteStart,
                                                                            info.TypeId,
                                                                            assetInst.file.GetScriptIndex(info),
                                                                            AssetReadFlags.None);
                bytes = importer.ImportJsonAsset(tempField, sr, out string exceptionMessage);

                if (bytes == null)
                {
                    Debug.LogError($"Something went wrong when reading the dump file: {exceptionMessage}");
                    return;
                }

                AssetsReplacer replacer = AssetImportExport.CreateAssetReplacer(info.PathId,
                                                                                info.TypeId,
                                                                                assetInst.file.GetScriptIndex(info),
                                                                                bytes);
                // MemoryStream newStream = new();
                // AssetsFileWriter newWriter = new(newStream);
                // replacer.Write(newWriter);
                // newStream.Position = 0;
                string outputPath = bundlePath;
                AssetsFile file = assetInst.file;
                List<AssetsReplacer> replacers = new() { replacer };

                string tempPath = bundlePath + ".tmp";

                using (FileStream outFs = File.Create(tempPath))
                using (AssetsFileWriter writer = new(outFs))
                {
                    file.Write(writer, 0, replacers, am.ClassDatabase);
                }

                // Unload the file from AssetsManager before replacing
                am.UnloadAllBundleFiles();

                File.Replace(tempPath, bundlePath, null); // replace original with modified
            }
        }
    }
}