using UABS.Services;
using UABS.UnityAssets;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task LoadFileAsync_ReplacesSuccessfulSessionAndDisposesPrevious()
    {
        FakeSession first = FakeSession.Create("first.assets", [Entry(1)]);
        FakeSession second = FakeSession.Create("second.assets", [Entry(2), Entry(3)]);
        using MainWindowViewModel viewModel = new(new NullPicker(), new QueueFactory(first, second));

        await viewModel.LoadFileAsync(first.Metadata.SourcePath);
        await viewModel.LoadFileAsync(second.Metadata.SourcePath);

        Assert.True(first.IsDisposed);
        Assert.False(second.IsDisposed);
        Assert.Equal(second.Metadata.SourcePath, viewModel.ActiveFilePath);
        Assert.Equal([2L, 3L], viewModel.Assets.Select(asset => asset.PathId));
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task LoadFileAsync_FailedReplacementKeepsSuccessfulSession()
    {
        FakeSession first = FakeSession.Create("first.assets", [Entry(1)]);
        QueueFactory factory = new(first, new AssetLoadException(AssetLoadErrorCode.UnsupportedFormat, "Unsupported test file."));
        using MainWindowViewModel viewModel = new(new NullPicker(), factory);

        await viewModel.LoadFileAsync(first.Metadata.SourcePath);
        await viewModel.LoadFileAsync("invalid.bin");

        Assert.False(first.IsDisposed);
        Assert.Equal(first.Metadata.SourcePath, viewModel.ActiveFilePath);
        Assert.Single(viewModel.Assets);
        Assert.True(viewModel.HasError);
        Assert.Contains("Still showing", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadFileAsync_AcceptsEmptyAssetList()
    {
        FakeSession empty = FakeSession.Create("empty.assets", []);
        using MainWindowViewModel viewModel = new(new NullPicker(), new QueueFactory(empty));

        await viewModel.LoadFileAsync(empty.Metadata.SourcePath);

        Assert.Empty(viewModel.Assets);
        Assert.Contains("Loaded 0 assets", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dispose_ReleasesCurrentSessionAndIsIdempotent()
    {
        FakeSession session = FakeSession.Create("current.assets", [Entry(1)]);
        MainWindowViewModel viewModel = new(new NullPicker(), new QueueFactory(session));
        await viewModel.LoadFileAsync(session.Metadata.SourcePath);

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(session.IsDisposed);
        Assert.Equal(1, session.DisposeCount);
    }

    [Fact]
    public async Task BrowserNavigation_DoesNotReplaceOrDisposeAssetSession()
    {
        FakeSession session = FakeSession.Create("current.assets", [Entry(1)]);
        using MainWindowViewModel viewModel = new(
            new NullPicker(), new QueueFactory(session), new SingleDirectoryBrowser("A"), "A");
        await viewModel.LoadFileAsync(session.Metadata.SourcePath);

        await viewModel.InitializeAsync();
        await viewModel.Browser.RefreshAsync();

        Assert.False(session.IsDisposed);
        Assert.Single(viewModel.Assets);
    }

    private static AssetEntry Entry(long pathId) =>
        new($"Asset {pathId}", "TextAsset", 49, pathId, 10, "fixture.assets", "fixture.assets");

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class QueueFactory(params object[] results) : IAssetSessionFactory
    {
        private readonly Queue<object> _results = new(results);

        public Task<IAssetSession> OpenAsync(string path, CancellationToken cancellationToken = default)
        {
            object result = _results.Dequeue();
            return result is Exception exception
                ? Task.FromException<IAssetSession>(exception)
                : Task.FromResult((IAssetSession)result);
        }
    }

    private sealed class SingleDirectoryBrowser(string path) : IDirectoryBrowserService
    {
        public Task<UABS.Models.DirectorySnapshot> ReadAsync(string requestedPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new UABS.Models.DirectorySnapshot(path, []));
    }

    private sealed class FakeSession(
        AssetSessionMetadata metadata,
        IReadOnlyList<AssetEntry> assets) : IAssetSession
    {
        public AssetSessionMetadata Metadata { get; } = metadata;

        public IReadOnlyList<AssetEntry> Assets { get; } = assets;

        public IReadOnlyList<string> Warnings { get; } = [];

        public bool IsDisposed { get; private set; }

        public int DisposeCount { get; private set; }

        public Task<AssetDetails> GetDetailsAsync(AssetEntry asset, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AssetDetails(asset.Name, asset.Type, asset.ClassId, asset.PathId, asset.ByteSize,
                asset.LogicalFile, asset.SourcePath, Metadata.Kind, Metadata.UnityVersion, Metadata.LogicalFileCount));

        public static FakeSession Create(string fileName, IReadOnlyList<AssetEntry> assets)
        {
            string path = Path.GetFullPath(fileName);
            return new FakeSession(
                new AssetSessionMetadata(path, fileName, UnityFileKind.SerializedFile, "2021.3.0f1", 1),
                assets);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            DisposeCount++;
        }
    }
}
