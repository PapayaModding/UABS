using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.Event
{
    public class ClickFilterTypeEvent : AppEvent
    {
        public AssetClassID ClassID { get; }
        public bool IsFiltered { get; }

        public ClickFilterTypeEvent(AssetClassID classID, bool isFiltered)
        {
            ClassID = classID;
            IsFiltered = isFiltered;
        }
    }
}