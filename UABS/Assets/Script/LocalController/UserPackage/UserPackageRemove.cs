using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc.Paths;
using UABS.Assets.Script.DropdownOptions.UserPackage;

namespace UABS.Assets.Script.LocalController.UserPackage
{
    public class UserPackageRemove : MonoBehaviour, IUserPackageScrollEntry
    {
        private string _shortPath;
        public string ShortPath { get => _shortPath;
            set
            {
                _shortPath = value;
                _text.text = value;
            } }

        [SerializeField]
        private Button _button;

        public Button ManagedButton => _button;

        [SerializeField]
        private TextMeshProUGUI _text;
        private EventDispatcher _dispatcher;

        public void ClickButton()
        {
            string fullRelPath = Path.Combine(PredefinedPaths.ExternalUserPackages, ShortPath);
            Debug.Log($"Removed cache folder '{fullRelPath}'");
            Directory.Delete(fullRelPath, true);
            _dispatcher.Dispatch(new PackageRefreshEvent());
        }

        public void AssignDispatcher(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }
    }
}