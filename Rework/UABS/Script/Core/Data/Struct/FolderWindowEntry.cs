using System.IO;
using UABS.Util;

namespace UABS.Data
{
    public class FolderWindowEntry
    {
        public string Name { get; private set; }

        public FolderWindowType Type { get; private set; }

        public string FullPath { get; private set; }

        public string CachedPath { get; private set; }

        public FolderWindowEntry(string fullPath, string cachedPath="")
        {
            FullPath = fullPath;
            CachedPath = cachedPath;
            Name = Path.GetFileName(fullPath);
            Type = PathHelper.IsDirectory(fullPath) ? FolderWindowType.Folder : FolderWindowType.File;
        }
    }
}