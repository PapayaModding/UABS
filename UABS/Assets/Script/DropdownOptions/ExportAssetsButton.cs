using UnityEngine;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Misc.AppCore;

namespace UABS.Assets.Script.DropdownOptions
{
    public class ExportAssetsButton : MonoBehaviour, IAppEnvironment, IDropdownButton
    {
        [SerializeField]
        private ExportKind _exportKind;
        [SerializeField]
        private ExportType _exportType;

        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public void ClickButton()
        {
            string[] folderPaths = _appEnvironment.Wrapper.FileBrowser.OpenFolderPanel("Export asset(s) to...", "", false);
            if (folderPaths.Length <= 0)
            {
                Debug.Log("Couldn't find path to Folder.");
            }
            else
            {
                AppEnvironment.Dispatcher.Dispatch(new ExportAssetsEvent(new()
                {
                    exportKind = _exportKind,
                    exportType = _exportType,
                    destination = folderPaths[0]
                }));
            }
        }
    }
}