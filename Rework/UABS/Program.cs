using System;
using System.Threading.Tasks;
using Avalonia;
using UABS.Util;

namespace UABS.AvaloniaUI
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            Log.RemoveAllLogged();
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error("Unhandled exception: " + e.ExceptionObject, file: "Program.cs", line: 18);
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Log.Error("Unobserved task exception: " + e.Exception, file: "Program.cs", line: 23);
            };

            // ! Used for converting color in unity to Avalonia.
            // ! Not ideal but works great.
            // Comment out when you don't need.
            ConvertColorIfNeed("#00C302", 2);

            BuildAvaloniaApp()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void ConvertColorIfNeed(string unityHex, double intensity)
        {
            Log.Info($"COLOR: {unityHex} -> {ColorHelper.Saturate(unityHex, intensity)}", 
                file: "Program.cs",
                member: "ConvertColorIfNeed",
                line: 47);
        }
    }
}