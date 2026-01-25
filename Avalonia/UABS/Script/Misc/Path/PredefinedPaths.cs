using System;
using System.IO;

namespace UABS;

public class PredefinedPaths
{
    public const string UI_PATH = "UABS/UI";
    public static readonly string Icons_Path = Path.Combine(UI_PATH, "Icons");
    public static readonly string LOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UABS", "logs");
}
