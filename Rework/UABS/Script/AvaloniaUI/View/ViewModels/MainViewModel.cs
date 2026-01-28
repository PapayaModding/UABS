using UABS.Util;
using UABS.Wrapper;

namespace UABS.AvaloniaUI
{
    public class MainViewModel
    {
        public ToolbarViewModel Toolbar { get; }

        public MainViewModel(
            IFileBrowser fileBrowser
        )
        {
            Toolbar = new ToolbarViewModel(fileBrowser);
            Toolbar.FolderSelected += path =>
            {
                Log.Info($"Opened Folder: {path}.", file: "MainViewModel.cs");
            };
            Toolbar.FileSelected += path =>
            {
                Log.Info($"Opened File: {path}.", file: "MainViewModel.cs");
            };
        }
    }
}