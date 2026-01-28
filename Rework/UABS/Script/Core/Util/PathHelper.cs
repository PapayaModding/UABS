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
    }
}