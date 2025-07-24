using UnityEngine;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.Misc.AppCore;

namespace UABS.Assets.Script.DropdownOptions.Opener
{
    public class OpenFolder : MonoBehaviour, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            string[] folderPaths = _appEnvironment.Wrapper.FileBrowser.OpenFolderPanel("", "", false);
            if (folderPaths.Length <= 0)
            {
                Debug.Log("Couldn't find path to Folder.");
            }
            else
            {
                AppEnvironment.Dispatcher.Dispatch(new FolderReadEvent(folderPaths[0]));
            }
        }
    }
}