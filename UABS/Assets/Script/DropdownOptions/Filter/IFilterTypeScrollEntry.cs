using UnityEngine.UI;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Dispatcher;

namespace UABS.Assets.Script.DropdownOptions.Filter
{
    public interface IFilterTypeScrollEntry
    {
        public Button ManagedButton { get; }

        public AssetClassID ClassID { get; set; }

        public bool IsFiltered { get; set; }

        void AssignDispatcher(EventDispatcher dispatcher);
    }
}