using System;
using System.IO;

namespace UABS
{
    public class PredefinedPaths
    {
        public const string RESOURCES_PATH = "UABS/Resources";
        public static readonly string Icons_Path = Path.Combine(RESOURCES_PATH, "Icons");
        public static readonly string LOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UABS", "logs");
    }
}