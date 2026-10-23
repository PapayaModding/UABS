using System.Diagnostics;
using UABS.PackageV2;
using UABS.PackageV2.Memos;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.UnityAssets.Tests;

/// <summary>Explicit opt-in diagnostic, not a unit-test latency threshold or a real-corpus benchmark.</summary>
public static class V2MemoIntegrationMeasurements
{
    public static async Task MeasureAsync(int count)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(count);
        string path = corpus.Packages[0].Path; var timer = Stopwatch.StartNew();
        var admission = await PackageReader.OpenAsync(path); double readerMs = timer.Elapsed.TotalMilliseconds;
        StoredAsset asset;
        using (var reader = admission.Reader!) asset = (await reader.ReadAssetsAsync())[0];
        using var catalog = new V2BrowseCatalog(); timer.Restart(); var entry = await catalog.RegisterAsync(path);
        double browseMs = timer.Elapsed.TotalMilliseconds;
        Dictionary<MemoBoundary, double> boundaries = [];
        var service = new InspectorMemoService(new RejectLegacy(), new PackageMemoResolver(), new PackageWriteCoordinator(),
            new PackageEditorSessionGuard(), () => [entry], boundary: b => boundaries[b] = timer.Elapsed.TotalMilliseconds,
            dispatch: action => { action(); return Task.CompletedTask; });
        service.ResolveIndexed(entry, asset.Key.PackageId, asset.Key.GenerationId, asset.Key.SourceId, asset.Key.SourceRevision,
            asset.Key.DirectoryOrdinal, asset.Key.PathId, asset.IdentityStatus, asset.Memo);
        timer.Restart(); await service.GetRegistrationAsync(entry); double registerMs = timer.Elapsed.TotalMilliseconds;
        var identity = InspectorMemoService.Identity(entry, MemoTarget.From(asset)!);
        timer.Restart(); var basis = await service.CaptureBasisAsync(identity, asset.Memo); double basisMs = timer.Elapsed.TotalMilliseconds;
        byte[] before = File.ReadAllBytes(path); double publishedMs = 0;
        service.Published += _ => publishedMs = timer.Elapsed.TotalMilliseconds;
        timer.Restart(); var result = await service.PersistAsync(new(identity, "Gate5C measured memo", basis.Memo, basis.Revision));
        double saveMs = timer.Elapsed.TotalMilliseconds; Xunit.Assert.True(result.IsSuccess, result.Diagnostic);
        byte[] after = File.ReadAllBytes(path);
        int changed = Enumerable.Range(0, (after.Length + 4095) / 4096).Count(i => i * 4096 >= before.Length
            || !before.AsSpan(i * 4096, Math.Min(4096, before.Length - i * 4096))
                .SequenceEqual(after.AsSpan(i * 4096, Math.Min(4096, after.Length - i * 4096))));
        Console.WriteLine($"GATE5C_MEMO assets={count} reader_admission_ms={readerMs:F2} browse_registration_ms={browseMs:F2} writer_registration_ms={registerMs:F2} draft_basis_ms={basisMs:F2} save_total_ms={saveMs:F2}");
        Console.WriteLine($"GATE5C_BOUNDARIES {string.Join(' ', boundaries.Select(b => b.Key + "_ms=" + b.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)))} published_ms={publishedMs:F2}");
        Console.WriteLine($"GATE5C_FILE before={before.Length} after={after.Length} changed_4k_blocks={changed}; logical file comparison, not measured physical I/O. One warm-process synthetic observation; no desktop latency or distribution claim.");
        await catalog.UnloadAsync(entry); await service.CleanupTask;
    }
    private sealed class RejectLegacy : IPackageMemoPersistenceService
    {
        public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => throw new InvalidOperationException("Unexpected V1 routing.");
        public Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default) => throw new InvalidOperationException("Unexpected V1 routing.");
    }
}
