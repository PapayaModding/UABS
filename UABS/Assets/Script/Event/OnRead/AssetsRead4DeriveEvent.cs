using System.Collections.Generic;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    // Bundles in dependency folder / search folder
    public class AssetsRead4DeriveEvent : AppEvent
    {
        public List<AssetsFileInstance> AssetsInsts { get; }

        public List<ParsedAssetAndEntry> ParsedAEs { get; }

        public BundleFileInstance BunInst { get; }

        public string FilePath { get; }
        public string OverridePath { get; }

        public AssetsRead4DeriveEvent(string filePath, string overridePath,
                                        List<AssetsFileInstance> assetsInsts,
                                        List<ParsedAssetAndEntry> parsedAEs)
        {
            FilePath = filePath;
            OverridePath = overridePath;
            AssetsInsts = assetsInsts;
            ParsedAEs = parsedAEs;
            
            if (assetsInsts.Count > 0)
            {
                BunInst = assetsInsts[0].parentBundle;
            }
        }
    }
}