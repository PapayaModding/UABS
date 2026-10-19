using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
using UABS.UnityAssets.Packages.Search;

namespace UABS.ViewModels;

public enum SearchWorkspaceFieldSelection
{
    All,
    Names,
    Paths,
    CabCodes,
    Assets,
    Dependencies
}

public sealed class SearchPackageScopeItemViewModel : ViewModelBase
{
    private readonly Action _selectionChanged;
    private bool _isSelected;

    internal SearchPackageScopeItemViewModel(
        PackageCatalogItemViewModel package,
        bool isSelected,
        Action selectionChanged)
    {
        Package = package;
        _isSelected = isSelected;
        _selectionChanged = selectionChanged;
    }

    internal PackageCatalogItemViewModel Package { get; private set; }

    public string Name => Package.Name;

    public string Path => Package.Path;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetField(ref _isSelected, value))
                return;
            _selectionChanged();
        }
    }

    internal void UpdatePackage(PackageCatalogItemViewModel package) => Package = package;

    internal bool SetSelectedWithoutCallback(bool value)
    {
        if (_isSelected == value)
            return false;
        _isSelected = value;
        OnPropertyChanged(nameof(IsSelected));
        return true;
    }
}

public sealed class SearchResultItemViewModel : ViewModelBase
{
    private string? _memo;

    internal SearchResultItemViewModel(PackageSearchResult result)
    {
        Result = result;
        _memo = result.Memo;
    }

    public PackageSearchResult Result { get; }
    public PackageSearchResultKind Kind => Result.Kind;
    public string Name => Result.AssetName ?? Result.SourceName ?? Result.PackageName;
    public string PackageName => Result.PackageName;
    public string? PackagePath => Result.PackagePath;
    public string? IndexDirectory => Result.IndexDirectory;
    public string? SourceName => Result.SourceName;
    public string? SourcePath => Result.SourcePath;
    public string? CabCode => Result.CabCode;
    public string? AssetName => Result.AssetName;
    public long? PathId => Result.PathId;
    public string? Memo
    {
        get => _memo;
        private set
        {
            if (SetField(ref _memo, value))
                OnPropertyChanged(nameof(MemoSummary));
        }
    }
    public string MemoSummary => string.IsNullOrWhiteSpace(Memo)
        ? "—"
        : Memo.Length <= 80 ? Memo : $"{Memo[..77]}...";
    public PackageSearchMatchCategory MatchCategories => Result.MatchCategories;

    internal void ApplyPersistedMemo(string? memo) => Memo = memo;
}

public sealed class SearchWorkspaceViewModel : ViewModelBase, IDisposable
{
    private readonly PackageManagementViewModel _packages;
    private readonly IPackageSearchService _searchService;
    private readonly IMemoDraftCoordinator _memoDrafts;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _searchCancellation;
    private IReadOnlyList<SearchPackageScopeItemViewModel> _packageScopes = [];
    private IReadOnlyList<SearchPackageScopeItemViewModel> _visiblePackageScopes = [];
    private IReadOnlyList<SearchResultItemViewModel> _results = [];
    private SearchResultItemViewModel? _selectedResult;
    private string _packageFilter = string.Empty;
    private string _includeText = string.Empty;
    private string _excludeText = string.Empty;
    private string _pathIdText = string.Empty;
    private bool _exactText;
    private bool _searchMemos;
    private bool _memoOnly;
    private bool _includePackageResults = true;
    private bool _includeSourceResults = true;
    private bool _includeAssetResults = true;
    private SearchWorkspaceFieldSelection _fieldSelection = SearchWorkspaceFieldSelection.Names;
    private bool _isSearching;
    private string _status = "Load a package catalog in Packages to establish search scope.";
    private string? _error;
    private bool _hasExecutedSearch;
    private bool _criteriaChanged;
    private string _lastExecutedCriteria = "No search has been executed.";
    private CriteriaSnapshot? _executedCriteria;
    private int _searchVersion;
    private bool _scopeInitialized;
    private bool _disposed;
    private readonly IPackageMemoResolver _memoResolver;
    private Task _memoTransitionTask = Task.CompletedTask;

    public SearchWorkspaceViewModel(
        PackageManagementViewModel packages,
        IPackageSearchService searchService,
        IPackageMemoResolver? memoResolver = null,
        IMemoDraftCoordinator? memoDraftCoordinator = null,
        IPackageEditorSessionGuard? editorSessionGuard = null)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _memoResolver = memoResolver ?? new PackageMemoResolver();
        _memoDrafts = memoDraftCoordinator ?? new UnavailableMemoDraftCoordinator();
        MemoInspector = new EditableMemoInspectorViewModel(
            _memoDrafts,
            editorSessionGuard ?? new PackageEditorSessionGuard());
        SearchCommand = new AsyncCommand(SearchAsync);
        CancelSearchCommand = new RelayCommand(CancelSearch, () => IsSearching);
        SelectAllPackagesCommand = new RelayCommand(() => SetAllPackages(true), () => PackageScopes.Count > 0);
        ClearPackagesCommand = new RelayCommand(() => SetAllPackages(false), () => SelectedPackageCount > 0);
        FieldSelections = Enum.GetValues<SearchWorkspaceFieldSelection>();
        _packages.PropertyChanged += PackagesOnPropertyChanged;
        _packages.Editor.PropertyChanged += EditorOnPropertyChanged;
        _memoDrafts.DraftChanged += MemoDraftsOnDraftChanged;
        ReconcilePackages();
    }

    public SearchWorkspaceLayoutViewModel Layout { get; } = new();
    public EditableMemoInspectorViewModel MemoInspector { get; }
    public AsyncCommand SearchCommand { get; }
    public RelayCommand CancelSearchCommand { get; }
    public RelayCommand SelectAllPackagesCommand { get; }
    public RelayCommand ClearPackagesCommand { get; }
    public IReadOnlyList<SearchWorkspaceFieldSelection> FieldSelections { get; }

    public IReadOnlyList<SearchPackageScopeItemViewModel> PackageScopes
    {
        get => _packageScopes;
        private set
        {
            if (SetField(ref _packageScopes, value))
            {
                OnPropertyChanged(nameof(HasPackages));
                UpdateScopeState();
            }
        }
    }

    public IReadOnlyList<SearchPackageScopeItemViewModel> VisiblePackageScopes
    {
        get => _visiblePackageScopes;
        private set
        {
            if (SetField(ref _visiblePackageScopes, value))
                OnPropertyChanged(nameof(HasVisiblePackages));
        }
    }

    public IReadOnlyList<SearchResultItemViewModel> Results
    {
        get => _results;
        private set
        {
            if (SetField(ref _results, value))
            {
                OnPropertyChanged(nameof(ResultCount));
                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public SearchResultItemViewModel? SelectedResult
    {
        get => _selectedResult;
        set
        {
            PackageMemoIdentity? previousIdentity = MemoInspector.Identity;
            if (SetField(ref _selectedResult, value))
            {
                OnPropertyChanged(nameof(HasSelectedResult));
                QueueMemoPresentation(flushIdentity: previousIdentity);
            }
        }
    }

    public string PackageFilter
    {
        get => _packageFilter;
        set
        {
            if (SetField(ref _packageFilter, value ?? string.Empty))
                RefreshVisiblePackages();
        }
    }

    public string IncludeText
    {
        get => _includeText;
        set => SetCriteriaField(ref _includeText, value ?? string.Empty);
    }

    public string ExcludeText
    {
        get => _excludeText;
        set => SetCriteriaField(ref _excludeText, value ?? string.Empty);
    }

    public string PathIdText
    {
        get => _pathIdText;
        set => SetCriteriaField(ref _pathIdText, value ?? string.Empty);
    }

    public bool ExactText
    {
        get => _exactText;
        set => SetCriteriaField(ref _exactText, value);
    }

    public bool SearchMemos
    {
        get => _searchMemos;
        set => SetCriteriaField(ref _searchMemos, value);
    }

    public bool MemoOnly
    {
        get => _memoOnly;
        set => SetCriteriaField(ref _memoOnly, value);
    }

    public bool IncludePackageResults
    {
        get => _includePackageResults;
        set => SetCriteriaField(ref _includePackageResults, value);
    }

    public bool IncludeSourceResults
    {
        get => _includeSourceResults;
        set => SetCriteriaField(ref _includeSourceResults, value);
    }

    public bool IncludeAssetResults
    {
        get => _includeAssetResults;
        set => SetCriteriaField(ref _includeAssetResults, value);
    }

    public SearchWorkspaceFieldSelection FieldSelection
    {
        get => _fieldSelection;
        set => SetCriteriaField(ref _fieldSelection, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set
        {
            if (SetField(ref _isSearching, value))
            {
                CancelSearchCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetField(ref _error, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public bool HasExecutedSearch
    {
        get => _hasExecutedSearch;
        private set
        {
            if (SetField(ref _hasExecutedSearch, value))
                OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    public bool CriteriaChanged
    {
        get => _criteriaChanged;
        private set => SetField(ref _criteriaChanged, value);
    }

    public string LastExecutedCriteria
    {
        get => _lastExecutedCriteria;
        private set => SetField(ref _lastExecutedCriteria, value);
    }

    public bool HasPackages => PackageScopes.Count > 0;
    public bool HasVisiblePackages => VisiblePackageScopes.Count > 0;
    public int SelectedPackageCount => PackageScopes.Count(package => package.IsSelected);
    public string PackageSelectionSummary => $"{SelectedPackageCount:N0} of {PackageScopes.Count:N0} package(s) selected";
    public int ResultCount => Results.Count;
    public bool HasResults => Results.Count > 0;
    public bool HasSelectedResult => SelectedResult is not null;
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool ShowEmptyState => !IsSearching && !HasError && !HasResults;

    public async Task SearchAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CancelAndDisposeSearch();
        int version = ++_searchVersion;
        IsSearching = false;
        Error = null;

        if (!TryCreateRequest(out PackageSearchRequest? request, out string? validationError))
        {
            Results = [];
            SelectedResult = null;
            Error = validationError;
            Status = "Search was not run.";
            return;
        }

        SearchPackageScopeItemViewModel[] selectedPackages = PackageScopes
            .Where(package => package.IsSelected && package.Package.Package is not null)
            .ToArray();
        if (selectedPackages.Length == 0)
        {
            Results = [];
            SelectedResult = null;
            Error = "Select at least one available package before searching.";
            Status = "Search was not run.";
            return;
        }

        CriteriaSnapshot criteria = CaptureCriteria();
        IReadOnlyList<PackageSearchSource> sources = selectedPackages
            .Select(package => new PackageSearchSource(package.Name, package.Package.Package!))
            .ToArray();
        _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationToken token = _searchCancellation.Token;
        IsSearching = true;
        Status = $"Searching {selectedPackages.Length:N0} package(s)...";

        try
        {
            IReadOnlyList<PackageSearchResult> results = await _searchService.SearchAsync(sources, request!, token);
            token.ThrowIfCancellationRequested();
            if (version != _searchVersion)
                return;

            Results = results.Select(result => new SearchResultItemViewModel(result)).ToArray();
            SelectedResult = null;
            _executedCriteria = criteria;
            LastExecutedCriteria = CreateCriteriaSummary(criteria);
            HasExecutedSearch = true;
            CriteriaChanged = false;
            Status = Results.Count == 0
                ? $"No results matched across {selectedPackages.Length:N0} package(s)."
                : $"{Results.Count:N0} result(s) across {selectedPackages.Length:N0} package(s).";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            if (version == _searchVersion)
                Status = "Search cancelled; existing results were preserved.";
        }
        catch (Exception exception)
        {
            if (version == _searchVersion)
            {
                Error = $"Search failed: {exception.Message}";
                Status = "Search failed; existing results were preserved.";
            }
        }
        finally
        {
            if (version == _searchVersion)
            {
                _searchCancellation?.Dispose();
                _searchCancellation = null;
                IsSearching = false;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _packages.PropertyChanged -= PackagesOnPropertyChanged;
        _packages.Editor.PropertyChanged -= EditorOnPropertyChanged;
        _memoDrafts.DraftChanged -= MemoDraftsOnDraftChanged;
        _searchVersion++;
        _lifetime.Cancel();
        CancelAndDisposeSearch();
        MemoInspector.Dispose();
        _lifetime.Dispose();
    }

    private void PackagesOnPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(PackageManagementViewModel.Packages))
        {
            ReconcilePackages();
            QueueMemoPresentation();
        }
    }

    private void EditorOnPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(PackageEditorViewModel.IsEditing) or nameof(PackageEditorViewModel.IsSaving))
            QueueMemoPresentation();
    }

    private void MemoDraftsOnDraftChanged(object? sender, MemoDraftChangedEventArgs eventArgs)
    {
        MemoDraftSnapshot draft = eventArgs.Draft;
        if (draft.Identity is null
            || draft.LastPersistenceStatus is not (PackageMemoPersistenceStatus.Saved
                or PackageMemoPersistenceStatus.Unchanged))
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
            ApplyPersistedMemoToResults(draft.Identity, _memoDrafts.GetSnapshot(draft.Identity).ConfirmedMemo);
        else
            Dispatcher.UIThread.Post(() => ApplyPersistedMemoToResults(
                draft.Identity, _memoDrafts.GetSnapshot(draft.Identity).ConfirmedMemo));
    }

    private void ApplyPersistedMemoToResults(PackageMemoIdentity identity, string? memo)
    {
        if (_disposed)
            return;
        foreach (SearchResultItemViewModel result in Results)
        {
            if (MatchesIdentity(result, identity))
                result.ApplyPersistedMemo(memo);
        }
    }

    private static bool MatchesIdentity(SearchResultItemViewModel result, PackageMemoIdentity identity) =>
        result.Kind == PackageSearchResultKind.Asset
        && result.PathId == identity.PathId
        && PackagePathComparer.Equals(result.PackagePath, identity.PackagePath)
        && string.Equals(result.IndexDirectory, identity.IndexDirectory, StringComparison.Ordinal)
        && PackagePathComparer.Equals(result.SourcePath, identity.SourcePath)
        && string.Equals(result.AssetName, identity.AssetName, StringComparison.Ordinal);

    private void ReconcilePackages()
    {
        Dictionary<string, SearchPackageScopeItemViewModel> existing = PackageScopes
            .ToDictionary(package => package.Path, StringComparer.OrdinalIgnoreCase);
        PackageCatalogItemViewModel[] available = _packages.Packages
            .Where(package => package.IsAvailable)
            .ToArray();
        bool selectNew = !_scopeInitialized && available.Length > 0;
        List<SearchPackageScopeItemViewModel> reconciled = new(available.Length);

        foreach (PackageCatalogItemViewModel package in available)
        {
            if (existing.TryGetValue(package.Path, out SearchPackageScopeItemViewModel? scope))
            {
                scope.UpdatePackage(package);
                reconciled.Add(scope);
            }
            else
            {
                reconciled.Add(new SearchPackageScopeItemViewModel(package, selectNew, OnScopeSelectionChanged));
            }
        }

        if (available.Length > 0)
            _scopeInitialized = true;
        PackageScopes = reconciled.AsReadOnly();
        RefreshVisiblePackages();
        UpdateCriteriaChanged();
        Status = available.Length == 0
            ? "Load a package catalog in Packages to establish search scope."
            : $"{PackageSelectionSummary}. Ready to search.";
    }

    private void SetAllPackages(bool selected)
    {
        bool changed = false;
        foreach (SearchPackageScopeItemViewModel package in PackageScopes)
            changed |= package.SetSelectedWithoutCallback(selected);
        if (changed)
            OnScopeSelectionChanged();
    }

    private void OnScopeSelectionChanged()
    {
        UpdateScopeState();
        UpdateCriteriaChanged();
    }

    private void UpdateScopeState()
    {
        OnPropertyChanged(nameof(SelectedPackageCount));
        OnPropertyChanged(nameof(PackageSelectionSummary));
        SelectAllPackagesCommand?.NotifyCanExecuteChanged();
        ClearPackagesCommand?.NotifyCanExecuteChanged();
    }

    private void RefreshVisiblePackages()
    {
        VisiblePackageScopes = string.IsNullOrWhiteSpace(PackageFilter)
            ? PackageScopes
            : PackageScopes.Where(package =>
                package.Name.Contains(PackageFilter.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private bool TryCreateRequest(out PackageSearchRequest? request, out string? validationError)
    {
        request = null;
        validationError = null;
        long? pathId = null;
        if (!string.IsNullOrWhiteSpace(PathIdText))
        {
            if (!long.TryParse(PathIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            {
                validationError = "Path ID must be a whole 64-bit number.";
                return false;
            }
            pathId = value;
        }

        if (!HasUsableTerm(IncludeText) && pathId is null)
        {
            validationError = "Enter at least one include keyword or an exact Path ID.";
            return false;
        }

        PackageSearchResultKinds resultKinds = PackageSearchResultKinds.None;
        if (IncludePackageResults) resultKinds |= PackageSearchResultKinds.Package;
        if (IncludeSourceResults) resultKinds |= PackageSearchResultKinds.SourceFile;
        if (IncludeAssetResults) resultKinds |= PackageSearchResultKinds.Asset;
        if (resultKinds == PackageSearchResultKinds.None)
        {
            validationError = "Enable at least one result kind.";
            return false;
        }

        PackageSearchFields fields = MapFields(FieldSelection);
        if (SearchMemos)
            fields |= PackageSearchFields.Memo;
        request = new PackageSearchRequest(IncludeText, ExcludeText)
        {
            ExactText = ExactText,
            RequireNonEmptyMemo = MemoOnly,
            PathId = pathId,
            ResultKinds = resultKinds,
            Fields = fields,
            SortBy = PackageSearchSortField.PackageName
        };
        return true;
    }

    private void CancelSearch()
    {
        if (_searchCancellation is null)
            return;
        _searchCancellation.Cancel();
    }

    private void CancelAndDisposeSearch()
    {
        CancellationTokenSource? cancellation = _searchCancellation;
        _searchCancellation = null;
        if (cancellation is null)
            return;
        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        cancellation.Dispose();
    }

    private void SetCriteriaField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!SetField(ref field, value, propertyName))
            return;
        UpdateCriteriaChanged();
    }

    private void UpdateCriteriaChanged()
    {
        CriteriaChanged = _executedCriteria is not null && _executedCriteria != CaptureCriteria();
    }

    private CriteriaSnapshot CaptureCriteria() => new(
        IncludeText,
        ExcludeText,
        PathIdText,
        ExactText,
        SearchMemos,
        MemoOnly,
        IncludePackageResults,
        IncludeSourceResults,
        IncludeAssetResults,
        FieldSelection,
        string.Join('\u001f', PackageScopes.Where(package => package.IsSelected)
            .Select(package => package.Path)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)));

    private static string CreateCriteriaSummary(CriteriaSnapshot criteria)
    {
        string include = string.IsNullOrWhiteSpace(criteria.IncludeText) ? "(none)" : criteria.IncludeText;
        string pathId = string.IsNullOrWhiteSpace(criteria.PathIdText) ? "(none)" : criteria.PathIdText;
        int packages = string.IsNullOrEmpty(criteria.SelectedPaths)
            ? 0
            : criteria.SelectedPaths.Count(character => character == '\u001f') + 1;
        return $"Include: {include}; Path ID: {pathId}; packages: {packages:N0}.";
    }

    private static bool HasUsableTerm(string value) => value
        .Split([',', '，'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(term => term.Length > 0);

    private static PackageSearchFields MapFields(SearchWorkspaceFieldSelection selection) => selection switch
    {
        SearchWorkspaceFieldSelection.Names => PackageSearchFields.PackageName
                                               | PackageSearchFields.SourceName
                                               | PackageSearchFields.AssetName,
        SearchWorkspaceFieldSelection.Paths => PackageSearchFields.PackagePath
                                               | PackageSearchFields.IndexDirectory
                                               | PackageSearchFields.SourcePath,
        SearchWorkspaceFieldSelection.CabCodes => PackageSearchFields.CabCode,
        SearchWorkspaceFieldSelection.Assets => PackageSearchFields.AssetName,
        SearchWorkspaceFieldSelection.Dependencies => PackageSearchFields.Dependency,
        _ => PackageSearchFields.All & ~PackageSearchFields.Memo
    };

    public Task FlushActiveMemoForNavigationAsync(CancellationToken cancellationToken = default) =>
        MemoInspector.FlushForNavigationAsync(cancellationToken);

    public Task MemoTransitionTask => _memoTransitionTask;

    private void QueueMemoPresentation(PackageMemoIdentity? flushIdentity = null)
    {
        PackageMemoResolution resolution = ResolveSelectedMemo();
        Task flush = flushIdentity is null
            ? Task.CompletedTask
            : _draftsFlush(flushIdentity);
        _memoTransitionTask = ObserveMemoTransitionAsync(flush, MemoInspector.ShowAsync(resolution));

        async Task _draftsFlush(PackageMemoIdentity identity)
        {
            await MemoInspector.FlushIdentityForNavigationAsync(identity, _lifetime.Token);
        }
    }

    private async Task ObserveMemoTransitionAsync(Task flush, Task presentation)
    {
        try
        {
            await Task.WhenAll(flush, presentation).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Printer.PrintException("Search memo transition failed.", exception);
        }
    }

    private PackageMemoResolution ResolveSelectedMemo()
    {
        SearchResultItemViewModel? selected = SelectedResult;
        if (selected is null)
        {
            return new PackageMemoResolution(
                PackageMemoResolutionStatus.Unsupported,
                null,
                null,
                null,
                null,
                null,
                "Select an asset result to edit its package memo.");
        }

        if (selected.Kind != PackageSearchResultKind.Asset)
        {
            return new PackageMemoResolution(
                PackageMemoResolutionStatus.Unsupported,
                null,
                selected.PackageName,
                selected.PackagePath,
                null,
                null,
                "Package and source results do not own asset memos.");
        }

        if (selected.PackagePath is null
            || selected.IndexDirectory is null
            || selected.SourcePath is null
            || selected.AssetName is null
            || selected.PathId is null)
        {
            return new PackageMemoResolution(
                PackageMemoResolutionStatus.MissingOrStale,
                null,
                selected.PackageName,
                selected.PackagePath,
                null,
                null,
                "The selected asset result does not contain complete package provenance.");
        }

        PackageMemoCatalogEntry[] catalog = _packages.Packages
            .Where(package => package.Package is not null)
            .Select(package => new PackageMemoCatalogEntry(package.Name, package.Path, package.Package!))
            .ToArray();
        return _memoResolver.ResolveExact(
            catalog,
            new PackageMemoIdentity(
                selected.PackagePath,
                selected.IndexDirectory,
                selected.SourcePath,
                selected.AssetName,
                selected.PathId.Value));
    }

    private sealed record CriteriaSnapshot(
        string IncludeText,
        string ExcludeText,
        string PathIdText,
        bool ExactText,
        bool SearchMemos,
        bool MemoOnly,
        bool IncludePackageResults,
        bool IncludeSourceResults,
        bool IncludeAssetResults,
        SearchWorkspaceFieldSelection FieldSelection,
        string SelectedPaths);
}
