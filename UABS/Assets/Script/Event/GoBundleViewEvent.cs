using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class GoBundleViewEvent : AppEvent
    {
        public List<ParsedAssetAndEntry> EntryInfos { get; }

        public GoBundleViewEvent(List<ParsedAssetAndEntry> entryInfos)
        {
            EntryInfos = entryInfos;
        }
    }
}