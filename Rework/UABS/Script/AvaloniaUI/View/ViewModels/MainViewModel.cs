using System.ComponentModel;
using UABS.App;
using UABS.Util;
using UABS.Wrapper;

namespace UABS.AvaloniaUI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object? _currentViewModel;
        public object? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                PropertyChanged?.Invoke(this, new(nameof(CurrentViewModel)));
            }
        }

        public ToolbarViewModel ToolbarVM { get; }
        public FileWindowViewModel FileWindowVM { get; }
        public FolderWindowViewModel FolderWindowVM { get; }

        public MainViewModel(
            Workspace workspace,
            IFileBrowser fileBrowser
        )
        {
            FileWindowVM = new FileWindowViewModel();
            FolderWindowVM = new FolderWindowViewModel(workspace.GetFolderWindow);

            ToolbarVM = new ToolbarViewModel(fileBrowser);
            ToolbarVM.FolderSelected += path =>
            {
                Log.Info($"Opened Folder: {path}.", file: "MainViewModel.cs");
                FolderWindowVM.Refresh(path);
                CurrentViewModel = FolderWindowVM;
            };
            ToolbarVM.FileSelected += path =>
            {
                Log.Info($"Opened File: {path}.", file: "MainViewModel.cs");
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}