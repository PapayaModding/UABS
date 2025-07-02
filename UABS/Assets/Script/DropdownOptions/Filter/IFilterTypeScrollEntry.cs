using UnityEngine.UI;
using UABS.Assets.Script.Dispatcher;
using AssetsTools.NET.Extra;

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