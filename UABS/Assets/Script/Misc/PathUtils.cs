using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace UABS.Assets.Script.Misc
{
    public static class PathUtils
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetLongPathName(string shortPath, StringBuilder longPath, uint bufferLength);

        public static string GetLongPath(string shortPath)
        {
            if (!System.IO.Directory.Exists(shortPath) && !System.IO.File.Exists(shortPath))
            {
                Debug.LogWarning("Path does not exist: " + shortPath);
                return shortPath;
            }

            StringBuilder sb = new StringBuilder(1024);
            uint result = GetLongPathName(shortPath, sb, (uint)sb.Capacity);

            if (result == 0)
            {
                Debug.LogWarning("GetLongPathName failed for: " + shortPath);
                return shortPath;
            }

            return sb.ToString();
        }
    }
}