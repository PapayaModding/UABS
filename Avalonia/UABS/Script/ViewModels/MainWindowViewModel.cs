using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using UABS.Services;
using UABS.UnityAssets;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Dependencies;
using UABS.UnityAssets.Packages.Memos;
using UABS.UnityAssets.Packages.Search;

namespace UABS.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IFilePickerService _filePicker;
    private readonly IAssetSessionFactory _sessionFactory;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _loadCancellation;
    private IAssetSession? _session;
    private string _activeFilePath = "No file loaded";
    private string _statusText = "Choose a Unity asset file or bundle to inspect.";
    private string? _errorMessage;
    private bool _isLoading;
    private int _loadVersion;
    private int _selectedWorkspaceIndex;
    private bool _disposed;
    private bool _isV2Assets;
    public V2BrowseViewModel V2 => Packages.V2;
    public bool IsV2Assets { get => _isV2Assets; private set { if (SetField(ref _isV2Assets, value)) { OnPropertyChanged(nameof(IsUnityAssets)); AddSelectedAssetToPackageCommand?.NotifyCanExecuteChanged(); } } }
    public bool IsUnityAssets => !IsV2Assets;
    public RelayCommand BrowseV2Command { get; }
    public RelayCommand BrowseUnityCommand { get; }
    public Task V2NavigationTask { get; private set; } = Task.CompletedTask;
    public Task SearchNavigationTask { get; private set; } = Task.CompletedTask;
    private void V2OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(V2BrowseViewModel.SelectedPackage)) IsV2Assets = V2.SelectedPackage is not null; }

    public MainWindowViewModel(
        IFilePickerService filePicker,
        IAssetSessionFactory sessionFactory,
        IDirectoryBrowserService? directoryBrowserService = null,
        string? initialPath = null,
        IPackageService? packageService = null,
        IPackageSearchService? packageSearchService = null,
        IPackageDependencyResolver? packageDependencyResolver = null,
        IUserConfirmationService? userConfirmationService = null,
        IPackageWriteCoordinator? packageWriteCoordinator = null,
        IMemoDraftCoordinator? memoDraftCoordinator = null)
    {
        _filePicker = filePicker;
        _sessionFactory = sessionFactory;
        AssetsLayout = new AssetsWorkspaceLayoutViewModel();
        IPackageMemoResolver memoResolver = new PackageMemoResolver();
        Browser = new BrowserViewModel(
            directoryBrowserService ?? new DirectoryBrowserService(),
            filePicker,
            LoadFileAsync,
            initialPath);
        IPackageSearchService searchService = packageSearchService ?? new PackageSearchService();
        IPackageWriteCoordinator writeCoordinator = packageWriteCoordinator ?? new PackageWriteCoordinator();
        PackageEditorSessionGuard editorSessionGuard = new();
        IPackageService packages = packageService ?? new PackageService();
        Packages = new PackageManagementViewModel(
            filePicker,
            packages,
            searchService,
            packageDependencyResolver ?? new PackageDependencyResolver(),
            userConfirmationService,
            writeCoordinator,
            editorSessionGuard);
        InspectorMemos = new InspectorMemoService(new PackageMemoPersistenceService(
                packages,
                writeCoordinator,
                editorSessionGuard: editorSessionGuard,
                catalogPublisher: PublishMemoCatalogAsync), memoResolver, writeCoordinator, editorSessionGuard,
                () => Packages.V2.Registrations.ToArray());
        MemoDrafts = memoDraftCoordinator ?? new MemoDraftCoordinator(InspectorMemos);
        AssetList = new AssetListViewModel(InspectorMemos, MemoDrafts, InspectorMemos);
        V2.ConfigureMemos(InspectorMemos, MemoDrafts);
        Search = new SearchWorkspaceViewModel(
            Packages,
            searchService,
            memoResolver,
            MemoDrafts,
            InspectorMemos,
            v2Memos: InspectorMemos,
            useBundleProjection: true);
        Packages.PropertyChanged += PackagesOnPropertyChanged;
        V2.PropertyChanged += V2OnPropertyChanged;
        BrowseV2Command = new RelayCommand(() => { IsV2Assets = V2.SelectedPackage is not null; V2NavigationTask = V2.VerifyCurrentAsync(); });
        BrowseUnityCommand = new RelayCommand(() => IsV2Assets = false);
        Packages.Editor.PropertyChanged += EditorOnPropertyChanged;
        UpdateAssetMemoCatalog();
        OpenFileCommand = new AsyncCommand(OpenFromPickerAsync);
        ShowAssetsCommand = new RelayCommand(() => SelectedWorkspaceIndex = 0);
        ShowSearchCommand = new RelayCommand(() => SelectedWorkspaceIndex = 1);
        ShowPackagesCommand = new RelayCommand(() => SelectedWorkspaceIndex = 2);
        AddSelectedAssetToPackageCommand = new AsyncCommand(AddSelectedAssetToPackageAsync, () => !IsV2Assets);
    }

    public BrowserViewModel Browser { get; }
    public AssetsWorkspaceLayoutViewModel AssetsLayout { get; }
    public AssetListViewModel AssetList { get; }
    public SearchWorkspaceViewModel Search { get; }
    public PackageManagementViewModel Packages { get; }
    public IMemoDraftCoordinator MemoDrafts { get; }
    public InspectorMemoService InspectorMemos { get; }
    public IReadOnlyList<AssetEntry> Assets => AssetList.Assets;
    public AsyncCommand OpenFileCommand { get; }
    public RelayCommand ShowAssetsCommand { get; }
    public RelayCommand ShowSearchCommand { get; }
    public RelayCommand ShowPackagesCommand { get; }
    public AsyncCommand AddSelectedAssetToPackageCommand { get; }

    public int SelectedWorkspaceIndex
    {
        get => _selectedWorkspaceIndex;
        set
        {
            int previous = _selectedWorkspaceIndex;
            if (!SetField(ref _selectedWorkspaceIndex, value))
                return;
            if ((value == 0 && IsV2Assets) || (value == 2 && Packages.IsV2OverviewVisible)) V2NavigationTask = V2.VerifyCurrentAsync();
            if (previous == 1 && value != 1)
            {
                Search.Deactivate();
                _ = ObserveSearchMemoFlushAsync();
            }
            if (value == 1) SearchNavigationTask = Search.ActivateAsync();
            if (previous == 0 && value != 0)
            {
                _ = ObserveAssetsMemoFlushAsync();
                _ = ObserveV2MemoFlushAsync();
            }
        }
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

    public void ReportUnexpectedFailure(string context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ErrorMessage = $"{context}: {exception.Message}";
        StatusText = "An unexpected application error occurred. Details were written to the diagnostic log.";
    }

    public async Task AddSelectedAssetToPackageAsync()
    {
        if (IsV2Assets) { ErrorMessage = "V2 metadata is read-only; no V1 editor/save route is available."; return; }
        if (AssetList.SelectedAsset is null)
        {
            ErrorMessage = "Select a Texture2D or Sprite asset before adding it to a package.";
            return;
        }

        bool added = await Packages.AddSelectedAssetAsync(AssetList.SelectedAsset);
        if (Packages.Editor.IsEditing)
            SelectedWorkspaceIndex = 2;
        if (added)
        {
            ErrorMessage = null;
            StatusText = $"Added {AssetList.SelectedAsset.Name} to the package editor.";
        }
    }

    public async Task LoadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _loadCancellation?.Cancel();
        CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        _loadCancellation = linked;
        CancellationToken token = linked.Token;
        int version = ++_loadVersion;

        IsLoading = true;
        ErrorMessage = null;
        StatusText = $"Loading {Path.GetFileName(path)}...";
        IAssetSession? candidate = null;

        try
        {
            if (path.EndsWith(".uabs.sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var registration = await V2.OpenAsync(path, token);
                token.ThrowIfCancellationRequested(); if (version != _loadVersion) return;
                if (registration is not null) { IsV2Assets = true; StatusText = registration.Summary; }
                else { ErrorMessage = V2.Status; StatusText = "V2 registration failed; the prior view is retained."; }
                return;
            }
            candidate = await _sessionFactory.OpenAsync(path, token);
            token.ThrowIfCancellationRequested();
            if (version != _loadVersion)
                return;

            IAssetSession? previous = _session;
            _session = candidate;
            candidate = null;
            AssetList.ReplaceSession(_session);
            IsV2Assets = false;

            ActiveFilePath = _session.Metadata.SourcePath;
            string warningText = _session.Warnings.Count == 0
                ? string.Empty
                : $" {_session.Warnings.Count} warning(s): {string.Join(" ", _session.Warnings)}";
            StatusText = $"Loaded {Assets.Count:N0} assets from {_session.Metadata.LogicalFileCount:N0} file(s).{warningText}";
            previous?.Dispose();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            if (version == _loadVersion)
                StatusText = _session is null ? "Load cancelled." : $"Load cancelled. Still showing {_session.Metadata.DisplayName}.";
        }
        catch (Exception exception) when (exception is AssetLoadException or IOException or UnauthorizedAccessException)
        {
            if (version == _loadVersion)
            {
                ErrorMessage = exception.Message;
                StatusText = _session is null
                    ? "The file could not be loaded."
                    : $"The file could not be loaded. Still showing {_session.Metadata.DisplayName}.";
            }
        }
        finally
        {
            candidate?.Dispose();
            if (version == _loadVersion)
            {
                _loadCancellation = null;
                IsLoading = false;
            }
            linked.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _loadVersion++;
        _lifetime.Cancel();
        _loadCancellation?.Cancel();
        _loadCancellation = null;
        Browser.Dispose();
        AssetList.Dispose();
        Search.Dispose();
        Packages.PropertyChanged -= PackagesOnPropertyChanged;
        V2.PropertyChanged -= V2OnPropertyChanged;
        Packages.Editor.PropertyChanged -= EditorOnPropertyChanged;
        Packages.Dispose();
        if (MemoDrafts is IDisposable disposableMemoDrafts)
            disposableMemoDrafts.Dispose();
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

    private void PackagesOnPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(PackageManagementViewModel.Packages))
            UpdateAssetMemoCatalog();
    }

    private void UpdateAssetMemoCatalog()
    {
        PackageMemoCatalogEntry[] catalog = Packages.Packages
            .Where(package => package.Package is not null)
            .Select(package => new PackageMemoCatalogEntry(package.Name, package.Path, package.Package!))
            .ToArray();
        AssetList.UpdateMemoCatalog(catalog);
    }

    private void EditorOnPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(PackageEditorViewModel.IsEditing) or nameof(PackageEditorViewModel.IsSaving))
        {
            AssetList.RefreshMemoEligibility();
            V2.RefreshMemoEligibility();
        }
    }

    private async Task PublishMemoCatalogAsync(
        PackageDocument persistedPackage,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Dispatcher.UIThread.CheckAccess())
        {
            Packages.PublishPersistedMemoPackage(persistedPackage);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(
            () => Packages.PublishPersistedMemoPackage(persistedPackage));
    }

    private async Task ObserveSearchMemoFlushAsync()
    {
        try
        {
            await Search.FlushActiveMemoForNavigationAsync(_lifetime.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Printer.PrintException("Search memo navigation flush failed.", exception);
        }
    }

    private async Task ObserveAssetsMemoFlushAsync()
    {
        try
        {
            await AssetList.FlushActiveMemoForNavigationAsync(_lifetime.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Printer.PrintException("Assets memo navigation flush failed.", exception);
        }
    }
    private async Task ObserveV2MemoFlushAsync()
    {
        try { await V2.MemoInspector.FlushForNavigationAsync(_lifetime.Token); }
        catch (OperationCanceledException) { }
        catch (Exception e) { Printer.PrintException("V2 memo navigation flush failed.", e); }
    }
}
