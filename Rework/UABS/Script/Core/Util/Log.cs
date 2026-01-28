using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using UABS.Misc;

namespace UABS.Util
{
    public class Log
    {
        private static readonly string LogDir = PredefinedPaths.LOG_PATH;
        private static readonly string LogFile = Path.Combine(LogDir, "app.log");
        private static readonly Lock _lock = new();
        
        public static void Info(
            string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0
        ) => Write("INFO", message, file, member, line);
        public static void Warn(
            string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0
        ) => Write("WARN", message, file, member, line);
        public static void Error(
            string message,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0
        ) => Write("ERROR", message, file, member, line);

        private static void Write(
            string level, 
            string message, 
            string file,
            string member,
            int line)
        {
            Directory.CreateDirectory(LogDir);

            var source =
                $"{Path.GetFileNameWithoutExtension(file)}.{member}:{line}";

            var lineText =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} " +
                $"{level,-5} " +
                $"{source,-30} " +
                message +
                Environment.NewLine;

            lock (_lock)
            {
                File.AppendAllText(LogFile, lineText);
            }
        }

        public static void RotateIfNeeded()
        {
            const long maxSize = 5 * 1024 * 1024; // 5 MB

            if (!File.Exists(LogFile))
                return;

            var info = new FileInfo(LogFile);
            if (info.Length < maxSize)
                return;

            var backup = Path.Combine(LogDir, $"app_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.Move(LogFile, backup);
        }

        public static void RemoveAllLogged()
        {
            if (File.Exists(LogDir))
                File.WriteAllText(LogDir, "");
        }
    }
}