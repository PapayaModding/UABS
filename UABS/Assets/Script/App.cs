using UnityEngine;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;

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
            // string PlatformFolder = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\StreamingAssets\aa\StandaloneWindows64";
            // string TestBundlePath = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\StreamingAssets\aa\StandaloneWindows64\graphiceffecttextureseparatelygroup_assets_assets\sprites\unit\unit_other_pile.psd_0678876b821c494df01ee1384bec84f2.bundle";
            // string TestBundlePath2 = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\StreamingAssets\aa\StandaloneWindows64\graphiceffecttextureseparatelygroup_assets_assets\sprites\uniteffect_0.spriteatlas_66b2db9fb94b5bda5b7794c6ed82cf3f.bundle";
            // string TestBundlePath3 = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\StreamingAssets\aa\StandaloneWindows64\spriteassetgroup_assets_assets\needdynamicloadresources\spritereference\unit_hero_quanhuying_bartender.asset_d6edc88258e9f90ae565393468c0fc94.bundle";
            // string TestBundlePath4 = @"\\?\C:\Program Files (x86)\Steam\steamapps\common\Otherworld Legends\Otherworld Legends_Data\StreamingAssets\aa\StandaloneWindows64\spriteassetgroup_assets_assets\needdynamicloadresources\spritereference\unit_hero_gangdan.asset_266134690b1c6daffbecb67815ff8868.bundle";

            // BundleReader bundleReader = new(AppEnvironment);
            // bundleReader.ReadBundle(TestBundlePath3);

            // AppEnvironment.Dispatcher.Dispatch(new FolderReadEvent(PlatformFolder));
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