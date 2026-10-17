using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UABS.Services;
using UABS.UnityAssets;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Dependencies;
using UABS.UnityAssets.Packages.Search;

namespace UABS.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IFilePickerService _filePicker;
    private readonly IAssetSessionFactory _sessionFactory;
    private readonly CancellationTokenSource _lifetime = new();
    private IAssetSession? _session;
    private string _activeFilePath = "No file loaded";
    private string _statusText = "Choose a Unity asset file or bundle to inspect.";
    private string? _errorMessage;
    private bool _isLoading;
    private int _selectedWorkspaceIndex;
    private bool _disposed;

    public MainWindowViewModel(
        IFilePickerService filePicker,
        IAssetSessionFactory sessionFactory,
        IDirectoryBrowserService? directoryBrowserService = null,
        string? initialPath = null,
        IPackageService? packageService = null,
        IPackageSearchService? packageSearchService = null,
        IPackageDependencyResolver? packageDependencyResolver = null,
        IUserConfirmationService? userConfirmationService = null)
    {
        _filePicker = filePicker;
        _sessionFactory = sessionFactory;
        AssetList = new AssetListViewModel();
        Browser = new BrowserViewModel(
            directoryBrowserService ?? new DirectoryBrowserService(),
            filePicker,
            LoadFileAsync,
            initialPath);
        Packages = new PackageManagementViewModel(
            filePicker,
            packageService ?? new PackageService(),
            packageSearchService ?? new PackageSearchService(),
            packageDependencyResolver ?? new PackageDependencyResolver(),
            userConfirmationService);
        OpenFileCommand = new AsyncCommand(OpenFromPickerAsync);
        ShowAssetsCommand = new RelayCommand(() => SelectedWorkspaceIndex = 0);
        ShowPackagesCommand = new RelayCommand(() => SelectedWorkspaceIndex = 1);
        AddSelectedAssetToPackageCommand = new AsyncCommand(AddSelectedAssetToPackageAsync);
    }

    public BrowserViewModel Browser { get; }
    public AssetListViewModel AssetList { get; }
    public PackageManagementViewModel Packages { get; }
    public IReadOnlyList<AssetEntry> Assets => AssetList.Assets;
    public AsyncCommand OpenFileCommand { get; }
    public RelayCommand ShowAssetsCommand { get; }
    public RelayCommand ShowPackagesCommand { get; }
    public AsyncCommand AddSelectedAssetToPackageCommand { get; }

    public int SelectedWorkspaceIndex
    {
        get => _selectedWorkspaceIndex;
        set => SetField(ref _selectedWorkspaceIndex, value);
    }

    public string ActiveFilePath
    {
        get => _activeFilePath;
        private set => SetField(ref _activeFilePath, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public Task InitializeAsync() => Browser.InitializeAsync();

    public Task<bool> ConfirmCloseAsync() => Packages.ConfirmCloseAsync();

    public async Task AddSelectedAssetToPackageAsync()
    {
        if (AssetList.SelectedAsset is null)
        {
            ErrorMessage = "Select a Texture2D or Sprite asset before adding it to a package.";
            return;
        }

        bool added = await Packages.AddSelectedAssetAsync(AssetList.SelectedAsset);
        if (Packages.Editor.IsEditing)
            SelectedWorkspaceIndex = 1;
        if (added)
        {
            ErrorMessage = null;
            StatusText = $"Added {AssetList.SelectedAsset.Name} to the package editor.";
        }
    }

    public async Task LoadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);

        IsLoading = true;
        ErrorMessage = null;
        StatusText = $"Loading {Path.GetFileName(path)}...";
        IAssetSession? candidate = null;

        try
        {
            candidate = await _sessionFactory.OpenAsync(path, linked.Token);
            linked.Token.ThrowIfCancellationRequested();

            IAssetSession? previous = _session;
            _session = candidate;
            candidate = null;
            AssetList.ReplaceSession(_session);

            ActiveFilePath = _session.Metadata.SourcePath;
            string warningText = _session.Warnings.Count == 0
                ? string.Empty
                : $" {_session.Warnings.Count} warning(s): {string.Join(" ", _session.Warnings)}";
            StatusText = $"Loaded {Assets.Count:N0} assets from {_session.Metadata.LogicalFileCount:N0} file(s).{warningText}";
            previous?.Dispose();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            StatusText = _session is null ? "Load cancelled." : $"Load cancelled. Still showing {_session.Metadata.DisplayName}.";
        }
        catch (Exception exception) when (exception is AssetLoadException or IOException or UnauthorizedAccessException)
        {
            ErrorMessage = exception.Message;
            StatusText = _session is null
                ? "The file could not be loaded."
                : $"The file could not be loaded. Still showing {_session.Metadata.DisplayName}.";
        }
        finally
        {
            candidate?.Dispose();
            IsLoading = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetime.Cancel();
        Browser.Dispose();
        AssetList.Dispose();
        Packages.Dispose();
        _session?.Dispose();
        _lifetime.Dispose();
    }

    private async Task OpenFromPickerAsync()
    {
        try
        {
            string? path = await _filePicker.PickUnityFileAsync(_lifetime.Token);
            if (!string.IsNullOrWhiteSpace(path))
                await LoadFileAsync(path, _lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = $"The file picker could not be opened: {exception.Message}";
            StatusText = _session is null ? "No file was selected." : $"Still showing {_session.Metadata.DisplayName}.";
        }
    }
}
