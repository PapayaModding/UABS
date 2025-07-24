using UnityEngine;
using TMPro;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.UI.OptionPanel
{
    public class PickPackagePathButton : MonoBehaviour, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        [SerializeField]
        private TMP_InputField _pathField;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            string[] selections = _appEnvironment.Wrapper.FileBrowser.OpenFolderPanel("Pick a package folder.", PredefinedPaths.ExternalUserPackages, false);
            if (selections.Length <= 0)
            {
                Debug.Log("Couldn't find path to File.");
            }
            else
            {
                _pathField.text = selections[0];
            }
        }
    }
}