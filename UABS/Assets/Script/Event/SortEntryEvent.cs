using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class SortEntryEvent : AppEvent
    {
        public List<ParsedAssetAndEntry> EntryInfos { get; }

        public Dictionary<long, ParsedAssetAndEntry> PathID2EntryInfo
        {
            get
            {
                Dictionary<long, ParsedAssetAndEntry> result = new();
                foreach (ParsedAssetAndEntry entryInfo in EntryInfos)
                {
                    result[entryInfo.assetEntryInfo.pathID] = entryInfo;
                }
                return result;
            }
        }

        public SortEntryEvent(List<ParsedAssetAndEntry> entryInfos)
        {
            EntryInfos = entryInfos;
        }
    }
}