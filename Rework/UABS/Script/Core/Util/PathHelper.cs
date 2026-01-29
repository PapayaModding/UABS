using System;
using System.IO;

namespace UABS.Util
{
    public static class PathHelper
    {
        public static bool IsDirectory(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return false;

            var attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.Directory);
        }

        public static bool IsRootPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath);

            return string.Equals(
                fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                root!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}