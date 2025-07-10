using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UABS.Assets.Script.UI;

namespace UABS.Assets.Script.LocalController.OptionController
{
    public class OptionButtonController : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private TextMeshProUGUI _buttonText;

        [SerializeField]
        private HoverDropdown _hoverDropdown;

        [SerializeField]
        private Color _enableTextColor;

        [SerializeField]
        private Color _disableTextColor;

        private void OnDisable()
        {
            if (_button != null)
                _button.interactable = false;
            if (_buttonText != null)
                _buttonText.color = _disableTextColor;
            if (_hoverDropdown != null)
                _hoverDropdown.enabled = false;
        }

        private void OnEnable()
        {
            if (_button != null)
                _button.interactable = true;
            if (_buttonText != null)
                _buttonText.color = _enableTextColor;
            if (_hoverDropdown != null)
                _hoverDropdown.enabled = true;
        }
    }
}