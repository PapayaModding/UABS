using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class SortScrollViewEvent : AppEvent
    {
        public SortProp SortProp { get; }

        public SortScrollViewEvent(SortProp sortProp)
        {
            SortProp = sortProp;
        }
    }
}