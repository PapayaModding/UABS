using UnityEngine;
using UnityEngine.UI;
using AssetsTools.NET.Extra;
using TMPro;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.DropdownOptions.Filter;
using UABS.Assets.Script.Event;

namespace UABS.Assets.Script.LocalController
{
    public class FilterTypeButton : MonoBehaviour, IFilterTypeScrollEntry
    {
        [SerializeField]
        private Button _button;
        public Button ManagedButton => _button;

        private AssetClassID _classID;
        public AssetClassID ClassID
        {
            get => _classID;
            set
            {
                _classID = value;

                int classIdInt = (int)_classID;
                if (System.Enum.IsDefined(typeof(AssetClassID), classIdInt))
                {
                    string className = ((AssetClassID)classIdInt).ToString();
                    _text.text = className;
                }
                else
                {
                    Debug.LogWarning($"Unknown AssetClassID: {classIdInt}");
                }
            }
        }

        [SerializeField]
        private Image _bgImage;

        [SerializeField]
        private Color _notFilterColor;

        [SerializeField]
        private Color _filteredColor;

        [SerializeField]
        private TextMeshProUGUI _text;

        private bool _isFiltered = false;
        public bool IsFiltered
        {
            get => _isFiltered;
            set
            {
                _isFiltered = value;
                _bgImage.color = _isFiltered ? _filteredColor : _notFilterColor;
            }
        }

        private EventDispatcher _dispatcher;

        public void AssignDispatcher(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void ClickButton()
        {
            if (_dispatcher != null)
            {
                // Toggle the filtering state
                IsFiltered = !IsFiltered;

                // Send a new to notify others
                _dispatcher.Dispatch(new ClickFilterTypeEvent(ClassID, IsFiltered));
            }
            else
            {
                Debug.LogWarning("Event dispatcher not found. Please assign one first.");
            }
        }
    }
}