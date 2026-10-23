using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Data.Sqlite;
using UABS.Page;
using UABS.PackageV2.Search;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class V2SearchHeaderSortTests
{
    private static void Configure(SearchWorkspaceViewModel search)
    {
        V2SearchIntegrationTests.AssetsOnly(search);
        search.IncludeText = "sort"; search.FieldSelection = SearchWorkspaceFieldSelection.Names; search.SearchMemos = true;
    }
    private static void SeedSearchMemo(V2SearchTests.Corpus corpus)
    {
        // Disposable fixtures only. Match through memos so NULL/empty raw names
        // remain in the tested universe without changing production validation.
        foreach (var package in corpus.Packages)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = package.Path, Pooling = false }.ToString());
            connection.Open(); connection.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
            using var command = connection.CreateCommand(); command.CommandText = "UPDATE asset SET memo='sort'"; command.ExecuteNonQuery();
            foreach (var index in package.Legacy.Package.Indexes)
                foreach (var bundle in index.Bundles)
                    foreach (var asset in bundle.AssetInfos!) asset.Memo = "sort";
        }
    }
    private static DataGridColumnHeader Header(DataGrid grid, int index) => grid.GetVisualDescendants()
        .OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, grid.Columns[index].Header));
    private static void Click(Window window, Control control)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }
    private static async Task ClickSort(Window window, SearchWorkspaceView view, DataGrid grid, int index)
    {
        Click(window, Header(grid, index)); await view.HeaderSortTask;
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTheory]
    [InlineData(1, PackageSearchSortField.AssetName)]
    [InlineData(2, PackageSearchSortField.PackageName)]
    [InlineData(3, PackageSearchSortField.SourceName)]
    [InlineData(4, PackageSearchSortField.PathId)]
    public async Task ActualHeadersToggleGlobalOrderAndNativeIndicators(int column, PackageSearchSortField field)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(30); SeedSearchMemo(corpus);
        using var packages = V2SearchIntegrationTests.Packages();
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        Configure(search); await search.SearchAsync();
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1900, Height = 650 }; window.Show();
        try
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            var grid = view.FindControl<DataGrid>("ResultsGrid")!;
            var selected = search.Results[0]; grid.SelectedItem = selected; await search.MemoTransitionTask;
            Assert.Same(selected, search.SelectedResult);
            Assert.Same(selected, grid.SelectedItem);
            var initial = search.DisplayedSortBy == field;
            await ClickSort(window, view, grid, column);
            Assert.Null(search.Error); Assert.Equal(field, search.DisplayedSortBy);
            Assert.Same(selected, search.SelectedResult);
            Assert.Equal(initial ? PackageSearchSortDirection.Descending : PackageSearchSortDirection.Ascending, search.DisplayedSortDirection);
            Assert.Contains(initial ? ":sortdescending" : ":sortascending", Header(grid, column).Classes);
            await ClickSort(window, view, grid, column);
            Assert.Equal(initial ? PackageSearchSortDirection.Ascending : PackageSearchSortDirection.Descending, search.DisplayedSortDirection);
            Assert.Same(selected, search.SelectedResult);
            Assert.Contains(initial ? ":sortascending" : ":sortdescending", Header(grid, column).Classes);
            window.MouseMove(new Point(1, 1), RawInputModifiers.None); Dispatcher.UIThread.RunJobs();
            Assert.Contains(initial ? ":sortascending" : ":sortdescending", Header(grid, column).Classes);
            Assert.False(grid.Columns[0].CanUserSort); Assert.False(grid.Columns[5].CanUserSort);
            Assert.Empty(grid.CollectionView.SortDescriptions);
            Assert.Contains("unavailable", AutomationProperties.GetHelpText(Header(grid, 5)));
            Assert.Equal(52, grid.Columns[0].Width.Value); Assert.Equal(80, grid.Columns[0].MaxWidth);
            Assert.Contains("assetList", grid.Classes);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(PackageSearchSortField.AssetName)]
    [InlineData(PackageSearchSortField.PackageName)]
    [InlineData(PackageSearchSortField.SourceName)]
    [InlineData(PackageSearchSortField.PathId)]
    public async Task GlobalMultiPackageOrderExceedsWindowAndReplaysBackward(PackageSearchSortField field)
    {
        using var corpus = new V2SearchTests.Corpus();
        await corpus.Add(360, "Zulu", sourceCount: 2); await corpus.Add(360, "Éclair", sourceCount: 2);
        SeedSearchMemo(corpus);
        using var packages = V2SearchIntegrationTests.Packages();
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        foreach (var package in corpus.Packages) await packages.V2.OpenAsync(package.Path, select: false);
        search.SelectAllPackagesCommand.Execute(null); Configure(search); await search.SearchAsync();
        var selected = search.SelectedResult = search.Results[30]; await search.MemoTransitionTask;
        var criteria = search.LastExecutedCriteria;
        foreach (var direction in new[] { PackageSearchSortDirection.Ascending, PackageSearchSortDirection.Descending })
        {
            // Set another supported order first, so the target direction is explicit.
            if (search.DisplayedSortBy == field) await search.SortV2Async(field == PackageSearchSortField.PathId ? PackageSearchSortField.SourceName : PackageSearchSortField.PathId);
            await search.SortV2Async(field);
            if (direction == PackageSearchSortDirection.Descending) await search.SortV2Async(field);
            Assert.Null(search.Error); Assert.Same(selected, search.SelectedResult); Assert.Equal(criteria, search.LastExecutedCriteria);
            var actual = new List<SearchResult>(); long seen = 0;
            while (true)
            {
                actual.AddRange(search.Results.Skip((int)Math.Max(0, seen - search.ResultWindow!.StartIndex)).Select(r => r.V2Result!));
                seen = search.ResultWindow.StartIndex + search.Results.Count;
                Assert.InRange(search.Results.Count, 1, 600); Assert.InRange(search.ResultWindow.CheckpointCount, 1, 128);
                if (!search.CanReadNextPage) break;
                await search.NextPageAsync(); Assert.Null(search.Error);
            }
            Assert.Equal(1440, actual.Count); Assert.Equal(1440, actual.Select(r => r.Key).Distinct().Count());
            var expected = new PackageSearchService().Search(corpus.Packages.Select(p => p.Legacy).ToArray(),
                new PackageSearchRequest("sort") { Fields = PackageSearchFields.Memo | PackageSearchFields.AssetName,
                    ResultKinds = PackageSearchResultKinds.Asset, SortBy = field, SortDirection = direction });
            static object Signature(string package, string? source, string? name, long? path) => (package, source, name, path);
            Assert.Equal(expected.Select(r => Signature(r.PackageName, r.SourceName, r.AssetName, r.PathId)),
                actual.Select(r => Signature(r.PackageName, r.SourceName, r.AssetName, r.PathId)));
            Assert.Contains(actual, r => r.AssetName is null); Assert.Contains(actual, r => r.AssetName == "钩爪");
            while (search.CanReadPreviousPage) await search.PreviousPageAsync();
            Assert.Equal(0, search.ResultWindow!.StartIndex);
            Assert.Equal(actual.Take(100).Select(r => r.Key), search.Results.Take(100).Select(r => r.V2Result!.Key));
            Assert.Same(selected, search.SelectedResult);
        }
    }

    [AvaloniaFact]
    public async Task RapidSortCancelsOldPageAndIntermediateSortsWithoutChangingCriteria()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(800); SeedSearchMemo(corpus);
        using var packages = V2SearchIntegrationTests.Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int pages = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () =>
            new V2SearchService(observe: (b, _) => {
                if (b == SearchBoundary.BeforePublication && Interlocked.Increment(ref pages) == 2)
                { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); }
            }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        Configure(search); await search.SearchAsync(); var selected = search.SelectedResult = search.Results[0];
        var oldPage = search.NextPageAsync(); await reached.Task.WaitAsync(TimeSpan.FromSeconds(15));
        search.IncludeText = "pending controls must not replace executed criteria";
        Task first = Task.CompletedTask, second = Task.CompletedTask, last = Task.CompletedTask;
        try
        {
            first = search.SortV2Async(PackageSearchSortField.AssetName);
            second = search.SortV2Async(PackageSearchSortField.PathId);
            last = search.SortV2Async(PackageSearchSortField.PathId);
        }
        finally { release.Set(); }
        await Task.WhenAll(oldPage, first, second, last);
        Assert.Null(search.Error); Assert.Equal(PackageSearchSortField.PathId, search.DisplayedSortBy);
        Assert.Equal(PackageSearchSortDirection.Descending, search.DisplayedSortDirection);
        Assert.Equal(long.MaxValue, search.Results[0].PathId); Assert.Equal(100, search.Results.Count);
        Assert.Same(selected, search.SelectedResult); Assert.True(search.CriteriaChanged);
        Assert.Equal("pending controls must not replace executed criteria", search.IncludeText);
    }

    [AvaloniaFact]
    public async Task MixedNameUnsupportedAndMetadataSelectionRetained()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        using var packages = V2SearchIntegrationTests.Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        search.IncludeText = "Package,source"; await search.SearchAsync();
        var selected = search.SelectedResult = search.Results.First(r => r.Kind == PackageSearchResultKind.Package);
        var rows = search.Results; await search.SortV2Async(PackageSearchSortField.AssetName); Assert.Same(rows, search.Results);
        await search.SortV2Async(PackageSearchSortField.PathId); Assert.Same(selected, search.SelectedResult);
        Assert.False(search.CanSortV2AssetName); Assert.Null(search.Error);
        Configure(search); SeedSearchMemo(corpus); await search.SearchAsync();
        await search.SortV2Async(PackageSearchSortField.AssetName);
        Assert.Equal(PackageSearchSortField.AssetName, search.DisplayedSortBy);
        search.IncludePackageResults = true; search.IncludeSourceResults = true; search.IncludeText = "Package,source,sort";
        await search.SearchAsync();
        Assert.Equal(PackageSearchSortField.PackageName, search.DisplayedSortBy);
        Assert.Equal(PackageSearchSortDirection.Ascending, search.DisplayedSortDirection);
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1900, Height = 650 }; window.Show();
        try
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            var grid = view.FindControl<DataGrid>("ResultsGrid")!;
            Assert.False(grid.Columns[1].CanUserSort);
            Assert.Contains("Assets-only", AutomationProperties.GetHelpText(Header(grid, 1)));
            Assert.True(grid.Columns[2].CanUserSort); Assert.True(grid.Columns[3].CanUserSort); Assert.True(grid.Columns[4].CanUserSort);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateSortCompletionCannotReplaceLatestOrderOrCancelledRows(bool cancel)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(800); SeedSearchMemo(corpus);
        using var packages = V2SearchIntegrationTests.Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int pages = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () =>
            new V2SearchService(observe: (boundary, _) => {
                if (boundary == SearchBoundary.BeforePublication && Interlocked.Increment(ref pages) == 2)
                { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); }
            }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var rows = search.Results; var selected = search.SelectedResult = rows[0];
        var first = search.SortV2Async(PackageSearchSortField.AssetName);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Task second = Task.CompletedTask;
        try
        {
            second = search.SortV2Async(PackageSearchSortField.PathId);
            if (cancel) search.CancelSearchCommand.Execute(null);
        }
        finally { release.Set(); }
        await Task.WhenAll(first, second);
        Assert.Same(selected, search.SelectedResult); Assert.Null(search.Error);
        if (cancel)
        {
            Assert.Same(rows, search.Results); Assert.Equal(PackageSearchSortField.PackageName, search.DisplayedSortBy);
            Assert.Contains("cancelled", search.Status); Assert.False(search.IsSearching);
        }
        else
        {
            Assert.Equal(PackageSearchSortField.PathId, search.DisplayedSortBy);
            Assert.Equal(PackageSearchSortDirection.Ascending, search.DisplayedSortDirection);
            Assert.Equal(-77, search.Results[0].PathId); Assert.Equal(100, search.Results.Count);
        }
    }

    [AvaloniaFact]
    public async Task ExternalCatalogChangeDuringSortRequiresExplicitRefresh()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(50); SeedSearchMemo(corpus);
        using var packages = V2SearchIntegrationTests.Packages(); int admissions = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () => {
            if (++admissions == 2)
            {
                using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = corpus.Packages[0].Path, Pooling = false }.ToString());
                connection.Open(); connection.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
                using var command = connection.CreateCommand(); command.CommandText = "UPDATE asset SET memo='external sort' WHERE table_ordinal=0"; command.ExecuteNonQuery();
            }
            return new V2SearchService();
        });
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var rows = search.Results; var selected = search.SelectedResult = rows[0];
        await search.SortV2Async(PackageSearchSortField.AssetName);
        Assert.True(search.IsStale); Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult);
        Assert.False(search.CanReadNextPage); Assert.False(search.CanChangeV2Sort); Assert.NotNull(search.Error);
        Assert.Equal(2, admissions); // no implicit Refresh/retry
    }

    private sealed class NeverDelay : IMemoDraftDelay
    { public Task DelayAsync(TimeSpan delay, CancellationToken token) => Task.Delay(Timeout.InfiniteTimeSpan, token); }
    private sealed class RejectV1 : IPackageMemoPersistenceService
    {
        public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => throw new InvalidOperationException();
        public Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default) => throw new InvalidOperationException();
    }

    [AvaloniaFact]
    public async Task V1HeadersStillUseNativeCollectionSorting()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(24); SeedSearchMemo(corpus);
        using var packages = V2SearchIntegrationTests.Packages();
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1900, Height = 650 }; window.Show();
        try
        {
            var grid = view.FindControl<DataGrid>("ResultsGrid")!;
            grid.ItemsSource = new PackageSearchService().Search(corpus.Packages.Select(p => p.Legacy).ToArray(),
                new PackageSearchRequest("sort") { Fields = PackageSearchFields.All, ResultKinds = PackageSearchResultKinds.Asset })
                .ToArray();
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.True(search.CanSortResultsLocally); Assert.All(grid.Columns, c => Assert.True(c.CanUserSort));
            await ClickSort(window, view, grid, 4);
            Assert.Single(grid.CollectionView.SortDescriptions);
            Assert.Equal(-77, grid.CollectionView.Cast<PackageSearchResult>().First().PathId);
            await ClickSort(window, view, grid, 4);
            Assert.Equal(long.MaxValue, grid.CollectionView.Cast<PackageSearchResult>().First().PathId);
            Assert.Null(search.DisplayedSortBy);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public async Task SortRetainsInspectorDraftAndSaveRequiresExplicitRefresh()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(800); SeedSearchMemo(corpus);
        using var packages = V2SearchIntegrationTests.Packages();
        var memo = new InspectorMemoService(new RejectV1(), new PackageMemoResolver(), new PackageWriteCoordinator(),
            new PackageEditorSessionGuard(), () => packages.V2.Registrations.ToArray());
        using var drafts = new MemoDraftCoordinator(memo, new NeverDelay()); int executions = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), memo, drafts, memo,
            () => { executions++; return new V2SearchService(); }, memo);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var selected = search.SelectedResult = search.Results[0]; await search.MemoTransitionTask;
        search.MemoInspector.MemoText = "unsaved 钩爪"; var identity = search.MemoInspector.Identity;
        await search.SortV2Async(PackageSearchSortField.PathId); await search.SortV2Async(PackageSearchSortField.PathId);
        Assert.Same(selected, search.SelectedResult); Assert.Same(identity, search.MemoInspector.Identity);
        Assert.Equal("unsaved 钩爪", search.MemoInspector.MemoText); Assert.True(search.IsSelectionOutsideWindow);
        await search.MemoInspector.SaveNowCommand.ExecuteAsync(); Assert.Equal(MemoDraftSaveState.Saved, search.MemoInspector.SaveState);
        Assert.True(search.IsStale); var rows = search.Results; int before = executions;
        await search.SortV2Async(PackageSearchSortField.SourceName); await search.NextPageAsync();
        Assert.Same(rows, search.Results); Assert.Equal(before, executions);
        await search.SearchAsync(); Assert.False(search.IsStale); Assert.Equal(PackageSearchSortField.PathId, search.DisplayedSortBy);
        Assert.Equal(PackageSearchSortDirection.Descending, search.DisplayedSortDirection);
        search.Dispose(); packages.Dispose(); await memo.CleanupTask;
    }
}
