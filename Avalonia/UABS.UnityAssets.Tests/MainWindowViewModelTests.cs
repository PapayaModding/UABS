using UABS.Services;
using UABS.UnityAssets;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
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
    public async Task LoadFileAsync_SupersededResultCannotReplaceNewerSession()
    {
        FakeSession stale = FakeSession.Create("stale.assets", [Entry(1)]);
        FakeSession current = FakeSession.Create("current.assets", [Entry(2)]);
        ControlledFactory factory = new();
        using MainWindowViewModel viewModel = new(new NullPicker(), factory);

        Task first = viewModel.LoadFileAsync(stale.Metadata.SourcePath);
        await factory.FirstStarted.Task;
        Task second = viewModel.LoadFileAsync(current.Metadata.SourcePath);
        await factory.SecondStarted.Task;
        factory.SecondResult.SetResult(current);
        await second;
        factory.FirstResult.SetResult(stale);
        await first;

        Assert.True(stale.IsDisposed);
        Assert.False(current.IsDisposed);
        Assert.Equal(current.Metadata.SourcePath, viewModel.ActiveFilePath);
        Assert.Equal(2, Assert.Single(viewModel.Assets).PathId);
        Assert.False(viewModel.IsLoading);
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

    [Fact]
    public async Task WorkspaceNavigation_PreservesDirtyPackageEditorAndSelection()
    {
        using MainWindowViewModel viewModel = new(new NullPicker(), new QueueFactory());
        PackageDocument package = new([new PackageIndex(string.Empty, [new PackageBundleReference
        {
            Name = "source.ab",
            Path = @"C:\game\source.ab",
            CabCode = "CAB-SOURCE",
            AssetInfos = [new PackageAssetReference { Name = "Hero", PathId = 7 }]
        }])], @"C:\packages\one");
        await viewModel.Packages.Editor.StartEditAsync("one", package);
        PackageEditorItemViewModel selected = viewModel.Packages.Editor.Items.Single(item =>
            item.Kind == PackageEditorItemKind.Asset);
        viewModel.Packages.Editor.SelectedItem = selected;
        viewModel.Packages.Editor.PackageName = "renamed";
        viewModel.Packages.Editor.MemoText = "unsaved workspace memo";
        AssetEntry inspectedAsset = new("Hero", "Texture2D", 28, 7, 10, "source.assets", @"C:\game\source.ab");
        FakeSession inspectedSession = FakeSession.Create("source.ab", [inspectedAsset]);
        viewModel.AssetList.ReplaceSession(inspectedSession);
        viewModel.AssetList.UpdateMemoCatalog([
            new PackageMemoCatalogEntry("one", package.PackagePath!, package)
        ]);
        viewModel.AssetList.SelectedAsset = inspectedAsset;
        Assert.Equal(PackageMemoResolutionStatus.Available, viewModel.AssetList.MemoInspector.Status);
        Assert.Equal(PackageMemoStorageState.Missing, viewModel.AssetList.MemoInspector.StorageState);
        Assert.True(viewModel.Packages.Editor.IsDirty);
        Assert.Equal("unsaved workspace memo", viewModel.Packages.Editor.MemoText);
        viewModel.Packages.Layout.SidebarWidth = 402;
        viewModel.Packages.Layout.ToggleSidebarCommand.Execute(null);
        PackageManagementViewModel packages = viewModel.Packages;
        PackageEditorViewModel editor = packages.Editor;

        for (int index = 0; index < 20; index++)
        {
            viewModel.ShowPackagesCommand.Execute(null);
            viewModel.ShowAssetsCommand.Execute(null);
        }
        viewModel.ShowPackagesCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedWorkspaceIndex);
        Assert.Same(packages, viewModel.Packages);
        Assert.Same(editor, viewModel.Packages.Editor);
        Assert.True(editor.IsEditing);
        Assert.True(editor.IsDirty);
        Assert.Equal("renamed", editor.PackageName);
        Assert.Equal("unsaved workspace memo", editor.MemoText);
        Assert.Equal("unsaved workspace memo", editor.SelectedItem!.Memo);
        Assert.Equal(2, editor.Items.Count);
        Assert.Same(selected, editor.SelectedItem);
        Assert.False(packages.Layout.IsSidebarVisible);
        Assert.Equal(402, packages.Layout.SidebarWidth);
        Assert.True(viewModel.AssetsLayout.IsNavigatorVisible);
        Assert.True(viewModel.Search.Layout.AreControlsVisible);
    }

    [Fact]
    public async Task WorkspaceNavigation_DuringImportPreservesEditorAndPublishesCompletedResult()
    {
        BlockingFolderPackageService packages = new();
        using MainWindowViewModel viewModel = new(
            new FolderPicker(@"C:\game"),
            new QueueFactory(),
            packageService: packages);
        await viewModel.Packages.LoadCatalogAsync("catalog");
        await viewModel.Packages.EditSelectedPackageAsync();

        Task import = viewModel.Packages.Editor.AddFolderAsync();
        await packages.ImportStarted.Task;
        viewModel.ShowAssetsCommand.Execute(null);
        viewModel.Packages.SelectedPackage = null; // Simulates a transient grid clear on detach.
        viewModel.ShowPackagesCommand.Execute(null);
        packages.ReleaseImport.SetResult();
        await import;

        Assert.Equal(2, viewModel.SelectedWorkspaceIndex);
        Assert.Equal("one", viewModel.Packages.SelectedPackage!.Name);
        Assert.True(viewModel.Packages.Editor.IsEditing);
        Assert.True(viewModel.Packages.Editor.IsDirty);
        Assert.False(viewModel.Packages.Editor.IsImporting);
        Assert.Equal(2, viewModel.Packages.Editor.Items.Count);
        Assert.False(packages.ImportToken.IsCancellationRequested);
    }

    [Fact]
    public async Task WorkspaceNavigation_PreservesAssetsPanelLayoutSelectionAndSession()
    {
        AssetEntry selected = Entry(17);
        FakeSession session = FakeSession.Create("current.assets", [selected]);
        using MainWindowViewModel viewModel = new(new NullPicker(), new QueueFactory(session));
        await viewModel.LoadFileAsync(session.Metadata.SourcePath);
        viewModel.AssetList.SelectedAsset = selected;
        await viewModel.AssetList.DetailsLoadingTask;
        viewModel.AssetsLayout.NavigatorWidth = 405;
        viewModel.AssetsLayout.InspectorWidth = 375;
        viewModel.AssetsLayout.ToggleNavigatorCommand.Execute(null);
        viewModel.AssetsLayout.ToggleInspectorCommand.Execute(null);

        viewModel.ShowPackagesCommand.Execute(null);
        viewModel.ShowAssetsCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedWorkspaceIndex);
        Assert.False(viewModel.AssetsLayout.IsNavigatorVisible);
        Assert.False(viewModel.AssetsLayout.IsInspectorVisible);
        Assert.Equal(405, viewModel.AssetsLayout.NavigatorWidth);
        Assert.Equal(375, viewModel.AssetsLayout.InspectorWidth);
        Assert.Same(selected, viewModel.AssetList.SelectedAsset);
        Assert.Equal(17, viewModel.AssetList.SelectedDetails?.PathId);
        Assert.False(session.IsDisposed);

        viewModel.AssetsLayout.ToggleNavigatorCommand.Execute(null);
        viewModel.AssetsLayout.ToggleInspectorCommand.Execute(null);
        Assert.Equal(405, viewModel.AssetsLayout.NavigatorColumnWidth.Value);
        Assert.Equal(375, viewModel.AssetsLayout.InspectorColumnWidth.Value);
    }

    [Fact]
    public void WorkspaceNavigation_PreservesIndependentSearchState()
    {
        using MainWindowViewModel viewModel = new(new NullPicker(), new QueueFactory());
        SearchWorkspaceViewModel search = viewModel.Search;
        search.IncludeText = "hero";
        search.ExcludeText = "unused";
        search.Layout.ControlsWidth = 388;
        search.Layout.InspectorWidth = 366;
        search.Layout.ToggleControlsCommand.Execute(null);

        viewModel.ShowSearchCommand.Execute(null);
        Assert.Equal(1, viewModel.SelectedWorkspaceIndex);
        viewModel.ShowPackagesCommand.Execute(null);
        viewModel.ShowAssetsCommand.Execute(null);
        viewModel.ShowSearchCommand.Execute(null);

        Assert.Equal(1, viewModel.SelectedWorkspaceIndex);
        Assert.Same(search, viewModel.Search);
        Assert.Equal("hero", viewModel.Search.IncludeText);
        Assert.Equal("unused", viewModel.Search.ExcludeText);
        Assert.False(viewModel.Search.Layout.AreControlsVisible);
        Assert.True(viewModel.Search.Layout.IsInspectorVisible);
        Assert.Equal(388, viewModel.Search.Layout.ControlsWidth);
        Assert.Equal(366, viewModel.Search.Layout.InspectorWidth);
        Assert.True(viewModel.AssetsLayout.IsNavigatorVisible);
        Assert.True(viewModel.AssetsLayout.IsInspectorVisible);
    }

    [Fact]
    public async Task LeavingSearchWorkspaceFlushesActiveMemoDraftWithoutChangingSearchState()
    {
        PackageDocument package = new([new PackageIndex(string.Empty, [new PackageBundleReference
        {
            Name = "source.ab",
            Path = @"C:\game\source.ab",
            CabCode = "CAB-SOURCE",
            AssetInfos = [new PackageAssetReference { Name = "Hero", PathId = 7, Memo = "old" }]
        }])], @"C:\packages\one");
        WorkspaceDraftCoordinator drafts = new();
        using MainWindowViewModel viewModel = new(
            new NullPicker(),
            new QueueFactory(),
            packageService: new CatalogPackageService(package),
            memoDraftCoordinator: drafts);
        await viewModel.Packages.LoadCatalogAsync("catalog");
        viewModel.Search.IncludeText = "Hero";
        viewModel.Search.IncludePackageResults = false;
        viewModel.Search.IncludeSourceResults = false;
        await viewModel.Search.SearchAsync();
        viewModel.Search.SelectedResult = Assert.Single(viewModel.Search.Results);
        await viewModel.Search.MemoTransitionTask;
        viewModel.Search.MemoInspector.MemoText = "pending";
        SearchResultItemViewModel selected = viewModel.Search.SelectedResult;

        viewModel.ShowSearchCommand.Execute(null);
        viewModel.ShowAssetsCommand.Execute(null);
        PackageMemoIdentity flushed = await drafts.FlushObserved.Task;

        Assert.Equal(7, flushed.PathId);
        Assert.Equal("pending", drafts.GetSnapshot(flushed).ConfirmedMemo);
        Assert.Same(selected, viewModel.Search.SelectedResult);
        Assert.Equal("Hero", viewModel.Search.IncludeText);
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

    private sealed class FolderPicker(string folder) : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(folder);
    }

    private sealed class CatalogPackageService(PackageDocument package) : IPackageService
    {
        public Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(string packageParentPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PackageCatalogEntry>>([
                new PackageCatalogEntry("one", package.PackagePath!, package, null)
            ]);
        public Task<PackageDocument> LoadAsync(string packagePath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> CreateAsync(string sourceRoot, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageBundleReference> CreateReferenceAsync(string sourcePath, IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, PackageFolderSearchMode searchMode, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> BuildAndSaveAsync(string sourceRoot, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PackageSummary>> ListAsync(string packageParentPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class WorkspaceDraftCoordinator : IMemoDraftCoordinator
    {
        private MemoDraftSnapshot _snapshot = MemoDraftSnapshot.Unavailable;
        public event EventHandler<MemoDraftChangedEventArgs>? DraftChanged;
        public TaskCompletionSource<PackageMemoIdentity> FlushObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public MemoDraftSnapshot Track(PackageMemoIdentity identity, string? persistedMemo, string? packageRevision) =>
            _snapshot.Identity is null
                ? _snapshot = new MemoDraftSnapshot(identity, persistedMemo, persistedMemo, packageRevision, 0, MemoDraftSaveState.Saved)
                : _snapshot;
        public Task<MemoDraftSnapshot> TrackAsync(PackageMemoIdentity identity, string? persistedMemo, CancellationToken cancellationToken = default) =>
            Task.FromResult(Track(identity, persistedMemo, "revision"));
        public MemoDraftSnapshot Edit(PackageMemoIdentity identity, string? memo)
        {
            _snapshot = _snapshot with { DraftMemo = memo, DraftRevision = _snapshot.DraftRevision + 1, State = MemoDraftSaveState.Unsaved };
            DraftChanged?.Invoke(this, new MemoDraftChangedEventArgs(_snapshot));
            return _snapshot;
        }
        public MemoDraftSnapshot GetSnapshot(PackageMemoIdentity? identity) => _snapshot;
        public Task<MemoDraftSnapshot> SaveNowAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) => FlushForNavigationAsync(identity, cancellationToken);
        public Task<MemoDraftSnapshot> RetryAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) => FlushForNavigationAsync(identity, cancellationToken);
        public Task<MemoDraftSnapshot> FlushForNavigationAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default)
        {
            _snapshot = _snapshot with { ConfirmedMemo = _snapshot.DraftMemo, State = MemoDraftSaveState.Saved };
            FlushObserved.TrySetResult(identity);
            DraftChanged?.Invoke(this, new MemoDraftChangedEventArgs(_snapshot));
            return Task.FromResult(_snapshot);
        }
        public Task<MemoDraftDrainResult> DrainForShutdownAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new MemoDraftDrainResult([_snapshot]));
    }

    private sealed class BlockingFolderPackageService : IPackageService
    {
        public TaskCompletionSource ImportStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseImport { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ImportToken { get; private set; }

        public Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(
            string packageParentPath,
            CancellationToken cancellationToken = default)
        {
            PackageDocument package = new([new PackageIndex(string.Empty)], @"C:\packages\one");
            return Task.FromResult<IReadOnlyList<PackageCatalogEntry>>([
                new PackageCatalogEntry("one", package.PackagePath!, package, null)
            ]);
        }

        public async Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(
            string folderPath,
            PackageFolderSearchMode searchMode,
            IProgress<PackageProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ImportToken = cancellationToken;
            ImportStarted.SetResult();
            await ReleaseImport.Task.WaitAsync(cancellationToken);
            PackageBundleReference source = new()
            {
                Name = "source.ab",
                Path = @"C:\game\source.ab",
                CabCode = "CAB-SOURCE",
                AssetInfos = [new PackageAssetReference { Name = "Hero", PathId = 7 }]
            };
            return new PackageFolderImportResult(
                1, 1, [source], 0, [], 0, searchMode,
                MappedReferences: [new PackageFolderImportReference(string.Empty, source)]);
        }

        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(
            string folderPath,
            IProgress<PackageProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            CreateReferencesFromFolderAsync(
                folderPath,
                PackageFolderSearchMode.CurrentFolderOnly,
                progress,
                cancellationToken);

        public Task<PackageDocument> LoadAsync(string packagePath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> CreateAsync(string sourceRoot, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageBundleReference> CreateReferenceAsync(string sourcePath, IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> BuildAndSaveAsync(string sourceRoot, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PackageSummary>> ListAsync(string packageParentPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

    private sealed class ControlledFactory : IAssetSessionFactory
    {
        private int _calls;
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<IAssetSession> FirstResult { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<IAssetSession> SecondResult { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IAssetSession> OpenAsync(string path, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                FirstStarted.SetResult();
                return FirstResult.Task;
            }

            SecondStarted.SetResult();
            return SecondResult.Task;
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
