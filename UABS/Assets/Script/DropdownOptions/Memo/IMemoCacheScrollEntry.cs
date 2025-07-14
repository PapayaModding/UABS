using UnityEngine.UI;
using UABS.Assets.Script.Dispatcher;

namespace UABS.Assets.Script.DropdownOptions.Memo
{
    public interface IMemoCacheScrollEntry
    {
        public Button ManagedButton { get; }

        public string ShortPath { get; set; }

        public bool IsSelected { get; set; }

        void AssignDispatcher(EventDispatcher dispatcher);
    }
}