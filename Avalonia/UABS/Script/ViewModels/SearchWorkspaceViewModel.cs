using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using UABS.Services;
using UABS.PackageV2.Search;
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
    public string ScopeKey => Package.V2 is { } v2 ? $"V2:{v2.Epoch}" : $"V1:{Path}";
    public string DisplayName => $"[{(Package.V2 is null ? "V1" : "V2")}] {Name}" + (Package.V2 is { } v ? $" · {v.Status}" : "");

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

    internal void UpdatePackage(PackageCatalogItemViewModel package)
    { Package = package; OnPropertyChanged(nameof(Name)); OnPropertyChanged(nameof(DisplayName)); OnPropertyChanged(nameof(Path)); }

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
    private readonly PackageSearchResult? _legacyResult;

    internal SearchResultItemViewModel(PackageSearchResult result)
    {
        _legacyResult = result;
        _memo = result.Memo;
    }

    internal SearchResultItemViewModel(SearchResult result, SearchRevision revision)
    { V2Result = result; V2Revision = revision; _memo = result.Memo; }
    // Preserve the V1 API without fabricating V1 identities for SQLite results.
    public PackageSearchResult Result => _legacyResult ?? throw new InvalidOperationException("V2 rows must use V2Result, never V1 memo/persistence APIs.");
    public SearchResult? V2Result { get; private set; }
    public SearchRevision? V2Revision { get; private set; }
    internal bool SameOccurrence(SearchResult result, SearchRevision revision) => V2Result?.Key == result.Key
        && V2Revision?.LocationEpoch == revision.LocationEpoch;
    internal void RefreshOccurrence(SearchResult result, SearchRevision revision)
    {
        V2Result = result; V2Revision = revision; Memo = result.Memo;
        OnPropertyChanged(nameof(V2Result)); OnPropertyChanged(nameof(V2Revision)); OnPropertyChanged(nameof(V2Details));
    }
    internal void ApplyVerifiedV2Memo(V2MemoPublication publication)
    {
        if (V2Result is not { } row || V2Revision?.LocationEpoch != publication.Registration.LocationEpoch) return;
        var target = publication.Change.Target;
        if (row.Key.PackageId != target.PackageId || row.Key.GenerationId != target.GenerationId || row.Key.SourceId != target.SourceId
            || row.Key.SourceRevision != target.SourceRevision || row.Key.DirectoryOrdinal != target.DirectoryOrdinal || row.PathId != target.PathId
            || row.MemoRevision >= publication.Change.MemoRevision) return;
        V2Result = row with { Memo = publication.Change.Memo, MemoRevision = publication.Change.MemoRevision };
        V2Revision = V2Revision! with { CatalogRevision = Math.Max(V2Revision.CatalogRevision, publication.Change.CatalogRevision) };
        Memo = publication.Change.Memo;
        OnPropertyChanged(nameof(V2Result)); OnPropertyChanged(nameof(V2Revision)); OnPropertyChanged(nameof(V2Details));
    }
    public bool IsV2 => V2Result is not null;
    public PackageSearchResultKind Kind => V2Result?.Key.Kind ?? Result!.Kind;
    public string Name => V2Result is { } v ? v.Key.Kind switch
        { PackageSearchResultKind.Package => v.PackageName, PackageSearchResultKind.SourceFile => v.SourceName ?? "<unnamed source>", _ => v.AssetName ?? "<unnamed asset>" }
        : Result!.AssetName ?? Result.SourceName ?? Result.PackageName;
    public string PackageName => V2Result?.PackageName ?? Result!.PackageName;
    public string? PackagePath => V2Result?.PackagePath ?? Result?.PackagePath;
    public string? IndexDirectory => V2Result is { } v ? v.IndexDirectory : Result?.IndexDirectory;
    public string? SourceName => V2Result is { } v ? v.SourceName : Result?.SourceName;
    public string? SourcePath => V2Result is { } v ? v.SourcePath : Result?.SourcePath;
    public string? CabCode => _legacyResult?.CabCode;
    public string? AssetName => V2Result is { } v ? v.AssetName : Result?.AssetName;
    public long? PathId => V2Result is { } v ? v.PathId : Result?.PathId;
    public object IconKind => Kind == PackageSearchResultKind.Asset && !string.IsNullOrWhiteSpace(V2Result?.TypeName)
        ? V2Result.TypeName : Kind;
    public string V2Details => V2Result is not { } v ? "" :
        $"V2 read-only indexed metadata\nRegistration: {V2Revision?.RegistrationId}\nLocation epoch: {V2Revision?.LocationEpoch}\nCatalog revision: {V2Revision?.CatalogRevision}\n{v.Key}\nPath ID: {v.PathId}\nClass ID: {v.ClassId}\nType: {v.TypeName ?? "<unknown>"}\nRaw name: {Raw(v.AssetName)}\nIdentity: {v.IdentityStatus ?? "not an asset"}\nMemo revision: {v.MemoRevision}\nMemo: {Raw(v.Memo)}";
    private static string Raw(string? text) => text is null ? "<missing>" : text.Length == 0 ? "<empty>" : text;
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
    public PackageSearchMatchCategory MatchCategories => V2Result?.MatchCategories ?? Result!.MatchCategories;

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
    private V2SearchOperation? _v2Operation;
    private V2SearchWindow? _v2Window;
    private CriteriaSnapshot? _v2Criteria;
    private long? _failedPage;
    private int _loadingDirection;
    private bool _viewportCancelled;
    private bool _workspaceActive = true;
    private bool _isLoadingPage;
    private long _lastViewportStart = -1;
    private bool _isStale;
    private string _countSummary = "0 loaded";
    private string _warnings = "";
    private string _classIdText = "";
    private string _typeNameText = "";
    private readonly Func<V2SearchService> _v2EngineFactory;
    private readonly InspectorMemoService? _v2Memos;

    public SearchWorkspaceViewModel(
        PackageManagementViewModel packages,
        IPackageSearchService searchService,
        IPackageMemoResolver? memoResolver = null,
        IMemoDraftCoordinator? memoDraftCoordinator = null,
        IPackageEditorSessionGuard? editorSessionGuard = null,
        Func<V2SearchService>? v2EngineFactory = null,
        InspectorMemoService? v2Memos = null)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _memoResolver = memoResolver ?? new PackageMemoResolver();
        _memoDrafts = memoDraftCoordinator ?? new UnavailableMemoDraftCoordinator();
        _v2EngineFactory = v2EngineFactory ?? (() => new V2SearchService());
        _v2Memos = v2Memos;
        if (v2Memos is not null) v2Memos.Published += OnVerifiedV2Memo;
        MemoInspector = new EditableMemoInspectorViewModel(
            _memoDrafts,
            editorSessionGuard ?? new PackageEditorSessionGuard());
        SearchCommand = new AsyncCommand(SearchAsync);
        RefreshSearchCommand = new AsyncCommand(SearchAsync);
        NextPageCommand = new AsyncCommand(NextPageAsync, () => CanReadNextPage);
        RetryPageCommand = new AsyncCommand(RetryPageAsync, () => CanRetryPage);
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
    public AsyncCommand RefreshSearchCommand { get; }
    public AsyncCommand NextPageCommand { get; }
    public AsyncCommand RetryPageCommand { get; }
    public event Action? ResultWindowChanging;
    public event Action? ResultWindowChanged;
    public V2SearchWindow? ResultWindow => _v2Window;
    public bool IsV2Search => _v2Operation is not null;
    public bool CanSortResultsLocally => !IsV2Search && (Results.Count == 0 || !Results[0].IsV2);
    public bool IsUpdatingResultWindow => _v2Window?.IsChanging == true;
    public bool IsLoadingPage { get => _isLoadingPage; private set { SetField(ref _isLoadingPage, value); OnPropertyChanged(nameof(IsInitialSearching)); } }
    public bool IsInitialSearching => IsSearching && !IsLoadingPage;
    public bool CanRetryPage => _failedPage.HasValue && !IsSearching && !IsStale && _v2Operation is not null;
    public bool IsStale { get => _isStale; private set { SetField(ref _isStale, value); NotifyPaging(); } }
    public bool CanReadNextPage => _v2Window?.CanForward == true && !IsStale && !IsSearching;
    public bool CanReadPreviousPage => _v2Window?.CanBackward == true && !IsStale && !IsSearching;
    public bool IsSelectionOutsideWindow => IsV2Search && SelectedResult is not null && !Results.Contains(SelectedResult);
    public bool IsV2ResultSelected => SelectedResult?.IsV2 == true;
    public bool CanShowMemoEditor => !IsV2ResultSelected || _v2Memos is not null;
    public string CountSummary { get => _countSummary; private set => SetField(ref _countSummary, value); }
    public string Warnings { get => _warnings; private set => SetField(ref _warnings, value); }
    public bool HasWarnings => Warnings.Length > 0;
    public string ClassIdText { get => _classIdText; set => SetCriteriaField(ref _classIdText, value ?? ""); }
    public string TypeNameText { get => _typeNameText; set => SetCriteriaField(ref _typeNameText, value ?? ""); }
    private void NotifyPaging()
    {
        OnPropertyChanged(nameof(CanReadNextPage)); OnPropertyChanged(nameof(CanReadPreviousPage));
        OnPropertyChanged(nameof(CanRetryPage)); OnPropertyChanged(nameof(ResultCount)); OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(IsSelectionOutsideWindow)); OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(IsV2Search)); OnPropertyChanged(nameof(CanSortResultsLocally));
        NextPageCommand?.NotifyCanExecuteChanged(); RetryPageCommand?.NotifyCanExecuteChanged();
    }
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
                if (value.Count == 0) CountSummary = "0 loaded";
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
            if (IsUpdatingResultWindow) return;
            PackageMemoIdentity? previousIdentity = MemoInspector.Identity;
            if (SetField(ref _selectedResult, value))
            {
                OnPropertyChanged(nameof(HasSelectedResult));
                OnPropertyChanged(nameof(IsV2ResultSelected));
                OnPropertyChanged(nameof(CanShowMemoEditor));
                OnPropertyChanged(nameof(IsSelectionOutsideWindow));
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
                NotifyPaging();
                OnPropertyChanged(nameof(IsInitialSearching));
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
        IsLoadingPage = false;
        IsSearching = false;
        Error = null;

        var scope = PackageScopes.Where(p => p.IsSelected).ToArray();
        if (scope.Any(p => p.Package.V2 is not null))
        {
            if (scope.Any(p => p.Package.V2 is null))
            {
                Error = "Select one package format per search (V1 or V2). Mixed-format federation is not supported.";
                Status = "Search was not run; previous rows were preserved.";
                return;
            }
            await RunV2SearchAsync(scope, version);
            return;
        }
        RetireV2Operation();
        IsStale = false; Warnings = ""; OnPropertyChanged(nameof(HasWarnings));
        if (!string.IsNullOrWhiteSpace(ClassIdText) || !string.IsNullOrWhiteSpace(TypeNameText))
        { Error = "Class ID / type-name filters require V2 asset-only search."; return; }

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
            CountSummary = $"{Results.Count:N0} loaded / {Results.Count:N0} exact";
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

    private async Task RunV2SearchAsync(SearchPackageScopeItemViewModel[] scope, int version)
    {
        if (!TryCreateRequest(out var legacy, out var validation))
        { Error = validation; Status = "Search was not run; previous rows were preserved."; return; }
        int? classId = null;
        if (!string.IsNullOrWhiteSpace(ClassIdText))
        {
            if (!int.TryParse(ClassIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            { Error = "Class ID must be a signed 32-bit number."; return; }
            classId = value;
        }
        if ((classId is not null || !string.IsNullOrWhiteSpace(TypeNameText)) && legacy!.ResultKinds != PackageSearchResultKinds.Asset)
        { Error = "Class ID / type-name filters require Assets only (disable Packages and Sources)."; return; }
        CriteriaSnapshot criteria = CaptureCriteria();
        var request = new SearchRequest(IncludeText, ExcludeText, legacy!.Fields, legacy.ResultKinds,
            ExactText, MemoOnly, legacy.PathId, ClassId: classId, TypeName: string.IsNullOrWhiteSpace(TypeNameText) ? null : TypeNameText);
        RetireV2Operation(); IsStale = false;
        _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var owned = _searchCancellation; IsSearching = true; Status = "Admitting selected V2 packages for Search...";
        try
        {
            var operation = await V2SearchOperation.OpenAsync(scope.Select(p => p.Package.V2!).ToArray(), request, owned.Token, _v2EngineFactory());
            if (version != _searchVersion || owned.IsCancellationRequested)
            { await operation.DisposeAsync(); return; }
            if (criteria.SelectedPaths != CaptureCriteria().SelectedPaths)
            {
                await operation.DisposeAsync(); MarkV2Stale();
                Error = "Selected package scope changed during admission. Refresh Search explicitly.";
                return;
            }
            _v2Operation = operation;
            _v2Criteria = criteria;
            var window = _v2Window = new V2SearchWindow((cursor, ct) => operation.ReadPageAsync(cursor, token: ct));
            window.Changing += () => { if (ReferenceEquals(Results, window.Rows)) ResultWindowChanging?.Invoke(); };
            window.Changed += () => { if (ReferenceEquals(Results, window.Rows)) ResultWindowChanged?.Invoke(); };
            _failedPage = null; _lastViewportStart = -1; NotifyPaging();
            operation.Invalidated += OnV2Invalidated;
            var page = await window.ReadAsync(0, owned.Token);
            if (version != _searchVersion || _v2Operation != operation) return;
            if (operation.Session.IsStale) { MarkV2Stale(); return; }
            if (!PublishV2Page(page, 0)) return;
        }
        catch (OperationCanceledException) when (owned.IsCancellationRequested)
        { if (version == _searchVersion) Status = "Search cancelled; previous rows were preserved."; }
        catch (Exception e)
        { if (version == _searchVersion) { Error = "V2 Search failed: " + e.Message; Status = "No new rows published."; } }
        finally
        {
            if (version == _searchVersion) { _searchCancellation = null; IsSearching = false; }
            owned.Dispose();
        }
    }

    public async Task NextPageAsync()
    {
        if (CanReadNextPage) await LoadWindowPageAsync(_v2Window!.EndPage + 1);
    }
    public Task PreviousPageAsync() => CanReadPreviousPage ? LoadWindowPageAsync(_v2Window!.StartPage - 1) : Task.CompletedTask;
    private Task RetryPageAsync() => CanRetryPage ? LoadWindowPageAsync(_failedPage!.Value) : Task.CompletedTask;

    public Task ReportViewportAsync(int first, int last)
    {
        if (!_workspaceActive || _disposed || IsStale || HasError || _v2Window is not { } window || first < 0 || last < first) return Task.CompletedTask;
        long global = window.StartIndex + first;
        bool backward = _lastViewportStart >= 0 && global < _lastViewportStart;
        _lastViewportStart = global;
        int direction = backward && first < V2SearchWindow.PrefetchRows && window.CanBackward ? -1
            : last >= window.Rows.Count - V2SearchWindow.PrefetchRows && window.CanForward ? 1
            : first < V2SearchWindow.PrefetchRows && window.CanBackward ? -1 : 0;
        if (IsSearching)
        {
            if (IsLoadingPage && ((_loadingDirection > 0 && last < window.Rows.Count - V2SearchWindow.PrefetchRows)
                || (_loadingDirection < 0 && first >= V2SearchWindow.PrefetchRows)))
            { _viewportCancelled = true; _searchCancellation?.Cancel(); }
            return Task.CompletedTask;
        }
        return direction == 0 ? Task.CompletedTask : LoadWindowPageAsync(direction < 0 ? window.StartPage - 1 : window.EndPage + 1);
    }

    private async Task LoadWindowPageAsync(long pageIndex)
    {
        if (IsSearching || IsStale || _v2Operation is not { } operation || _v2Window is not { } window) return;
        int version = _searchVersion;
        _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var owned = _searchCancellation; _loadingDirection = pageIndex < window.StartPage ? -1 : 1; _viewportCancelled = false;
        IsLoadingPage = true; IsSearching = true; Error = null;
        try
        {
            var page = await window.ReadAsync(pageIndex, owned.Token);
            if (version == _searchVersion && operation == _v2Operation && window == _v2Window)
            { if (operation.Session.IsStale) MarkV2Stale(); else if (!_viewportCancelled) PublishV2Page(page, pageIndex); }
        }
        catch (OperationCanceledException) { if (version == _searchVersion && !_viewportCancelled) { Error = "Cancelled page; rows retained. Retry to continue."; _failedPage = pageIndex; } }
        catch (Exception e)
        { if (version == _searchVersion) { Error = "V2 page failed; previous rows/checkpoints preserved: " + e.Message; _failedPage = pageIndex; } }
        finally
        {
            if (version == _searchVersion) { _searchCancellation = null; IsSearching = false; IsLoadingPage = false; NotifyPaging(); }
            owned.Dispose();
        }
    }

    private bool PublishV2Page(SearchPage page, long pageIndex)
    {
        if (page.Status != SearchStatus.Success)
        {
            if (page.Status == SearchStatus.StaleCursor) MarkV2Stale();
            Error = $"{page.Status}: " + string.Join("; ", page.Diagnostics.Select(d => d.Message));
            _failedPage = pageIndex;
            Status = "Page not published; previous rows and cursor preserved.";
            return false;
        }
        var window = _v2Window!;
        using var phase = SearchTrace.Start("collection-publication");
        bool replacing = !ReferenceEquals(Results, window.Rows);
        window.Apply(pageIndex, page, (r, revision) => {
            if (SelectedResult is { } selected && selected.SameOccurrence(r, revision))
            { selected.RefreshOccurrence(r, revision); return selected; }
            return new SearchResultItemViewModel(r, revision);
        });
        Results = window.Rows;
        if (SelectedResult?.IsV2 != true) SelectedResult = null;
        if (replacing) OnPropertyChanged(nameof(SelectedResult));
        _failedPage = null;
        CountSummary = $"{window.StartIndex + (Results.Count == 0 ? 0 : 1):N0}–{window.StartIndex + Results.Count:N0} displayed / "
            + (window.ExactCount is long exact ? $"{exact:N0} exact indexed matches" : $"at least {window.SeenRows:N0} indexed matches");
        Warnings = string.Join("\n", page.Diagnostics.GroupBy(d => (d.Code, d.Message))
            .Select(g => $"{g.Key.Code} ({g.Count()} package warning(s)): {g.Key.Message}"));
        OnPropertyChanged(nameof(HasWarnings));
        Status = "V2 · scroll to load either direction · bounded600-row window · global keyset order.";
        if (_v2Criteria is { } criteria)
        {
            _executedCriteria = criteria; LastExecutedCriteria = CreateCriteriaSummary(criteria);
            HasExecutedSearch = true; UpdateCriteriaChanged();
        }
        Error = null; NotifyPaging(); return true;
    }

    private void OnVerifiedV2Memo(V2MemoPublication publication)
    {
        if (_disposed) return;
        foreach (var row in Results) row.ApplyVerifiedV2Memo(publication);
        if (IsSelectionOutsideWindow) SelectedResult?.ApplyVerifiedV2Memo(publication);
        if (_v2Operation?.Session.Revisions.Any(r => r.LocationEpoch == publication.Registration.LocationEpoch
            && r.PackageId == publication.Change.Target.PackageId && r.GenerationId == publication.Change.Target.GenerationId
            && r.CatalogRevision < publication.Change.CatalogRevision) == true)
        {
            ++_searchVersion; CancelAndDisposeSearch(); IsSearching = false; _v2Operation.Invalidate(); MarkV2Stale();
        }
    }
    private void OnV2Invalidated(V2SearchOperation operation)
    {
        void Apply() { if (!_disposed && operation is not null && operation == _v2Operation) MarkV2Stale(); }
        if (Dispatcher.UIThread.CheckAccess()) Apply(); else Dispatcher.UIThread.Post(Apply);
    }

    private void MarkV2Stale()
    {
        IsStale = true;
        Status = "Search snapshot changed. Saved memos are shown; rows and selection stay put. Refresh Search when ready to continue scrolling.";
        CountSummary = $"{Results.Count:N0} displayed · stale count";
    }

    public async Task ActivateAsync()
    {
        _workspaceActive = true;
        var operation = _v2Operation;
        if (operation is null) return;
        try
        {
            bool current = await operation.CheckCurrentAsync(_lifetime.Token);
            if (!current && operation == _v2Operation && !_disposed) MarkV2Stale();
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { if (!_disposed && operation == _v2Operation) { MarkV2Stale(); Error = e.Message; } }
    }

    public void Deactivate() { _workspaceActive = false; CancelSearch(); }
    public Task RetirementTask { get; private set; } = Task.CompletedTask;
    private void RetireV2Operation()
    {
        var operation = _v2Operation; _v2Operation = null; _v2Window = null; _v2Criteria = null; _failedPage = null; NotifyPaging();
        if (operation is null) return;
        operation.Invalidated -= OnV2Invalidated;
        RetirementTask = ObserveRetirementAsync(operation);
    }
    private static async Task ObserveRetirementAsync(V2SearchOperation operation)
    {
        try { await operation.DisposeAsync().ConfigureAwait(false); }
        catch (Exception e) { Printer.PrintException("V2 Search session disposal failed.", e); }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _packages.PropertyChanged -= PackagesOnPropertyChanged;
        _packages.Editor.PropertyChanged -= EditorOnPropertyChanged;
        _memoDrafts.DraftChanged -= MemoDraftsOnDraftChanged;
        if (_v2Memos is not null) _v2Memos.Published -= OnVerifiedV2Memo;
        _searchVersion++;
        _lifetime.Cancel();
        CancelAndDisposeSearch();
        RetireV2Operation();
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
        if (draft.Identity is null || draft.Identity.V2 is not null
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
        !result.IsV2 && result.Kind == PackageSearchResultKind.Asset
        && result.PathId == identity.PathId
        && PackagePathComparer.Equals(result.PackagePath, identity.PackagePath)
        && string.Equals(result.IndexDirectory, identity.IndexDirectory, StringComparison.Ordinal)
        && PackagePathComparer.Equals(result.SourcePath, identity.SourcePath)
        && string.Equals(result.AssetName, identity.AssetName, StringComparison.Ordinal);

    private void ReconcilePackages()
    {
        Dictionary<string, SearchPackageScopeItemViewModel> existing = PackageScopes
            .ToDictionary(package => package.ScopeKey, StringComparer.OrdinalIgnoreCase);
        PackageCatalogItemViewModel[] available = _packages.Packages
            .Where(package => package.IsAvailable || package.V2 is not null)
            .ToArray();
        bool selectNew = !_scopeInitialized && available.Length > 0;
        List<SearchPackageScopeItemViewModel> reconciled = new(available.Length);

        foreach (PackageCatalogItemViewModel package in available)
        {
            string key = package.V2 is { } v2 ? $"V2:{v2.Epoch}" : $"V1:{package.Path}";
            if (existing.TryGetValue(key, out SearchPackageScopeItemViewModel? scope))
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
        if (_v2Operation is not null && _v2Operation.Session.Revisions.Any(r =>
            !available.Any(p => p.V2?.LocationEpoch == r.LocationEpoch))) MarkV2Stale();
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
        if (_v2Operation is not null)
        { ++_searchVersion; CancelAndDisposeSearch(); IsSearching = false; _v2Operation.Invalidate(); MarkV2Stale(); }
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
        ClassIdText,
        TypeNameText,
        string.Join('\u001f', PackageScopes.Where(package => package.IsSelected)
            .Select(package => package.ScopeKey)
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
        if (selected?.V2Result is { } v2 && selected.V2Revision is { } revision && _v2Memos is not null)
            return _v2Memos.ResolveSearch(v2, revision);
        if (selected?.IsV2 == true)
            return new(PackageMemoResolutionStatus.Unsupported, null, selected.PackageName, selected.PackagePath,
                null, null, "V2 Search memos are read-only in Gate5B; no V1 persistence route.");
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
        string ClassIdText,
        string TypeNameText,
        string SelectedPaths);
}
