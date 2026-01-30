using UABS.App;
using UABS.Data;
using UABS.Misc;
using UABS.Util;
using UABS.Wrapper;

namespace UABS.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private object? _currentViewModel;
        public object? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ToolbarViewModel ToolbarVM { get; }
        public FileWindowViewModel FileWindowVM { get; }
        public FolderWindowViewModel FolderWindowVM { get; }

        private AssetPreviewViewModel? _assetPreview;
        public AssetPreviewViewModel? AssetPreview
        {
            get => _assetPreview;
            set => SetProperty(ref _assetPreview, value);
        }

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
                FileWindowVM.OpenNewBundle(path);
                CurrentViewModel = FileWindowVM;
                ToolbarVM.CanExport = true;
                ToolbarVM.CanDepend = true;
                ToolbarVM.CanFilter = true;
                ToolbarVM.CanBack = true;
            };
        }

        public void UpdatePreview(AssetPreviewInfo asset)
        {
            // TODO: Rework AssetPreviewInfo to include relevant information, for now just all strings.
            AssetPreview = asset.Type switch
            {
                AssetPreviewType.Image2D => new ImagePreviewViewModel(asset.AssetPath),
                AssetPreviewType.Model3D => new ModelPreviewViewModel(asset.AssetPath),
                AssetPreviewType.Text    => new TextPreviewViewModel(asset.AssetPath),
                AssetPreviewType.Audio   => new AudioPreviewViewModel(asset.AssetPath),
                _                        => new UnknownPreviewViewModel("Unsupported type")
            };
        }
    }
}