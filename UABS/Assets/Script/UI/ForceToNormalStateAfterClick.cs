using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UABS.Assets.Script.UI
{
    public class ForceToNormalStateAfterClick : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        public void ResetButtonVisuals()
        {
            // Force "exit" to reset button's internal state
            ExecuteEvents.Execute<IPointerExitHandler>(_button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);

            // Optionally clear selection to prevent highlight stickiness
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}