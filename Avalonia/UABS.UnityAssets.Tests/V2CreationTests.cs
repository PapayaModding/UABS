using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using UABS.PackageV2;
using UABS.Services;
using UABS.ViewModels;
using UABS.Page;
using UABS.UnityAssets.Metadata;

namespace UABS.UnityAssets.Tests;

public sealed class V2CreationTests
{
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Directory.CreateDirectory(Path.Combine(
            Environment.GetEnvironmentVariable("UABS_V2_TEST_ROOT") ?? Path.GetTempPath(), "uabs-v2-create-" + Guid.NewGuid().ToString("N"))).FullName;
        public string Source(string name = "source.bundle", bool partial = false)
        {
            string path = Path.Combine(Root, name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, V2MetadataFixture.Bundle("CAB-same", partial, "virtual-路径")); return path;
        }
        public V2CreationRequest Request(params string[] paths) => new("Created", Root, paths);
        public void Dispose() => Directory.Delete(Root, true);
    }
    private sealed class Picker(params string[] paths) : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(paths.FirstOrDefault());
        public Task<IReadOnlyList<string>> PickUnityFilesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(paths);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
    private sealed class Sink<T>(Action<T> action) : IProgress<T> { public void Report(T value) => action(value); }
    private sealed class BatchPicker(params IReadOnlyList<string>[] batches) : IFilePickerService
    {
        private int _next;
        public Task<IReadOnlyList<string>> PickUnityFilesAsync(CancellationToken cancellationToken = default) => Task.FromResult(batches[_next++]);
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
    private sealed class DeferredPicker(IReadOnlyList<string> initial) : IFilePickerService
    {
        private bool _first = true;
        public TaskCompletionSource<IReadOnlyList<string>> Pending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<string>> PickUnityFilesAsync(CancellationToken cancellationToken = default)
        {
            if (_first) { _first = false; return Task.FromResult(initial); }
            return Pending.Task.WaitAsync(cancellationToken);
        }
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }

    [AvaloniaTheory]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task LargeSelection_HasNoCountCeilingAndPublishesOnce(int count)
    {
        // Selection-only paths: no expensive source or file I/O is needed to verify picker/VM capacity.
        string[] paths = Enumerable.Range(0, count).Select(i => Path.Combine(Path.GetTempPath(), $"selection-{i}.bundle")).ToArray();
        using var vm = new V2CreationViewModel(new Picker(paths), _ => Task.FromResult<string?>(null), _ => Task.CompletedTask);
        int notifications = 0; vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.Sources)) notifications++; };
        await vm.AddSourcesCommand.ExecuteAsync();
        Assert.Equal(count, vm.SelectedFileCount); Assert.Equal(paths, vm.Sources); Assert.Equal(1, notifications);
        Assert.Contains(count.ToString("N0"), vm.SelectionSummary); Assert.True(vm.CanEditSelection);
    }

    [AvaloniaFact]
    public async Task RepeatedLargeAdds_PreserveOrderDuplicatesAndAllowIndividualRemoval()
    {
        string[] first = Enumerable.Range(0, 700).Select(i => $"source-{i}.bundle").ToArray();
        string[] second = first.Take(300).ToArray(); // Duplicates are intentionally retained and rejected at Create, not silently dropped.
        using var vm = new V2CreationViewModel(new BatchPicker(first, second), _ => Task.FromResult<string?>(null), _ => Task.CompletedTask);
        await vm.AddSourcesAsync(); await vm.AddSourcesAsync(); Assert.Equal(1000, vm.SelectedFileCount);
        Assert.Equal(first.Concat(second), vm.Sources);
        vm.SelectedSourceIndex = 705; await vm.RemoveSelectedSourceCommand.ExecuteAsync();
        var expected = first.Concat(second).ToList(); expected.RemoveAt(705);
        Assert.Equal(expected, vm.Sources); Assert.Equal(999, vm.SelectedFileCount);
        await vm.ClearSourcesCommand.ExecuteAsync(); Assert.Empty(vm.Sources); Assert.Equal(0, vm.SelectedFileCount);
        Assert.False(vm.RemoveSelectedSourceCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task CancellingPendingSelection_PreservesPriorSourcesAndIgnoresLatePickerCompletion()
    {
        var initial = Enumerable.Range(0, 500).Select(i => $"prior-{i}.bundle").ToArray();
        var picker = new DeferredPicker(initial); using var vm = new V2CreationViewModel(picker, _ => Task.FromResult<string?>(null), _ => Task.CompletedTask);
        await vm.AddSourcesAsync();
        var running = vm.AddSourcesAsync(); Assert.True(vm.IsSelecting); Assert.False(vm.CreateCommand.CanExecute(null));
        Assert.False(vm.ClearSourcesCommand.CanExecute(null)); await vm.CancelCommand.ExecuteAsync(); await running;
        picker.Pending.TrySetResult(Enumerable.Range(0, 1000).Select(i => $"late-{i}.bundle").ToArray());
        Assert.Equal(initial, vm.Sources); Assert.False(vm.IsSelecting); Assert.Contains("cancelled", vm.Status);
    }

    [AvaloniaFact]
    public async Task LargeSelectionView_IsVirtualizedAndBindsCountAndRemoval()
    {
        string[] paths = Enumerable.Range(0, 1000).Select(i => $"source-{i}.bundle").ToArray();
        using var vm = new V2CreationViewModel(new Picker(paths), _ => Task.FromResult<string?>(null), _ => Task.CompletedTask);
        await vm.AddSourcesAsync(); var view = new V2CreationView { DataContext = vm };
        var window = new Window { Content = view, Width = 1100, Height = 700 }; window.Show();
        try
        {
            view.UpdateLayout(); var list = view.FindControl<ListBox>("CreationSources")!;
            Assert.Equal(1000, list.ItemCount); Assert.Equal(vm.SelectionSummary, view.FindControl<TextBlock>("SelectedSourceCount")!.Text);
            Assert.NotEmpty(list.GetVisualDescendants().OfType<VirtualizingStackPanel>());
            Assert.InRange(list.GetVisualDescendants().OfType<ListBoxItem>().Count(), 1, 50);
            list.SelectedIndex = 550; await vm.RemoveSelectedSourceCommand.ExecuteAsync(); view.UpdateLayout();
            Assert.Equal(999, list.ItemCount); Assert.DoesNotContain("source-550.bundle", vm.Sources);
        }
        finally { window.Close(); }
    }

    private sealed class SmallSourceExtractor(IReadOnlyList<string> inputs, CancellationTokenSource? cancel = null) : IV2MetadataExtractor
    {
        public List<V2ExtractionRequest> Seen { get; } = [];
        public int Active { get; private set; }
        public int Peak { get; private set; }
        public async Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
            Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive, V2ExtractionOptions? options = null,
            IProgress<V2ExtractionProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            Active++; Peak = Math.Max(Peak, Active);
            try
            {
                // Observe actual OS sharing: only the current source is pinned, not all selected sources.
                Assert.Throws<IOException>(() => File.Open(request.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None));
                string other = Seen.Count == 0 ? inputs[^1] : inputs[Seen.Count - 1];
                using (FileStream exclusive = new(other, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                Seen.Add(request);
                await receive([new V2LogicalFile(0, "same-logical", "same-CAB", "2022.3.57f1", 22, true, 0, 0,
                    V2ExtractionStatus.Complete, V2DependencyStatus.Complete)], cancellationToken);
                if (Seen.Count == 4) cancel?.Cancel();
                string hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(request.Path, cancellationToken)));
                return new(new(request.SourceId, request.RevisionId, request.Path, Path.GetFullPath(request.Path), UnityFileKind.SerializedFile,
                    new FileInfo(request.Path).Length, File.GetLastWriteTimeUtc(request.Path), hash, true), V2ExtractionStatus.Complete, 1, 0,
                    new(default, default, default, default, default, 0, 0, 1));
            }
            finally { Active--; }
        }
    }
    private static string[] SmallSources(Fixture fixture, int count)
    {
        byte[] header = V2MetadataFixture.Serialized(0);
        return Enumerable.Range(0, count).Select(i => { string path = Path.Combine(fixture.Root, $"source-{i}.assets"); File.WriteAllBytes(path, header); return path; }).ToArray();
    }

    [Theory]
    [InlineData(257)]
    [InlineData(1000)]
    public async Task LargeActualSourceBuild_UsesOneExtractorAndReleasesNonCurrentHandles(int count)
    {
        using var f = new Fixture(); string[] inputs = SmallSources(f, count); var extractor = new SmallSourceExtractor(inputs);
        var result = await new V2PackageCreationService(extractor).CreateAsync(f.Request(inputs));
        Assert.True(result.Verified, result.Message); Assert.Equal(count, result.Info!.Sources); Assert.Equal(1, extractor.Peak); Assert.Equal(0, extractor.Active);
        Assert.Equal(inputs, extractor.Seen.Select(s => s.Path)); Assert.Equal(count, extractor.Seen.Select(s => s.SourceId).Distinct().Count());
        Assert.Equal(count, extractor.Seen.Select(s => s.RevisionId).Distinct().Count());
        foreach (string path in inputs) { using FileStream exclusive = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
    }

    [Fact]
    public async Task LargeBuildCancellation_CleansStagingAndClosesSourceHandles()
    {
        using var f = new Fixture(); string[] inputs = SmallSources(f, 1000); using var cancel = new CancellationTokenSource();
        var extractor = new SmallSourceExtractor(inputs, cancel); var result = await new V2PackageCreationService(extractor).CreateAsync(f.Request(inputs), token: cancel.Token);
        Assert.False(result.Published); Assert.Equal(4, extractor.Seen.Count); Assert.Equal(1, extractor.Peak);
        Assert.Empty(Directory.GetFiles(f.Root, "*.sqlite")); Assert.Empty(Directory.GetFiles(f.Root, ".uabs-build-*"));
        using FileStream exclusive = new(inputs[0], FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Fact]
    public async Task ReleasedSourceChangedAfterExtraction_IsRejectedByFinalFingerprintCheck()
    {
        using var f = new Fixture(); string source = f.Source();
        var result = await new V2PackageCreationService(boundary: (b, _) =>
        { if (b == BuildBoundary.BeforeValidation) File.AppendAllText(source, "external edit after extraction"); }).CreateAsync(f.Request(source));
        Assert.False(result.Published); Assert.Contains("Source changed since extraction", result.Message);
        Assert.Empty(Directory.GetFiles(f.Root, "*.sqlite"));
    }
    private sealed class ChangedExtractor : IV2MetadataExtractor
    {
        public async Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
            Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive,
            V2ExtractionOptions? options = null, IProgress<V2ExtractionProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            var result = await new AssetsToolsV2MetadataExtractor().ExtractAsync(request, receive, options, progress, cancellationToken);
            return result with { Status = V2ExtractionStatus.RevisionMismatch, Source = result.Source with { Verified = false } };
        }
    }

    [Fact]
    public async Task SingleSource_RoundTripsAllTypesNamesSignedIdsOrdinalsAndDependencies()
    {
        using var f = new Fixture(); string source = f.Source(); var bytes = File.ReadAllBytes(source);
        var result = await new V2PackageCreationService().CreateAsync(f.Request(source));
        Assert.True(result.Verified, result.Message); Assert.True(result.Published); Assert.Equal("complete", result.Info!.IndexStatus);
        using var reader = (await PackageReader.OpenAsync(result.Build!.PublishedPath!)).Reader!;
        var rows = await reader.ReadAssetsAsync(); Assert.Equal(12, rows.Count);
        Assert.Contains(rows, r => r.ClassId == 1234567); Assert.Contains(rows, r => r.Name is null); Assert.Contains(rows, r => r.Name == "");
        Assert.Contains(rows, r => r.Key.PathId == -77); Assert.Contains(rows, r => r.Key.PathId == long.MaxValue);
        Assert.Equal(new[] { 1, 3 }, rows.Select(r => r.Key.DirectoryOrdinal).Distinct());
        var dependencies = await reader.ReadMetadataAsync(MetadataTable.Dependencies);
        Assert.Contains(dependencies, d => Equals(d.Values["raw_virtual_path"], "virtual-路径"));
        Assert.Contains(dependencies, d => Equals(d.Values["raw_path"], "CAB-duplicate"));
        Assert.Equal(bytes, File.ReadAllBytes(source)); Assert.True(result.Build.Metrics.MaxBatch <= 4096);
        Assert.Empty(Directory.GetFiles(f.Root, ".uabs-build-*"));
    }

    [Fact]
    public async Task MultipleSources_EqualNamesAndBytesKeepOrderedDistinctOccurrenceIdentities()
    {
        using var f = new Fixture(); string a = f.Source("first/same.bundle"), b = f.Source("second/same.bundle");
        var result = await new V2PackageCreationService().CreateAsync(f.Request(b, a)); Assert.True(result.Verified, result.Message);
        using var reader = (await PackageReader.OpenAsync(result.Build!.PublishedPath!)).Reader!;
        var sources = await reader.ReadMetadataAsync(MetadataTable.Sources);
        Assert.Equal(new[] { b, a }, sources.Select(s => (string)s.Values["original_path"]!));
        Assert.NotEqual((byte[])sources[0].Values["source_uuid"]!, (byte[])sources[1].Values["source_uuid"]!);
        Assert.Equal((byte[])sources[0].Values["content_sha256"]!, (byte[])sources[1].Values["content_sha256"]!);
        Assert.Equal(24, reader.Info.Assets); Assert.Equal(2, result.Extraction.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialCoverage_RequiresExplicitDecisionAndDoesNotReextract(bool approve)
    {
        using var f = new Fixture(); int calls = 0; string source = f.Source(partial: true);
        var result = await new V2PackageCreationService().CreateAsync(f.Request(source), acknowledge: (summary, ct) =>
        {
            calls++; Assert.True(summary.Issues > 0); Assert.NotEmpty(summary.Details); Assert.True(summary.Details.Count <= 50);
            Assert.False(File.Exists(Path.Combine(f.Root, "Created.uabs.sqlite"))); return Task.FromResult(approve);
        });
        Assert.Equal(1, calls); Assert.Equal(approve, result.Verified); Assert.Equal(approve, result.Published);
        Assert.Single(result.Extraction); if (approve) Assert.Equal("partial", result.Info!.IndexStatus);
        Assert.Empty(Directory.GetFiles(f.Root, ".uabs-build-*"));
    }

    [Fact]
    public async Task NoPartialCallback_NeverPublishesIncompleteCoverage()
    {
        using var f = new Fixture(); var result = await new V2PackageCreationService().CreateAsync(f.Request(f.Source(partial: true)));
        Assert.Equal(StorageStatus.PartialAcknowledgementRequired, result.Build!.Status); Assert.False(result.Published);
    }

    [Fact]
    public async Task OptionalEnrichmentWarnings_DoNotRequestPartialApproval()
    {
        using var f = new Fixture(); string source = f.Source();
        var result = await new V2PackageCreationService().CreateAsync(f.Request(source), acknowledge: (_, _) => throw new Exception("Not coverage loss"));
        Assert.True(result.Verified, result.Message); Assert.Equal("complete", result.Info!.IndexStatus); Assert.True(result.Info.Issues > 0);
    }

    [Theory]
    [InlineData("same")]
    [InlineData("normalized")]
    public async Task DuplicatePhysicalInputs_AreRejected(string mode)
    {
        using var f = new Fixture(); string source = f.Source(), duplicate = source;
        if (mode == "normalized") duplicate = Path.Combine(f.Root, ".", "source.bundle");
        var result = await new V2PackageCreationService().CreateAsync(f.Request(source, duplicate));
        Assert.False(result.Published); Assert.Contains("Duplicate", result.Message); Assert.Null(result.Build);
    }

    [Fact(Skip = "This restricted verification environment denies CreateHardLink with Win32 error 5. Do not bypass; verify physical aliases on an authorized host.")]
    public async Task PhysicalHardlinkAlias_IsRejected()
    {
        using var f = new Fixture(); string source = f.Source(), alias = Path.Combine(f.Root, "alias.bundle");
        Assert.True(CreateHardLink(alias, source, IntPtr.Zero));
        var result = await new V2PackageCreationService().CreateAsync(f.Request(source, alias));
        Assert.False(result.Published); Assert.Contains("Duplicate physical", result.Message);
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string newPath, string existingPath, IntPtr attributes);

    [Theory]
    [InlineData("unsupported")]
    [InlineData("missing")]
    [InlineData("unreadable")]
    public async Task BadSources_HaveActionableDiagnosticsWithoutPublication(string mode)
    {
        using var f = new Fixture(); string source = mode == "missing" ? Path.Combine(f.Root, "missing.bundle") : f.Source();
        if (mode == "unsupported") File.WriteAllText(source, "not Unity");
        using var locked = mode == "unreadable" ? new FileStream(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None) : null;
        var result = await new V2PackageCreationService().CreateAsync(f.Request(source));
        Assert.False(result.Published); Assert.Contains(mode == "unsupported" ? "Unsupported" : Path.GetFileName(source), result.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../escape")]
    [InlineData("name.uabs.sqlite")]
    public async Task InvalidDestinationNames_DoNotWrite(string name)
    {
        using var f = new Fixture(); var result = await new V2PackageCreationService().CreateAsync(f.Request(f.Source()) with { Name = name });
        Assert.False(result.Published); Assert.Null(result.Build); Assert.Empty(Directory.GetFiles(f.Root, "*.sqlite"));
    }

    [Fact]
    public async Task ExistingOutput_IsPreservedExactly()
    {
        using var f = new Fixture(); string source = f.Source(), output = Path.Combine(f.Root, "Created.uabs.sqlite"); File.WriteAllText(output, "preserve me");
        var result = await new V2PackageCreationService().CreateAsync(f.Request(source));
        Assert.False(result.Published); Assert.Equal("preserve me", File.ReadAllText(output));
    }

    [Fact]
    public async Task ExistingDestinationSidecars_AreNeverDeletedOrAdmitted()
    {
        using var f = new Fixture(); string sidecar = Path.Combine(f.Root, "Created.uabs.sqlite-journal"); File.WriteAllText(sidecar, "unrelated recovery data");
        var result = await new V2PackageCreationService().CreateAsync(f.Request(f.Source()));
        Assert.False(result.Published); Assert.Equal("unrelated recovery data", File.ReadAllText(sidecar)); Assert.Contains("sidecar", result.Message);
    }

    [Fact]
    public async Task PublicationCollisionAfterValidation_PreservesCompetingOutput()
    {
        using var f = new Fixture(); string output = Path.Combine(f.Root, "Created.uabs.sqlite");
        var result = await new V2PackageCreationService(boundary: (b, _) => { if (b == BuildBoundary.BeforePublication) File.WriteAllText(output, "competing output"); }).CreateAsync(f.Request(f.Source()));
        Assert.False(result.Published); Assert.Equal("competing output", File.ReadAllText(output)); Assert.Equal(StorageStatus.DestinationExists, result.Build!.Status);
    }

    public static class Measurements
    {
    public static async Task MeasureAsync(int assets)
    {
        using var f = new Fixture(); string source = Path.Combine(f.Root, "scale.assets");
        File.WriteAllBytes(source, V2MetadataFixture.Serialized(assets));
        var result = await new V2PackageCreationService().CreateAsync(f.Request(source));
        Assert.True(result.Verified, result.Message); Assert.Equal(assets, result.Info!.Assets);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { Assets = assets, result.Elapsed, result.Extraction, result.Build!.Metrics }));
        using var catalog = new V2BrowseCatalog(); var registered = await catalog.RegisterAsync(result.Build.PublishedPath!);
        Assert.Equal(100, (await registered.AssetsAsync(0)).Rows.Count); Assert.Equal(assets, registered.AssetCount);
    }
    }

    [Theory]
    [InlineData(BuildBoundary.BeforeSchema)]
    [InlineData(BuildBoundary.BeforeBatch)]
    [InlineData(BuildBoundary.BeforeValidation)]
    [InlineData(BuildBoundary.BeforePublication)]
    public async Task FailureAndCancellationAtBoundaries_LeaveNoOutputOrOwnedStaging(BuildBoundary boundary)
    {
        using var f = new Fixture(); string source = f.Source();
        foreach (bool cancel in new[] { false, true })
        {
            using var cts = new CancellationTokenSource();
            var service = new V2PackageCreationService(boundary: (b, _) => { if (b != boundary) return; if (cancel) cts.Cancel(); else throw new IOException("Injected failure"); });
            var result = await service.CreateAsync(f.Request(source), token: cts.Token);
            Assert.False(result.Published); Assert.Empty(Directory.GetFiles(f.Root, "*.sqlite")); Assert.Empty(Directory.GetFiles(f.Root, ".uabs-build-*"));
        }
    }

    [Fact]
    public async Task SourceRevisionFailure_DiscardsProvisionalRowsWithoutPartialOverride()
    {
        using var f = new Fixture(); var result = await new V2PackageCreationService(new ChangedExtractor()).CreateAsync(f.Request(f.Source()), acknowledge: (_, _) => throw new Exception("Must not acknowledge byte mismatch"));
        Assert.False(result.Published); Assert.Contains("RevisionMismatch", result.Message); Assert.Empty(Directory.GetFiles(f.Root, "*.sqlite"));
    }

    [Fact]
    public async Task LateCancellation_DoesNotClaimRollbackAfterPublication()
    {
        using var f = new Fixture(); using var cts = new CancellationTokenSource();
        var result = await new V2PackageCreationService().CreateAsync(f.Request(f.Source()), new Sink<PackageBuildProgress>(p => { if (p.Stage == "Verifying published package") cts.Cancel(); }), token: cts.Token);
        Assert.True(result.Verified, result.Message); Assert.True(result.Published); Assert.True(File.Exists(result.Build!.PublishedPath));
    }

    [Fact]
    public async Task VerificationFailure_AcknowledgesPublishedFileWithoutDeletingIt()
    {
        using var f = new Fixture(); string output = Path.Combine(f.Root, "Created.uabs.sqlite");
        var result = await new V2PackageCreationService().CreateAsync(f.Request(f.Source()), new Sink<PackageBuildProgress>(p => { if (p.Stage == "Verifying published package") File.WriteAllText(output, "fixture corruption"); }));
        Assert.True(result.Published); Assert.False(result.Verified); Assert.Contains("verification failed", result.Message); Assert.True(File.Exists(output));
    }

    [Fact]
    public async Task GenuineInputs_CopiedToDisposableSources_RoundTrip86Assets()
    {
        using var f = new Fixture(); string a = Path.Combine(f.Root, "real.bundle"), b = Path.Combine(f.Root, "real.assets");
        File.Copy(TestPaths.BundleFixture, a); File.Copy(TestPaths.SerializedFixture, b);
        var result = await new V2PackageCreationService().CreateAsync(f.Request(a, b));
        Assert.True(result.Verified, result.Message); Assert.Equal(86, result.Info!.Assets);
    }

    [AvaloniaFact]
    public async Task CreationForm_RegistersAndOpensThroughExistingCatalogWithoutChangingDirtyEditor()
    {
        using var f = new Fixture(); using var app = new MainWindowViewModel(new Picker(f.Source()), new AssetsToolsAssetSessionFactory());
        Assert.True(await app.Packages.StartNewPackageAsync()); var document = app.Packages.Editor;
        var vm = app.Packages.Creation; vm.Directory = f.Root; await app.Packages.CreateV2PackageCommand.ExecuteAsync(); await vm.AddSourcesCommand.ExecuteAsync();
        var view = new PackageManagementView { DataContext = app.Packages }; var window = new Window { Content = view, Width = 1300, Height = 800 }; window.Show();
        try
        {
            view.UpdateLayout(); Assert.True(Assert.Single(view.GetVisualDescendants().OfType<V2CreationView>()).IsEffectivelyVisible);
            await vm.CreateCommand.ExecuteAsync(); Assert.True(vm.Result!.Verified, vm.Status);
            Assert.Single(app.Packages.V2.Registrations); Assert.True(document.IsDirty); Assert.True(document.IsEditing);
            Assert.True(vm.OpenPackageCommand.CanExecute(null)); Assert.False(vm.CreateCommand.CanExecute(null));
            await vm.OpenPackageCommand.ExecuteAsync(); // DenyConfirmation keeps the dirty V1 editor and creation result intact.
            Assert.True(document.IsDirty); Assert.True(vm.IsOpen); Assert.Null(app.Packages.SelectedPackage);
            await vm.CloseCommand.ExecuteAsync(); Assert.False(vm.IsOpen); Assert.True(document.IsDirty);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task ExplicitApprovalAndCommandDisabling_AreWiredInActualCreationView()
    {
        using var f = new Fixture(); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var vm = new V2CreationViewModel(new Picker(f.Source(partial: true)), _ => Task.FromResult<string?>(null), _ => Task.CompletedTask);
        vm.Directory = f.Root; vm.Show(); await vm.AddSourcesAsync();
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.AwaitingAcknowledgement) && vm.AwaitingAcknowledgement) entered.TrySetResult(); };
        var view = new V2CreationView { DataContext = vm }; var window = new Window { Content = view, Width = 1000, Height = 700 }; window.Show();
        try
        {
            var running = vm.CreateCommand.ExecuteAsync(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(15)); view.UpdateLayout();
            Assert.True(view.FindControl<Button>("AcknowledgeIncomplete")!.IsVisible);
            Assert.False(vm.AddSourcesCommand.CanExecute(null)); Assert.False(vm.CloseCommand.CanExecute(null)); Assert.False(vm.CreateCommand.CanExecute(null));
            Assert.True(vm.CancelCommand.CanExecute(null)); Assert.Contains("Incomplete", vm.Status);
            await vm.AcknowledgeCommand.ExecuteAsync(); await running;
            Assert.True(vm.Result!.Verified, vm.Status); Assert.Equal("partial", vm.Result.Info!.IndexStatus);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task CancellationWhileAwaitingApproval_LeavesNoPackageAndPreservesDetails()
    {
        using var f = new Fixture(); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var vm = new V2CreationViewModel(new Picker(f.Source(partial: true)), _ => throw new Exception("No registration"), _ => Task.CompletedTask);
        vm.Directory = f.Root; await vm.AddSourcesAsync(); vm.PropertyChanged += (_, e) => { if (vm.AwaitingAcknowledgement) entered.TrySetResult(); };
        var run = vm.CreateAsync(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(15)); await vm.CancelCommand.ExecuteAsync(); await run;
        Assert.False(vm.Result!.Published); Assert.NotEmpty(vm.Details); Assert.False(vm.IsBusy); Assert.Empty(Directory.GetFiles(f.Root, "*.sqlite"));
    }

    [AvaloniaFact]
    public async Task RegistrationFailure_IsSeparateFromVerifiedCreationAndKeepsSavedFile()
    {
        using var f = new Fixture(); using var vm = new V2CreationViewModel(new Picker(f.Source()), _ => Task.FromResult<string?>("Injected admission failure"), _ => Task.CompletedTask);
        vm.Directory = f.Root; await vm.AddSourcesAsync(); await vm.CreateAsync();
        Assert.True(vm.Result!.Verified); Assert.Contains("registration failed", vm.Status); Assert.Contains(vm.Result.Build!.PublishedPath!, vm.Details);
        Assert.True(File.Exists(vm.Result.Build.PublishedPath)); Assert.True(vm.OpenPackageCommand.CanExecute(null));
    }
}
