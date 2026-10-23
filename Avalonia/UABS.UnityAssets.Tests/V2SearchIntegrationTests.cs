using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using UABS.PackageV2.Search;
using UABS.Services;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;
using UABS.Page;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Dependencies;

namespace UABS.UnityAssets.Tests;

public static class V2SearchIntegrationMeasurements
{
    /// <summary>Explicit disposable diagnostic only; not a Fact or a production acceptance budget.</summary>
    public static async Task MeasureAsync(int assets)
    {
        if (assets is not (10_000 or 100_000)) throw new ArgumentOutOfRangeException(nameof(assets));
        using var corpus = new V2SearchTests.Corpus(); var timer = System.Diagnostics.Stopwatch.StartNew();
        await corpus.Add(assets / 2, "A"); await corpus.Add(assets / 2, "B");
        foreach (var p in corpus.Packages) p.Legacy.Package.Indexes.Clear();
        Console.WriteLine($"GATE5B_SETUP assets={assets} packages=2 setup_ms={timer.Elapsed.TotalMilliseconds:F2} runtime={Environment.Version} fixture_build_excluded=true physical_cold=false");
        using var packages = V2SearchIntegrationTests.Packages(); using var ui = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        timer.Restart();
        foreach (var p in corpus.Packages) Assert.NotNull(await packages.V2.OpenAsync(p.Path, select: false));
        Console.WriteLine($"GATE5B_BROWSE_ADMISSION packages=2 ms={timer.Elapsed.TotalMilliseconds:F2}");
        var scope = packages.V2.Registrations.ToArray();
        (string name, SearchRequest request, int packages)[] queries =
        [
            ("single-broad", new("asset", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset), 1),
            ("path-id", new(PathId: -77, ResultKinds: PackageSearchResultKinds.Asset), 2),
            ("exact-name", new("Hero", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset, ExactText: true), 2),
            ("cjk-two", new("钩爪", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset), 2),
            ("multi-broad", new("asset", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset), 2),
            ("include-exclude", new("asset，钩爪", "asset_00000099", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset), 2)
        ];
        foreach (var query in queries)
        {
            timer.Restart(); await using var operation = await V2SearchOperation.OpenAsync(scope.Take(query.packages).ToArray(), query.request, default);
            double admission = timer.Elapsed.TotalMilliseconds;
            timer.Restart(); var first = await operation.ReadPageAsync(); double firstMs = timer.Elapsed.TotalMilliseconds;
            Assert.Equal(SearchStatus.Success, first.Status); Assert.InRange(first.Rows.Count, 1, 100);
            timer.Restart(); var second = first.NextCursor is null ? null : await operation.ReadPageAsync(first.NextCursor);
            double nextMs = timer.Elapsed.TotalMilliseconds;
            if (second is not null) { Assert.Equal(SearchStatus.Success, second.Status); Assert.InRange(second.Rows.Count, 1, 100); }
            Console.WriteLine($"GATE5B_QUERY name={query.name} packages={query.packages} admission_ms={admission:F2} first_ms={firstMs:F2} first_rows={first.Rows.Count} next_ms={(second is null ? "n/a" : nextMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))} next_rows={second?.Rows.Count ?? 0} count={first.Count.Kind}");
        }
        ui.SelectAllPackagesCommand.Execute(null); V2SearchIntegrationTests.AssetsOnly(ui); ui.FieldSelection = SearchWorkspaceFieldSelection.Assets; ui.IncludeText = "asset";
        timer.Restart(); await ui.SearchAsync(); double uiFirst = timer.Elapsed.TotalMilliseconds; Assert.Null(ui.Error); Assert.Equal(100, ui.Results.Count);
        timer.Restart(); await ui.NextPageAsync(); Assert.Null(ui.Error); Assert.Equal(100, ui.Results.Count);
        Console.WriteLine($"GATE5B_UI first_including_search_admission_ms={uiFirst:F2} next_ms={timer.Elapsed.TotalMilliseconds:F2} retained_rows={ui.Results.Count} total_indexed={assets}");
        using var cancellation = new CancellationTokenSource(); var interrupted = new System.Diagnostics.Stopwatch();
        await using (var operation = await V2SearchOperation.OpenAsync(scope, new("asset"), default,
            new V2SearchService(observe: (b, _) => { if (b == SearchBoundary.Predicate && !cancellation.IsCancellationRequested) { interrupted.Start(); cancellation.Cancel(); } })))
        {
            var cancelled = await operation.ReadPageAsync(token: cancellation.Token); Assert.Equal(SearchStatus.Cancelled, cancelled.Status);
            Console.WriteLine($"GATE5B_CANCEL signal_to_return_ms={interrupted.Elapsed.TotalMilliseconds:F2} rows={cancelled.Rows.Count}");
        }
        ui.Dispose(); await ui.RetirementTask;
        using var ownedCatalog = new V2BrowseCatalog(); var entry = await ownedCatalog.RegisterAsync(scope[0].Path);
        Task unload = Task.CompletedTask; int once = 0; interrupted.Reset();
        await using (var operation = await V2SearchOperation.OpenAsync([entry], new("asset"), default,
            new V2SearchService(observe: (b, _) => { if (b == SearchBoundary.Predicate && Interlocked.Exchange(ref once, 1) == 0) { interrupted.Start(); unload = ownedCatalog.UnloadAsync(entry); } })))
        {
            var unloaded = await operation.ReadPageAsync(); await unload; Assert.Equal(SearchStatus.StaleCursor, unloaded.Status);
            Console.WriteLine($"GATE5B_UNLOAD signal_to_return_ms={interrupted.Elapsed.TotalMilliseconds:F2} rows={unloaded.Rows.Count}");
        }
    }
}

public sealed class V2SearchIntegrationTests
{
    private sealed class Picker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
    internal static PackageManagementViewModel Packages() => new(new Picker(), new PackageService(), new PackageSearchService(), new PackageDependencyResolver());
    internal static void AssetsOnly(SearchWorkspaceViewModel search)
    { search.IncludePackageResults = false; search.IncludeSourceResults = false; }

    [AvaloniaFact]
    public async Task Workspace_UsesExistingGrid_ReadOnlyInspector_ExactIdentityAndBoundedPages()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250); await corpus.Add(20, "Other");
        using var packages = Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        var entry = await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Assert.NotNull(entry);
        await packages.V2.OpenAsync(corpus.Packages[1].Path, select: false);
        Assert.Equal(2, search.PackageScopes.Count); Assert.True(search.PackageScopes[0].IsSelected); Assert.False(search.PackageScopes[1].IsSelected);
        Assert.All(search.PackageScopes, s => Assert.StartsWith("[V2]", s.DisplayName));
        search.IncludeText = "asset"; AssetsOnly(search); await search.SearchAsync();
        Assert.Null(search.Error); Assert.Equal(100, search.Results.Count); Assert.Contains("at least 100", search.CountSummary);
        var firstKeys = search.Results.Select(r => r.V2Result!.Key).ToArray(); search.SelectedResult = search.Results[0]; await search.MemoTransitionTask;
        Assert.False(search.MemoInspector.IsEditable); Assert.True(search.IsV2ResultSelected);
        Assert.Equal(entry.PackageId, search.SelectedResult.V2Result!.Key.PackageId); Assert.Equal(3, search.SelectedResult.V2Result.Key.DirectoryOrdinal);
        Assert.Equal(entry.LocationEpoch, search.SelectedResult.V2Revision!.LocationEpoch);
        Assert.Contains("Memo revision:", search.SelectedResult.V2Details);
        Assert.Throws<InvalidOperationException>(() => search.SelectedResult.Result); // No fabricated V1 persistence target.
        var view = new SearchWorkspaceView { DataContext = search }; var window = new Window { Content = view, Width = 1400, Height = 800 }; window.Show();
        try
        {
            var grid = view.FindControl<DataGrid>("ResultsGrid")!; window.UpdateLayout();
            Assert.Contains("assetList", grid.Classes); Assert.Equal(52, grid.Columns[0].Width.Value); Assert.Equal(0, grid.Columns[0].MinWidth); Assert.Equal(80, grid.Columns[0].MaxWidth);
            Assert.True(grid.Columns[0].CanUserResize); Assert.Empty(((TextBlock)grid.Columns[0].Header!).Text!);
            Assert.Same(search.NextPageCommand, view.FindControl<Button>("NextSearchPageButton")!.Command);
            Assert.Same(search.RefreshSearchCommand, view.FindControl<Button>("RefreshSearchButton")!.Command);
            Assert.True(view.FindControl<SelectableTextBlock>("V2SearchDetails")!.IsVisible);
            Assert.False(view.FindControl<EditableMemoInspectorView>("SearchMemoInspector")!.IsEffectivelyVisible);
            await search.NextPageAsync(); Assert.Equal(100, search.Results.Count);
            Assert.DoesNotContain(search.Results, r => firstKeys.Contains(r.V2Result!.Key));
            await search.NextPageAsync(); Assert.Equal(32, search.Results.Count); Assert.False(search.CanReadNextPage); Assert.Contains("232 exact", search.CountSummary);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("Hero，钩爪", "", false, false)]
    [InlineData("Hero,钩爪", "hero", false, false)]
    [InlineData("Hero", "", true, false)]
    [InlineData("memo", "blocked", false, true)]
    [InlineData("钩爪", "", false, true)]
    public async Task Workspace_CriteriaMatchExistingEngine(string include, string exclude, bool exact, bool memos)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); await corpus.Add(name: "Second");
        using var packages = Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        foreach (var source in corpus.Packages) await packages.V2.OpenAsync(source.Path, select: false);
        search.SelectAllPackagesCommand.Execute(null); AssetsOnly(search);
        search.IncludeText = include; search.ExcludeText = exclude; search.ExactText = exact; search.SearchMemos = memos; search.MemoOnly = memos;
        var engine = new V2SearchService(); var registrations = await V2SearchTests.Register(engine, corpus);
        var request = new SearchRequest(include, exclude, Fields: PackageSearchFields.PackageName | PackageSearchFields.SourceName | PackageSearchFields.AssetName | (memos ? PackageSearchFields.Memo : 0),
            ResultKinds: PackageSearchResultKinds.Asset, ExactText: exact, RequireNonEmptyMemo: memos);
        using var session = engine.CreateSession(request, registrations.Select(r => r.RegistrationId).ToArray()).Session!;
        var expected = await engine.ReadPageAsync(session); await search.SearchAsync();
        Assert.Null(search.Error); Assert.Equal(expected.Rows.Select(r => r.Key), search.Results.Select(r => r.V2Result!.Key));
        Assert.Equal(expected.Rows.Select(r => r.Memo), search.Results.Select(r => r.Memo));
        foreach (var registration in registrations) await engine.UnloadAsync(registration.RegistrationId);
    }

    [AvaloniaFact]
    public async Task Workspace_NumericUnknownTypesNullableNamesAndNonAssetKindsRetainProvenance()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(name: "Package");
        using var packages = Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); AssetsOnly(search);
        search.PathIdText = "-77"; search.ClassIdText = "1234567"; search.TypeNameText = "unknowntype";
        await search.SearchAsync(); Assert.Equal(-77, Assert.Single(search.Results).PathId);
        Assert.Equal(1234567, search.Results[0].V2Result!.ClassId);
        search.PathIdText = "9"; await search.SearchAsync(); Assert.Null(Assert.Single(search.Results).AssetName); Assert.Contains("<missing>", search.Results[0].V2Details);
        search.ClassIdText = ""; search.TypeNameText = ""; search.PathIdText = ""; search.IncludeText = "Package";
        search.IncludePackageResults = true; search.IncludeAssetResults = false;
        await search.SearchAsync(); var package = Assert.Single(search.Results); Assert.Equal(PackageSearchResultKind.Package, package.Kind); Assert.Null(package.V2Result!.AssetIdentity);
        search.IncludePackageResults = false; search.IncludeSourceResults = true; search.IncludeText = "source";
        await search.SearchAsync(); var source = Assert.Single(search.Results); Assert.Equal(PackageSearchResultKind.SourceFile, source.Kind);
        Assert.NotNull(source.V2Result!.Key.SourceId); Assert.Null(source.V2Result.AssetIdentity);
        var retained = search.Results; search.IncludeText = "， ,"; await search.SearchAsync(); Assert.NotNull(search.Error); Assert.Same(retained, search.Results);
    }

    [AvaloniaFact]
    public async Task Workspace_RevisionChangePreservesRowsSelection_RefreshIsExplicit()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250, partial: true);
        using var packages = Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        var entry = await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Assert.NotNull(entry);
        search.IncludeText = "asset"; AssetsOnly(search); await search.SearchAsync();
        Assert.Contains("PartialIndex", search.Warnings); Assert.True(search.HasWarnings);
        var rows = search.Results; search.SelectedResult = rows[0]; var selected = search.SelectedResult;
        using (var connection = V2SearchTests.Connect(entry.Path))
        {
            connection.CreateFunction<string?, int>("uabs_has_text_v1", s => string.IsNullOrWhiteSpace(s) ? 0 : 1);
            using var command = connection.CreateCommand(); command.CommandText = "UPDATE asset SET memo='changed' WHERE table_ordinal=18"; command.ExecuteNonQuery();
        }
        await search.ActivateAsync(); Assert.True(search.IsStale); Assert.False(search.CanReadNextPage);
        Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult); Assert.Contains("stale", search.CountSummary);
        await search.NextPageAsync(); Assert.Same(rows, search.Results);
        await search.RefreshSearchCommand.ExecuteAsync(); Assert.False(search.IsStale); Assert.NotSame(rows, search.Results);
        Assert.Contains(search.Results, r => r.Memo == "changed");
    }

    [AvaloniaFact]
    public async Task Workspace_V1StillWorks_AndMixedFormatScopeIsNeverSilentlyIgnored()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        var legacy = new PackageDocument([new PackageIndex("characters", [new PackageBundleReference { Name = "legacy.bundle", Path = "legacy.bundle", AssetInfos = [new() { Name = "Hero", PathId = 42 }] }])]);
        await new PackageService().SaveAsync(legacy, Path.Combine(corpus.DirectoryPath, "legacy"));
        using var packages = Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        await packages.LoadCatalogAsync(corpus.DirectoryPath); search.ClearPackagesCommand.Execute(null);
        search.PackageScopes.Single(s => s.DisplayName.StartsWith("[V1]")).IsSelected = true;
        search.IncludeText = "Hero"; AssetsOnly(search); await search.SearchAsync();
        Assert.Null(search.Error); Assert.False(Assert.Single(search.Results).IsV2); Assert.Equal(42, search.Results[0].PathId);
        var old = search.Results; search.SelectAllPackagesCommand.Execute(null); await search.SearchAsync();
        Assert.Contains("one package format", search.Error); Assert.Same(old, search.Results);
    }

    [AvaloniaFact]
    public async Task Workspace_LateOlderCompletionCannotOverwriteNewQuery()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250);
        using var packages = Packages(); var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim(); int blocked = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () => new V2SearchService(observe: (b, _) =>
        {
            if (b == SearchBoundary.BeforePublication && Interlocked.CompareExchange(ref blocked, 1, 0) == 0)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException("Test release not signalled."); }
        }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); AssetsOnly(search); search.IncludeText = "asset";
        var first = search.SearchAsync();
        try
        {
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(15)); search.IncludeText = "Éclair"; await search.SearchAsync();
            Assert.Equal("Éclair", Assert.Single(search.Results).Name); release.Set(); await first;
            Assert.Equal("Éclair", Assert.Single(search.Results).Name); Assert.Null(search.Error);
        }
        finally { release.Set(); await first; }
    }

    [AvaloniaFact]
    public async Task Workspace_QueryBudgetFailureIsNotEmptySuccess()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250);
        using var packages = Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(),
            v2EngineFactory: () => new V2SearchService(new(MaxPredicateEvaluations: 1)));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); search.IncludeText = "asset"; AssetsOnly(search);
        await search.SearchAsync(); Assert.Contains("BudgetExceeded", search.Error); Assert.False(search.HasExecutedSearch);
    }

    [AvaloniaFact]
    public async Task Workspace_UnloadDuringFetchCannotPublishOrAdvanceCursor()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250);
        using var packages = Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int publications = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () => new V2SearchService(observe: (boundary, _) =>
        {
            if (boundary == SearchBoundary.BeforePublication && Interlocked.Increment(ref publications) == 2)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException(); }
        }));
        var entry = await packages.V2.OpenAsync(corpus.Packages[0].Path); await packages.V2.SelectionTask;
        search.IncludeText = "asset"; AssetsOnly(search); await search.SearchAsync(); var rows = search.Results;
        var next = search.NextPageAsync();
        try
        {
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(15)); await packages.V2.RemoveAsync(); release.Set(); await next;
            Assert.True(search.IsStale); Assert.False(search.CanReadNextPage); Assert.Same(rows, search.Results);
            Assert.Empty(search.PackageScopes); Assert.NotNull(entry);
        }
        finally { release.Set(); await next; }
    }

    [AvaloniaFact]
    public async Task Workspace_CancelledPageRetainsCursor_AndCriteriaAndLayoutSurviveNavigation()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250);
        using var packages = Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int publications = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () => new V2SearchService(observe: (boundary, _) =>
        {
            if (boundary == SearchBoundary.BeforePublication && Interlocked.Increment(ref publications) == 2)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException(); }
        }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); AssetsOnly(search); search.IncludeText = "asset"; await search.SearchAsync();
        var rows = search.Results; search.SelectedResult = rows[0]; var selected = search.SelectedResult;
        search.Layout.ControlsWidth = 380; search.Layout.ToggleControlsCommand.Execute(null);
        var next = search.NextPageAsync();
        try
        {
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(15)); search.Deactivate(); release.Set(); await next; await search.ActivateAsync();
            Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult); Assert.False(search.IsStale); Assert.True(search.CanReadNextPage);
            Assert.Contains("Cancelled", search.Error); Assert.Equal("asset", search.IncludeText); Assert.False(search.Layout.AreControlsVisible);
            await search.NextPageAsync(); Assert.Equal(100, search.Results.Count); Assert.NotSame(rows, search.Results);
        }
        finally { release.Set(); await next; }
    }

    [AvaloniaFact]
    public async Task Workspace_OnlySelectedPackagesAreSearched_ChangedCriteriaNeverRerunsImplicitly()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(36, "Selected"); await corpus.Add(36, "Other");
        using var packages = Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        var selected = await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); await packages.V2.OpenAsync(corpus.Packages[1].Path, select: false);
        search.IncludeText = "Hero"; AssetsOnly(search); await search.SearchAsync();
        Assert.All(search.Results, r => Assert.Equal(selected!.PackageId, r.V2Result!.Key.PackageId));
        var rows = search.Results; search.ExcludeText = "Hero"; Assert.True(search.CriteriaChanged); Assert.Same(rows, search.Results);
        search.PackageScopes[0].IsSelected = false; Assert.True(search.IsStale); Assert.Same(rows, search.Results);
        search.PackageScopes[1].IsSelected = true; await search.RefreshSearchCommand.ExecuteAsync(); Assert.Empty(search.Results); Assert.False(search.CriteriaChanged);
        Assert.Contains("0 exact", search.CountSummary);
    }

    [AvaloniaFact]
    public async Task Workspace_ScopeChangeAtAdmissionBoundaryCannotPublishOldScope()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); using var packages = Packages();
        SearchWorkspaceViewModel? search = null;
        using var owned = search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () =>
        {
            // Deterministic capture/admission boundary, before the operation is attached to the UI.
            search!.PackageScopes[0].IsSelected = false; return new V2SearchService();
        });
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); search.IncludeText = "Hero"; AssetsOnly(search);
        await search.SearchAsync(); Assert.True(search.IsStale); Assert.Empty(search.Results);
        Assert.False(search.CanReadNextPage); Assert.Contains("scope changed during admission", search.Error);
    }

    [Fact]
    public async Task ProductionOperation_FederatesExactScope_PagesWithoutGapsOrDuplicates()
    {
        using var corpus = new V2SearchTests.Corpus();
        await corpus.Add(210, "A"); await corpus.Add(210, "B"); await corpus.Add(20, "Unselected");
        using var catalog = new V2BrowseCatalog();
        var a = await catalog.RegisterAsync(corpus.Packages[0].Path);
        var b = await catalog.RegisterAsync(corpus.Packages[1].Path);
        await using var search = await V2SearchOperation.OpenAsync([a, b], new("asset", Fields: PackageSearchFields.AssetName,
            ResultKinds: PackageSearchResultKinds.Asset), default);
        List<SearchResult> all = []; SearchCursor? cursor = null;
        do
        {
            var page = await search.ReadPageAsync(cursor);
            Assert.Equal(SearchStatus.Success, page.Status); Assert.InRange(page.Rows.Count, 1, 100);
            Assert.Equal(page.HasMore ? SearchCountKind.LoadedAtLeast : SearchCountKind.Exact, page.Count.Kind);
            all.AddRange(page.Rows); cursor = page.NextCursor;
        } while (cursor is not null);
        Assert.Equal(384, all.Count); Assert.Equal(all.Count, all.Select(r => r.Key).Distinct().Count());
        Assert.All(all, r => Assert.Contains(r.Key.PackageId, new[] { a.PackageId, b.PackageId }));
        Assert.All(all.Take(192), r => Assert.Equal(a.PackageId, r.Key.PackageId));
        var invalid = await search.ReadPageAsync(pageSize: 501); Assert.Equal(SearchStatus.InvalidRequest, invalid.Status);
    }

    [Fact]
    public async Task ProductionOperation_ExternalMemoChangeInvalidates_ExplicitReadmissionSeesNewRevision()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        using var catalog = new V2BrowseCatalog(); var entry = await catalog.RegisterAsync(corpus.Packages[0].Path);
        await using var first = await V2SearchOperation.OpenAsync([entry], new(PathId: -77, ResultKinds: PackageSearchResultKinds.Asset), default);
        var before = await first.ReadPageAsync(); Assert.Equal(SearchStatus.Success, before.Status);
        using (var connection = V2SearchTests.Connect(entry.Path))
        {
            connection.CreateFunction<string?, int>("uabs_has_text_v1", s => string.IsNullOrWhiteSpace(s) ? 0 : 1);
            using var command = connection.CreateCommand(); command.CommandText = "UPDATE asset SET memo='new memo' WHERE path_id=-77"; command.ExecuteNonQuery();
        }
        var stale = await first.ReadPageAsync(); Assert.Equal(SearchStatus.StaleCursor, stale.Status); Assert.Empty(stale.Rows);
        await using var refreshed = await V2SearchOperation.OpenAsync([entry], new(PathId: -77, ResultKinds: PackageSearchResultKinds.Asset), default);
        var after = await refreshed.ReadPageAsync(); Assert.Equal("new memo", Assert.Single(after.Rows).Memo);
        Assert.True(after.Revisions.Single().CatalogRevision > before.Revisions.Single().CatalogRevision);
    }

    [Fact]
    public async Task ProductionOperation_UnloadInvalidatesAndCancellationNeverPublishesSuccess()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        using var catalog = new V2BrowseCatalog(); var entry = await catalog.RegisterAsync(corpus.Packages[0].Path);
        await using var operation = await V2SearchOperation.OpenAsync([entry], new("Hero"), default);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Equal(SearchStatus.Cancelled, (await operation.ReadPageAsync(token: cancellation.Token)).Status);
        await catalog.UnloadAsync(entry);
        Assert.Equal(SearchStatus.StaleCursor, (await operation.ReadPageAsync()).Status);
    }
}
