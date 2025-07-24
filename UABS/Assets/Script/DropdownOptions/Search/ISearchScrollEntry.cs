using UnityEngine.UI;
using UABS.Assets.Script.Dispatcher;

namespace UABS.Assets.Script.DropdownOptions.Search
{
    public interface ISearchScrollEntry
    {
        public Button ManagedButton { get; }

        public string ShortPath { get; set; }

        public bool IsIncluded { get; set; }

        void AssignDispatcher(EventDispatcher dispatcher);
    }
}