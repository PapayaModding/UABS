using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class AssetDisplayInfoEvent : AppEvent
    {
        public ParsedAssetAndEntry EntryInfo;

        public AssetDisplayInfoEvent(ParsedAssetAndEntry entryInfo)
        {
            EntryInfo = entryInfo;
        }
    }
}