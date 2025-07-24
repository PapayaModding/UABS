using UnityEngine.UI;
using UABS.Assets.Script.Dispatcher;

namespace UABS.Assets.Script.DropdownOptions.UserPackage
{
    public interface IUserPackageScrollEntry
    {
        public Button ManagedButton { get; }

        public string ShortPath { get; set; }

        void AssignDispatcher(EventDispatcher dispatcher);
    }
}