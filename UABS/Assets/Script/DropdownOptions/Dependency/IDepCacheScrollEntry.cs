using UnityEngine.UI;
using UABS.Assets.Script.Dispatcher;

namespace UABS.Assets.Script.DropdownOptions.Dependency
{
    public interface IDepCacheScrollEntry
    {
        public Button ManagedButton { get; }

        public string ShortPath { get; set; }

        void AssignDispatcher(EventDispatcher dispatcher);
    }
}