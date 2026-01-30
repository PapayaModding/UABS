using System.Collections.Generic;
using System.ComponentModel;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UABS.Data
{
    public class AssetEntry : INotifyPropertyChanged
    {
        public static readonly List<string> Alternative_Row_Background_Colors = new()
        {
            "#d6ffd7",
            "#FFFFFF"
        };

        public static readonly string Selected_Row_Background_Color = "#00FF00";

        public string Name { get; set; } = string.Empty;
        public AssetClassID ClassID { get; set; } = AssetClassID.@void;
        public long PathID { get; set; }
        public uint UnCompressedSize { get; set; }
        public uint CompressedSize { get; set; }
        public string OriginalPath { get; set; } = string.Empty;
        public string CachedPath { get; set; } = string.Empty;
        public long FileID { get; set; }
        public string Memo { get; set; } = string.Empty;
        public AssetFileInfo? AssetFileInfo { get; set; }
        public AssetsFileInstance? AssetsInst { get; set; }

        private string _rowBackground = "#FFFFFF";

        public string RowBackground
        {
            get => _rowBackground;
            set
            {
                if (_rowBackground != value)
                {
                    _rowBackground = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBackground)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}