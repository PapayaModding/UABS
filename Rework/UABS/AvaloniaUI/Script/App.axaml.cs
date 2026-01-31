using System;
using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using UABS.__Test__;
using UABS.App;
using UABS.Service;
using UABS.Util;
using UABS.ViewModel;
using UABS.Wrapper;

namespace UABS.AvaloniaUI
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; } = null!;
        
        public override void Initialize()
        {
            var services = new ServiceCollection();
            
            // Core / domain services
            services.AddSingleton<AssetsManagerService>();
            services.AddSingleton<FolderWindow>();
            services.AddSingleton<FileWindow>();

            // Platform services
            services.AddSingleton<IFileBrowser, AvaloniaFileBrowserWrapper>();
            services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();

            // ViewModel services
            services.AddSingleton<MainViewModel>();

            Services = services.BuildServiceProvider();

            // Save the log file to another one if the current one has grown too big (> 5MB)
            Log.RotateIfNeeded();
            Log.RemoveAllLogged();
            // FileNavigationTester.Test1();
            // FileNavigationTester.Test2();
            // FileNavigationTester.Test3();

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