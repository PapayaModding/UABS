using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.DropdownOptions.Search;
using UABS.Assets.Script.Event;

namespace UABS.Assets.Script.LocalController
{
    public class SearchCacheButton : MonoBehaviour, ISearchCacheScrollEntry
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
        private Image _bgImage;

        [SerializeField]
        private Color _notIncludeColor;

        [SerializeField]
        private Color _includedColor;

        [SerializeField]
        private TextMeshProUGUI _text;

        private bool _isIncluded = false;
        public bool IsIncluded
        {
            get => _isIncluded;
            set
            {
                _isIncluded = value;
                _bgImage.color = _isIncluded ? _includedColor : _notIncludeColor;
            }
        }

        public void ClickButton()
        {
            if (_dispatcher != null)
            {
                IsIncluded = !IsIncluded;

                _dispatcher.Dispatch(new ClickSearchCacheEvent(ShortPath, IsIncluded));
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