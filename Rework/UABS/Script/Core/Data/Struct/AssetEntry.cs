using System.Collections.Generic;
using System.ComponentModel;
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

        public string Name { get; } = string.Empty;
        public AssetClassID ClassID { get; } = AssetClassID.@void;
        public long PathID { get; }
        public uint UnCompressedSize { get; }
        public uint CompressedSize { get; }
        public string OriginalPath { get; } = string.Empty;
        public string CachedPath { get; } = string.Empty;
        public long FileID { get; }
        public string Memo { get; } = string.Empty;

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