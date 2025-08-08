using UnityEngine;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.DropdownOptions.Opener
{
    public class OpenFile : MonoBehaviour, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            // string[] filePaths = _appEnvironment.Wrapper.FileBrowser.OpenFilePanel("Select .bundle File", "", new[] { "bundle", "ab", "assets" }, false);
            string[] filePaths = _appEnvironment.Wrapper.FileBrowser.OpenFilePanel("Select File", "", false);
            if (filePaths.Length <= 0)
            {
                Debug.Log("Couldn't find path to File.");
            }
            else
            {
                // Debug.Log(filePath);
                AppEnvironment.BundleReader.ReadBundle(PathUtils.GetLongPath(filePaths[0]));
            }
        }
    }
}