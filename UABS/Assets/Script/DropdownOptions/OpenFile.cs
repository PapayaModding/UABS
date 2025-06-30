using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc;
using UABS.Assets.Script.Reader;
using UABS.Assets.Script.Writer;
using UnityEngine;

namespace UABS.Assets.Script.DropdownOptions
{
    public class OpenFile : MonoBehaviour, IAppEnvironment, IDropdownButton
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        private SfbManager _sfbManager = new();

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            string filePath = _sfbManager.PickFile("Select .bundle File", "bundle");
            if (filePath == "")
            {
                Debug.Log("Couldn't find path to File.");
            }
            else
            {
                BundleReader bundleReader = new(AppEnvironment);
                // Debug.Log(filePath);
                bundleReader.ReadBundle(PathUtils.GetLongPath(filePath));
            }
        }
    }
}