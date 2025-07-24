using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.DropdownOptions.UserPackage;

namespace UABS.Assets.Script.LocalController.SystemCache
{
    // Purely visual, do not interact
    public class PackageBuildEntry : MonoBehaviour, IUserPackageScrollEntry
    {
        private EventDispatcher _dispatcher;
        private string _shortPath;
        public string ShortPath
        {
            get => _shortPath;
            set
            {
                _shortPath = value;
                _text.text = value;
            }
        }

        [SerializeField]
        private Button _button;

        public Button ManagedButton => _button;

        [SerializeField]
        private TextMeshProUGUI _text;

        public void ClickButton()
        {
            if (_dispatcher != null)
            {
                // Do nothing, display only
            }
            else
            {
                Debug.LogWarning("Event dispatcher not found. Please assign one first.");
            }
        }

        public void AssignDispatcher(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }
    }
}