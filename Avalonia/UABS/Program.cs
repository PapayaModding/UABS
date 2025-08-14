using Avalonia;
using System;
using System.Threading.Tasks;

namespace UABS;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Printer.RemoveAllFromDefaultLogTxt();
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Printer.Print("Unhandled exception: " + e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Printer.Print("Unobserved task exception: " + e.Exception);
        };

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
}
