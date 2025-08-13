using System.Collections.Generic;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class AssetsReadEvent : AppEvent
    {
        public List<AssetsFileInstance> AssetsInsts { get; }

        public List<ParsedAssetAndEntry> ParsedAEs { get; }

        public BundleFileInstance BunInst { get; }

        public string FilePath { get; }

        public AssetsReadEvent(string filePath,
                                List<AssetsFileInstance> assetsInsts,
                                List<ParsedAssetAndEntry> parsedAEs)
        {
            FilePath = filePath;
            AssetsInsts = assetsInsts;
            ParsedAEs = parsedAEs;

            if (assetsInsts.Count > 0)
            {
                BunInst = assetsInsts[0].parentBundle;
            }
        }
    }
}