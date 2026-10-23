using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using UABS.PackageV2;
using UABS.PackageV2.Memos;
using UABS.Services;
using UABS.ViewModels;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.UnityAssets.Tests;

/// <summary>Explicit disposable production-path calibration, not a timing test or direct UPDATE benchmark.</summary>
public static class V2MemoSaveMeasurements
{
    public static async Task MeasureAsync(int count)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(count);
        ConcurrentQueue<(string Phase, double Ms)> phases = new();
        using var listener = new ActivityListener { ShouldListenTo = s => s.Name == "UABS.Memos",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => phases.Enqueue((a.OperationName, a.Duration.TotalMilliseconds)) };
        ActivitySource.AddActivityListener(listener);
        string path = corpus.Packages[0].Path;
        long journalAtCommit = 0;
        async Task Record(string operation, Func<Task> action)
        {
            phases.Clear(); journalAtCommit = 0;
            long allocated = GC.GetTotalAllocatedBytes(), length = new FileInfo(path).Length;
            var time = Stopwatch.StartNew(); await action(); double ms = time.Elapsed.TotalMilliseconds;
            Console.WriteLine(JsonSerializer.Serialize(new { count, operation, ms, allocated = GC.GetTotalAllocatedBytes() - allocated,
                lengthBefore = length, lengthAfter = new FileInfo(path).Length, journalAtCommit,
                journalRemains = File.Exists(path + "-journal"),
                phases = phases.GroupBy(p => p.Phase).ToDictionary(g => g.Key, g => new { calls = g.Count(), ms = g.Sum(p => p.Ms) }) }));
        }
        using var catalog = new V2BrowseCatalog(); V2BrowseRegistration entry = null!;
        await Record("cold-admission", async () => entry = await catalog.RegisterAsync(path));
        StoredAsset[] rows;
        using (var reader = (await PackageReader.OpenAsync(path)).Reader!) rows = (await reader.ReadAssetsAsync(pageSize: 10)).ToArray();
        var service = new InspectorMemoService(new RejectLegacy(), new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(),
            () => [entry], boundary: b => { if (b == MemoBoundary.BeforeCommit && File.Exists(path + "-journal")) journalAtCommit = new FileInfo(path + "-journal").Length; },
            dispatch: a => { a(); return Task.CompletedTask; });
        using var drafts = new MemoDraftCoordinator(service, new NoDebounce());
        using var inspector = new EditableMemoInspectorViewModel(drafts, service);
        long retainedBefore = GC.GetTotalMemory(true); int revision = 0;
        async Task Select(int n)
        {
            var a = rows[n]; await inspector.ShowAsync(service.ResolveIndexed(entry, a.Key.PackageId, a.Key.GenerationId,
                a.Key.SourceId, a.Key.SourceRevision, a.Key.DirectoryOrdinal, a.Key.PathId, a.IdentityStatus, a.Memo));
            Xunit.Assert.True(inspector.IsEditable, inspector.Explanation);
        }
        async Task Save()
        {
            inspector.MemoText = "calibration " + ++revision;
            await inspector.SaveNowCommand.ExecuteAsync(); Xunit.Assert.Equal(MemoDraftSaveState.Saved, inspector.SaveState);
        }
        await Select(0);
        await Record("first-save", Save);
        for (int i = 0; i < 5; i++) await Record(i == 0 ? "second-save" : "warm-save", Save);
        for (int i = 0; i < 3; i++)
        {
            await Select(1); await Save(); await Select(0);
            await Record("after-in-process-save", Save);
            using (var c = V2SearchTests.Connect(path))
            {
                c.CreateFunction<string?, int>("uabs_has_text_v1", s => string.IsNullOrWhiteSpace(s) ? 0 : 1);
                using var command = c.CreateCommand(); command.CommandText = "UPDATE asset SET memo=@memo WHERE path_id=5";
                command.Parameters.AddWithValue("@memo", "external " + i); command.ExecuteNonQuery();
            }
            await Record("after-external-save", Save);
        }
        for (int i = 0; i < 3; i++)
        {
            await catalog.UnloadAsync(entry); await service.CleanupTask;
            // All fixture connections are quiesced. Never live-copy a user database.
            File.Move(path, path + ".previous-" + i); File.Copy(path + ".previous-" + i, path);
            await Record("replacement-admission", async () => entry = await catalog.RegisterAsync(path));
            await Select(0); await Record("after-replacement-save", Save);
        }
        await catalog.UnloadAsync(entry); await service.CleanupTask;
        Console.WriteLine(JsonSerializer.Serialize(new { count, operation = "memory", retainedDelta = GC.GetTotalMemory(true) - retainedBefore,
            processPeakWorkingSet = Process.GetCurrentProcess().PeakWorkingSet64,
            note = "managed delta excludes fixture baseline; process peak includes fixture creation/native/runtime, not isolated save peak" }));
    }
    private sealed class NoDebounce : IMemoDraftDelay
    { public Task DelayAsync(TimeSpan delay, CancellationToken token) => Task.Delay(Timeout.InfiniteTimeSpan, token); }
    private sealed class RejectLegacy : IPackageMemoPersistenceService
    {
        public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => throw new InvalidOperationException("Unexpected V1 routing.");
        public Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default) => throw new InvalidOperationException("Unexpected V1 routing.");
    }
}
