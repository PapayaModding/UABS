using UABS.Services;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

/// <summary>Explicit disposable diagnostics, not benchmarks run with the regression suite.</summary>
public static class V2BrowseMeasurements
{
    private sealed class Picker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
    public static async Task RunAsync(int count)
    {
        if (count is not (10_000 or 100_000 or 1_000_000)) throw new ArgumentOutOfRangeException(nameof(count));
        using var corpus = new V2SearchTests.Corpus(); var setup = System.Diagnostics.Stopwatch.StartNew();
        await corpus.Add(count); corpus.Packages[0].Legacy.Package.Indexes.Clear();
        Console.WriteLine($"BROWSE_SETUP assets={count} elapsed_ms={setup.Elapsed.TotalMilliseconds:F2} file_bytes={new FileInfo(corpus.Packages[0].Path).Length}");
        GC.Collect(); GC.WaitForPendingFinalizers(); long before = GC.GetTotalMemory(true), allocations = GC.GetTotalAllocatedBytes();
        using var catalog = new V2BrowseCatalog(); var timer = System.Diagnostics.Stopwatch.StartNew();
        var registration = await catalog.RegisterAsync(corpus.Packages[0].Path); double admission = timer.Elapsed.TotalMilliseconds;
        using var vm = new V2BrowseViewModel(new Picker(), catalog); vm.Registrations.Add(registration);
        timer.Restart(); vm.SelectedPackage = registration; await vm.SelectionTask; double first = timer.Elapsed.TotalMilliseconds;
        Assert.Equal(100, vm.Assets.Count); Assert.Equal(count, registration.AssetCount);
        timer.Restart(); await vm.NextCommand.ExecuteAsync(); double next = timer.Elapsed.TotalMilliseconds;
        Assert.Equal(100, vm.Assets.Count);
        vm.SelectedAsset = vm.Assets[0]; timer.Restart(); await vm.InspectorTask; double inspector = timer.Elapsed.TotalMilliseconds;
        long churn = GC.GetTotalAllocatedBytes() - allocations, retained = GC.GetTotalMemory(true) - before;
        Console.WriteLine($"BROWSE_MEASURE assets={count} admission_ms={admission:F2} first_assets_plus_roots_ms={first:F2} next_ms={next:F2} inspector_ms={inspector:F2} visible_rows={vm.Assets.Count} evidence_rows={vm.Metadata.Count} managed_allocated={churn} live_managed_delta={retained} runtime={Environment.Version} physical_cold=false fixture_build_excluded=true");
    }
}
