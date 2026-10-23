using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;
using UABS.Page;
using UABS.PackageV2.Search;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class V2SearchInfiniteScrollTests
{
    private static async Task Drain(SearchWorkspaceView view, Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); await view.ViewportLoadTask;
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); await view.ViewportLoadTask;
        window.UpdateLayout();
    }
    private static DataGridRow Top(DataGrid grid)
    {
        double top = grid.GetVisualDescendants().OfType<DataGridColumnHeader>()
            .Select(h => h.TranslatePoint(default, grid)!.Value.Y + h.Bounds.Height).DefaultIfEmpty(0).Max();
        return grid.GetVisualDescendants().OfType<DataGridRow>().Where(r => r.IsVisible && r.Bounds.Height > 0
            && r.TranslatePoint(default, grid) is { } p && p.Y + r.Bounds.Height > top && p.Y < grid.Bounds.Height)
            .OrderBy(r => r.Index).First();
    }
    [AvaloniaTheory]
    [InlineData(520, false)]
    [InlineData(700, true)]
    public async Task ActualViewportLoadsAndEvicts_WithoutMovingAnchor_AndReloadsBackward(int height, bool dark)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(1500);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        search.IncludeText = "asset"; V2SearchIntegrationTests.AssetsOnly(search); await search.SearchAsync();
        for (int i = 0; i < 5; i++) await search.NextPageAsync();
        Assert.Equal(600, search.Results.Count); var collection = search.Results;
        var selected = search.SelectedResult = search.Results[0]; await search.MemoTransitionTask;
        var view = new SearchWorkspaceView { DataContext = search }; var window = new Window { Content = view, Width = 1400, Height = height,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light }; window.Show();
        try
        {
            window.UpdateLayout(); var grid = view.FindControl<DataGrid>("ResultsGrid")!;
            double capturedY = double.NaN, capturedHeight = double.NaN;
            search.ResultWindowChanging += () => { capturedY = Top(grid).TranslatePoint(default, grid)!.Value.Y; capturedHeight = grid.Bounds.Height; };
            grid.ScrollIntoView(search.Results[^1], null); window.UpdateLayout();
            var anchor = Top(grid); var identity = ((SearchResultItemViewModel)anchor.DataContext!).V2Result!.Key;
            double y = anchor.TranslatePoint(default, grid)!.Value.Y;
            await Drain(view, window);
            Assert.Null(search.Error); Assert.True(search.ResultWindow!.StartIndex > 0,
                $"Viewport did not advance: top={Top(grid).Index}, rows={search.Results.Count}, loading={search.IsLoadingPage}, task={view.ViewportLoadTask.Status}, visible={view.IsEffectivelyVisible}");
            Assert.Same(collection, search.Results); Assert.InRange(search.Results.Count, 1, 600);
            Assert.Same(selected, search.SelectedResult); Assert.True(search.IsSelectionOutsideWindow, "Selection was evicted but not retained independently.");
            var after = Top(grid); Assert.Equal(identity, ((SearchResultItemViewModel)after.DataContext!).V2Result!.Key);
            Assert.True(Math.Abs(after.TranslatePoint(default, grid)!.Value.Y - y) <= 1,
                $"Anchor moved: before={y}, capture={capturedY}, after={after.TranslatePoint(default, grid)!.Value.Y}, captureHeight={capturedHeight}, finalHeight={grid.Bounds.Height}, bar={grid.GetVisualDescendants().OfType<ScrollBar>().First(b => b.Orientation == Avalonia.Layout.Orientation.Vertical).Value}");
            Assert.InRange(grid.GetVisualDescendants().OfType<DataGridRow>().Count(r => r.IsVisible), 1, 60);
            Assert.Contains("assetList", grid.Classes); Assert.False(grid.CanUserSortColumns);
            Assert.Equal(52, grid.Columns[0].Width.Value); Assert.Equal(0, grid.Columns[0].MinWidth);
            Assert.Equal(80, grid.Columns[0].MaxWidth); Assert.True(grid.Columns[0].CanUserResize);
            foreach (var row in grid.GetVisualDescendants().OfType<DataGridRow>().Where(r => r.IsVisible))
                Assert.Contains(row.Index % 2 == 0 ? "assetRowEven" : "assetRowOdd", row.Classes);
            grid.ScrollIntoView(search.Results[0], null); window.UpdateLayout();
            var backwardAnchor = Top(grid); var backwardKey = ((SearchResultItemViewModel)backwardAnchor.DataContext!).V2Result!.Key;
            double by = backwardAnchor.TranslatePoint(default, grid)!.Value.Y;
            await Drain(view, window);
            Assert.Equal(0, search.ResultWindow.StartIndex); Assert.Contains(selected, search.Results); Assert.Same(selected, search.SelectedResult);
            Assert.Equal(backwardKey, ((SearchResultItemViewModel)Top(grid).DataContext!).V2Result!.Key);
            Assert.InRange(Math.Abs(Top(grid).TranslatePoint(default, grid)!.Value.Y - by), 0, 1);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public async Task NearBoundaryPrefetch_CoalescesRepeatedRequests_AndDirectionChangeCancels()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(1200);
        using var packages = V2SearchIntegrationTests.Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int pages = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () => new V2SearchService(observe: (b, _) => {
            if (b == SearchBoundary.BeforePublication && Interlocked.Increment(ref pages) == 2)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); }
        }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); search.IncludeText = "asset"; V2SearchIntegrationTests.AssetsOnly(search);
        await search.SearchAsync(); var original = search.Results.ToArray();
        await search.ReportViewportAsync(10, 60); Assert.Equal(1, pages); // not near boundary
        var load = search.ReportViewportAsync(70, 85); await reached.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try
        {
            for (int i = 0; i < 30; i++) await search.ReportViewportAsync(71, 89);
            Assert.Equal(2, pages); Assert.True(search.IsLoadingPage);
            await search.ReportViewportAsync(0, 12); // changed direction; do not evict a viewport that moved away
            release.Set(); await load; Assert.Equal(original, search.Results); Assert.Null(search.Error);
            await search.ReportViewportAsync(70, 85); Assert.Equal(200, search.Results.Count); Assert.Equal(3, pages);
        }
        finally { release.Set(); await load; }
    }
    [AvaloniaFact]
    public async Task RecoverableFailure_RetryDoesNotPretendEndOrLoseSelection()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(260);
        using var packages = V2SearchIntegrationTests.Packages(); int pages = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () => new V2SearchService(observe: (b, _) => {
            if (b == SearchBoundary.BeforePublication && Interlocked.Increment(ref pages) == 2) throw new IOException("Injected recoverable query failure");
        }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); search.IncludeText = "asset"; V2SearchIntegrationTests.AssetsOnly(search);
        await search.SearchAsync(); var selected = search.SelectedResult = search.Results[0]; var rows = search.Results;
        await search.ReportViewportAsync(70, 90); Assert.True(search.CanRetryPage); Assert.True(search.CanReadNextPage); Assert.False(search.IsStale);
        Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult); Assert.Equal(100, search.Results.Count);
        await search.RetryPageCommand.ExecuteAsync(); Assert.Null(search.Error); Assert.False(search.CanRetryPage); Assert.Equal(200, search.Results.Count);
        await search.NextPageAsync(); Assert.Contains("242 exact", search.CountSummary); Assert.False(search.CanReadNextPage);
    }
    [AvaloniaFact]
    public async Task DeactivationStopsAutoLoading_WhileExplicitRefreshRemainsUserDriven()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(300);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService());
        var entry = await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        search.IncludeText = "asset"; V2SearchIntegrationTests.AssetsOnly(search); await search.SearchAsync();
        var rows = search.Results; var selected = search.SelectedResult = rows[0];
        search.Deactivate(); await search.ReportViewportAsync(70, 99); Assert.Equal(100, rows.Count);
        await search.ActivateAsync(); await search.ReportViewportAsync(70, 99); Assert.Equal(200, rows.Count);
        packages.V2.SelectedPackage = entry; await packages.V2.SelectionTask;
        await packages.V2.RemoveAsync();
        Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult);
        Assert.True(search.IsStale); Assert.False(search.CanReadNextPage);
    }
    [AvaloniaFact]
    public async Task NewSearchRejectsLateOldWindowCompletion()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(350);
        using var packages = V2SearchIntegrationTests.Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int pages = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () => new V2SearchService(observe: (b, _) => {
            if (b == SearchBoundary.BeforePublication && Interlocked.Increment(ref pages) == 2)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); }
        }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); search.IncludeText = "asset"; V2SearchIntegrationTests.AssetsOnly(search);
        await search.SearchAsync(); var load = search.ReportViewportAsync(70, 99); await reached.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try
        {
            search.IncludeText = "Hero"; search.ExactText = true; await search.SearchAsync(); var current = search.Results;
            Assert.Equal(2, current.Count); release.Set(); await load;
            Assert.Same(current, search.Results); Assert.All(current, r => Assert.Equal("hero", r.Name.ToLowerInvariant()));
            Assert.False(search.IsSearching); Assert.False(search.IsStale);
        }
        finally { release.Set(); await load; await search.RetirementTask; }
    }
    private sealed class NeverDelay : IMemoDraftDelay
    { public Task DelayAsync(TimeSpan delay, CancellationToken token) => Task.Delay(Timeout.InfiniteTimeSpan, token); }
    private sealed class RejectV1 : IPackageMemoPersistenceService
    {
        public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => throw new InvalidOperationException("No V1 routing");
        public Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default) => throw new InvalidOperationException("No V1 routing");
    }
    [AvaloniaFact]
    public async Task MemoDraftSurvivesEviction_AndVerifiedSaveKeepsWindowStaleWithoutRerun()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(1000);
        using var packages = V2SearchIntegrationTests.Packages();
        var memo = new InspectorMemoService(new RejectV1(), new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(),
            () => packages.V2.Registrations.ToArray());
        using var drafts = new MemoDraftCoordinator(memo, new NeverDelay()); int searches = 0;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), memo, drafts, memo,
            () => { searches++; return new V2SearchService(); }, memo);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); search.IncludeText = "asset"; V2SearchIntegrationTests.AssetsOnly(search);
        await search.SearchAsync(); var selected = search.SelectedResult = search.Results[0]; await search.MemoTransitionTask;
        Assert.True(search.MemoInspector.IsEditable); search.MemoInspector.MemoText = "evicted shared draft 钩爪";
        try
        {
            for (int i = 0; i < 7; i++) await search.NextPageAsync();
            Assert.Same(selected, search.SelectedResult); Assert.True(search.IsSelectionOutsideWindow);
            Assert.Equal("evicted shared draft 钩爪", search.MemoInspector.MemoText);
            var window = search.Results; var keys = window.Select(r => r.V2Result!.Key).ToArray();
            await search.MemoInspector.SaveNowCommand.ExecuteAsync();
            Assert.Equal(MemoDraftSaveState.Saved, search.MemoInspector.SaveState);
            Assert.Equal("evicted shared draft 钩爪", selected.Memo); Assert.True(search.IsStale);
            Assert.Same(window, search.Results); Assert.Equal(keys, window.Select(r => r.V2Result!.Key)); Assert.Equal(1, searches);
            await search.ReportViewportAsync(0, 10); Assert.Equal(keys, window.Select(r => r.V2Result!.Key));
            Assert.False(search.CanReadNextPage); Assert.False(search.CanReadPreviousPage);
        }
        finally { search.Dispose(); packages.Dispose(); await memo.CleanupTask; }
    }
}
