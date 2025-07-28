using System;
using System.IO;
using UnityEngine;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.__Test__.TestUtil;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Reader.BundlesRead;

namespace UABS.Assets.Script.__Test__.UABEA
{
    public class TestExportDump : MonoBehaviour, ITestable
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

                string EXPORT_DUMP_PATH = Path.Combine(PredefinedTestPaths.LabDeskPath, "Dump_Export");
                string spritereferencePath = Path.Combine(EXPORT_DUMP_PATH, "spriteassetgroup_assets_assets/needdynamicloadresources/spritereference");
                Directory.CreateDirectory(spritereferencePath);
                string firstSpriteDumpPath = Path.Combine(spritereferencePath, "unit_hero_gangdan_0-CAB-7570f5ae7807c50c425af095d0113220--452988096852721839.json");
                SingleExportJsonDump(appEnvironment.AssetsManager, assetInst, firstSpritePathID, firstSpriteDumpPath);

                appEnvironment.AssetsManager.UnloadAll();
                onComplete?.Invoke();
            });
        }

        private void SingleExportJsonDump(AssetsManager am,
                                            AssetsFileInstance assetInst,
                                            long pathID,
                                            string writeTo)
        {
            AssetFileInfo info = assetInst.file.GetAssetInfo(pathID);
            AssetTypeValueField baseField = am.GetBaseField(assetInst, info);
            AssetImportExport exporter = new();

            using (FileStream fs = File.Open(writeTo, FileMode.Create))
            using (StreamWriter sw = new(fs))
            {
                exporter.DumpJsonAsset(sw, baseField);
            }
        }
    }
}
