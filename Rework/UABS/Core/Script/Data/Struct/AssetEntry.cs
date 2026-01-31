using System.Collections.Generic;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABS.Misc;
using UABS.Service;

namespace UABS.Data
{
    public class AssetEntry : ObservableObject
    {
        public static readonly List<string> Alternative_Row_Background_Colors = new()
        {
            "#d6ffd7",
            "#FFFFFF"
        };

        public static readonly string Selected_Row_Background_Color = "#00FF00";

        public string Name { get; set; } = string.Empty;
        public AssetClassIDService ClassID { get; set; } = new(AssetClassID.@void);
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
            set => SetProperty(ref _rowBackground, value);
        }
    }
}