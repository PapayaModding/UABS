using System.Collections.Generic;
using System.IO;
using UABS.Misc;
using UABS.Util;

namespace UABS.Data
{
    public sealed class FolderWindowEntry : ObservableObject
    {
        public static readonly List<string> Alternative_Row_Background_Colors = new()
        {
            "#d6ffd7",
            "#FFFFFF"
        };

        public string Name { get; }

        public FolderWindowType Type { get; }

        public string FullPath { get; }

        public string CachedPath { get; }

        private string _rowBackground = "#FFFFFF";

        public string RowBackground
        {
            get => _rowBackground;
            set => SetProperty(ref _rowBackground, value);
        }

        public FolderWindowEntry(string fullPath, string cachedPath="")
        {
            FullPath = fullPath;
            CachedPath = cachedPath;
            Name = Path.GetFileName(fullPath);
            Type = PathHelper.IsDirectory(fullPath) ? FolderWindowType.Folder : FolderWindowType.File;
        }
    }
}