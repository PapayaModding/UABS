using System.IO;

namespace UABS;

public class PredefinedPaths
{
    public const string UI_PATH = "UABS/UI";
    public static readonly string Icons_Path = Path.Combine(UI_PATH, "Icons");

    public const string LOG_PATH = "uabs_log.txt";
}
