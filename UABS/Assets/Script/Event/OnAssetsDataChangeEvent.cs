using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class OnAssetsDataChangeEvent : AppEvent
    {
        public List<ParsedAssetAndEntry> RenderEntryInfos;

        public OnAssetsDataChangeEvent(List<ParsedAssetAndEntry> renderEntryInfos)
        {
            RenderEntryInfos = renderEntryInfos;
        }
    }
}