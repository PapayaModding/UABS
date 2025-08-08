using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace UABS.Assets.Script.Misc.Paths
{
    public static class PathUtils
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetLongPathName(string shortPath, StringBuilder longPath, uint bufferLength);

        public static string GetRawPath(string shortPath)
        {
            if (!Directory.Exists(shortPath) && !File.Exists(shortPath))
            {
                Debug.LogWarning("Path does not exist: " + shortPath);
                return shortPath;
            }

            StringBuilder sb = new(1024);
            uint result = GetLongPathName(shortPath, sb, (uint)sb.Capacity);

            if (result == 0)
            {
                Debug.LogWarning("GetRawPathName failed for: " + shortPath);
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

        public static bool IsSystemCacheFolder(string path)
        {
            return PathStartsWith(path, PredefinedPaths.ExternalSystemDependencyCache)
                    || PathStartsWith(path, PredefinedPaths.ExternalSystemSearchCache);
        }

        public static string RemovePrefix(string path, string prefix)
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
                Debug.LogError($"{path} doesn't start with {prefix}");
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
    }
}