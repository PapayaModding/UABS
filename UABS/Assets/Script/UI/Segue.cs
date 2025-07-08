using System.IO;
using UnityEngine;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc;

namespace UABS.Assets.Script.Controller
{
    public class Segue : MonoBehaviour, IAppEnvironment, IAppEventListener
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        [SerializeField]
        private RectTransform _bundlePage;

        [SerializeField]
        private RectTransform _folderPage;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }
        public void OnEvent(AppEvent e)
        {
            if (e is FolderReadEvent fre)
            {
                if (Directory.Exists(fre.FolderPath))
                {
                    _bundlePage.gameObject.SetActive(false);
                    _folderPage.gameObject.SetActive(true);
                }
            }
            else if (e is BundleReadEvent bre)
            {
                if (File.Exists(bre.FilePath))
                {
                    _bundlePage.gameObject.SetActive(true);
                    _folderPage.gameObject.SetActive(false);
                }
            }
            else if (e is FolderRead4DeriveEvent fr4d)
            {
                if (Directory.Exists(fr4d.FolderPath))
                {
                    _bundlePage.gameObject.SetActive(false);
                    _folderPage.gameObject.SetActive(true);
                }
            }
            else if (e is BundleRead4DeriveEvent br4d)
            {
                if (File.Exists(br4d.FilePath))
                {
                    _bundlePage.gameObject.SetActive(true);
                    _folderPage.gameObject.SetActive(false);
                }
            }
        }
    }
}