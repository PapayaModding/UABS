using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UABS.Services;
using UABS.UnityAssets;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Dependencies;
using UABS.UnityAssets.Packages.Search;

namespace UABS.ViewModels;

public enum PackageContentKind
{
    Source,
    Asset
}

public enum PackageSearchFieldSelection
{
    All,
    Names,
    Paths,
    CabCodes,
    Assets,
    Memos,
    Dependencies
}

public sealed record PackageContentItem(
    PackageContentKind Kind,
    string PackageName,
    string IndexDirectory,
    string? Name,
    string? SourceName,
    string? SourcePath,
    string? CabCode,
    long? PathId,
    string? Memo);

public sealed class PackageCatalogItemViewModel
{
    internal PackageCatalogItemViewModel(PackageCatalogEntry entry)
    {
        Entry = entry;
        Name = entry.Name;
        Path = entry.Path;
        IndexCount = entry.Package?.Indexes.Count ?? 0;
        SourceCount = entry.Package?.Bundles.Count() ?? 0;
        WarningCount = entry.ValidationIssues.Count;
        Status = entry.ErrorMessage is not null
            ? "Error"
            : WarningCount > 0 ? "Warning" : "Ready";
        Diagnostic = entry.ErrorMessage
            ?? (WarningCount > 0
                ? string.Join(" ", entry.ValidationIssues.Select(issue => issue.Message))
                : "Package metadata loaded.");
    }

    internal PackageCatalogEntry Entry { get; }
    internal PackageDocument? Package => Entry.Package;

    public string Name { get; }
    public string Path { get; }
    public int IndexCount { get; }
    public int SourceCount { get; }
    public int WarningCount { get; }
    public string Status { get; }
    public string Diagnostic { get; }
    public bool IsAvailable => Package is not null;
}

public sealed class PackageDependencyResultViewModel
{
    internal PackageDependencyResultViewModel(PackageDependencyResult result)
    {
        Result = result;
        RequestedCabCode = result.RequestedCabCode ?? "(missing)";
        Status = result.Status;
        RequestingPackage = result.Requester.PackageName;
        RequestingSource = result.Requester.SourceName ?? "(unknown source)";
        CandidateCount = result.Candidates.Count;
        CandidatePaths = result.Candidates.Count == 0
            ? "—"
            : string.Join(" | ", result.Candidates.Select(candidate => candidate.SourcePath ?? "(path unavailable)"));
        Diagnostic = result.Diagnostic;
        Candidates = result.Candidates
            .SelectMany(candidate => candidate.Occurrences.Select(occurrence => new PackageDependencyCandidateViewModel(
                occurrence.PackageName,
                occurrence.SourceName,
                occurrence.SourcePath,
                occurrence.CabCode,
                candidate.HasCompleteProvenance)))
            .ToArray();
    }

    internal PackageDependencyResult Result { get; }

    public string RequestedCabCode { get; }
    public PackageDependencyStatus Status { get; }
    public string RequestingPackage { get; }
    public string RequestingSource { get; }
    public int CandidateCount { get; }
    public string CandidatePaths { get; }
    public string Diagnostic { get; }
    public IReadOnlyList<PackageDependencyCandidateViewModel> Candidates { get; }
}

public sealed record PackageDependencyCandidateViewModel(
    string PackageName,
    string? SourceName,
    string? SourcePath,
    string? CabCode,
    bool HasCompleteProvenance);

public sealed class PackageManagementViewModel : ViewModelBase, IDisposable
{
    private readonly IFilePickerService _filePicker;
    private readonly IPackageService _packageService;
    private readonly IPackageSearchService _searchService;
    private readonly IPackageDependencyResolver _dependencyResolver;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _catalogCancellation;
    private CancellationTokenSource? _searchCancellation;
    private CancellationTokenSource? _dependencyCancellation;
    private IReadOnlyList<PackageCatalogItemViewModel> _packages = [];
    private IReadOnlyList<PackageContentItem> _contents = [];
    private IReadOnlyList<PackageSearchResult> _searchResults = [];
    private IReadOnlyList<PackageDependencyResultViewModel> _allDependencyResults = [];
    private IReadOnlyList<PackageDependencyResultViewModel> _dependencyResults = [];
    private PackageCatalogItemViewModel? _selectedPackage;
    private PackageContentItem? _selectedContent;
    private PackageSearchResult? _selectedSearchResult;
    private PackageDependencyResultViewModel? _selectedDependency;
    private string _packageDirectory = string.Empty;
    private string _catalogStatus = "Choose a directory containing package folders.";
    private string? _catalogError;
    private string _selectedPackageStatus = "No package selected.";
    private string _searchText = string.Empty;
    private string _excludedSearchText = string.Empty;
    private string _searchPathIdText = string.Empty;
    private string? _searchError;
    private bool _searchExact;
    private bool _searchPackages = true;
    private bool _searchSources = true;
    private bool _searchAssets = true;
    private PackageSearchFieldSelection _searchField = PackageSearchFieldSelection.All;
    private bool _isCatalogLoading;
    private bool _isSearching;
    private bool _isDependencyLoading;
    private int _catalogVersion;
    private int _searchVersion;
    private int _dependencyVersion;
    private bool _disposed;

    public PackageManagementViewModel(
        IFilePickerService filePicker,
        IPackageService packageService,
        IPackageSearchService searchService,
        IPackageDependencyResolver dependencyResolver,
        IUserConfirmationService? confirmations = null)
    {
        _filePicker = filePicker;
        _packageService = packageService;
        _searchService = searchService;
        _dependencyResolver = dependencyResolver;
        Editor = new PackageEditorViewModel(
            filePicker,
            confirmations ?? new DenyConfirmationService(),
            packageService,
            OnPackageSavedAsync);
        ChoosePackageDirectoryCommand = new AsyncCommand(ChoosePackageDirectoryAsync);
        RefreshCatalogCommand = new AsyncCommand(RefreshCatalogAsync, () => HasPackageDirectory);
        SearchCommand = new AsyncCommand(SearchAsync, () => HasLoadedPackages);
        NewPackageCommand = new AsyncCommand(StartNewPackageAsync);
        EditPackageCommand = new AsyncCommand(EditSelectedPackageAsync, () => SelectedPackage?.IsAvailable == true);
        SearchFields = Enum.GetValues<PackageSearchFieldSelection>();
    }

    public AsyncCommand ChoosePackageDirectoryCommand { get; }
    public AsyncCommand RefreshCatalogCommand { get; }
    public AsyncCommand SearchCommand { get; }
    public AsyncCommand NewPackageCommand { get; }
    public AsyncCommand EditPackageCommand { get; }
    public PackageEditorViewModel Editor { get; }
    public IReadOnlyList<PackageSearchFieldSelection> SearchFields { get; }

    public IReadOnlyList<PackageCatalogItemViewModel> Packages
    {
        get => _packages;
        private set
        {
            if (SetField(ref _packages, value))
            {
                OnPropertyChanged(nameof(HasPackages));
                OnPropertyChanged(nameof(HasLoadedPackages));
                SearchCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<PackageContentItem> Contents
    {
        get => _contents;
        private set => SetField(ref _contents, value);
    }

    public IReadOnlyList<PackageSearchResult> SearchResults
    {
        get => _searchResults;
        private set => SetField(ref _searchResults, value);
    }

    public IReadOnlyList<PackageDependencyResultViewModel> DependencyResults
    {
        get => _dependencyResults;
        private set => SetField(ref _dependencyResults, value);
    }

    public PackageCatalogItemViewModel? SelectedPackage
    {
        get => _selectedPackage;
        set
        {
            if (ReferenceEquals(_selectedPackage, value))
                return;
            _selectionTask = ChangePackageSelectionAsync(value);
        }
    }

    public PackageContentItem? SelectedContent
    {
        get => _selectedContent;
        set
        {
            if (SetField(ref _selectedContent, value))
            {
                OnPropertyChanged(nameof(HasSelectedContent));
                FilterDependencyResults();
            }
        }
    }

    public PackageSearchResult? SelectedSearchResult
    {
        get => _selectedSearchResult;
        set => SetField(ref _selectedSearchResult, value);
    }

    public PackageDependencyResultViewModel? SelectedDependency
    {
        get => _selectedDependency;
        set
        {
            if (SetField(ref _selectedDependency, value))
                OnPropertyChanged(nameof(HasSelectedDependency));
        }
    }

    public string PackageDirectory
    {
        get => _packageDirectory;
        set
        {
            if (SetField(ref _packageDirectory, value))
            {
                OnPropertyChanged(nameof(HasPackageDirectory));
                RefreshCatalogCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string CatalogStatus
    {
        get => _catalogStatus;
        private set => SetField(ref _catalogStatus, value);
    }

    public string? CatalogError
    {
        get => _catalogError;
        private set
        {
            if (SetField(ref _catalogError, value))
                OnPropertyChanged(nameof(HasCatalogError));
        }
    }

    public string SelectedPackageStatus
    {
        get => _selectedPackageStatus;
        private set => SetField(ref _selectedPackageStatus, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public string ExcludedSearchText
    {
        get => _excludedSearchText;
        set => SetField(ref _excludedSearchText, value);
    }

    public string SearchPathIdText
    {
        get => _searchPathIdText;
        set => SetField(ref _searchPathIdText, value);
    }

    public bool SearchExact
    {
        get => _searchExact;
        set => SetField(ref _searchExact, value);
    }

    public bool SearchPackages
    {
        get => _searchPackages;
        set => SetField(ref _searchPackages, value);
    }

    public bool SearchSources
    {
        get => _searchSources;
        set => SetField(ref _searchSources, value);
    }

    public bool SearchAssets
    {
        get => _searchAssets;
        set => SetField(ref _searchAssets, value);
    }

    public PackageSearchFieldSelection SearchField
    {
        get => _searchField;
        set => SetField(ref _searchField, value);
    }

    public string? SearchError
    {
        get => _searchError;
        private set
        {
            if (SetField(ref _searchError, value))
                OnPropertyChanged(nameof(HasSearchError));
        }
    }

    public bool IsCatalogLoading
    {
        get => _isCatalogLoading;
        private set => SetField(ref _isCatalogLoading, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set => SetField(ref _isSearching, value);
    }

    public bool IsDependencyLoading
    {
        get => _isDependencyLoading;
        private set => SetField(ref _isDependencyLoading, value);
    }

    public bool HasPackageDirectory => !string.IsNullOrWhiteSpace(PackageDirectory);
    public bool HasPackages => Packages.Count > 0;
    public bool HasLoadedPackages => Packages.Any(package => package.IsAvailable);
    public bool HasCatalogError => !string.IsNullOrWhiteSpace(CatalogError);
    public bool HasSearchError => !string.IsNullOrWhiteSpace(SearchError);
    public bool HasSelectedContent => SelectedContent is not null;
    public bool HasSelectedDependency => SelectedDependency is not null;

    public async Task ChoosePackageDirectoryAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            string? selected = await _filePicker.PickPackageCatalogAsync(_lifetime.Token);
            if (!string.IsNullOrWhiteSpace(selected))
                await LoadCatalogAsync(selected);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            CatalogError = $"The package directory picker could not be opened: {exception.Message}";
        }
    }

    public Task RefreshCatalogAsync() => HasPackageDirectory
        ? LoadCatalogAsync(PackageDirectory)
        : Task.CompletedTask;

    public async Task LoadCatalogAsync(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (Editor.IsEditing && !await Editor.TryAbandonAsync("Reload the package catalog?").ConfigureAwait(true))
            return;

        _catalogCancellation?.Cancel();
        _catalogCancellation?.Dispose();
        _catalogCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationToken token = _catalogCancellation.Token;
        int version = ++_catalogVersion;
        string? previousSelectionPath = SelectedPackage?.Path;
        IsCatalogLoading = true;
        CatalogError = null;
        CatalogStatus = $"Loading packages from {path}...";

        try
        {
            IReadOnlyList<PackageCatalogEntry> entries = await _packageService.ReadCatalogAsync(path, token);
            token.ThrowIfCancellationRequested();
            if (version != _catalogVersion)
                return;

            IReadOnlyList<PackageCatalogItemViewModel> packages = entries
                .Select(entry => new PackageCatalogItemViewModel(entry))
                .ToArray();
            Packages = packages;
            PackageDirectory = Path.GetFullPath(path);

            int loaded = packages.Count(package => package.IsAvailable);
            int errors = packages.Count - loaded;
            int warnings = packages.Sum(package => package.WarningCount);
            CatalogStatus = packages.Count == 0
                ? "No package directories were found."
                : $"Found {packages.Count:N0} package(s): {loaded:N0} loaded, {errors:N0} error(s), {warnings:N0} warning(s).";

            PackageCatalogItemViewModel? selected = packages.FirstOrDefault(package =>
                string.Equals(package.Path, previousSelectionPath, StringComparison.OrdinalIgnoreCase));
            selected ??= packages.FirstOrDefault(package => package.IsAvailable);
            await SelectPackageAsync(selected);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or ArgumentException
                                           or NotSupportedException
                                           or PackageException)
        {
            if (version == _catalogVersion)
            {
                CatalogError = exception.Message;
                CatalogStatus = Packages.Count == 0
                    ? "The package catalog could not be loaded."
                    : "Refresh failed; the previous package catalog is still shown.";
            }
        }
        finally
        {
            if (version == _catalogVersion)
                IsCatalogLoading = false;
        }
    }

    public Task SelectPackageAsync(PackageCatalogItemViewModel? package)
    {
        if (ReferenceEquals(SelectedPackage, package))
            return ApplyPackageSelectionAsync(package);
        _selectionTask = ChangePackageSelectionAsync(package);
        return _selectionTask;
    }

    private Task _selectionTask = Task.CompletedTask;

    private async Task ChangePackageSelectionAsync(PackageCatalogItemViewModel? package)
    {
        if (Editor.IsEditing && !await Editor.TryAbandonAsync("Switch packages?").ConfigureAwait(true))
        {
            OnPropertyChanged(nameof(SelectedPackage));
            return;
        }

        _selectedPackage = package;
        OnPropertyChanged(nameof(SelectedPackage));
        EditPackageCommand.NotifyCanExecuteChanged();
        await ApplyPackageSelectionAsync(package);
    }

    public Task<bool> StartNewPackageAsync() => Editor.StartNewAsync(HasPackageDirectory ? PackageDirectory : null);

    public Task<bool> EditSelectedPackageAsync() => SelectedPackage?.Package is null
        ? Task.FromResult(false)
        : Editor.StartEditAsync(SelectedPackage.Name, SelectedPackage.Package);

    public async Task<bool> AddSelectedAssetAsync(AssetEntry asset)
    {
        if (!Editor.IsEditing && SelectedPackage?.Package is not null)
        {
            bool opened = await Editor.StartEditAsync(SelectedPackage.Name, SelectedPackage.Package);
            if (!opened)
                return false;
        }

        return await Editor.AddSelectedAssetAsync(asset, HasPackageDirectory ? PackageDirectory : null);
    }

    public Task<bool> ConfirmCloseAsync() => Editor.ConfirmCloseAsync();

    private async Task OnPackageSavedAsync(string targetPath, PackageDocument package)
    {
        if (Editor.IsDirty)
            return;

        string parent = Path.GetDirectoryName(targetPath)!;
        await LoadCatalogAsync(parent);
        PackageCatalogItemViewModel? saved = Packages.FirstOrDefault(item =>
            string.Equals(item.Path, targetPath, StringComparison.OrdinalIgnoreCase));
        if (saved is not null && !ReferenceEquals(saved, SelectedPackage))
            await SelectPackageAsync(saved);
    }

    private Task ApplyPackageSelectionAsync(PackageCatalogItemViewModel? package)
    {
        _dependencyCancellation?.Cancel();
        _dependencyCancellation?.Dispose();
        int version = ++_dependencyVersion;
        SelectedContent = null;
        SelectedDependency = null;
        _allDependencyResults = [];
        DependencyResults = [];

        if (package?.Package is null)
        {
            Contents = [];
            SelectedPackageStatus = package is null ? "No package selected." : package.Diagnostic;
            IsDependencyLoading = false;
            _selectionTask = Task.CompletedTask;
            return _selectionTask;
        }

        Contents = BuildContents(package);
        SelectedPackageStatus = package.WarningCount == 0
            ? $"{package.SourceCount:N0} source record(s), {Contents.Count(item => item.Kind == PackageContentKind.Asset):N0} asset reference(s)."
            : package.Diagnostic;

        _dependencyCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _selectionTask = LoadDependenciesAsync(package, version, _dependencyCancellation.Token);
        return _selectionTask;
    }

    public async Task SearchAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationToken token = _searchCancellation.Token;
        int version = ++_searchVersion;
        SearchError = null;
        IsSearching = true;

        try
        {
            if (!TryReadPathId(out long? pathId))
            {
                SearchError = "Path ID must be a whole 64-bit number.";
                SearchResults = [];
                return;
            }

            PackageSearchResultKinds kinds = PackageSearchResultKinds.None;
            if (SearchPackages) kinds |= PackageSearchResultKinds.Package;
            if (SearchSources) kinds |= PackageSearchResultKinds.SourceFile;
            if (SearchAssets) kinds |= PackageSearchResultKinds.Asset;

            PackageSearchRequest request = new(SearchText, ExcludedSearchText)
            {
                ExactText = SearchExact,
                PathId = pathId,
                ResultKinds = kinds,
                Fields = MapSearchFields(SearchField),
                SortBy = PackageSearchSortField.PackageName
            };

            IReadOnlyList<PackageSearchResult> results = await _searchService.SearchAsync(
                GetSearchSources(), request, token);
            token.ThrowIfCancellationRequested();
            if (version != _searchVersion)
                return;

            SearchResults = results;
            SelectedSearchResult = null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (version == _searchVersion)
                SearchError = $"Search failed: {exception.Message}";
        }
        finally
        {
            if (version == _searchVersion)
                IsSearching = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetime.Cancel();
        _catalogCancellation?.Cancel();
        _searchCancellation?.Cancel();
        _dependencyCancellation?.Cancel();
        _catalogCancellation?.Dispose();
        _searchCancellation?.Dispose();
        _dependencyCancellation?.Dispose();
        Editor.Dispose();
        _lifetime.Dispose();
    }

    private async Task LoadDependenciesAsync(
        PackageCatalogItemViewModel package,
        int version,
        CancellationToken cancellationToken)
    {
        IsDependencyLoading = true;
        try
        {
            PackageDependencySource requester = new(package.Name, package.Package!);
            IReadOnlyList<PackageDependencyResult> results = await _dependencyResolver.ResolveForPackageAsync(
                GetDependencySources(), requester, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (version != _dependencyVersion || !ReferenceEquals(SelectedPackage, package))
                return;

            _allDependencyResults = results.Select(result => new PackageDependencyResultViewModel(result)).ToArray();
            FilterDependencyResults();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (version == _dependencyVersion)
                SelectedPackageStatus = $"Dependency inspection failed: {exception.Message}";
        }
        finally
        {
            if (version == _dependencyVersion)
                IsDependencyLoading = false;
        }
    }

    private void FilterDependencyResults()
    {
        string? sourcePath = SelectedContent?.SourcePath;
        DependencyResults = string.IsNullOrWhiteSpace(sourcePath)
            ? _allDependencyResults
            : _allDependencyResults.Where(result =>
                string.Equals(result.Result.Requester.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        SelectedDependency = null;
    }

    private IReadOnlyList<PackageSearchSource> GetSearchSources() => Packages
        .Where(package => package.Package is not null)
        .Select(package => new PackageSearchSource(package.Name, package.Package!))
        .ToArray();

    private IReadOnlyList<PackageDependencySource> GetDependencySources() => Packages
        .Where(package => package.Package is not null)
        .Select(package => new PackageDependencySource(package.Name, package.Package!))
        .ToArray();

    private bool TryReadPathId(out long? pathId)
    {
        pathId = null;
        if (string.IsNullOrWhiteSpace(SearchPathIdText))
            return true;
        if (!long.TryParse(SearchPathIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            return false;
        pathId = value;
        return true;
    }

    private static PackageSearchFields MapSearchFields(PackageSearchFieldSelection selection) => selection switch
    {
        PackageSearchFieldSelection.Names => PackageSearchFields.PackageName | PackageSearchFields.SourceName | PackageSearchFields.AssetName,
        PackageSearchFieldSelection.Paths => PackageSearchFields.PackagePath | PackageSearchFields.IndexDirectory | PackageSearchFields.SourcePath,
        PackageSearchFieldSelection.CabCodes => PackageSearchFields.CabCode,
        PackageSearchFieldSelection.Assets => PackageSearchFields.AssetName,
        PackageSearchFieldSelection.Memos => PackageSearchFields.Memo,
        PackageSearchFieldSelection.Dependencies => PackageSearchFields.Dependency,
        _ => PackageSearchFields.All
    };

    private static IReadOnlyList<PackageContentItem> BuildContents(PackageCatalogItemViewModel package)
    {
        List<PackageContentItem> items = [];
        foreach (PackageIndex index in package.Package!.Indexes)
        {
            foreach (PackageBundleReference source in index.Bundles)
            {
                items.Add(new PackageContentItem(
                    PackageContentKind.Source,
                    package.Name,
                    index.RelativeDirectory,
                    source.Name,
                    source.Name,
                    source.Path,
                    source.CabCode,
                    null,
                    null));

                foreach (PackageAssetReference asset in source.AssetInfos ?? [])
                {
                    items.Add(new PackageContentItem(
                        PackageContentKind.Asset,
                        package.Name,
                        index.RelativeDirectory,
                        asset.Name,
                        source.Name,
                        source.Path,
                        source.CabCode,
                        asset.PathId,
                        asset.Memo));
                }
            }
        }

        return items.AsReadOnly();
    }

    private sealed class DenyConfirmationService : IUserConfirmationService
    {
        public Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmText,
            string cancelText,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
