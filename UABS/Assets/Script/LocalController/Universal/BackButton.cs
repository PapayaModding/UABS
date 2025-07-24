using UnityEngine;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc.AppCore;

namespace UABS.Assets.Script.LocalController.Universal
{
    public class BackButton : MonoBehaviour, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            AppEnvironment.Dispatcher.Dispatch(new GoBackEvent());
        }
    }
}