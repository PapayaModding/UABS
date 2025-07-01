using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class ClickAssetEntryEvent : AppEvent
    {
        public ParsedAssetAndEntry EntryInfo;

        public ClickAssetEntryEvent(ParsedAssetAndEntry entryInfo)
        {
            EntryInfo = entryInfo;
        }
    }
}