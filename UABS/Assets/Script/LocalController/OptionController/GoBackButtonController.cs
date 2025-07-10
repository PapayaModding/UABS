using UnityEngine;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UnityEngine.UI;

namespace UABS.Assets.Script.LocalController.OptionController
{
    public class GoBackButtonController : MonoBehaviour, IAppEventListener
    {
        [SerializeField]
        private Button _goBackButton;

        public void OnEvent(AppEvent e)
        {
            if (e is ControlGoBackButtonEvent cgbb)
            {
                _goBackButton.interactable = cgbb.Enable;
            }
        }
    }
}