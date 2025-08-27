using System;
using System.IO;

namespace UABS;

public class Printer
{
    private const string default_log_path = PredefinedPaths.LOG_PATH;
    public static void Print(string message)
    {
        Print(default_log_path, message);
    }

    public static void Print(string filePath, string message)
    {
        if (!File.Exists(filePath))
            File.Create(filePath);
        File.AppendAllText(filePath, $"{message} +++ at {DateTime.Now}\n");
    }

    public static void RemoveAllFromDefaultLogTxt()
    {
        if (File.Exists(default_log_path))
            File.WriteAllText(default_log_path, "");
    }
}
