using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AssetsTools.NET.Extra;

namespace UABS.Util
{
    public static class PathHelper
    {

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetLongPathName(string shortPath, StringBuilder longPath, uint bufferLength);

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

        public static string GetRawPath(string shortPath)
        {
            if (!Directory.Exists(shortPath) && !File.Exists(shortPath))
            {
                Log.Warn("Path does not exist: " + shortPath, file: "PathHelper.cs");
                return shortPath;
            }

            StringBuilder sb = new(1024);
            uint result = GetLongPathName(shortPath, sb, (uint)sb.Capacity);

            if (result == 0)
            {
                Log.Warn("GetRawPathName failed for: " + shortPath, file: "PathHelper.cs");
                return shortPath;
            }

            return sb.ToString();
        }

        public static bool ArePathsEqual(string path1, string path2)
        {
            string fullPath1 = Path.GetFullPath(path1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            string fullPath2 = Path.GetFullPath(path2).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();

            return fullPath1 == fullPath2;
        }

        public static bool PathStartsWith(string pathA, string pathB)
        {
            string fullPathA = Path.GetFullPath(pathA).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            string fullPathB = Path.GetFullPath(pathB).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();

            return fullPathA.StartsWith(fullPathB);
        }

        public static string? RemovePrefix(string path, string prefix)
        {
            if (!prefix.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                prefix += Path.DirectorySeparatorChar;
            }
            if (path.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = path.Substring(prefix.Length);
                return relativePath;
            }
            else
            {
                Log.Warn($"{path} doesn't start with {prefix}", file: "PathHelper.cs");
                return null;
            }
        }

        public static string ToLongPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Normalize to full absolute path
            path = Path.GetFullPath(path);

            // Already long path?
            if (path.StartsWith(@"\\?\") || path.StartsWith(@"\\.\"))
                return path;

            if (path.StartsWith(@"\\"))
            {
                // UNC path (e.g. \\server\share)
                return @"\\?\UNC\" + path[2..];
            }
            else
            {
                // Local path (e.g. C:\folder\file)
                return @"\\?\" + path;
            }
        }

        public static string GetAssetsFileDirectory(AssetsFileInstance fileInst)
        {
            if (fileInst.parentBundle != null)
            {
                string dir = Path.GetDirectoryName(fileInst.parentBundle.path)!;

                // addressables
                string upDir = Path.GetDirectoryName(dir);
                string upDir2 = Path.GetDirectoryName(upDir ?? string.Empty);
                if (upDir != null && upDir2 != null)
                {
                    if (Path.GetFileName(upDir) == "aa" && Path.GetFileName(upDir2) == "StreamingAssets")
                    {
                        dir = Path.GetDirectoryName(upDir2)!;
                    }
                }

                return dir;
            }
            else
            {
                string dir = Path.GetDirectoryName(fileInst.path)!;
                if (fileInst.name == "unity default resources" || fileInst.name == "unity_builtin_extra")
                {
                    dir = Path.GetDirectoryName(dir)!;
                }
                return dir;
            }
        }
    }
}