using UABS.App;
using UABS.Util;
using UABS.Wrapper;

namespace UABS.AvaloniaUI
{
    public class MainViewModel
    {
        public ToolbarViewModel Toolbar { get; }
        public FileWindowViewModel FileWindow { get; }
        public FolderWindowViewModel FolderWindow { get; }

        public MainViewModel(
            Workspace workspace,
            IFileBrowser fileBrowser
        )
        {
            FileWindow = new FileWindowViewModel();
            FolderWindow = new FolderWindowViewModel(workspace.GetFolderWindow);

            Toolbar = new ToolbarViewModel(fileBrowser);
            Toolbar.FolderSelected += path =>
            {
                Log.Info($"Opened Folder: {path}.", file: "MainViewModel.cs");
                FolderWindow.Refresh(path);
            };
            Toolbar.FileSelected += path =>
            {
                Log.Info($"Opened File: {path}.", file: "MainViewModel.cs");
            };
        }
    }
}