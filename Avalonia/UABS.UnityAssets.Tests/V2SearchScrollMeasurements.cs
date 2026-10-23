using System.Diagnostics;
using System.Text.Json;
using UABS.PackageV2.Search;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

/// <summary>Explicit disposable production-VM calibration, not desktop paint timing or a test budget.</summary>
public static class V2SearchScrollMeasurements
{
    public static async Task RunAsync(int count)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(count);
        corpus.Packages[0].Legacy.Package.Indexes.Clear();
        using var packages = V2SearchIntegrationTests.Packages();
        var memo = new InspectorMemoService(new RejectV1(), new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(),
            () => packages.V2.Registrations.ToArray(), dispatch: a => { a(); return Task.CompletedTask; });
        using var drafts = new MemoDraftCoordinator(memo, new NoDelay());
        SearchWorkspaceViewModel search = null!; V2SearchService engine = null!;
        bool cancelNext = false, unloadNext = false; Task unloading = Task.CompletedTask;
        using var ownedSearch = search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), memo, drafts, memo, () => {
            return engine = new V2SearchService(observe: (b, _) => {
                if (b != SearchBoundary.BeforePublication) return;
                if (cancelNext) { cancelNext = false; search.CancelSearchCommand.Execute(null); }
                if (unloadNext) { unloadNext = false; unloading = packages.V2.RemoveAsync(); }
            });
        }, memo);
        int peakRows = 0, peakCheckpoints = 0;
        async Task Record(string operation, Func<Task> action)
        {
            long allocated = GC.GetTotalAllocatedBytes(); var clock = Stopwatch.StartNew(); await action();
            Console.WriteLine(JsonSerializer.Serialize(new { count, operation, ms = clock.Elapsed.TotalMilliseconds,
                allocated = GC.GetTotalAllocatedBytes() - allocated, retainedRows = search.Results.Count,
                start = search.ResultWindow?.StartIndex, seen = search.ResultWindow?.SeenRows,
                checkpoints = search.ResultWindow?.CheckpointCount, enginePeakWorkers = engine?.PeakWorkers,
                engineActiveWorkers = engine?.ActiveWorkers, stale = search.IsStale, error = search.Error }));
        }
        void TrackWindow()
        {
            search.ResultWindow!.Rows.CollectionChanged += (_, _) => peakRows = Math.Max(peakRows, search.Results.Count);
        }
        await Record("browse-admission", async () => await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false));
        search.IncludeText = "asset"; V2SearchIntegrationTests.AssetsOnly(search);
        await Record("first-results-model", search.SearchAsync); Assert.Null(search.Error); TrackWindow();
        search.SelectedResult = search.Results[0]; await search.MemoTransitionTask;
        long retainedBefore = GC.GetTotalMemory(true);
        int forward = count >= 1_000_000 ? 12 : count >= 100_000 ? 140 : 80;
        for (int i = 0; i < forward && search.CanReadNextPage; i++)
        {
            await Record("scroll-forward", () => search.ReportViewportAsync(search.Results.Count - 15, search.Results.Count - 1));
            Assert.Null(search.Error); Assert.InRange(search.Results.Count, 1, 600);
            Assert.Equal(search.Results.Count, search.Results.Select(r => r.V2Result!.Key).Distinct().Count());
            peakCheckpoints = Math.Max(peakCheckpoints, search.ResultWindow!.CheckpointCount);
        }
        for (int i = 0; i < 12 && search.CanReadPreviousPage; i++)
        { await Record("scroll-backward", () => search.ReportViewportAsync(0, 20)); Assert.Null(search.Error); }
        var rows = search.Results;
        search.MemoInspector.MemoText = "calibrated retained memo 钩爪";
        await Record("memo-save-stale-window", () => search.MemoInspector.SaveNowCommand.ExecuteAsync());
        Assert.Equal(MemoDraftSaveState.Saved, search.MemoInspector.SaveState); Assert.True(search.IsStale); Assert.Same(rows, search.Results);
        await Record("explicit-refresh", search.SearchAsync); Assert.False(search.IsStale); Assert.Null(search.Error);
        cancelNext = true; await Record("cancel-scroll", () => search.ReportViewportAsync(70, 99)); Assert.Contains("Cancelled", search.Error);
        search.IncludeText = "Hero"; search.ExactText = true;
        await Record("restart-search", search.SearchAsync); Assert.Equal(2, search.Results.Count);
        search.IncludeText = "asset"; search.ExactText = false; await search.SearchAsync();
        packages.V2.SelectedPackage = packages.V2.Registrations[0]; await packages.V2.SelectionTask;
        unloadNext = true; await Record("unload-during-scroll", () => search.ReportViewportAsync(70, 99)); await unloading;
        Assert.True(search.IsStale); Assert.Equal(0, engine.ActiveWorkers);
        search.Dispose(); await search.RetirementTask; packages.Dispose(); await memo.CleanupTask;
        Console.WriteLine(JsonSerializer.Serialize(new { count, operation = "summary", peakRowsDuringMutation = peakRows,
            maximumPublishedRows = V2SearchWindow.MaximumRows, peakCheckpoints,
            retainedManagedDelta = GC.GetTotalMemory(true) - retainedBefore, processPeakWorkingSet = Process.GetCurrentProcess().PeakWorkingSet64,
            note = "single-package model/inline dispatch, not UI paint; peak includes fixture build/native/runtime; connections bounded by engine workers plus admitted browse/writer ownership" }));
    }
    private sealed class NoDelay : IMemoDraftDelay
    { public Task DelayAsync(TimeSpan delay, CancellationToken token) => Task.Delay(Timeout.InfiniteTimeSpan, token); }
    private sealed class RejectV1 : IPackageMemoPersistenceService
    {
        public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => throw new InvalidOperationException("No V1 routing");
        public Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default) => throw new InvalidOperationException("No V1 routing");
    }
}
