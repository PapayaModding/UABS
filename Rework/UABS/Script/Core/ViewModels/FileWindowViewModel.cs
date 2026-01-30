using System.Collections.Generic;
using System.Collections.ObjectModel;
using AssetsTools.NET.Extra;
using UABS.App;
using UABS.Data;
using UABS.Misc;

namespace UABS.ViewModel
{
    public class FileWindowViewModel : ObservableObject
    {
        private FileWindow FileWindow { get; }
        public ObservableCollection<AssetEntry> Assets { get; } = new();
        public ObservableCollection<AssetEntry> SelectedAssets { get; } = new();
        public IReadOnlyList<AssetsFileInstance>? AssetsInsts { get; }

        private AssetPreviewViewModel? _assetPreview;
        public AssetPreviewViewModel? AssetPreview
        {
            get => _assetPreview;
            set => SetProperty(ref _assetPreview, value);
        }

        private readonly static List<string> Alternative_Row_Background_Colors = AssetEntry.Alternative_Row_Background_Colors;
        private readonly static string Selected_Row_Background_Color = AssetEntry.Selected_Row_Background_Color;

        public FileWindowViewModel(FileWindow fileWindow)
        {
            FileWindow = fileWindow;
            FileWindow.OnNewBundleOpened += assets =>
            {
                Assets.Clear();
                for (int i = 0; i < assets.Count; i++)
                {
                    var entry = assets[i];
                    entry.RowBackground = Alternative_Row_Background_Colors[i % Alternative_Row_Background_Colors.Count];
                    if (SelectedAssets.Contains(entry))
                    {
                        entry.RowBackground = Selected_Row_Background_Color;
                    }
                    Assets.Add(entry);
                }
            };
        }

        public void OpenNewBundle(string path)
        {
            FileWindow.OpenNewBundle(path);
        }

        public void SelectAsset(AssetEntry entry)
        {
            if (!SelectedAssets.Contains(entry))
                SelectedAssets.Add(entry);
            
            UpdateRowBackgrounds();
        }

        public void DeselectAsset(AssetEntry entry)
        {
            if (SelectedAssets.Contains(entry))
                SelectedAssets.Remove(entry);
            
            UpdateRowBackgrounds();
        }

        private void UpdateRowBackgrounds()
        {
            for (int i = 0; i < Assets.Count; i++)
            {
                var entry = Assets[i];
                entry.RowBackground = Alternative_Row_Background_Colors[i % Alternative_Row_Background_Colors.Count];
                if (SelectedAssets.Contains(entry))
                {
                    entry.RowBackground = Selected_Row_Background_Color;
                }
            }
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