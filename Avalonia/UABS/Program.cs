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
            if (e.ExceptionObject is Exception exception)
                Printer.PrintException("Unhandled application exception.", exception);
            else
                Printer.Print("Unhandled application exception object: " + e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Printer.PrintException("Unobserved task exception.", e.Exception);
            e.SetObserved();
        };

        // ! Used for converting color in unity to Avalonia.
        // ! Not ideal but works great.
        // Comment out when you don't need.
        // ConvertColorIfYouNeed("#00C302", 2);

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

    private static void ConvertColorIfYouNeed(string unityHex, double intensity)
    {
        Printer.Print($"COLOR: {unityHex} -> {ColorHelper.Saturate(unityHex, intensity)}");
    }
}
