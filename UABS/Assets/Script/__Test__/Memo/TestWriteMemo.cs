using System.IO;
using UnityEngine;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Writer;

namespace UABS.Assets.Script.__Test__
{
    public class TestWriteMemo : MonoBehaviour
    {
        private void Start()
        {
            Test();
        }

        private void Test()
        {
            AppEnvironment appEnvironment = new();
            MemoWriter memoWriter = new(appEnvironment);
            string PACKAGE_PATH = Path.Combine(PredefinedPaths.ExternalUserPackages, "战魂铭人2.10.0.4");

            // This is the first item in the first index.json that has AssetInfos
            string BUNDLE_PATH = "\\\\?\\C:\\Program Files (x86)\\Steam\\steamapps\\common\\Otherworld Legends\\Otherworld Legends_Data\\StreamingAssets\\aa\\StandaloneWindows64\\bodygroup_assets_all_2d25edfe2a44d351d4079093e6d8239b.bundle";
            // This is the first asset info in the above bundle
            string ASSET_NAME = "unit_hero_gangdan_hammer_soldier_7";

            string MEMO = "战士钢弹挥锤动画7";
            memoWriter.WriteMemo(PACKAGE_PATH, BUNDLE_PATH, ASSET_NAME, MEMO);
        }
    }
}