using System.IO;
using UABS.Util;

namespace UABS.Data
{
    public class FolderWindowEntry
    {
        public string Name { get; }

        public FolderWindowType Type { get; }

        public string FullPath { get; }

        public string CachedPath { get; }

        public string RowBackground { get; set; } = "#FFFFFF";

        public FolderWindowEntry(string fullPath, string cachedPath="")
        {
            FullPath = fullPath;
            CachedPath = cachedPath;
            Name = Path.GetFileName(fullPath);
            Type = PathHelper.IsDirectory(fullPath) ? FolderWindowType.Folder : FolderWindowType.File;
        }
    }
}