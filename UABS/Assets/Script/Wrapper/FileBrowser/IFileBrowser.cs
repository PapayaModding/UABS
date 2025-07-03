namespace UABS.Assets.Script.Wrapper.FileBrowser
{
    public interface IFileBrowser
    {
        public string[] OpenFilePanel(string title, string directory, string[] extensions, bool multiselect);
        public string[] OpenFolderPanel(string title, string directory, bool multiselect);
        public string SaveFilePanel(string title, string directory, string defaultName, string extension);
    }
}