using Avalonia.Controls;
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
        Opened += async (_, _) => await viewModel.InitializeAsync();
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
            finally
            {
                _closePromptActive = false;
            }
        };
        Closed += (_, _) => viewModel.Dispose();
    }
}
