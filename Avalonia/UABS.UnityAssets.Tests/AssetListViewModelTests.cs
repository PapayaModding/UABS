using UABS.UnityAssets;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class AssetListViewModelTests
{
    [Fact]
    public void Sort_UsesNumericValuesAndPreservesSelectedInstance()
    {
        AssetEntry ten = Entry("ten", "Texture2D", 28, 10, 100);
        AssetEntry two = Entry("two", "Sprite", 213, 2, 20);
        using FakeSession session = new([ten, two]);
        using AssetListViewModel list = new();
        list.ReplaceSession(session);
        list.SelectedAsset = ten;

        list.SortField = AssetSortField.PathId;

        Assert.Equal([2L, 10L], list.Assets.Select(asset => asset.PathId));
        Assert.Same(ten, list.SelectedAsset);
    }

    [Fact]
    public void Filters_CombineAndClearHiddenSelection()
    {
        AssetEntry keep = Entry("Hero Body", "Texture2D", 28, 12345, 100);
        AssetEntry hide = Entry("Enemy", "Sprite", 213, 678, 20);
        using FakeSession session = new([keep, hide]);
        using AssetListViewModel list = new();
        list.ReplaceSession(session);
        list.SelectedAsset = hide;

        list.NameFilter = "hero";
        list.TypeFilter = "texture";
        list.PathIdFilter = "234";

        Assert.Same(keep, Assert.Single(list.Assets));
        Assert.Null(list.SelectedAsset);
    }

    [Fact]
    public async Task Selection_LoadsSessionBackedDetailsLazily()
    {
        AssetEntry asset = Entry("Hero", "Texture2D", 28, 42, 100);
        using FakeSession session = new([asset]);
        using AssetListViewModel list = new();
        list.ReplaceSession(session);

        Assert.Equal(0, session.DetailsCalls);
        list.SelectedAsset = asset;
        await list.DetailsLoadingTask;

        Assert.Equal(1, session.DetailsCalls);
        Assert.Equal(UnityFileKind.SerializedFile, list.SelectedDetails?.FileKind);
        Assert.Equal("2021.3.0f1", list.SelectedDetails?.UnityVersion);
    }

    [Fact]
    public async Task RapidSelection_IgnoresStaleDetails()
    {
        AssetEntry first = Entry("First", "TextAsset", 49, 1, 10);
        AssetEntry second = Entry("Second", "TextAsset", 49, 2, 10);
        using DelayedSession session = new([first, second]);
        using AssetListViewModel list = new();
        list.ReplaceSession(session);

        list.SelectedAsset = first;
        Task firstLoad = list.DetailsLoadingTask;
        list.SelectedAsset = second;
        Task secondLoad = list.DetailsLoadingTask;
        session.Complete(first);
        session.Complete(second);
        await Task.WhenAll(firstLoad, secondLoad);

        Assert.Equal("Second", list.SelectedDetails?.Name);
    }

    [Fact]
    public async Task ReplacingSession_CancelsOldDetailsAndClearsSelection()
    {
        AssetEntry oldAsset = Entry("Old", "TextAsset", 49, 1, 10);
        AssetEntry newAsset = Entry("New", "Texture2D", 28, 2, 20);
        using DelayedSession oldSession = new([oldAsset]);
        using FakeSession newSession = new([newAsset]);
        using AssetListViewModel list = new();
        list.ReplaceSession(oldSession);
        list.SelectedAsset = oldAsset;
        Task oldLoad = list.DetailsLoadingTask;

        list.ReplaceSession(newSession);
        oldSession.Complete(oldAsset);
        await oldLoad;

        Assert.Null(list.SelectedAsset);
        Assert.Null(list.SelectedDetails);
        Assert.Same(newAsset, Assert.Single(list.Assets));
    }

    [Fact]
    public void SelectionAndCatalogChangesRefreshReadOnlyMemoIndependently()
    {
        AssetEntry asset = new("Hero Script", "TextAsset", 49, 7, 100, "main.assets", @"C:\game\source.ab");
        using FakeSession session = new([asset]);
        using AssetListViewModel list = new();
        list.ReplaceSession(session);
        list.SelectedAsset = asset;

        Assert.Equal(PackageMemoResolutionStatus.NoMatch, list.MemoInspector.Status);

        PackageDocument package = new([
            new PackageIndex(string.Empty, [new PackageBundleReference
            {
                Name = "source.ab",
                Path = asset.SourcePath,
                AssetInfos = [new PackageAssetReference { Name = asset.Name, PathId = asset.PathId, Memo = "full memo" }]
            }])
        ], @"C:\packages\alpha");
        list.UpdateMemoCatalog([new PackageMemoCatalogEntry("Alpha", package.PackagePath!, package)]);

        Assert.Equal(PackageMemoResolutionStatus.Available, list.MemoInspector.Status);
        Assert.Equal("full memo", list.MemoInspector.MemoText);
        list.UpdateMemoCatalog([]);
        Assert.Equal(PackageMemoResolutionStatus.NoMatch, list.MemoInspector.Status);
    }

    [Fact]
    public void DuplicateLogicalFileIdentityIsNotReportedAsUnique()
    {
        AssetEntry first = new("Hero", "Texture2D", 28, 7, 100, "one.assets", @"C:\game\source.ab");
        AssetEntry second = first with { LogicalFile = "two.assets" };
        using FakeSession session = new([first, second]);
        using AssetListViewModel list = new();
        list.ReplaceSession(session);
        list.SelectedAsset = first;

        Assert.Equal(PackageMemoResolutionStatus.Ambiguous, list.MemoInspector.Status);
    }

    private static AssetEntry Entry(string name, string type, int classId, long pathId, uint size) =>
        new(name, type, classId, pathId, size, "fixture.assets", "fixture.assets");

    private class FakeSession(IReadOnlyList<AssetEntry> assets) : IAssetSession
    {
        public AssetSessionMetadata Metadata { get; } = new("fixture.assets", "fixture.assets", UnityFileKind.SerializedFile, "2021.3.0f1", 1);
        public IReadOnlyList<AssetEntry> Assets { get; } = assets;
        public IReadOnlyList<string> Warnings { get; } = [];
        public int DetailsCalls { get; private set; }

        public virtual Task<AssetDetails> GetDetailsAsync(AssetEntry asset, CancellationToken cancellationToken = default)
        {
            DetailsCalls++;
            return Task.FromResult(Details(asset));
        }

        protected AssetDetails Details(AssetEntry asset) => new(asset.Name, asset.Type, asset.ClassId, asset.PathId,
            asset.ByteSize, asset.LogicalFile, asset.SourcePath, Metadata.Kind, Metadata.UnityVersion, Metadata.LogicalFileCount);
        public void Dispose() { }
    }

    private sealed class DelayedSession(IReadOnlyList<AssetEntry> assets) : FakeSession(assets)
    {
        private readonly Dictionary<AssetEntry, TaskCompletionSource<AssetDetails>> _pending = [];

        public override Task<AssetDetails> GetDetailsAsync(AssetEntry asset, CancellationToken cancellationToken = default)
        {
            var completion = new TaskCompletionSource<AssetDetails>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[asset] = completion;
            return completion.Task;
        }

        public void Complete(AssetEntry asset) => _pending[asset].SetResult(Details(asset));
    }
}
