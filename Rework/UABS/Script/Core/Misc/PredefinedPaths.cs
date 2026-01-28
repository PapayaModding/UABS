using System;
using System.IO;

namespace UABS.Misc
{
    public class PredefinedPaths
    {
        public static readonly string LOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UABS", "logs");
    }
}