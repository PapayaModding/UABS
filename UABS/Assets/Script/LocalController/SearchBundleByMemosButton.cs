using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.LocalController
{
    public class SearchBundleByMemosButton : MonoBehaviour, IAppEnvironment, IAppEventListener
    {
        [SerializeField]
        private TMP_InputField _searchMemosField;
        [SerializeField]
        private TMP_InputField _excludeMemosField;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        private List<string> _fullIncludedPaths = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            foreach (string path in _fullIncludedPaths)
            {
                AppEnvironment.Dispatcher.Dispatch(new RequestSearchEvent(path,
                                                                        _searchMemosField.text,
                                                                        _excludeMemosField.text,
                                                                        searchMemo: true));
            }
        }

        public void OnEvent(AppEvent e)
        {
            if (e is SearchBundleEvent sce)
            {
                Debug.Log($"Current searching package paths ({sce.IncludePaths.Count}):");
                foreach (string path in sce.IncludePaths)
                {
                    Debug.Log(path);
                }
                List<string> fullIncludedPaths = sce.IncludePaths.Select(x => GetFullPackagePath(x)).ToList();
                _fullIncludedPaths = fullIncludedPaths;
            }
        }

        private string GetFullPackagePath(string shortPath)
        {
            return Path.Combine(PredefinedPaths.ExternalUserPackages, shortPath);
        }
    }
}