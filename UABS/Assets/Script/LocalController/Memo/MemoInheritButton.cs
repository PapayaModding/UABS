using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.DropdownOptions.Memo;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;

namespace UABS.Assets.Script.LocalController.Memo
{
    public class MemoInheritButton : MonoBehaviour, IMemoInheritModeEntry, IAppEventListener
    {
        private EventDispatcher _dispatcher;

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

        private MemoInheritMode _memoInheritMode = MemoInheritMode.Safe;
        public MemoInheritMode MemoInheritMode {
            get => _memoInheritMode;
            set
            {
                _memoInheritMode = value;
                _text.text = _memoInheritMode.ToString();
            }
        }

        public void ClickButton()
        {
            if (_dispatcher != null)
            {
                _dispatcher.Dispatch(new ClickInheritMemoEvent(MemoInheritMode));
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

        public void OnEvent(AppEvent e)
        {
            if (e is MemoInheritEvent mie)
            {
                IsSelected = mie.MemoInheritMode == MemoInheritMode;
            }
        }
    }
}