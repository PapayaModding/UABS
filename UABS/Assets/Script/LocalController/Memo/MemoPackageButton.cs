using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.DropdownOptions.Memo;
using UABS.Assets.Script.Event;

namespace UABS.Assets.Script.LocalController.Memo
{
    public class MemoPackageButton : MonoBehaviour, IMemoPackageScrollEntry
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
        private Color _notSelectedColor;

        [SerializeField]
        private Color _selectedColor;

        [SerializeField]
        private TextMeshProUGUI _text;

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                _bgImage.color = _isSelected ? _selectedColor : _notSelectedColor;
            }
        }

        public void ClickButton()
        {
            if (_dispatcher != null)
            {
                IsSelected = !IsSelected;

                _dispatcher.Dispatch(new ClickMemoPackageEvent(ShortPath, IsSelected));
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