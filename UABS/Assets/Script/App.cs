using UnityEngine;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc.AppCore;

namespace UABS.Assets.Script
{
    public class App : MonoBehaviour
    {
        public static App Instance { get; private set; }
        public AppEnvironment AppEnvironment { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            AppEnvironment = new();

            InitializeAllAppEnvironment();
            RegisterListeners();
            StartCoreSystem();
        }

        private void RegisterListeners()
        {
            var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            foreach (var mb in allBehaviours)
            {
                if (mb is IAppEventListener listener)
                {
                    AppEnvironment.Dispatcher.Register(listener);
                }
            }
        }

        private void StartCoreSystem()
        {

        }

        private void InitializeAllAppEnvironment()
        {
            var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            foreach (var behaviour in allBehaviours)
            {
                if (behaviour is IAppEnvironment env)
                {
                    env.Initialize(AppEnvironment);
                }
            }
        }
    }
}