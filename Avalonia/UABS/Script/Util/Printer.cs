using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;

namespace UABS;

public static class Printer
{
    private const string DefaultLogPath = PredefinedPaths.LOG_PATH;
    private static readonly object Sync = new();

    public static void Print(string message) => Print(DefaultLogPath, message);

    public static void Print(string filePath, string message)
    {
        TryWrite(filePath, append: true, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
    }

    public static void PrintException(string context, Exception exception) =>
        Print($"{context}{Environment.NewLine}{exception}");

    public static void RemoveAllFromDefaultLogTxt() => TryWrite(DefaultLogPath, append: false, string.Empty);

    private static void TryWrite(string filePath, bool append, string contents)
    {
        try
        {
            lock (Sync)
            {
                string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                if (append)
                    File.AppendAllText(filePath, contents, Encoding.UTF8);
                else
                    File.WriteAllText(filePath, contents, Encoding.UTF8);
            }
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or ArgumentException
                                           or NotSupportedException
                                           or SecurityException)
        {
            Trace.WriteLine($"UABS logging failed for '{filePath}': {exception}");
        }
    }
}
