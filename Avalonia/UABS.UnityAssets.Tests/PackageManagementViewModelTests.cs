using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Dependencies;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class PackageManagementViewModelTests
{
    [Fact]
    public async Task ChoosePackageDirectory_LoadsPickerSelectionAndListsPackages()
    {
        FakePackageService packages = new([Entry("one", Document("one"))]);
        using PackageManagementViewModel viewModel = CreateViewModel(new QueuePicker("catalog"), packages);

        await viewModel.ChoosePackageDirectoryAsync();

        Assert.EndsWith("catalog", viewModel.PackageDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("one", Assert.Single(viewModel.Packages).Name);
        Assert.Equal("one", viewModel.SelectedPackage!.Name);
        Assert.Contains("1 package", viewModel.CatalogStatus, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadCatalog_ReportsEmptyDirectory()
    {
        using PackageManagementViewModel viewModel = CreateViewModel(packageService: new FakePackageService([]));

        await viewModel.LoadCatalogAsync("empty");

        Assert.Empty(viewModel.Packages);
        Assert.Null(viewModel.SelectedPackage);
        Assert.Equal("No package directories were found.", viewModel.CatalogStatus);
    }

    [Fact]
    public async Task LoadCatalog_ShowsMalformedAndWarningEntriesWithoutLosingValidPackages()
    {
        PackageDocument warning = Document("warning");
        warning.ValidationIssues.Add(new PackageValidationIssue(
            PackageIssueSeverity.Error, "missing-marker", "Validation marker missing."));
        FakePackageService packages = new([
            new PackageCatalogEntry("broken", "broken", null, "Malformed JSON."),
            Entry("warning", warning)
        ]);
        using PackageManagementViewModel viewModel = CreateViewModel(packageService: packages);

        await viewModel.LoadCatalogAsync("catalog");

        Assert.Equal(2, viewModel.Packages.Count);
        Assert.Equal("Error", viewModel.Packages[0].Status);
        Assert.Equal("Warning", viewModel.Packages[1].Status);
        Assert.Equal("warning", viewModel.SelectedPackage!.Name);
    }

    [Fact]
    public async Task RefreshCatalog_PreservesSelectionByPackagePath()
    {
        PackageCatalogEntry[] first = [Entry("one", Document("one")), Entry("two", Document("two"))];
        PackageCatalogEntry[] second = [Entry("one", Document("one")), Entry("two", Document("two"))];
        FakePackageService packages = new(first, second);
        using PackageManagementViewModel viewModel = CreateViewModel(packageService: packages);
        await viewModel.LoadCatalogAsync("catalog");
        await viewModel.SelectPackageAsync(viewModel.Packages[1]);

        await viewModel.RefreshCatalogAsync();

        Assert.Equal("two", viewModel.SelectedPackage!.Name);
        Assert.Equal(2, packages.ReadCatalogCalls);
    }

    [Fact]
    public async Task SwitchingPackages_ReplacesContentsAndClearsStaleSelection()
    {
        PackageDocument first = Document("one", Bundle("first.ab", "CAB-FIRST", Asset("First", 1)));
        PackageDocument second = Document("two", Bundle("second.ab", "CAB-SECOND", Asset("Second", 2)));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", first), Entry("two", second)]));
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SelectedContent = viewModel.Contents[1];

        await viewModel.SelectPackageAsync(viewModel.Packages[1]);

        Assert.Null(viewModel.SelectedContent);
        Assert.Equal(["second.ab", "Second"], viewModel.Contents.Select(item => item.Name));
    }

    [Fact]
    public async Task ContentSelection_FiltersDependenciesToItsSourceWithoutPathIdAssumptions()
    {
        PackageDocument request = Document("request",
            Bundle("a.ab", "CAB-A", dependencies: ["CAB-X"]),
            Bundle("b.ab", "CAB-B", dependencies: ["CAB-Y"]));
        PackageDocument candidates = Document("targets",
            Bundle("x.ab", "CAB-X"), Bundle("y.ab", "CAB-Y"));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("request", request), Entry("targets", candidates)]));
        await viewModel.LoadCatalogAsync("catalog");

        viewModel.SelectedContent = viewModel.Contents.Single(item => item.SourceName == "b.ab");

        PackageDependencyResultViewModel result = Assert.Single(viewModel.DependencyResults);
        Assert.Equal("CAB-Y", result.RequestedCabCode);
        Assert.Equal("b.ab", result.RequestingSource);
    }

    [Fact]
    public async Task Search_UsesExistingSyntaxAndReturnsProvenance()
    {
        PackageDocument package = Document("one", Bundle("source.ab", "CAB-A", Asset("Hero", 42, "important")));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]));
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SearchText = "hero";
        viewModel.SearchPackages = false;
        viewModel.SearchSources = false;

        await viewModel.SearchAsync();

        PackageSearchResult result = Assert.Single(viewModel.SearchResults);
        Assert.Equal(PackageSearchResultKind.Asset, result.Kind);
        Assert.Equal("one", result.PackageName);
        Assert.Equal("source.ab", result.SourceName);
        Assert.Equal(42, result.PathId);
    }

    [Fact]
    public async Task Search_FieldAndResultFiltersAreForwardedToSearchService()
    {
        PackageDocument package = Document("one", Bundle("source.ab", "CAB-A", Asset("Hero", 42, "needle")));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]));
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SearchText = "needle";
        viewModel.SearchField = PackageSearchFieldSelection.Memos;
        viewModel.SearchPackages = false;
        viewModel.SearchSources = false;
        viewModel.SearchAssets = true;

        await viewModel.SearchAsync();

        Assert.Equal(PackageSearchMatchCategory.Memo, Assert.Single(viewModel.SearchResults).MatchCategories);
    }

    [Fact]
    public async Task Search_InvalidPathIdShowsActionableError()
    {
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", Document("one"))]));
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SearchPathIdText = "not-a-number";

        await viewModel.SearchAsync();

        Assert.True(viewModel.HasSearchError);
        Assert.Empty(viewModel.SearchResults);
    }

    [Fact]
    public async Task Search_CancelsAndSuppressesStaleResults()
    {
        PackageBundleReference source = Bundle("source.ab", "CAB-A", Asset("Alpha", 1));
        source.AssetInfos.Add(Asset("Beta", 2));
        PackageDocument package = Document("one", source);
        ControlledSearchService search = new(blockCallNumber: 1);
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]), searchService: search);
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SearchText = "Alpha";
        Task first = viewModel.SearchAsync();
        await search.BlockedCallStarted.Task;

        viewModel.SearchText = "Beta";
        await viewModel.SearchAsync();
        search.ReleaseBlockedCall.SetResult();
        await first;

        Assert.True(search.BlockedToken.IsCancellationRequested);
        Assert.Equal("Beta", Assert.Single(viewModel.SearchResults).AssetName);
    }

    [Fact]
    public async Task DependencyInspection_DisplaysResolvedUnresolvedAndAmbiguousWithoutChoosingWinner()
    {
        PackageDocument request = Document("request", Bundle("request.ab", "CAB-A",
            dependencies: ["CAB-ONE", "CAB-MISSING", "CAB-DUP"]));
        PackageDocument targets = Document("targets",
            Bundle("one.ab", "CAB-ONE"),
            Bundle("dup-1.ab", "CAB-DUP"),
            Bundle("dup-2.ab", "CAB-DUP"));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("request", request), Entry("targets", targets)]));

        await viewModel.LoadCatalogAsync("catalog");

        Assert.Equal(3, viewModel.DependencyResults.Count);
        Assert.Equal(PackageDependencyStatus.Resolved, viewModel.DependencyResults[0].Status);
        Assert.Equal(PackageDependencyStatus.Unresolved, viewModel.DependencyResults[1].Status);
        PackageDependencyResultViewModel ambiguous = viewModel.DependencyResults[2];
        Assert.Equal(PackageDependencyStatus.Ambiguous, ambiguous.Status);
        Assert.Equal(2, ambiguous.CandidateCount);
        Assert.Equal(2, ambiguous.Candidates.Count);
    }

    [Fact]
    public async Task RapidPackageSelection_PreventsStaleDependencyPublication()
    {
        PackageDocument first = Document("first", Bundle("first.ab", "CAB-FIRST", dependencies: ["CAB-X"]));
        PackageDocument second = Document("second", Bundle("second.ab", "CAB-SECOND", dependencies: ["CAB-Y"]));
        PackageDocument targets = Document("targets", Bundle("x.ab", "CAB-X"), Bundle("y.ab", "CAB-Y"));
        ControlledDependencyResolver resolver = new(blockCallNumber: 2);
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([
                Entry("first", first), Entry("second", second), Entry("targets", targets)]),
            dependencyResolver: resolver);
        await viewModel.LoadCatalogAsync("catalog");

        Task stale = viewModel.SelectPackageAsync(viewModel.Packages[1]);
        await resolver.BlockedCallStarted.Task;
        await viewModel.SelectPackageAsync(viewModel.Packages[0]);
        resolver.ReleaseBlockedCall.SetResult();
        await stale;

        Assert.True(resolver.BlockedToken.IsCancellationRequested);
        Assert.Equal("first", viewModel.SelectedPackage!.Name);
        Assert.Equal("CAB-X", Assert.Single(viewModel.DependencyResults).RequestedCabCode);
    }

    [Fact]
    public async Task RefreshFailure_PreservesLastGoodCatalog()
    {
        FakePackageService packages = new(
            (IReadOnlyList<PackageCatalogEntry>)[Entry("one", Document("one"))],
            new IOException("disk unavailable"));
        using PackageManagementViewModel viewModel = CreateViewModel(packageService: packages);
        await viewModel.LoadCatalogAsync("catalog");

        await viewModel.RefreshCatalogAsync();

        Assert.Equal("one", Assert.Single(viewModel.Packages).Name);
        Assert.True(viewModel.HasCatalogError);
        Assert.Contains("previous", viewModel.CatalogStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NewPackage_WorksWithoutCatalogOrSelectedDirectory()
    {
        using PackageManagementViewModel viewModel = CreateViewModel();

        Assert.True(await viewModel.StartNewPackageAsync());

        Assert.True(viewModel.Editor.IsEditing);
        Assert.True(viewModel.Editor.IsNew);
        Assert.True(viewModel.Editor.IsDirty);
        Assert.Empty(viewModel.Editor.Items);
        Assert.Equal(string.Empty, viewModel.Editor.DestinationDirectory);
    }

    [Fact]
    public async Task SwitchingPackages_WithUnsavedEditor_RequiresDiscardConfirmation()
    {
        QueueConfirmation confirmations = new(false, true);
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", Document("one")), Entry("two", Document("two"))]),
            confirmations: confirmations);
        await viewModel.LoadCatalogAsync("catalog");
        await viewModel.StartNewPackageAsync();

        await viewModel.SelectPackageAsync(viewModel.Packages[1]);
        Assert.Equal("one", viewModel.SelectedPackage!.Name);
        Assert.True(viewModel.Editor.IsDirty);

        await viewModel.SelectPackageAsync(viewModel.Packages[1]);
        Assert.Equal("two", viewModel.SelectedPackage!.Name);
        Assert.False(viewModel.Editor.IsEditing);
        Assert.Equal(2, confirmations.Calls);
    }

    [Fact]
    public async Task AddSelectedAsset_UsesSelectedPackageWhenNoEditorIsOpen()
    {
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", Document("one"))]));
        await viewModel.LoadCatalogAsync("catalog");
        AssetEntry selected = new("Hero", "Texture2D", 28, 42, 100, "main.assets", @"C:\game\new.assets");

        Assert.True(await viewModel.AddSelectedAssetAsync(selected));

        Assert.True(viewModel.Editor.IsEditing);
        Assert.False(viewModel.Editor.IsNew);
        Assert.Contains(viewModel.Editor.Items, item => item.PathId == 42 && item.SourcePath == selected.SourcePath);
    }

    private static PackageManagementViewModel CreateViewModel(
        IFilePickerService? picker = null,
        IPackageService? packageService = null,
        IPackageSearchService? searchService = null,
        IPackageDependencyResolver? dependencyResolver = null,
        IUserConfirmationService? confirmations = null) =>
        new(
            picker ?? new QueuePicker((string?[])[null]),
            packageService ?? new FakePackageService(),
            searchService ?? new PackageSearchService(),
            dependencyResolver ?? new PackageDependencyResolver(),
            confirmations);

    private static PackageCatalogEntry Entry(string name, PackageDocument document) =>
        new(name, document.PackagePath!, document, null);

    private static PackageDocument Document(string name, params PackageBundleReference[] bundles) =>
        new([new PackageIndex(string.Empty, bundles)], $@"C:\packages\{name}");

    private static PackageBundleReference Bundle(
        string name,
        string cabCode,
        PackageAssetReference? asset = null,
        string[]? dependencies = null) =>
        new()
        {
            Name = name,
            Path = $@"C:\game\{name}",
            CabCode = cabCode,
            AssetInfos = asset is null ? [] : [asset],
            Dependencies = dependencies?.ToList() ?? []
        };

    private static PackageAssetReference Asset(string name, long pathId, string? memo = null) =>
        new() { Name = name, PathId = pathId, Memo = memo };

    private sealed class QueuePicker(params string?[] paths) : IFilePickerService
    {
        private readonly Queue<string?> _paths = new(paths);
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_paths.Count == 0 ? null : _paths.Dequeue());
    }

    private sealed class QueueConfirmation(params bool[] answers) : IUserConfirmationService
    {
        private readonly Queue<bool> _answers = new(answers);
        public int Calls { get; private set; }

        public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_answers.Count > 0 && _answers.Dequeue());
        }
    }

    private sealed class FakePackageService(params object[] responses) : IPackageService
    {
        private readonly Queue<object> _responses = new(NormalizeResponses(responses));
        public int ReadCatalogCalls { get; private set; }

        public Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(string packageParentPath, CancellationToken cancellationToken = default)
        {
            ReadCatalogCalls++;
            object response = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return response is Exception exception
                ? Task.FromException<IReadOnlyList<PackageCatalogEntry>>(exception)
                : Task.FromResult((IReadOnlyList<PackageCatalogEntry>)response);
        }

        public Task<PackageDocument> LoadAsync(string packagePath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> CreateAsync(string sourceRoot, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageBundleReference> CreateReferenceAsync(string sourcePath, IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null, CancellationToken cancellationToken = default)
        {
            PackageAssetSelection selection = selectedAssets?.Single() ?? new PackageAssetSelection("Asset", 1, "main.assets");
            return Task.FromResult(new PackageBundleReference
            {
                Name = Path.GetFileName(sourcePath),
                Path = sourcePath,
                AssetInfos = [new PackageAssetReference { Name = selection.Name, PathId = selection.PathId }]
            });
        }
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, PackageFolderSearchMode searchMode, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> BuildAndSaveAsync(string sourceRoot, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PackageSummary>> ListAsync(string packageParentPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        private static IEnumerable<object> NormalizeResponses(object[] values)
        {
            if (values.Length == 0)
                return [(IReadOnlyList<PackageCatalogEntry>)Array.Empty<PackageCatalogEntry>()];
            if (values.All(value => value is PackageCatalogEntry))
                return [(IReadOnlyList<PackageCatalogEntry>)values.Cast<PackageCatalogEntry>().ToArray()];
            return values;
        }
    }

    private sealed class ControlledSearchService(int blockCallNumber) : IPackageSearchService
    {
        private readonly PackageSearchService _inner = new();
        private int _calls;
        public TaskCompletionSource BlockedCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseBlockedCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken BlockedToken { get; private set; }

        public IReadOnlyList<PackageSearchResult> Search(IReadOnlyList<PackageSearchSource> sources, PackageSearchRequest request, CancellationToken cancellationToken = default) =>
            _inner.Search(sources, request, cancellationToken);

        public async Task<IReadOnlyList<PackageSearchResult>> SearchAsync(IReadOnlyList<PackageSearchSource> sources, PackageSearchRequest request, CancellationToken cancellationToken = default)
        {
            int call = Interlocked.Increment(ref _calls);
            if (call == blockCallNumber)
            {
                BlockedToken = cancellationToken;
                BlockedCallStarted.SetResult();
                await ReleaseBlockedCall.Task;
            }
            return _inner.Search(sources, request, CancellationToken.None);
        }
    }

    private sealed class ControlledDependencyResolver(int blockCallNumber) : IPackageDependencyResolver
    {
        private readonly PackageDependencyResolver _inner = new();
        private int _calls;
        public TaskCompletionSource BlockedCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseBlockedCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken BlockedToken { get; private set; }

        public IReadOnlyList<PackageDependencyResult> Resolve(IReadOnlyList<PackageDependencySource> sources, CancellationToken cancellationToken = default) => _inner.Resolve(sources, cancellationToken);
        public Task<IReadOnlyList<PackageDependencyResult>> ResolveAsync(IReadOnlyList<PackageDependencySource> sources, CancellationToken cancellationToken = default) => _inner.ResolveAsync(sources, cancellationToken);
        public IReadOnlyList<PackageDependencyResult> ResolveForPackage(IReadOnlyList<PackageDependencySource> catalog, PackageDependencySource requester, CancellationToken cancellationToken = default) => _inner.ResolveForPackage(catalog, requester, cancellationToken);

        public async Task<IReadOnlyList<PackageDependencyResult>> ResolveForPackageAsync(IReadOnlyList<PackageDependencySource> catalog, PackageDependencySource requester, CancellationToken cancellationToken = default)
        {
            int call = Interlocked.Increment(ref _calls);
            if (call == blockCallNumber)
            {
                BlockedToken = cancellationToken;
                BlockedCallStarted.SetResult();
                await ReleaseBlockedCall.Task;
            }
            return _inner.ResolveForPackage(catalog, requester, CancellationToken.None);
        }
    }
}
