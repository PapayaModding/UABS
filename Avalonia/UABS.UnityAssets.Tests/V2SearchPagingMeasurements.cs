using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using UABS.PackageV2.Search;
using UABS.Services;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

/// <summary>Explicit sequential disposable calibration; inclusive spans are not additive or UI paint.</summary>
public static class V2SearchPagingMeasurements
{
    public static async Task RunAsync(int count, string variant)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(count);
        corpus.Packages[0].Legacy.Package.Indexes.Clear();
        ConcurrentQueue<(string Phase, double Ms, Dictionary<string, object?> Tags)> phases = new();
        using var listener = new ActivityListener {
            ShouldListenTo = s => s.Name is "UABS.Search" or "UABS.Memos",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => phases.Enqueue((a.OperationName, a.Duration.TotalMilliseconds, a.TagObjects.ToDictionary(p => p.Key, p => p.Value))) };
        ActivitySource.AddActivityListener(listener);
        using var packages = V2SearchIntegrationTests.Packages();
        V2SearchService engine = null!;
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), v2EngineFactory: () => engine = new());
        async Task Record(string operation, Func<Task> action)
        {
            phases.Clear(); long allocated = GC.GetTotalAllocatedBytes(); var clock = Stopwatch.StartNew(); await action();
            Console.WriteLine(JsonSerializer.Serialize(new { count, variant, operation, ms = clock.Elapsed.TotalMilliseconds,
                allocated = GC.GetTotalAllocatedBytes() - allocated, rows = search.Results.Count,
                start = search.ResultWindow?.StartIndex, checkpoints = search.ResultWindow?.CheckpointCount,
                activeWorkers = engine?.ActiveWorkers, peakWorkers = engine?.PeakWorkers, stale = search.IsStale, error = search.Error,
                phases = phases.GroupBy(p => p.Phase).ToDictionary(g => g.Key, g => new { calls = g.Count(), ms = g.Sum(p => p.Ms) }),
                counters = phases.Where(p => p.Phase == "query-counters").Select(p => p.Tags).ToArray() }));
        }
        await Record("browse-admission", async () => await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false));
        search.IncludeText = "asset"; V2SearchIntegrationTests.AssetsOnly(search);
        for (int i = 0; i < 3; i++) { await Record("first", search.SearchAsync); Assert.Null(search.Error); }
        for (int i = 0; i < 3; i++) { await Record("warm-forward", search.NextPageAsync); Assert.Null(search.Error); }
        for (int i = 0; i < 5; i++) { await search.NextPageAsync(); Assert.Null(search.Error); }
        for (int i = 0; i < 3; i++) { await Record("deep-forward", search.NextPageAsync); Assert.Null(search.Error); }
        for (int i = 0; i < 3; i++) { await Record("backward", search.PreviousPageAsync); Assert.Null(search.Error); }
        Assert.InRange(search.Results.Count, 1, 600); Assert.True(search.ResultWindow!.CheckpointCount <= 128);
        // A separate SQLite connection, never a real package. Same-generation memo/catalog change.
        using (var connection = V2SearchTests.Connect(corpus.Packages[0].Path))
        {
            connection.CreateFunction<string?, int>("uabs_has_text_v1", s => string.IsNullOrWhiteSpace(s) ? 0 : 1);
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE asset SET memo='external calibration' WHERE path_id=5"; command.ExecuteNonQuery();
        }
        await Record("external-invalidation", search.NextPageAsync); Assert.True(search.IsStale);
        for (int i = 0; i < 3; i++) { await Record("refresh", search.SearchAsync); Assert.Null(search.Error); Assert.False(search.IsStale); }
        search.Dispose(); await search.RetirementTask;
        Console.WriteLine(JsonSerializer.Serialize(new { count, variant, operation = "summary",
            managed = GC.GetTotalMemory(true), peakWorkingSet = Process.GetCurrentProcess().PeakWorkingSet64,
            note = "3 samples each; synthetic single package; traced model path, not desktop/physical cold I/O; inclusive spans overlap" }));
    }
}
