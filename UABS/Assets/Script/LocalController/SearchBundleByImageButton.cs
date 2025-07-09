using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.EventListener;

namespace UABS.Assets.Script.LocalController
{
    public class SearchBundleByImageButton : MonoBehaviour, IAppEnvironment, IAppEventListener
    {
        [SerializeField]
        private TMP_InputField _imagePathField;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        private List<string> _fullIncludedPaths = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            string imageName = GetImageNameFromField();
            if (string.IsNullOrWhiteSpace(imageName))
                return;
            
            // ! Only matching by name, not the actual image
            foreach (string path in _fullIncludedPaths)
            {
                AppEnvironment.Dispatcher.Dispatch(new RequestSearchEvent(path, imageName, "", true));
            }
        }

        private string GetImageNameFromField()
        {
            string path = _imagePathField.text;
            return Path.GetFileNameWithoutExtension(path);
        }

        public void OnEvent(AppEvent e)
        {
            if (e is SearchCacheEvent sce)
            {
                Debug.Log($"Current searching cache paths ({sce.IncludePaths.Count}):");
                foreach (string path in sce.IncludePaths)
                {
                    Debug.Log(path);
                }
                List<string> fullIncludedPaths = sce.IncludePaths.Select(x => GetFullCachePath(x)).ToList();
                _fullIncludedPaths = fullIncludedPaths;
            }
        }

        private string GetFullCachePath(string shortPath)
        {
            return Path.Combine(PredefinedPaths.ExternalCache, shortPath);
        }
    }
}