using UnityEngine.UI;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.DropdownOptions.Memo
{
    public interface IMemoInheritModeEntry
    {
        public Button ManagedButton { get; }

        public bool IsSelected { get; set; }

        public MemoInheritMode MemoInheritMode { get; set; }

        void AssignDispatcher(EventDispatcher dispatcher);
    }
}