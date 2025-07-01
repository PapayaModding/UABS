using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class AssetsDisplayInfoEvent : AppEvent
    {
        public List<AssetDisplayInfo> AssetsDisplayInfo { get; }

        public AssetsDisplayInfoEvent(List<AssetDisplayInfo> info)
        {
            AssetsDisplayInfo = info;
        }
    }
}