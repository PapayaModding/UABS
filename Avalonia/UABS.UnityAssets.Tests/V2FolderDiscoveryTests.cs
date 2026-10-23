using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using UABS.Page;
using UABS.Services;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class V2FolderDiscoveryTests
{
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Directory.CreateDirectory(Path.Combine(
            Environment.GetEnvironmentVariable("UABS_V2_TEST_ROOT") ?? Path.GetTempPath(), "uabs-v2-folder-" + Guid.NewGuid().ToString("N"))).FullName;
        private readonly byte[] _serialized = V2MetadataFixture.Serialized(0);
        public string Save(string relative, bool unsupported = false, bool bundle = false)
        {
            string path = Path.GetFullPath(Path.Combine(Root, relative)); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, unsupported ? "plain text"u8.ToArray() : bundle ? V2MetadataFixture.Bundle("CAB-repeat") : _serialized); return path;
        }
        public void Dispose() => Directory.Delete(Root, true);
    }
    private sealed class Picker(string folder, params string[] manual) : IFilePickerService
    {
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(folder);
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(manual.FirstOrDefault());
        public Task<IReadOnlyList<string>> PickUnityFilesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(manual);
    }
    private sealed class ControlledFiles : IV2FolderFileSystem
    {
        public string? Blocked { get; set; }
        public string? Link { get; set; }
        public string? Outside { get; set; }
        public bool Reverse { get; set; }
        public Action<string>? BeforeInspect { get; set; }
        public Func<string, string>? Identity { get; set; }
        public List<string> Enumerated { get; } = [];
        public int Active { get; private set; }
        public int Peak { get; private set; }
        public IEnumerable<string> EnumerateEntries(string directory)
        {
            if (directory == Link) throw new Exception("A link/cycle must never be enumerated.");
            if (directory == Blocked) throw new UnauthorizedAccessException("Controlled inaccessible directory");
            Enumerated.Add(directory);
            IEnumerable<string> entries = Directory.EnumerateFileSystemEntries(directory);
            if (Outside is not null) entries = entries.Concat([Outside]);
            return Reverse ? entries.Reverse() : entries;
        }
        public FileAttributes GetAttributes(string path) => path == Link ? FileAttributes.Directory | FileAttributes.ReparsePoint : File.GetAttributes(path);
        public (bool Supported, string Physical) InspectSource(string path)
        {
            Active++; Peak = Math.Max(Peak, Active);
            try
            {
                BeforeInspect?.Invoke(path);
                if (path == Blocked) throw new IOException("Controlled unreadable source");
                return (UnityFileDetector.Detect(path) != UnityFileKind.Unknown, Identity?.Invoke(path) ?? Path.GetFullPath(path).ToUpperInvariant());
            }
            finally { Active--; }
        }
    }
    private sealed class Progress(Action<V2FolderProgress> report) : IProgress<V2FolderProgress>
    { public void Report(V2FolderProgress value) => report(value); }

    [Fact]
    public async Task HeaderDiscovery_IncludesNestedExtensionlessSourcesAndReportsUnsupportedFiles()
    {
        using var f = new Fixture(); string root = f.Save("root.assets"), nested = f.Save("nested/game-data", bundle: true);
        f.Save("not-unity.bundle", unsupported: true); f.Save("nested/readme", unsupported: true);
        var result = await new V2FolderDiscoveryService().DiscoverAsync(f.Root, true, []);
        Assert.True(result.Succeeded, result.FirstError); Assert.Equal(2, result.Sources.Count); Assert.Contains(root, result.Sources); Assert.Contains(nested, result.Sources);
        Assert.Equal(2, result.Unsupported); Assert.Equal(4, result.Scanned); Assert.Contains(result.Details, d => d.Contains("Unsupported header"));
        var currentOnly = await new V2FolderDiscoveryService().DiscoverAsync(f.Root, false, []);
        Assert.Equal(root, Assert.Single(currentOnly.Sources));
    }

    [Theory]
    [InlineData(257)]
    [InlineData(1200)]
    public async Task LargeRecursiveDiscovery_HasNoLimitAndReleasesEveryHeaderHandle(int count)
    {
        using var f = new Fixture(); var expected = Enumerable.Range(0, count).Select(i => f.Save($"sub-{i % 8}/file-{i:D5}")).ToArray();
        var result = await new V2FolderDiscoveryService().DiscoverAsync(f.Root, true, []);
        Assert.True(result.Succeeded, result.FirstError); Assert.Equal(count, result.Sources.Count); Assert.Equal(count, result.Scanned);
        Assert.Equal(expected.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ThenBy(p => p, StringComparer.Ordinal), result.Sources);
        foreach (string path in expected) { using FileStream exclusive = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
    }

    [Fact]
    public async Task EnumerationOrderAndPhysicalAliasRepresentative_AreDeterministic()
    {
        using var f = new Fixture(); string a = f.Save("a.assets"), z = f.Save("z.assets"), other = f.Save("nested/other.assets");
        string Identity(string path) => path == a || path == z ? "same-physical-source" : path;
        var forward = await new V2FolderDiscoveryService(new ControlledFiles { Identity = Identity }).DiscoverAsync(f.Root, true, []);
        var reverse = await new V2FolderDiscoveryService(new ControlledFiles { Identity = Identity, Reverse = true }).DiscoverAsync(f.Root, true, []);
        Assert.Equal(forward.Sources, reverse.Sources); Assert.Contains(a, forward.Sources); Assert.DoesNotContain(z, forward.Sources); Assert.Contains(other, forward.Sources);
        Assert.Equal(1, forward.Duplicates);
    }

    [Fact]
    public async Task ExistingCanonicalAndPhysicalSelections_AreNotAddedAgain()
    {
        using var f = new Fixture(); string a = f.Save("a"), alias = f.Save("alias"), b = f.Save("b");
        var fs = new ControlledFiles { Identity = p => p == a || p == alias ? "shared" : p };
        var result = await new V2FolderDiscoveryService(fs).DiscoverAsync(f.Root, true, [a]);
        Assert.Equal(b, Assert.Single(result.Sources)); Assert.Equal(2, result.Duplicates); Assert.Equal(1, fs.Peak); Assert.Equal(0, fs.Active);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InaccessibleDirectoryOrUnreadableFile_IsAnExplicitFailedDiscovery(bool directory)
    {
        using var f = new Fixture(); string blocked = directory ? Directory.CreateDirectory(Path.Combine(f.Root, "blocked")).FullName : f.Save("blocked.assets");
        f.Save("valid.assets"); var fs = new ControlledFiles { Blocked = blocked };
        var result = await new V2FolderDiscoveryService(fs).DiscoverAsync(f.Root, true, []);
        Assert.False(result.Succeeded); Assert.Equal(1, result.Errors); Assert.Contains(blocked, result.FirstError); Assert.NotEmpty(result.Details);
    }

    [Fact]
    public async Task SimulatedJunctionCycle_IsSkippedWithoutFollowingIt()
    {
        using var f = new Fixture(); string cycle = Directory.CreateDirectory(Path.Combine(f.Root, "cycle")).FullName; f.Save("valid");
        var fs = new ControlledFiles { Link = cycle }; var result = await new V2FolderDiscoveryService(fs).DiscoverAsync(f.Root, true, []);
        Assert.True(result.Succeeded, result.FirstError); Assert.Single(result.Sources); Assert.Equal(1, result.SkippedLinks);
        Assert.DoesNotContain(cycle, fs.Enumerated);
        var linkedRoot = await new V2FolderDiscoveryService(fs).DiscoverAsync(cycle, true, []);
        Assert.False(linkedRoot.Succeeded); Assert.DoesNotContain(cycle, fs.Enumerated);
    }

    [Fact]
    public async Task OutsideRootEntry_IsRejectedBeforeOpeningIt()
    {
        using var f = new Fixture(); using var outside = new Fixture(); string foreign = outside.Save("do-not-read");
        var fs = new ControlledFiles { Outside = foreign, BeforeInspect = _ => throw new Exception("Outside input must not be inspected") };
        var result = await new V2FolderDiscoveryService(fs).DiscoverAsync(f.Root, true, []);
        Assert.False(result.Succeeded); Assert.Contains("outside the selected folder", result.FirstError);
    }

    [Fact]
    public async Task CancellationDuringHeaderEnumeration_AbortsWithoutResultOrLeakedHandle()
    {
        using var f = new Fixture(); string a = f.Save("a"), b = f.Save("b"); using var cancel = new CancellationTokenSource();
        var fs = new ControlledFiles();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new V2FolderDiscoveryService(fs).DiscoverAsync(f.Root, true, [],
            new Progress(_ => cancel.Cancel()), cancel.Token));
        Assert.Equal(0, fs.Active); Assert.Equal(1, fs.Peak);
        using FileStream exclusive = new(a, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Fact]
    public async Task ManyUnsupportedFiles_KeepOnlyBoundedDiagnosticSamples()
    {
        using var f = new Fixture(); for (int i = 0; i < 1000; i++) f.Save($"unsupported-{i}", unsupported: true);
        var result = await new V2FolderDiscoveryService().DiscoverAsync(f.Root, true, []);
        Assert.True(result.Succeeded); Assert.Empty(result.Sources); Assert.Equal(1000, result.Unsupported); Assert.Equal(50, result.Details.Count);
    }

    [AvaloniaFact]
    public async Task FolderCommand_MergesManualAndOverlappingFoldersAndSupportsRemoval()
    {
        using var f = new Fixture(); string manual = f.Save("manual"), nested = f.Save("child/nested");
        using var vm = new V2CreationViewModel(new Picker(f.Root, manual), _ => Task.FromResult<string?>(null), _ => Task.CompletedTask);
        Assert.True(vm.IncludeSubfolders); await vm.AddSourcesAsync(); await vm.AddFolderCommand.ExecuteAsync();
        Assert.Equal(new[] { manual, nested }, vm.Sources); Assert.Equal(2, vm.SelectedFileCount);
        var retained = vm.Sources; await vm.AddFolderAsync(); Assert.Same(retained, vm.Sources);
        vm.SelectedSourceIndex = 1; await vm.RemoveSelectedSourceCommand.ExecuteAsync(); Assert.Equal(manual, Assert.Single(vm.Sources));
        var view = new V2CreationView { DataContext = vm }; var window = new Window { Content = view, Width = 1100, Height = 700 }; window.Show();
        try
        {
            view.UpdateLayout(); Assert.Equal(vm.AddFolderCommand, view.FindControl<Button>("V2AddFolder")!.Command);
            var recursion = view.FindControl<CheckBox>("V2IncludeSubfolders")!; Assert.True(recursion.IsChecked);
            recursion.IsChecked = false; Assert.False(vm.IncludeSubfolders);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task FailedDiscovery_PreservesManualSelectionWithoutPartialMerge()
    {
        using var f = new Fixture(); string manual = f.Save("manual"), blocked = Directory.CreateDirectory(Path.Combine(f.Root, "blocked")).FullName;
        f.Save("new-valid"); var fs = new ControlledFiles { Blocked = blocked };
        using var vm = new V2CreationViewModel(new Picker(f.Root, manual), _ => Task.FromResult<string?>(null), _ => Task.CompletedTask,
            folderDiscovery: new V2FolderDiscoveryService(fs));
        await vm.AddSourcesAsync(); var retained = vm.Sources; await vm.AddFolderAsync();
        Assert.Same(retained, vm.Sources); Assert.Equal(manual, Assert.Single(vm.Sources)); Assert.Contains("preserved", vm.Status); Assert.Contains("blocked", vm.Details);
    }

    [AvaloniaFact]
    public async Task CancelledScanAndLateCompletion_CannotModifyPriorOrNewerSelection()
    {
        using var f = new Fixture(); string manual = f.Save("manual"), a = f.Save("a");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); using var release = new ManualResetEventSlim();
        var fs = new ControlledFiles { BeforeInspect = path => { if (path == a) { entered.TrySetResult(); Assert.True(release.Wait(TimeSpan.FromSeconds(15))); } } };
        using var vm = new V2CreationViewModel(new Picker(f.Root, manual), _ => Task.FromResult<string?>(null), _ => Task.CompletedTask,
            folderDiscovery: new V2FolderDiscoveryService(fs));
        await vm.AddSourcesAsync(); var retained = vm.Sources; var running = vm.AddFolderAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15)); Assert.True(vm.IsSelecting); Assert.False(vm.AddSourcesCommand.CanExecute(null));
        Assert.False(vm.CreateCommand.CanExecute(null)); await vm.CancelCommand.ExecuteAsync(); release.Set(); await running;
        Assert.Same(retained, vm.Sources); Assert.Contains("cancelled", vm.Status); Assert.False(vm.IsSelecting);
        await vm.ClearSourcesCommand.ExecuteAsync(); Assert.Empty(vm.Sources); // Late progress cannot add stale results back.
    }
}
