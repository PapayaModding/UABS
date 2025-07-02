using UnityEngine;
using UnityEngine.EventSystems;


namespace UABS.Assets.Script.UI
{
    public class HoverDropdown : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        public RectTransform dropdownPanel;
        private bool _isPointerInside = false;

        [SerializeField]
        private bool _shouldHideDropDown = true;

        private void Start()
        {
            dropdownPanel.gameObject.SetActive(!_shouldHideDropDown);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerInside = true;
            dropdownPanel.gameObject.SetActive(true);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            _isPointerInside = false;
            StartCoroutine(HideAfterDelay());
        }

        private System.Collections.IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(0.2f);
            if (!_isPointerInside)
                dropdownPanel.gameObject.SetActive(!_shouldHideDropDown);
        }
    }
}