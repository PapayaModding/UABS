using UnityEngine;
using TMPro;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Event;

namespace UABS.Assets.Script.LocalController
{
    public class SearchBundleByKeywordsButton : MonoBehaviour, IAppEnvironment
    {
        [SerializeField]
        private TMP_InputField  _searchKeywordsField;
        [SerializeField]
        private TMP_InputField  _excludeKeywordsField;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            Debug.Log(_searchKeywordsField.text);
            Debug.Log(_excludeKeywordsField.text);
            AppEnvironment.Dispatcher.Dispatch(new RequestSearchEvent(PredefinedPaths.ExternalCache,
                                                                        _searchKeywordsField.text,
                                                                        _excludeKeywordsField.text));
        }
    }
}