using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using UABS.PackageV2;
using UABS.Services;
using UABS.ViewModels;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.UnityAssets.Tests;

/// <summary>Explicit disposable calibration; no timing assertions or real package paths.</summary>
public static class V2MemoLifecycleMeasurements
{
    public static async Task MeasureAsync(int count, string label)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(count);
        ConcurrentQueue<(string Phase, double Ms)> phases = new();
        using var listener = new ActivityListener { ShouldListenTo = s => s.Name == "UABS.Memos",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => phases.Enqueue((a.OperationName, a.Duration.TotalMilliseconds)) };
        ActivitySource.AddActivityListener(listener);
        async Task Record(string operation, Func<Task> action)
        {
            phases.Clear(); long allocations = GC.GetTotalAllocatedBytes(); var time = Stopwatch.StartNew();
            await action(); double ms = time.Elapsed.TotalMilliseconds;
            long allocated = GC.GetTotalAllocatedBytes() - allocations;
            Console.WriteLine(JsonSerializer.Serialize(new { label, count, operation, ms, allocated,
                phases = phases.GroupBy(p => p.Phase).ToDictionary(g => g.Key, g => new { calls = g.Count(), ms = g.Sum(p => p.Ms) }) }));
        }
        using var catalog = new V2BrowseCatalog(); V2BrowseRegistration entry = null!;
        await Record("cold-browse-admission", async () => entry = await catalog.RegisterAsync(corpus.Packages[0].Path));
        using var reader = (await PackageReader.OpenAsync(entry.Path)).Reader!;
        var assets = await reader.ReadAssetsAsync(pageSize: 10);
        var service = new InspectorMemoService(new RejectLegacy(), new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(),
            () => [entry], dispatch: a => { a(); return Task.CompletedTask; });
        using var drafts = new MemoDraftCoordinator(service);
        using var inspector = new EditableMemoInspectorViewModel(drafts, service);
        long retainedBefore = GC.GetTotalMemory(true);
        for (int i = 0; i < 4; i++)
        {
            var asset = assets[i];
            await Record(i == 0 ? "cold-selection" : "warm-selection", async () => {
                var resolution = service.ResolveIndexed(entry, asset.Key.PackageId, asset.Key.GenerationId, asset.Key.SourceId, asset.Key.SourceRevision,
                    asset.Key.DirectoryOrdinal, asset.Key.PathId, asset.IdentityStatus, asset.Memo);
                var visible = Stopwatch.StartNew(); var pending = inspector.ShowAsync(resolution);
                Console.WriteLine(JsonSerializer.Serialize(new { label, count, operation = "selection-visible-model", ms = visible.Elapsed.TotalMilliseconds, matches = inspector.MemoText == (asset.Memo ?? "") }));
                await pending; Xunit.Assert.True(inspector.IsEditable, inspector.Explanation);
            });
        }
        for (int i = 0; i < 3; i++)
        {
            inspector.MemoText = "calibration " + i;
            await Record("save-now", async () => { await inspector.SaveNowCommand.ExecuteAsync(); Xunit.Assert.Equal(MemoDraftSaveState.Saved, inspector.SaveState); });
        }
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Changed(object? sender, MemoDraftChangedEventArgs e) { if (e.Draft.State == MemoDraftSaveState.Saved && e.Draft.DraftMemo == "auto calibration") complete.TrySetResult(); }
        drafts.DraftChanged += Changed;
        await Record("auto-save-including-debounce", async () => { inspector.MemoText = "auto calibration"; await complete.Task.WaitAsync(TimeSpan.FromMinutes(2)); });
        drafts.DraftChanged -= Changed;
        Console.WriteLine(JsonSerializer.Serialize(new { label, count, operation = "retained-memory-delta", bytes = GC.GetTotalMemory(true) - retainedBefore,
            note = "managed surviving workflow objects; fixture memory already allocated; not UI/GPU/process peak" }));
        await catalog.UnloadAsync(entry); await service.CleanupTask;
    }
    private sealed class RejectLegacy : IPackageMemoPersistenceService
    {
        public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => throw new InvalidOperationException("Unexpected V1 routing.");
        public Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default) => throw new InvalidOperationException("Unexpected V1 routing.");
    }
}
