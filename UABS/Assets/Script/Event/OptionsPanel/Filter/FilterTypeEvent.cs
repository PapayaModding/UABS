using System.Collections.Generic;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.Event
{
    public class FilterTypeEvent : AppEvent
    {
        public Dictionary<AssetClassID, bool> IsClassIDFiltered { get; }

        public FilterTypeEvent(Dictionary<AssetClassID, bool> isClassIDFiltered)
        {
            IsClassIDFiltered = isClassIDFiltered;
        }
    }
}