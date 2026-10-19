using UABS.Models;
using UABS.Services;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class DirectoryBrowserServiceTests
{
    [Fact]
    public async Task ReadAsync_ShowsSupportedContentRegardlessOfExtensionAndSortsFoldersFirst()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateDirectory("Zeta");
        directory.CopyFile(TestPaths.BundleFixture, "conventional.bundle");
        directory.CopyFile(TestPaths.BundleFixture, "bundle.png");
        directory.CopyFile(TestPaths.BundleFixture, "extensionless");
        directory.CopyFile(TestPaths.SerializedFixture, "serialized.data");

        DirectorySnapshot snapshot = await new DirectoryBrowserService().ReadAsync(directory.Path);

        Assert.Equal(Path.GetFullPath(directory.Path), snapshot.Path);
        Assert.Equal(
            ["Zeta", "bundle.png", "conventional.bundle", "extensionless", "serialized.data"],
            snapshot.Entries.Select(entry => entry.Name));
        Assert.Equal(BrowserEntryKind.Directory, snapshot.Entries[0].Kind);
        Assert.All(snapshot.Entries.Skip(1), entry => Assert.Equal(BrowserEntryKind.File, entry.Kind));
    }

    [Fact]
    public async Task ReadAsync_HidesFakeUnityExtensionsAndUnrelatedFiles()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteText("fake.bundle", "not a Unity file");
        directory.WriteText("notes.txt", "notes");
        directory.WriteBytes("image.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        directory.WriteText("data.json", "{\"valid\":true}");
        directory.WriteBytes("program.exe", [0x4D, 0x5A, 0, 0]);
        directory.WriteBytes("random.bin", [1, 2, 3, 4]);

        DirectorySnapshot snapshot = await new DirectoryBrowserService().ReadAsync(directory.Path);

        Assert.Empty(snapshot.Entries);
    }

    [Fact]
    public async Task ReadAsync_PreservesAllImmediateDirectoriesWithoutRecursing()
    {
        using var directory = new TemporaryDirectory();
        string empty = directory.CreateDirectory("Empty");
        string unsupportedOnly = directory.CreateDirectory("UnsupportedOnly");
        string nested = directory.CreateDirectory(Path.Combine("Nested", "Child"));
        File.WriteAllText(Path.Combine(unsupportedOnly, "notes.txt"), "notes");
        File.Copy(TestPaths.BundleFixture, Path.Combine(nested, "deep.bundle"));

        DirectorySnapshot root = await new DirectoryBrowserService().ReadAsync(directory.Path);
        DirectorySnapshot emptySnapshot = await new DirectoryBrowserService().ReadAsync(empty);
        DirectorySnapshot unsupportedSnapshot = await new DirectoryBrowserService().ReadAsync(unsupportedOnly);

        Assert.Equal(["Empty", "Nested", "UnsupportedOnly"], root.Entries.Select(entry => entry.Name));
        Assert.All(root.Entries, entry => Assert.True(entry.IsDirectory));
        Assert.Empty(emptySnapshot.Entries);
        Assert.Empty(unsupportedSnapshot.Entries);
    }

    [Fact]
    public async Task ReadAsync_HidesAndReportsFileDeletedDuringInspection()
    {
        using var directory = new TemporaryDirectory();
        string deleted = directory.WriteText("vanishing.bundle", "temporary");
        List<(string Context, Exception Error)> diagnostics = [];
        var recognizer = new DelegateRecognitionService((path, _) =>
        {
            File.Delete(path);
            return new UnityFileRecognition(
                UnityFileRecognitionStatus.Unreadable,
                UnityFileKind.Unknown,
                new FileNotFoundException("Deleted during scan.", path));
        });
        var service = new DirectoryBrowserService(
            recognizer,
            reportInspectionFailure: (context, error) => diagnostics.Add((context, error)));

        DirectorySnapshot snapshot = await service.ReadAsync(directory.Path);

        Assert.Empty(snapshot.Entries);
        Assert.False(File.Exists(deleted));
        Assert.Single(diagnostics);
        Assert.Contains("vanishing.bundle", diagnostics[0].Context);
        Assert.IsType<FileNotFoundException>(diagnostics[0].Error);
    }

    [Fact]
    public async Task ReadAsync_HidesRecognizedUnsupportedFilesWithoutReportingAccessFailure()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteText("recognized.resource", "placeholder");
        int diagnosticCount = 0;
        var recognizer = new DelegateRecognitionService((_, _) =>
            new UnityFileRecognition(UnityFileRecognitionStatus.RecognizedUnsupported, UnityFileKind.Unknown));
        var service = new DirectoryBrowserService(
            recognizer,
            reportInspectionFailure: (_, _) => diagnosticCount++);

        DirectorySnapshot snapshot = await service.ReadAsync(directory.Path);

        Assert.Empty(snapshot.Entries);
        Assert.Equal(0, diagnosticCount);
    }

    [Fact]
    public async Task ReadAsync_CancelsBoundedLargeDirectoryInspection()
    {
        using var directory = new TemporaryDirectory();
        for (int index = 0; index < 24; index++)
            directory.WriteText($"file-{index:D2}.bin", "data");

        var recognizer = new BlockingRecognitionService();
        var service = new DirectoryBrowserService(recognizer, maxConcurrentInspections: 2, (_, _) => { });
        using var cancellation = new CancellationTokenSource();
        Task<DirectorySnapshot> read = service.ReadAsync(directory.Path, cancellation.Token);
        await recognizer.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        Assert.InRange(recognizer.MaximumConcurrent, 1, 2);
        Assert.True(recognizer.Calls < 24);
    }

    [Fact]
    public async Task RefreshAsync_RescansAddedRemovedAndChangedFiles()
    {
        using var directory = new TemporaryDirectory();
        string changing = directory.WriteText("changing.bundle", "invalid");
        using BrowserViewModel browser = new(
            new DirectoryBrowserService(),
            new NullPicker(),
            (_, _) => Task.CompletedTask,
            directory.Path);

        await browser.InitializeAsync();
        Assert.Empty(browser.Entries);
        Assert.True(browser.ShowEmptyMessage);

        File.Copy(TestPaths.BundleFixture, changing, overwrite: true);
        await browser.RefreshAsync();
        Assert.Single(browser.Entries);

        File.WriteAllText(changing, "invalid again");
        directory.CopyFile(TestPaths.SerializedFixture, "added-without-extension");
        await browser.RefreshAsync();
        Assert.Equal("added-without-extension", Assert.Single(browser.Entries).Name);

        File.Delete(Path.Combine(directory.Path, "added-without-extension"));
        await browser.RefreshAsync();
        Assert.Empty(browser.Entries);
        Assert.True(browser.ShowEmptyMessage);
    }

    [Fact]
    public async Task ReadAsync_DisposesInspectedFileStream()
    {
        using var directory = new TemporaryDirectory();
        string file = directory.CopyFile(TestPaths.BundleFixture, "lock-check.bundle");

        DirectorySnapshot snapshot = await new DirectoryBrowserService().ReadAsync(directory.Path);
        using var exclusive = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Assert.Single(snapshot.Entries);
        Assert.True(exclusive.CanRead);
    }

    [Fact]
    public async Task ReadAsync_ReportsMissingDirectory() =>
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            new DirectoryBrowserService().ReadAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));

    private sealed class DelegateRecognitionService(
        Func<string, CancellationToken, UnityFileRecognition> recognize) : IUnityFileRecognitionService
    {
        public UnityFileRecognition Recognize(string path, CancellationToken cancellationToken = default) =>
            recognize(path, cancellationToken);
    }

    private sealed class BlockingRecognitionService : IUnityFileRecognitionService
    {
        private int _active;
        private int _maximumConcurrent;
        private int _calls;

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Calls => Volatile.Read(ref _calls);
        public int MaximumConcurrent => Volatile.Read(ref _maximumConcurrent);

        public UnityFileRecognition Recognize(string path, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            int active = Interlocked.Increment(ref _active);
            UpdateMaximum(active);
            Started.TrySetResult();
            try
            {
                cancellationToken.WaitHandle.WaitOne();
                cancellationToken.ThrowIfCancellationRequested();
                return new UnityFileRecognition(UnityFileRecognitionStatus.Invalid, UnityFileKind.Unknown);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }

        private void UpdateMaximum(int active)
        {
            int observed;
            do
            {
                observed = Volatile.Read(ref _maximumConcurrent);
                if (active <= observed)
                    return;
            }
            while (Interlocked.CompareExchange(ref _maximumConcurrent, active, observed) != observed);
        }
    }

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"uabs-browser-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateDirectory(string relativePath) => Directory.CreateDirectory(
            System.IO.Path.Combine(Path, relativePath)).FullName;

        public string CopyFile(string source, string relativePath)
        {
            string destination = System.IO.Path.Combine(Path, relativePath);
            File.Copy(source, destination);
            return destination;
        }

        public string WriteText(string relativePath, string contents)
        {
            string destination = System.IO.Path.Combine(Path, relativePath);
            File.WriteAllText(destination, contents);
            return destination;
        }

        public string WriteBytes(string relativePath, byte[] contents)
        {
            string destination = System.IO.Path.Combine(Path, relativePath);
            File.WriteAllBytes(destination, contents);
            return destination;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
