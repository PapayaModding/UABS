using System;
using System.Threading.Tasks;
using UABS.Wrapper;

namespace UABS.ViewModel
{
    public class ToolbarViewModel : ViewModelBase
    {
        private readonly IFileBrowser _fileBrowser;
        public IFileBrowser GetFileBrowser => _fileBrowser;

        public event Action<string>? FolderSelected;
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
        }

        public async Task OpenFolderAsync()
        {
            // Open the folder panel
            var folders = await _fileBrowser.OpenFolderPanelAsync("Select Folder");

            // Nothing selected? just return
            if (folders.Length == 0)
                return;

            // Notify subscribers
            FolderSelected?.Invoke(folders[0]);
        }

        public async Task OpenFileAsync()
        {
            // Title for the dialog
            string title = "Select File";

            // Call the IFileBrowser wrapper
            var files = await _fileBrowser.OpenFilePanelAsync(title);

            // Nothing selected? return
            if (files.Length == 0)
                return;

            // Notify subscribers
            FileSelected?.Invoke(files[0]);
        }
    }
}
