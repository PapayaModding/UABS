using UnityEngine;
using TMPro;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace UABS.Assets.Script.LocalController
{
    public class SearchBundleByKeywordsButton : MonoBehaviour, IAppEnvironment, IAppEventListener
    {
        [SerializeField]
        private TMP_InputField _searchKeywordsField;
        [SerializeField]
        private TMP_InputField _excludeKeywordsField;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        private List<string> _fullIncludedPaths;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            // Debug.Log(_searchKeywordsField.text);
            // Debug.Log(_excludeKeywordsField.text);
            foreach (string path in _fullIncludedPaths)
            {
                AppEnvironment.Dispatcher.Dispatch(new RequestSearchEvent(path,
                                                                        _searchKeywordsField.text,
                                                                        _excludeKeywordsField.text));
            }
        }

        public void OnEvent(AppEvent e)
        {
            if (e is SearchCacheEvent sce)
            {
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