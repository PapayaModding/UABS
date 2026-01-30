using System.ComponentModel;
using UABS.App;
using UABS.Util;
using UABS.Wrapper;

namespace UABS.ViewModel
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
            FileWindow fileWindow,
            FolderWindow folderWindow,
            IFileBrowser fileBrowser
        )
        {
            FileWindowVM = new FileWindowViewModel(fileWindow);
            FolderWindowVM = new FolderWindowViewModel(folderWindow);

            ToolbarVM = new ToolbarViewModel(fileBrowser);
            ToolbarVM.FolderSelected += path =>
            {
                Log.Info($"Opened Folder: {path}.", file: "MainViewModel.cs");
                FolderWindowVM.OpenNewPath(path);
                CurrentViewModel = FolderWindowVM;
                ToolbarVM.CanExport = false;
                ToolbarVM.CanDepend = false;
                ToolbarVM.CanFilter = false;
                if (PathHelper.IsRootPath(path))
                    ToolbarVM.CanBack = false;
            };
            ToolbarVM.FileSelected += path =>
            {
                Log.Info($"Opened File: {path}.", file: "MainViewModel.cs");
                ToolbarVM.CanExport = true;
                ToolbarVM.CanDepend = true;
                ToolbarVM.CanFilter = true;
                ToolbarVM.CanBack = true;
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}