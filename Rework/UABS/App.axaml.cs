using System;
using Microsoft.Extensions.DependencyInjection;
using AssetsTools.NET.Extra;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using UABS.Wrapper;
using UABS.Util;
using UABS.App;
using UABS.__Test__;

namespace UABS.AvaloniaUI
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; } = null!;
        
        public override void Initialize()
        {
            var services = new ServiceCollection();
            
            // Core / domain services
            services.AddSingleton<AssetsManager>();
            services.AddSingleton<Workspace>();

            // Platform services
            services.AddSingleton<IFileBrowser, AvaloniaFileBrowserWrapper>();
            services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();

            // ViewModel services
            services.AddSingleton<MainViewModel>();

            Services = services.BuildServiceProvider();

            // Save the log file to another one if the current one has grown too big (> 5MB)
            Log.RotateIfNeeded();
            Log.RemoveAllLogged();
            FileNavigationTester.Test();

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}