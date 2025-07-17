using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class MemoInheritEvent : AppEvent
    {
        public MemoInheritMode MemoInheritMode { get; }
        
        public MemoInheritEvent(MemoInheritMode memoInheritMode)
        {
            MemoInheritMode = memoInheritMode;
        }
    }
}