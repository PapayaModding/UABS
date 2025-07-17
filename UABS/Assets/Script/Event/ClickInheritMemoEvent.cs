using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class ClickInheritMemoEvent : AppEvent
    {
        public MemoInheritMode MemoInheritMode { get; }
        public ClickInheritMemoEvent(MemoInheritMode memoInheritMode)
        {
            MemoInheritMode = memoInheritMode;
        }
    }
}