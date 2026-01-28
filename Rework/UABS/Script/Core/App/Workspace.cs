namespace UABS.App
{
    public class Workspace
    {
        private readonly FileWindow _fileWindow = new();
        private readonly FolderWindow _folderWindow = new();

        public FileWindow GetFileWindow => _fileWindow;
        public FolderWindow GetFolderWindow => _folderWindow;
    }
}