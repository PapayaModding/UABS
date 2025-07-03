using SFB;

namespace UABS.Assets.Script.Wrapper.FileBrowser
{
    public class StandaloneFileBrowserWrapper : IFileBrowser
    {
        public string[] OpenFilePanel(string title, string directory, string[] extensions, bool multiselect)
        {
            var sfbFilters = new[]
            {
                new ExtensionFilter("Supported Files", extensions)
            };

            return StandaloneFileBrowser.OpenFilePanel(title, directory, sfbFilters, multiselect);
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect)
        {
            return StandaloneFileBrowser.OpenFolderPanel(title, directory, multiselect);
        }

        public string SaveFilePanel(string title, string directory, string defaultName, string extension)
        {
            return StandaloneFileBrowser.SaveFilePanel(title, directory, defaultName, extension);
        }
    }
}