using System.Linq;
using SFB;
using Unity.Mathematics;

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

            string[] result = StandaloneFileBrowser.OpenFilePanel(title, directory, sfbFilters, multiselect);
            return result.Select(x => @$"\\?\{x}").ToArray();
        }

        public string[] OpenFilePanel(string title, string directory, bool multiselect)
        {
            string[] result = StandaloneFileBrowser.OpenFilePanel(title, directory, "", multiselect);
            return result.Select(x => @$"\\?\{x}").ToArray();
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect)
        {
            string[] result = StandaloneFileBrowser.OpenFolderPanel(title, directory, multiselect);
            return result.Select(x => @$"\\?\{x}").ToArray();
        }

        public string SaveFilePanel(string title, string directory, string defaultName, string extension)
        {
            string result = StandaloneFileBrowser.SaveFilePanel(title, directory, defaultName, extension);
            return @$"\\?\{result}";
        }
    }
}