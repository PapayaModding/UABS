using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using UABS.Misc;
using UABS.Wrapper;

namespace UABS.AvaloniaUI
{
    public class ToolbarViewModel : ViewModelBase
    {
        private readonly IFileBrowser _fileBrowser;

        public ICommand OpenFolderCommand { get; }
        public event Action<string>? FolderSelected;

        public ICommand OpenFileCommand { get; }
        public event Action<string>? FileSelected;

        private bool _canExport = false;
        public bool CanExport
        {
            get => _canExport;
            set => SetProperty(ref _canExport, value);
        }

        private bool _canDepend = false;
        public bool CanDepend
        {
            get => _canDepend;
            set => SetProperty(ref _canDepend, value);
        }

        private bool _canFilter = false;
        public bool CanFilter
        {
            get => _canFilter;
            set => SetProperty(ref _canFilter, value);
        }

        private bool _canBack = false;
        public bool CanBack
        {
            get => _canBack;
            set => SetProperty(ref _canBack, value);
        }

        public ToolbarViewModel(IFileBrowser fileBrowser)
        {
            _fileBrowser = fileBrowser;

            OpenFolderCommand = new AsyncCommand<Window>(OpenFolderAsync);
            OpenFileCommand = new AsyncCommand<Window>(OpenFileAsync);
        }

        private async Task OpenFolderAsync(Window ownerWindow)
        {
            // Open the folder panel
            var folders = await _fileBrowser.OpenFolderPanelAsync(ownerWindow, "Select Folder");

            // Nothing selected? just return
            if (folders.Length == 0)
                return;

            // Notify subscribers
            FolderSelected?.Invoke(folders[0]);
        }

        private async Task OpenFileAsync(Window ownerWindow)
        {
            // Title for the dialog
            string title = "Select File";

            // Call the IFileBrowser wrapper
            var files = await _fileBrowser.OpenFilePanelAsync(ownerWindow, title);

            // Nothing selected? return
            if (files.Length == 0)
                return;

            // Notify subscribers
            FileSelected?.Invoke(files[0]);
        }
    }
}
