using Avalonia.Controls;
using System;
using UABS.Services;
using UABS.UnityAssets;
using UABS.ViewModels;

namespace UABS;

// [UNITY-DEPENDENCY:REPLACEMENT-REQUIRED]
// Finding: UE-005
// Legacy source: Assets/Script/App.cs, App.Awake/InitializeAllAppEnvironment/RegisterListeners
// Unity API/plugin: UnityEngine.MonoBehaviour, FindObjectsOfType, GameObject lifecycle
// Purpose: Compose application services, initialize UI components, and manage application lifetime.
// Migration status: REPLACED
// Replacement: Avalonia window composition plus explicit ViewModel initialization and disposal; verified by build/tests and UI smoke launch.
public partial class MainWindow : Window
{
    private bool _allowClose;
    private bool _closePromptActive;

    public MainWindow()
    {
        InitializeComponent();
        var confirmations = new AvaloniaUserConfirmationService(this);
        var viewModel = new MainWindowViewModel(
            new AvaloniaFilePickerService(this),
            new AssetsToolsAssetSessionFactory(),
            new DirectoryBrowserService(),
            userConfirmationService: confirmations);
        DataContext = viewModel;
        Opened += async (_, _) =>
        {
            try
            {
                await viewModel.InitializeAsync();
            }
            catch (Exception exception)
            {
                Printer.PrintException("Main window initialization failed.", exception);
                viewModel.ReportUnexpectedFailure("Application initialization failed", exception);
            }
        };
        Closing += async (_, args) =>
        {
            if (_allowClose || !viewModel.Packages.Editor.IsDirty)
                return;

            args.Cancel = true;
            if (_closePromptActive)
                return;

            _closePromptActive = true;
            try
            {
                if (await viewModel.ConfirmCloseAsync())
                {
                    _allowClose = true;
                    Close();
                }
            }
            catch (OperationCanceledException)
            {
                args.Cancel = true;
            }
            catch (Exception exception)
            {
                args.Cancel = true;
                Printer.PrintException("Close confirmation failed.", exception);
                viewModel.ReportUnexpectedFailure("Close confirmation failed", exception);
            }
            finally
            {
                _closePromptActive = false;
            }
        };
        Closed += (_, _) =>
        {
            try
            {
                viewModel.Dispose();
            }
            catch (Exception exception)
            {
                Printer.PrintException("Application shutdown cleanup failed.", exception);
            }
        };
    }
}
