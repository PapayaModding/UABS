using UnityEngine;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.LocalController
{
    public class FilterAllButton : MonoBehaviour, IAppEnvironment
    {
        [SerializeField, Tooltip("Set to false to enable all, true to disable them.")]
        private bool _config;

        private AppEnvironment _appEnvironment;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void ClickButton()
        {
            _appEnvironment.Dispatcher.Dispatch(new FilterAllEvent(_config));
        }

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }
    }
}