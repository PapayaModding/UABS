using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UABS.Assets.Script.Writer.UserPackage;
using UABS.Assets.Script.Misc.AppCore;
using UABS.Assets.Script.Misc.Paths;

namespace UABS.Assets.Script.LocalController
{
    public class WriteUserPackageButton : MonoBehaviour, IAppEnvironment
    {
        private AppEnvironment _appEnvironment = null;
        public AppEnvironment AppEnvironment => _appEnvironment;
        private WriteUserPackage _writeCache;

        public void Initialize(AppEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            _writeCache = new(AppEnvironment.AssetsManager, AppEnvironment.Wrapper.JsonSerializer);
        }

        public async void ClickButton()
        {
            string[] gameDataPaths = _appEnvironment.Wrapper.FileBrowser.OpenFolderPanel("Select the game data folder (such as StandaloneWindows64)", "", false);
            string gameDataPath = "";
            if (gameDataPaths.Length <= 0)
            {
                Debug.LogWarning("Path to game data not found, abort.");
                return;
            }
            else
            {
                gameDataPath = gameDataPaths[0];
            }

            Debug.Log(gameDataPath);
            if (!Directory.Exists(PredefinedPaths.ExternalUserPackages))
            {
                Directory.CreateDirectory(PredefinedPaths.ExternalUserPackages);
            }

            string[] newSavePaths = _appEnvironment.Wrapper.FileBrowser.OpenFolderPanel("Create a new folder under UABS_Cache and select it, then continue.", PredefinedPaths.ExternalUserPackages, false);
            string newSavePath = "";
            if (newSavePaths.Length <= 0)
            {
                Debug.LogWarning("Path to new cache save not found, abort.");
                return;
            }
            else
            {
                newSavePath = newSavePaths[0];
            }
            
            await Task.Run(() => _writeCache.CreateAndSaveNewPackage(gameDataPath, newSavePath, null));
        }

        private string GetDefaultName()
        {
            int counter = 0;
            string baseName = "Default";
            string newName = baseName;
            while (Directory.Exists(Path.Combine(PredefinedPaths.ExternalUserPackages, newName)))
            {
                counter++;
                newName = $"{baseName}_{counter}";
            }
            return counter == 0 ? baseName : newName;
        }
    }
}