using UABS.PackageV2;
using UABS.PackageV2.Memos;
using UABS.UnityAssets.Packages.Memos;
using UABS.Services;
using UABS.ViewModels;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Metadata;
using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using System.Globalization;

namespace UABS.UnityAssets.Tests;

public sealed class V2MemoIntegrationTests
{
    private sealed class NeverDelay : IMemoDraftDelay
    { public Task DelayAsync(TimeSpan delay, CancellationToken token) => Task.Delay(Timeout.InfiniteTimeSpan, token); }
    private sealed class NoWrites : IPackageMemoPersistenceService
    {
        public int Calls;
        public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => Task.FromResult<string?>("revision");
        public Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default) { Calls++; throw new InvalidOperationException("No V1 write expected."); }
    }
    private sealed class Picker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
    private sealed class ManualDelay : IMemoDraftDelay
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<TaskCompletionSource> _pending = new();
        public Task DelayAsync(TimeSpan delay, CancellationToken token)
        {
            Assert.Equal(TimeSpan.FromSeconds(1.5), delay);
            var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); _pending.Enqueue(signal);
            return signal.Task.WaitAsync(token);
        }
        public void Pulse() { while (_pending.TryDequeue(out var signal)) signal.TrySetResult(); }
    }
    private sealed class Harness : IAsyncDisposable
    {
        public V2BrowseCatalog Catalog { get; } = new();
        public V2BrowseRegistration Entry { get; private set; } = null!;
        public InspectorMemoService Service { get; private set; } = null!;
        public MemoDraftCoordinator Drafts { get; private set; } = null!;
        public StoredAsset[] Rows { get; private set; } = [];
        public ManualDelay Delay { get; } = new();
        public NoWrites Legacy { get; } = new();
        public static async Task<Harness> Open(string path, Action<MemoBoundary>? boundary = null, bool ui = false)
        {
            var h = new Harness(); h.Entry = await h.Catalog.RegisterAsync(path);
            h.Service = new(h.Legacy, new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(),
                () => [h.Entry], boundary: boundary, dispatch: ui ? null : action => { action(); return Task.CompletedTask; });
            h.Drafts = new(h.Service, h.Delay);
            using var reader = (await PackageReader.OpenAsync(path)).Reader!; h.Rows = (await reader.ReadAssetsAsync(pageSize: 500)).ToArray();
            return h;
        }
        public async Task<PackageMemoIdentity> Track(int row = 0)
        {
            var resolution = Resolve(Service, Entry, Rows[row]); var identity = resolution.Identity!;
            await Drafts.TrackAsync(identity, resolution.Value); return identity;
        }
        public async ValueTask DisposeAsync() { await Drafts.DisposeAsync(); Catalog.Dispose(); await Service.CleanupTask; }
    }
    private static PackageMemoResolution Resolve(InspectorMemoService service, V2BrowseRegistration entry, StoredAsset row) =>
        service.ResolveIndexed(entry, row.Key.PackageId, row.Key.GenerationId, row.Key.SourceId, row.Key.SourceRevision,
            row.Key.DirectoryOrdinal, row.Key.PathId, row.IdentityStatus, row.Memo);

    [AvaloniaFact]
    public async Task ActualFilesystemIdentityError_IsReproducedThenFixed_WithSharedAssetsSearchAndIndexedDraft()
    {
        using var corpus = new V2SearchTests.Corpus(); string source = Path.Combine(corpus.DirectoryPath, "native.bundle"); File.Copy(TestPaths.BundleFixture, source);
        string output = Path.Combine(corpus.DirectoryPath, "native.uabs.sqlite");
        var build = await new PackageBuilder(new AssetsToolsV2MetadataExtractor(TestPaths.ClassData)).BuildAsync(new(output, Guid.NewGuid(), Guid.NewGuid(), "Native",
            [new("game", corpus.DirectoryPath, OperatingSystem.IsWindows() ? "windows-lexical-v1" : "posix-lexical-v1")],
            [new(0, "native.bundle", "native", new(source, Guid.NewGuid(), Guid.NewGuid()))]));
        Assert.True(build.Succeeded, build.Message);
        using var app = new MainWindowViewModel(new Picker(), new AssetsToolsAssetSessionFactory(TestPaths.ClassData));
        await app.LoadFileAsync(source); app.AssetList.SelectedAsset = app.AssetList.Assets[0]; await app.AssetList.MemoTransitionTask;
        Assert.Contains("No loaded package contains this asset identity", app.AssetList.MemoInspector.Explanation);
        var entry = await app.V2.OpenAsync(output, select: false); Assert.NotNull(entry); await app.AssetList.MemoTransitionTask;
        Assert.True(app.AssetList.MemoInspector.IsEditable, app.AssetList.MemoInspector.Explanation);
        var identity = app.AssetList.MemoInspector.Identity!; Assert.NotNull(identity.V2);
        app.AssetList.MemoInspector.MemoText = "钩爪 memo 🦉"; await app.AssetList.MemoInspector.SaveNowCommand.ExecuteAsync();
        Assert.Equal(MemoDraftSaveState.Saved, app.AssetList.MemoInspector.SaveState);
        // Production Search now projects bundles; retain the asset-search/memo API
        // assertions without pretending a source row owns a contained asset's memo.
        using var assetSearch = new SearchWorkspaceViewModel(app.Packages, new UABS.UnityAssets.Packages.Search.PackageSearchService(),
            app.InspectorMemos, app.MemoDrafts, app.InspectorMemos, v2Memos: app.InspectorMemos);
        assetSearch.PathIdText = identity.PathId.ToString(CultureInfo.InvariantCulture); assetSearch.IncludePackageResults = false; assetSearch.IncludeSourceResults = false;
        await assetSearch.SearchAsync(); Assert.Null(assetSearch.Error); assetSearch.SelectedResult = Assert.Single(assetSearch.Results); await assetSearch.MemoTransitionTask;
        Assert.True(assetSearch.MemoInspector.IsEditable, assetSearch.MemoInspector.Explanation);
        Assert.True(PackageMemoIdentityComparer.Same(identity, assetSearch.MemoInspector.Identity));
        app.V2.SelectedPackage = entry; await app.V2.SelectionTask;
        app.V2.SelectedAsset = app.V2.Assets.Single(a => a.Identity.PathId == identity.PathId); await app.V2.MemoTransitionTask;
        Assert.True(PackageMemoIdentityComparer.Same(identity, app.V2.MemoInspector.Identity));
        assetSearch.MemoInspector.MemoText = "shared pending";
        Assert.Equal("shared pending", app.AssetList.MemoInspector.MemoText); Assert.Equal("shared pending", app.V2.MemoInspector.MemoText);
        app.Search.IncludePackageResults = false; app.Search.IncludeAssetResults = false; app.Search.IncludeSourceResults = true;
        app.Search.FieldSelection = SearchWorkspaceFieldSelection.Assets; app.Search.PathIdText = identity.PathId.ToString(CultureInfo.InvariantCulture);
        await app.Search.SearchAsync(); Assert.Null(app.Search.Error); app.Search.SelectedResult = Assert.Single(app.Search.Results);
        await app.Search.MemoTransitionTask; Assert.False(app.Search.CanShowMemoEditor); Assert.Null(app.Search.SelectedResult.Memo);
        Assert.Equal("shared pending", app.AssetList.MemoInspector.MemoText); Assert.Equal("shared pending", assetSearch.MemoInspector.MemoText);
        var sortView = new UABS.Page.SearchWorkspaceView { DataContext = app.Search };
        var sortWindow = new Avalonia.Controls.Window { Content = sortView, Width = 1300, Height = 700 }; sortWindow.Show();
        try
        {
            sortWindow.UpdateLayout(); Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var sortButton = sortView.FindControl<Avalonia.Controls.Button>("SearchNameSortButton")!;
            var selectedBundle = app.Search.SelectedResult;
            for (int action = 0; action < 2; action++)
            {
                Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(sortButton)!
                    .GetProvider<Avalonia.Automation.Provider.IInvokeProvider>()!.Invoke();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs(); await sortView.HeaderSortTask;
                Assert.Same(selectedBundle, app.Search.SelectedResult);
                Assert.True(PackageMemoIdentityComparer.Same(identity, assetSearch.MemoInspector.Identity));
                Assert.Equal("shared pending", app.AssetList.MemoInspector.MemoText);
                Assert.Equal("shared pending", assetSearch.MemoInspector.MemoText);
                Assert.Equal("shared pending", app.V2.MemoInspector.MemoText);
            }
        }
        finally { sortWindow.Close(); }
        var rows = assetSearch.Results; var selected = assetSearch.SelectedResult;
        await assetSearch.MemoInspector.SaveNowCommand.ExecuteAsync();
        Assert.Equal(MemoDraftSaveState.Saved, assetSearch.MemoInspector.SaveState);
        Assert.Same(rows, assetSearch.Results); Assert.Same(selected, assetSearch.SelectedResult); Assert.Equal("shared pending", selected.Memo);
        Assert.Equal("shared pending", app.V2.SelectedAsset!.Memo); Assert.True(assetSearch.IsStale); Assert.False(assetSearch.CanReadNextPage);
        Assert.True(app.Search.IsStale); Assert.Null(app.Search.SelectedResult.Memo);
        using var reader = (await PackageReader.OpenAsync(output)).Reader!;
        var target = identity.V2!; var saved = await reader.ReadMemoAsync(new(target.PackageId, target.GenerationId, target.SourceId, target.SourceRevision, target.DirectoryOrdinal, target.PathId));
        Assert.Equal("shared pending", saved.Memo); Assert.Equal(2, saved.MemoRevision);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unicode 钩爪 😀")]
    public async Task RealSQLiteRouting_PreservesNullableValues_AndNeverCallsV1(string? memo)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); using var catalog = new V2BrowseCatalog();
        var entry = await catalog.RegisterAsync(corpus.Packages[0].Path); var legacy = new NoWrites();
        var service = new InspectorMemoService(legacy, new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(), () => [entry], dispatch: a => { a(); return Task.CompletedTask; });
        using var reader = (await PackageReader.OpenAsync(entry.Path)).Reader!; var row = (await reader.ReadAssetsAsync())[0];
        var resolution = Resolve(service, entry, row); Assert.True(resolution.IsAvailable); var identity = resolution.Identity!;
        var basis = await service.CaptureBasisAsync(identity, "stale UI text"); Assert.Null(basis.Memo);
        var result = await service.PersistAsync(new(identity, memo, basis.Memo, basis.Revision));
        Assert.True(result.IsSuccess, result.Diagnostic); Assert.Equal(0, legacy.Calls);
        using var fresh = (await PackageReader.OpenAsync(entry.Path)).Reader!;
        var saved = await fresh.ReadMemoAsync(MemoTarget.From(row)!); Assert.Equal(memo, saved.Memo);
        Assert.Equal(memo is null ? 0 : 1, saved.MemoRevision);
    }

    [Fact]
    public async Task SharedDebounce_FlushAndSaveNowUseTheSameSQLiteDraft()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); await using var h = await Harness.Open(corpus.Packages[0].Path);
        var identity = await h.Track(); var saved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Drafts.DraftChanged += (_, e) => { if (e.Draft.State == MemoDraftSaveState.Saved && e.Draft.DraftMemo == "debounced") saved.TrySetResult(); };
        h.Drafts.Edit(identity, "superseded before debounce"); h.Drafts.Edit(identity, "debounced"); h.Delay.Pulse();
        await saved.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("debounced", h.Drafts.GetSnapshot(identity).ConfirmedMemo); Assert.Equal(0, h.Legacy.Calls);
        h.Drafts.Edit(identity, "navigation"); Assert.Equal(MemoDraftSaveState.Saved, (await h.Drafts.FlushForNavigationAsync(identity)).State);
        h.Drafts.Edit(identity, "now"); Assert.Equal(MemoDraftSaveState.Saved, (await h.Drafts.SaveNowAsync(identity)).State);
        using var reader = (await PackageReader.OpenAsync(h.Entry.Path)).Reader!;
        var read = await reader.ReadMemoAsync(MemoTarget.From(h.Rows[0])!); Assert.Equal("now", read.Memo); Assert.Equal(3, read.MemoRevision);
    }

    [AvaloniaFact]
    public async Task CleanV2EditorExcludesInspector_RetryAfterExplicitGuardRelease()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); await using var h = await Harness.Open(corpus.Packages[0].Path, ui: true);
        var identity = await h.Track(); var registration = await h.Service.GetRegistrationAsync(h.Entry);
        var editor = await h.Service.V2Writer.OpenEditorAsync(registration, new(registration.Info.PackageId, registration.Info.GenerationId, registration.Info.CatalogRevision));
        Assert.NotNull(editor.Session); Assert.True(h.Service.IsPackageOpen(h.Entry.Path));
        using var inspector = new EditableMemoInspectorViewModel(h.Drafts, h.Service); var resolution = Resolve(h.Service, h.Entry, h.Rows[0]);
        await inspector.ShowAsync(resolution); Assert.False(inspector.IsEditable); Assert.Contains("Package Editor", inspector.Explanation);
        h.Drafts.Edit(identity, "retained while editor open"); var blocked = await h.Drafts.SaveNowAsync(identity);
        Assert.Equal(MemoDraftSaveState.Failed, blocked.State); Assert.Equal(PackageMemoPersistenceStatus.PackageEditorActive, blocked.LastPersistenceStatus);
        Assert.True(h.Service.IsPackageOpen(h.Entry.Path)); editor.Session!.Dispose();
        await inspector.RefreshEligibilityAsync(); Assert.True(inspector.IsEditable); Assert.Equal("retained while editor open", inspector.MemoText);
        Assert.Equal(MemoDraftSaveState.Saved, (await h.Drafts.RetryAsync(identity)).State);
    }

    private static void Edit(string path, string sql)
    {
        using var connection = V2SearchTests.Connect(path); connection.CreateFunction<string?, int>("uabs_has_text_v1", s => string.IsNullOrWhiteSpace(s) ? 0 : 1);
        using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
    }

    [Fact]
    public async Task MemoAndExpectedOriginalConflictsNeverOverwriteExternalData()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); await using var h = await Harness.Open(corpus.Packages[0].Path);
        var identity = await h.Track(); h.Drafts.Edit(identity, "my draft");
        Edit(h.Entry.Path, "UPDATE asset SET memo='external' WHERE path_id=-77");
        var failed = await h.Drafts.SaveNowAsync(identity); Assert.Equal(PackageMemoPersistenceStatus.RevisionConflict, failed.LastPersistenceStatus);
        Assert.Equal("my draft", failed.DraftMemo); Assert.Equal(MemoDraftSaveState.Failed, failed.State);
        var basis = await h.Service.CaptureBasisAsync(identity, null);
        var wrongOriginal = await h.Service.PersistAsync(new(identity, "overwrite", "wrong original", basis.Revision));
        Assert.Equal(PackageMemoPersistenceStatus.RevisionConflict, wrongOriginal.Status);
        Assert.Equal(MemoDraftSaveState.Failed, (await h.Drafts.RetryAsync(identity)).State);
        using var reader = (await PackageReader.OpenAsync(h.Entry.Path)).Reader!; Assert.Equal("external", (await reader.ReadMemoAsync(MemoTarget.From(h.Rows[0])!)).Memo);
    }

    [Theory]
    [InlineData("generation")]
    [InlineData("missing")]
    [InlineData("ambiguous")]
    public async Task InvalidatedIdentityCannotBecomeWritable(string kind)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); await using var h = await Harness.Open(corpus.Packages[0].Path);
        var identity = await h.Track(); h.Drafts.Edit(identity, "must be retained");
        Edit(h.Entry.Path, kind == "generation" ? "UPDATE package SET generation_uuid=randomblob(16)" : kind == "missing"
            ? "DELETE FROM asset WHERE path_id=-77; UPDATE logical_file SET declared_asset_count=declared_asset_count-1"
            : "UPDATE asset SET path_id=-77,identity_status='ambiguous' WHERE path_id IN(-77,5)");
        var failed = await h.Drafts.SaveNowAsync(identity); Assert.Equal(MemoDraftSaveState.Failed, failed.State);
        Assert.Equal("must be retained", failed.DraftMemo); Assert.Equal(0, h.Legacy.Calls);
    }

    [Theory]
    [InlineData(MemoBoundary.BeforeCommit, false)]
    [InlineData(MemoBoundary.AfterCommit, true)]
    public async Task CancellationRespectsCommitBoundary(MemoBoundary boundary, bool committed)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); using var cancellation = new CancellationTokenSource();
        await using var h = await Harness.Open(corpus.Packages[0].Path, b => { if (b == boundary) cancellation.Cancel(); });
        var identity = await h.Track(); var basis = await h.Service.CaptureBasisAsync(identity, null);
        var result = await h.Service.PersistAsync(new(identity, "committed", null, basis.Revision), cancellation.Token);
        Assert.Equal(committed, result.IsSuccess); Assert.Equal(committed ? PackageMemoCommitState.Committed : PackageMemoCommitState.NotCommitted, result.CommitState);
        using var reader = (await PackageReader.OpenAsync(h.Entry.Path)).Reader!; Assert.Equal(committed ? "committed" : null, (await reader.ReadMemoAsync(MemoTarget.From(h.Rows[0])!)).Memo);
    }

    [Fact]
    public async Task ExternalSupersessionAfterCommitRetainsDraftAndReportsConflict()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        await using var h = await Harness.Open(path, b => { if (b == MemoBoundary.BeforeVerification) Edit(path, "UPDATE asset SET memo='newer external' WHERE path_id=-77"); });
        var identity = await h.Track(); h.Drafts.Edit(identity, "my committed draft"); var failed = await h.Drafts.SaveNowAsync(identity);
        Assert.Equal(MemoDraftSaveState.Failed, failed.State); Assert.Equal(PackageMemoPersistenceStatus.CommittedButSuperseded, failed.LastPersistenceStatus);
        Assert.Equal("my committed draft", failed.DraftMemo);
        using var reader = (await PackageReader.OpenAsync(path)).Reader!; Assert.Equal("newer external", (await reader.ReadMemoAsync(MemoTarget.From(h.Rows[0])!)).Memo);
    }

    [Fact]
    public async Task PublicationFailureRetainsDraft_RetryReverifiesAndPublishesRatherThanReportingFalseSaved()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); await using var h = await Harness.Open(corpus.Packages[0].Path);
        int publications = 0; Action<V2MemoPublication> fail = _ => { publications++; throw new IOException("simulated UI publication failure"); };
        h.Service.Published += fail; var identity = await h.Track(); h.Drafts.Edit(identity, "persisted draft");
        var failed = await h.Drafts.SaveNowAsync(identity); Assert.Equal(MemoDraftSaveState.Failed, failed.State);
        Assert.Equal(PackageMemoPersistenceStatus.CatalogPublicationFailure, failed.LastPersistenceStatus);
        h.Service.Published -= fail; h.Service.Published += _ => publications++;
        var retried = await h.Drafts.RetryAsync(identity); Assert.Equal(MemoDraftSaveState.Saved, retried.State); Assert.Equal(2, publications);
    }

    [AvaloniaFact]
    public async Task VerifiedSynchronizationUsesExactIdentity_AllVisibleRows_AndRetainsNewerDraftAndSelection()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(sourceCount: 2);
        using var release = new ManualResetEventSlim(); var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int blocked = 0;
        await using var h = await Harness.Open(corpus.Packages[0].Path, boundary: b =>
        {
            if (b == MemoBoundary.AfterCommit && Interlocked.Exchange(ref blocked, 1) == 0)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); }
        }, ui: true);
        using var packages = new PackageManagementViewModel(new Picker(), new PackageService(), new UABS.UnityAssets.Packages.Search.PackageSearchService(), new UABS.UnityAssets.Packages.Dependencies.PackageDependencyResolver());
        packages.V2.Registrations.Add(h.Entry);
        using var search = new SearchWorkspaceViewModel(packages, new UABS.UnityAssets.Packages.Search.PackageSearchService(),
            memoDraftCoordinator: h.Drafts, editorSessionGuard: h.Service, v2Memos: h.Service);
        search.PathIdText = "-77"; search.IncludePackageResults = false; search.IncludeSourceResults = false; await search.SearchAsync(); Assert.Equal(2, search.Results.Count);
        var first = search.Results[0]; var unrelated = search.Results[1];
        var copy = (SearchResultItemViewModel)Activator.CreateInstance(typeof(SearchResultItemViewModel), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, [first.V2Result!, first.V2Revision!], null)!;
        var rows = new[] { first, copy, unrelated };
        typeof(SearchWorkspaceViewModel).GetField("_results", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(search, rows);
        search.SelectedResult = first; await search.MemoTransitionTask; var identity = search.MemoInspector.Identity!;
        int notifications = 0; copy.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(copy.Memo)) { Assert.True(Avalonia.Threading.Dispatcher.UIThread.CheckAccess()); notifications++; } };
        h.Drafts.Edit(identity, "older save"); var save = h.Drafts.SaveNowAsync(identity);
        try
        {
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(10)); h.Drafts.Edit(identity, "newer retained draft");
            search.SelectedResult = unrelated; var transition = search.MemoTransitionTask; release.Set();
            var completion = await save; Assert.Equal("newer retained draft", completion.DraftMemo); Assert.NotEqual(MemoDraftSaveState.Saved, completion.State);
            await transition;
            Assert.Equal("newer retained draft", first.Memo); Assert.Equal(first.Memo, copy.Memo); Assert.Null(unrelated.Memo);
            Assert.True(notifications > 0); Assert.Same(rows, search.Results); Assert.Same(unrelated, search.SelectedResult);
            Assert.True(search.IsStale); Assert.False(search.CanReadNextPage); Assert.Equal("-77", search.PathIdText); Assert.Equal(0, h.Legacy.Calls);
            var v = identity.V2!;
            var stale = new V2MemoPublication(h.Entry, new(new(v.PackageId, v.GenerationId, v.SourceId, v.SourceRevision, v.DirectoryOrdinal, v.PathId), "stale completion", 1, 1));
            typeof(SearchResultItemViewModel).GetMethod("ApplyVerifiedV2Memo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(copy, [stale]);
            Assert.Equal("newer retained draft", copy.Memo);
        }
        finally { release.Set(); await save; }
    }

    [Theory]
    [InlineData(MemoBoundary.BeforeCommit, false)]
    [InlineData(MemoBoundary.AfterCommit, true)]
    public async Task UnloadDuringWriteDoesNotRetargetDraftOrClaimRollbackAfterCommit(MemoBoundary boundary, bool committed)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); Harness? captured = null; Task unload = Task.CompletedTask;
        await using var h = captured = await Harness.Open(corpus.Packages[0].Path, b => { if (b == boundary) unload = captured!.Catalog.UnloadAsync(captured.Entry); });
        var identity = await h.Track(); h.Drafts.Edit(identity, "retained on unload"); var result = await h.Drafts.SaveNowAsync(identity);
        await unload; await h.Service.CleanupTask; Assert.Equal(MemoDraftSaveState.Failed, result.State); Assert.Equal("retained on unload", result.DraftMemo);
        Assert.Equal(committed ? PackageMemoPersistenceStatus.CatalogPublicationFailure : PackageMemoPersistenceStatus.CancelledBeforeCommit, result.LastPersistenceStatus);
        using var reader = (await PackageReader.OpenAsync(h.Entry.Path)).Reader!; Assert.Equal(committed ? "retained on unload" : null, (await reader.ReadMemoAsync(MemoTarget.From(h.Rows[0])!)).Memo);
        var reopened = await h.Catalog.RegisterAsync(h.Entry.Path); var resolution = Resolve(h.Service, reopened, h.Rows[0]);
        Assert.False(PackageMemoIdentityComparer.Same(identity, resolution.Identity)); Assert.Equal("retained on unload", h.Drafts.GetSnapshot(identity).DraftMemo);
    }
    [Fact]
    public void SharedCoordinator_V2KeysAreDomainAndRegistrationSeparated()
    {
        var address = new V2MemoIdentity(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, -77);
        var first = new PackageMemoIdentity("package.uabs.sqlite", "", "", "", -77, address);
        var reopened = first with { V2 = address with { RegistrationEpoch = Guid.NewGuid() } };
        using var drafts = new MemoDraftCoordinator(new NoWrites(), new NeverDelay());
        drafts.Track(first, null, "v2:0:0"); drafts.Track(reopened, "other", "v2:0:0"); drafts.Edit(first, "draft");
        Assert.Equal("draft", drafts.GetSnapshot(first).DraftMemo); Assert.Equal("other", drafts.GetSnapshot(reopened).DraftMemo);
        Assert.True(PackageMemoIdentityComparer.Same(first, first)); Assert.False(PackageMemoIdentityComparer.Same(first, reopened));
        var legacy = new PackageMemoIdentity("package.uabs.sqlite", "index", "source", "name", -77);
        drafts.Track(legacy, "legacy", "revision"); Assert.Equal("legacy", drafts.GetSnapshot(legacy).DraftMemo);
        Assert.False(PackageMemoIdentityComparer.Same(first, legacy));
    }

    [Fact]
    public async Task BoundedReads_RequireFullAddressAndSessionSourceEvidence()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        using var reader = (await PackageReader.OpenAsync(corpus.Packages[0].Path)).Reader!;
        var first = (await reader.ReadAssetsAsync())[0]; var target = MemoTarget.From(first)!;
        var snapshot = await reader.ReadMemoAsync(target); Assert.Null(snapshot.Memo); Assert.Equal(first.MemoRevision, snapshot.MemoRevision);
        string path = corpus.Packages[0].Legacy.Package.Bundles.First().Path!;
        Assert.Equal(target, Assert.Single(await reader.FindMemoTargetsAsync(path, new string('A', 64), "CAB-shared", -77)));
        Assert.Empty(await reader.FindMemoTargetsAsync(path, new string('B', 64), "CAB-shared", -77));
        Assert.Empty(await reader.FindMemoTargetsAsync(path, new string('A', 64), "wrong logical", -77));
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadMemoAsync(target with { SourceRevision = Guid.NewGuid() }));
    }

    [Fact]
    public async Task ActualNativeSession_CapturesStableShaForMemoLinking()
    {
        using var session = await new AssetsToolsAssetSessionFactory(TestPaths.ClassData).OpenAsync(TestPaths.BundleFixture);
        using var stream = File.OpenRead(TestPaths.BundleFixture);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)), session.Metadata.SourceSha256);
        Assert.Equal(43, session.Assets.Count);
    }

    [Fact]
    public async Task NativeBridgeRequiresCapturedEvidence_AndRejectsMultipleExactPackageOccurrences()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        string first = corpus.Packages[0].Path, copy = Path.Combine(corpus.DirectoryPath, "second.uabs.sqlite");
        File.Copy(first, copy); Edit(copy, "UPDATE package SET package_uuid=randomblob(16),generation_uuid=randomblob(16)");
        using var catalog = new V2BrowseCatalog(); var one = await catalog.RegisterAsync(first); var two = await catalog.RegisterAsync(copy);
        var loaded = new List<V2BrowseRegistration> { one };
        var service = new InspectorMemoService(new NoWrites(), new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(), () => loaded);
        string source = corpus.Packages[0].Legacy.Package.Bundles.First().Path!;
        var lookup = new AssetMemoLookup(source, "wrong name is not an identity", -77, false, "CAB-shared", new string('A', 64));
        var unique = await service.ResolveAssetAsync([], lookup); Assert.True(unique.IsAvailable); Assert.NotNull(unique.Identity!.V2);
        Assert.False((await service.ResolveAssetAsync([], lookup with { SourceSha256 = null })).IsAvailable);
        Assert.False((await service.ResolveAssetAsync([], lookup with { SourceSha256 = new string('B', 64) })).IsAvailable);
        loaded.Add(two); var ambiguous = await service.ResolveAssetAsync([], lookup);
        Assert.Equal(PackageMemoResolutionStatus.Ambiguous, ambiguous.Status); Assert.Null(ambiguous.Identity);
    }

    [Fact]
    public async Task EditorGuardForOnePackageDoesNotBlockAnotherPackage()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); await corpus.Add();
        using var catalog = new V2BrowseCatalog(); var one = await catalog.RegisterAsync(corpus.Packages[0].Path); var two = await catalog.RegisterAsync(corpus.Packages[1].Path);
        var service = new InspectorMemoService(new NoWrites(), new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(), () => [one, two], dispatch: a => { a(); return Task.CompletedTask; });
        var registration = await service.GetRegistrationAsync(one);
        var editor = await service.V2Writer.OpenEditorAsync(registration, new(registration.Info.PackageId, registration.Info.GenerationId, registration.Info.CatalogRevision));
        using var lease = editor.Session; Assert.NotNull(lease); Assert.True(service.IsPackageOpen(one.Path)); Assert.False(service.IsPackageOpen(two.Path));
        using var reader = (await PackageReader.OpenAsync(two.Path)).Reader!; var row = (await reader.ReadAssetsAsync())[0];
        var identity = Resolve(service, two, row).Identity!; var basis = await service.CaptureBasisAsync(identity, row.Memo);
        var result = await service.PersistAsync(new(identity, "other package remains writable", basis.Memo, basis.Revision));
        Assert.True(result.IsSuccess, result.Diagnostic);
    }

    [AvaloniaFact]
    public async Task UnloadFlushCannotClearANewerPackageSelection()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); await corpus.Add();
        using var vm = new V2BrowseViewModel(new Picker());
        var first = (await vm.OpenAsync(corpus.Packages[0].Path))!;
        var second = (await vm.OpenAsync(corpus.Packages[1].Path, select: false))!;
        using var release = new ManualResetEventSlim(); var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new InspectorMemoService(new NoWrites(), new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(),
            () => vm.Registrations.ToArray(), boundary: b => { if (b == MemoBoundary.BeforeCommit) { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); } });
        using var drafts = new MemoDraftCoordinator(service, new ManualDelay()); vm.ConfigureMemos(service, drafts);
        vm.SelectedAsset = vm.Assets[0]; await vm.MemoTransitionTask; vm.MemoInspector.MemoText = "flush captured package";
        Task remove = vm.RemoveAsync();
        try
        {
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
            vm.SelectedPackage = second; await vm.SelectionTask; var rows = vm.Assets;
            vm.SelectedAsset = rows[0]; Task presentation = vm.MemoTransitionTask; release.Set();
            await remove; await presentation;
            Assert.Same(second, vm.SelectedPackage); Assert.Same(rows, vm.Assets); Assert.Same(rows[0], vm.SelectedAsset);
            Assert.DoesNotContain(first, vm.Registrations); Assert.Contains(second, vm.Registrations);
            Assert.Equal(second.Epoch, vm.MemoInspector.Identity!.V2!.RegistrationEpoch);
            Assert.True(vm.MemoInspector.IsEditable);
        }
        finally { release.Set(); await remove; }
    }
}
