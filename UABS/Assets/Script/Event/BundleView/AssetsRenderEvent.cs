using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    // Inform the loader to load and render assets entries in Bundle View
    public class AssetsRenderEvent : AppEvent
    {
        public List<ParsedAssetAndEntry> RenderEntryInfos;
        public int FocusIndex;

        public AssetsRenderEvent(List<ParsedAssetAndEntry> renderEntryInfos, int focusIndex)
        {
            RenderEntryInfos = renderEntryInfos;
            FocusIndex = focusIndex;
        }
    }
}