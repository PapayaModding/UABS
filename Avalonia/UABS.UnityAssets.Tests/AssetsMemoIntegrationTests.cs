using System.Threading.Channels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UABS.Page;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class AssetsMemoIntegrationTests
{
    [AvaloniaFact]
    public async Task VerifiedAssetsSaveUpdatesCatalogAndInactiveSearchWithoutReplacingRows()
    {
        using Fixture fixture = await Fixture.CreateAsync(useApplicationCoordinator: true);
        MainWindowViewModel app = fixture.App;
        await fixture.SearchAsync();
        SearchResultItemViewModel first = app.Search.Results.Single(row => row.PathId == 1);
        SearchResultItemViewModel second = app.Search.Results.Single(row => row.PathId == 2);
        app.Search.SelectedResult = second;
        await app.Search.MemoTransitionTask;
        var originalResults = app.Search.Results;
        await fixture.SelectAsync(1);

        app.AssetList.MemoInspector.MemoText = "verified through Assets";
        Assert.Equal("old one", first.Memo);
        await app.AssetList.MemoInspector.SaveNowCommand.ExecuteAsync();
        await app.AssetList.MemoTransitionTask;
        await app.Search.MemoTransitionTask;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(MemoDraftSaveState.Saved, app.AssetList.MemoInspector.SaveState);
        Assert.Equal("verified through Assets", first.Memo);
        Assert.Equal("old two", second.Memo);
        Assert.Same(originalResults, app.Search.Results);
        Assert.Same(second, app.Search.SelectedResult);
        Assert.Equal("Hero", app.Search.IncludeText);
        Assert.False(app.Search.CriteriaChanged);
        Assert.Equal("verified through Assets", app.Packages.Contents.Single(asset => asset.PathId == 1).Memo);
        PackageDocument reopened = await fixture.Packages.LoadAsync(fixture.PackagePath);
        Assert.Equal("verified through Assets", reopened.Bundles.Single().AssetInfos[0].Memo);
    }

    [AvaloniaTheory]
    [InlineData(null, PackageMemoStorageState.Missing)]
    [InlineData("", PackageMemoStorageState.Empty)]
    [InlineData("full memo", PackageMemoStorageState.Value)]
    public async Task IndexedAssetOfAnyAlreadyIndexedTypeIsEditableWithExactStorageState(
        string? memo, PackageMemoStorageState state)
    {
        using Fixture fixture = await Fixture.CreateAsync(firstMemo: memo);
        await fixture.SelectAsync(1);
        var inspector = fixture.App.AssetList.MemoInspector;

        Assert.True(inspector.IsEditable);
        Assert.Equal(state, inspector.StorageState);
        Assert.Equal(memo ?? string.Empty, inspector.MemoText);
        Assert.Equal("TextAsset", fixture.Assets[0].Type);
        Assert.Equal(fixture.PackagePath, inspector.Identity!.PackagePath);
        Assert.Equal(fixture.SourcePath, inspector.Identity.SourcePath);
        Assert.Equal(1, inspector.Identity.PathId);
    }

    [AvaloniaFact]
    public async Task MissingLooseAmbiguousAndUnloadedTargetsRemainUnavailable()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        var list = fixture.App.AssetList;
        list.SelectedAsset = fixture.Assets[0] with { SourcePath = Path.Combine(fixture.Root, "loose.assets") };
        await list.MemoTransitionTask;
        Assert.False(list.MemoInspector.IsEditable);
        Assert.Equal(PackageMemoResolutionStatus.NoMatch, list.MemoInspector.Status);

        PackageDocument loaded = await fixture.Packages.LoadAsync(fixture.PackagePath);
        PackageDocument duplicate = new(loaded.Indexes, Path.Combine(fixture.Root, "Duplicate"));
        list.UpdateMemoCatalog([
            new("Package", fixture.PackagePath, loaded),
            new("Duplicate", duplicate.PackagePath!, duplicate)
        ]);
        await fixture.SelectAsync(1);
        Assert.False(list.MemoInspector.IsEditable);
        Assert.Equal(PackageMemoResolutionStatus.Ambiguous, list.MemoInspector.Status);

        list.UpdateMemoCatalog([]);
        await list.MemoTransitionTask;
        Assert.False(list.MemoInspector.IsEditable);
        await fixture.App.Packages.RefreshCatalogAsync();
        await list.MemoTransitionTask;
        Assert.True(list.MemoInspector.IsEditable);
    }

    [AvaloniaFact]
    public async Task RemovedPackageOnDiskAndRemovedIndexAssetDisableEditing()
    {
        using Fixture fixture = await Fixture.CreateAsync(useApplicationCoordinator: true);
        PackageDocument loaded = await fixture.Packages.LoadAsync(fixture.PackagePath);
        Directory.Move(fixture.PackagePath, Path.Combine(fixture.Root, "MovedPackage"));
        await fixture.SelectAsync(1);
        Assert.False(fixture.App.AssetList.MemoInspector.IsEditable);

        loaded.Bundles.Single().AssetInfos.Clear();
        fixture.App.AssetList.UpdateMemoCatalog([new("Package", fixture.PackagePath, loaded)]);
        await fixture.App.AssetList.MemoTransitionTask;
        Assert.Equal(PackageMemoResolutionStatus.NoMatch, fixture.App.AssetList.MemoInspector.Status);
    }

    [AvaloniaFact]
    public async Task AssetsUsesTheSharedDebounceAndExplicitEmptyIsPersisted()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SelectAsync(1);
        var inspector = fixture.App.AssetList.MemoInspector;
        inspector.MemoText = string.Empty;
        ManualDelay.Pending pending = await fixture.Delay.NextAsync();
        Assert.Equal(TimeSpan.FromSeconds(1.5), pending.Duration);
        Assert.Equal(0, fixture.Persistence.Calls);
        TaskCompletionSource saved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.App.MemoDrafts.DraftChanged += (_, args) =>
        {
            if (args.Draft.LastPersistenceStatus == PackageMemoPersistenceStatus.Saved)
                saved.TrySetResult();
        };

        pending.Complete();
        await saved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, fixture.Persistence.Calls);
        Assert.Equal(MemoDraftSaveState.Saved, inspector.SaveState);
        PackageDocument reopened = await fixture.Packages.LoadAsync(fixture.PackagePath);
        Assert.Equal(string.Empty, reopened.Bundles.Single().AssetInfos[0].Memo);
    }

    [AvaloniaFact]
    public async Task SearchAssetsSearchSharesFailedDraftAndRetryWithoutChangingConfirmedRow()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SearchAsync();
        var search = fixture.App.Search;
        search.SelectedResult = search.Results.Single(row => row.PathId == 1);
        await search.MemoTransitionTask;
        search.MemoInspector.MemoText = "failed shared draft";
        fixture.Persistence.FailNext = true;
        await search.MemoInspector.SaveNowCommand.ExecuteAsync();

        await fixture.SelectAsync(1);
        var assets = fixture.App.AssetList.MemoInspector;
        Assert.Equal(search.MemoInspector.Identity, assets.Identity);
        Assert.Equal("failed shared draft", assets.MemoText);
        Assert.Equal(MemoDraftSaveState.Failed, assets.SaveState);
        Assert.Equal("old one", search.SelectedResult.Memo);
        await assets.RetryCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(MemoDraftSaveState.Saved, assets.SaveState);
        Assert.Equal("failed shared draft", search.SelectedResult.Memo);
        Assert.Equal("failed shared draft", search.MemoInspector.MemoText);
        Assert.Equal(2, fixture.Persistence.Calls);
    }

    [AvaloniaFact]
    public async Task SelectionChangeFlushesOriginalIdentityAndLateCompletionCannotRetargetNewAsset()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SelectAsync(1);
        fixture.App.AssetList.MemoInspector.MemoText = "save first only";
        fixture.Persistence.BlockNext = true;
        TaskCompletionSource<PackageMemoPersistenceRequest> started = fixture.Persistence.Started;
        TaskCompletionSource nextPresented = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.App.AssetList.MemoInspector.PropertyChanged += (_, _) =>
        {
            if (fixture.App.AssetList.MemoInspector.Identity?.PathId == 2)
                nextPresented.TrySetResult();
        };

        fixture.App.AssetList.SelectedAsset = fixture.Assets[1];
        PackageMemoPersistenceRequest request = await started.Task;
        await nextPresented.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, request.Identity.PathId);
        Assert.Equal(2, fixture.App.AssetList.MemoInspector.Identity!.PathId);
        Assert.Equal("old two", fixture.App.AssetList.MemoInspector.MemoText);
        fixture.Persistence.Release.TrySetResult();
        await fixture.App.AssetList.MemoTransitionTask;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, fixture.App.AssetList.MemoInspector.Identity!.PathId);
        Assert.Equal("old two", fixture.App.AssetList.MemoInspector.MemoText);
        var reopened = await fixture.Packages.LoadAsync(fixture.PackagePath);
        Assert.Equal("save first only", reopened.Bundles.Single().AssetInfos[0].Memo);
        Assert.Equal("old two", reopened.Bundles.Single().AssetInfos[1].Memo);
    }

    [AvaloniaFact]
    public async Task WorkspaceAndSourceChangesFlushAssetsWithoutLosingDraftIdentity()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SelectAsync(1);
        fixture.App.AssetList.MemoInspector.MemoText = "leaving Assets";
        fixture.App.SelectedWorkspaceIndex = 1;
        PackageMemoPersistenceRequest first = await fixture.Persistence.Started.Task;
        Assert.Equal(1, first.Identity.PathId);
        await fixture.App.MemoDrafts.SaveNowAsync(first.Identity);

        fixture.App.SelectedWorkspaceIndex = 0;
        await fixture.SelectAsync(2);
        fixture.App.AssetList.MemoInspector.MemoText = "leaving source";
        fixture.App.AssetList.ReplaceSession(new Session([]));
        await fixture.App.AssetList.MemoTransitionTask;
        Assert.Null(fixture.App.AssetList.SelectedAsset);
        Assert.False(fixture.App.AssetList.MemoInspector.IsEditable);
        var reopened = await fixture.Packages.LoadAsync(fixture.PackagePath);
        Assert.Equal("leaving Assets", reopened.Bundles.Single().AssetInfos[0].Memo);
        Assert.Equal("leaving source", reopened.Bundles.Single().AssetInfos[1].Memo);
    }

    [AvaloniaFact]
    public async Task EditorOpenBlocksBothInspectorsAndCleanCloseRestoresRetainedDraft()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SearchAsync();
        fixture.App.Search.SelectedResult = fixture.App.Search.Results.First();
        await fixture.App.Search.MemoTransitionTask;
        await fixture.SelectAsync(1);
        fixture.App.AssetList.MemoInspector.MemoText = "retained across editor";

        await fixture.App.Packages.EditSelectedPackageAsync();
        await fixture.App.AssetList.MemoTransitionTask;
        await fixture.App.Search.MemoTransitionTask;
        Assert.False(fixture.App.AssetList.MemoInspector.IsEditable);
        Assert.False(fixture.App.Search.MemoInspector.IsEditable);
        Assert.False(fixture.App.Packages.Editor.IsDirty);

        await fixture.App.Packages.Editor.CloseEditorAsync();
        await fixture.App.AssetList.MemoTransitionTask;
        await fixture.App.Search.MemoTransitionTask;
        Assert.True(fixture.App.AssetList.MemoInspector.IsEditable);
        Assert.True(fixture.App.Search.MemoInspector.IsEditable);
        Assert.Equal("retained across editor", fixture.App.AssetList.MemoInspector.MemoText);
        Assert.Equal("retained across editor", fixture.App.Search.MemoInspector.MemoText);
    }

    [AvaloniaFact]
    public async Task InspectorViewsReuseEditorAndPackageMemoIsReadOnlyWithMemoCopyStillAvailable()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SelectAsync(1);
        AssetInspectorView inspectorView = new() { DataContext = fixture.App.AssetList };
        Window window = new() { Width = 500, Height = 800, Content = inspectorView };
        window.Show();
        try
        {
            inspectorView.UpdateLayout();
            var memoView = inspectorView.FindControl<EditableMemoInspectorView>("AssetMemoInspector")!;
            Assert.Same(fixture.App.AssetList.MemoInspector, memoView.DataContext);
            TextBox memoEditor = memoView.FindControl<TextBox>("MemoEditor")!;
            Assert.True(memoEditor.IsEnabled);
            Assert.False(memoEditor.IsReadOnly);
            Assert.True(memoEditor.AcceptsReturn);

            await fixture.App.Packages.EditSelectedPackageAsync();
            fixture.App.Packages.Editor.SelectedItem = fixture.App.Packages.Editor.Items.First(item => !item.IsSource);
            PackageManagementView packagesView = new() { DataContext = fixture.App.Packages };
            window.Content = packagesView;
            packagesView.UpdateLayout();
            TextBox display = packagesView.FindControl<TextBox>("PackageEditorMemoDisplay")!;
            Assert.True(display.IsReadOnly);
            Assert.Equal("old one", display.Text);
            Assert.DoesNotContain(packagesView.GetVisualDescendants().OfType<Button>(),
                button => ReferenceEquals(button.Command, fixture.App.Packages.Editor.ClearMemoCommand));
            Assert.Contains(packagesView.GetVisualDescendants().OfType<Button>(),
                button => Equals(button.Content, "Apply Memo Copy"));
            Assert.False(fixture.App.Packages.Editor.IsDirty);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task QueuedOldSnapshotCannotReplaceNewerDraftForTheSameIdentity()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SelectAsync(1);
        PackageMemoIdentity identity = fixture.App.AssetList.MemoInspector.Identity!;
        ManualNotifications notifications = new(fixture.App.MemoDrafts);
        using EditableMemoInspectorViewModel inspector = new(notifications, new PackageEditorSessionGuard());
        await inspector.ShowAsync(new PackageMemoResolution(PackageMemoResolutionStatus.Available,
            identity, "Package", fixture.PackagePath, "old one", PackageMemoStorageState.Value, "Found"));
        MemoDraftSnapshot oldSnapshot = fixture.App.MemoDrafts.GetSnapshot(identity);

        // Queue an old background-thread notification without processing the UI queue yet.
        Task.Run(() => notifications.Publish(oldSnapshot)).GetAwaiter().GetResult();
        inspector.MemoText = "newer draft";
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("newer draft", inspector.MemoText);
        Assert.Equal(MemoDraftSaveState.Unsaved, inspector.SaveState);
    }

    [AvaloniaFact]
    public async Task SameNamesInDifferentPackagesAndCatalogRefreshPreserveDraftAndRowIdentity()
    {
        using Fixture fixture = await Fixture.CreateAsync(useApplicationCoordinator: true);
        PackageDocument other = await fixture.Packages.LoadAsync(fixture.PackagePath);
        other.Bundles.Single().Path = Path.Combine(fixture.Root, "other.assets");
        string otherPath = Path.Combine(fixture.Root, "Other");
        await fixture.Packages.SaveAsync(other, otherPath);
        await fixture.App.Packages.RefreshCatalogAsync();
        fixture.App.Search.SelectAllPackagesCommand.Execute(null);
        await fixture.SearchAsync();
        SearchResultItemViewModel original = fixture.App.Search.Results.Single(row =>
            row.PackagePath == fixture.PackagePath && row.PathId == 1);
        SearchResultItemViewModel otherRow = fixture.App.Search.Results.Single(row =>
            row.PackagePath == otherPath && row.PathId == 1);
        await fixture.SelectAsync(1);
        fixture.App.AssetList.MemoInspector.MemoText = "original only";

        await fixture.App.Packages.RefreshCatalogAsync();
        await fixture.App.AssetList.MemoTransitionTask;
        Assert.Equal("original only", fixture.App.AssetList.MemoInspector.MemoText);
        Assert.Equal(MemoDraftSaveState.Unsaved, fixture.App.AssetList.MemoInspector.SaveState);
        await fixture.App.AssetList.MemoInspector.SaveNowCommand.ExecuteAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("original only", original.Memo);
        Assert.Equal("old one", otherRow.Memo);
        Assert.Equal(4, fixture.App.Search.Results.Count);
        PackageDocument otherReopened = await fixture.Packages.LoadAsync(otherPath);
        Assert.Equal("old one", otherReopened.Bundles.Single().AssetInfos[0].Memo);
    }

    [AvaloniaFact]
    public async Task ReplacingCleanEditorSessionRechecksEligibilityEvenWhenIsEditingStaysTrue()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SelectAsync(1);
        await fixture.App.Packages.EditSelectedPackageAsync();
        await fixture.App.AssetList.MemoTransitionTask;
        Assert.False(fixture.App.AssetList.MemoInspector.IsEditable);

        PackageDocument other = new([new PackageIndex(string.Empty)], Path.Combine(fixture.Root, "Other"));
        await fixture.App.Packages.Editor.StartEditAsync("Other", other);
        await fixture.App.AssetList.MemoTransitionTask;

        Assert.True(fixture.App.Packages.Editor.IsEditing);
        Assert.True(fixture.App.AssetList.MemoInspector.IsEditable);
    }

    [AvaloniaFact]
    public async Task CatalogPresentationStartedBeforeSaveCannotRestoreItsOldMemoAfterSave()
    {
        using Fixture fixture = await Fixture.CreateAsync();
        await fixture.SelectAsync(1);
        PackageMemoIdentity identity = fixture.App.AssetList.MemoInspector.Identity!;
        fixture.App.AssetList.MemoInspector.MemoText = "newly confirmed";
        fixture.Persistence.BlockCaptureNext = true;
        fixture.App.AssetList.RefreshMemoEligibility();
        await fixture.Persistence.CaptureStarted.Task;

        await fixture.App.MemoDrafts.SaveNowAsync(identity);
        fixture.Persistence.ReleaseCapture.TrySetResult();
        await fixture.App.AssetList.MemoTransitionTask;

        Assert.Equal("newly confirmed", fixture.App.MemoDrafts.GetSnapshot(identity).ConfirmedMemo);
        Assert.Equal("newly confirmed", fixture.App.AssetList.MemoInspector.MemoText);
        Assert.Equal(MemoDraftSaveState.Saved, fixture.App.AssetList.MemoInspector.SaveState);
    }

    private sealed class ManualNotifications(IMemoDraftCoordinator inner) : IMemoDraftCoordinator
    {
        public event EventHandler<MemoDraftChangedEventArgs>? DraftChanged;
        public void Publish(MemoDraftSnapshot snapshot) => DraftChanged?.Invoke(this, new(snapshot));
        public MemoDraftSnapshot Track(PackageMemoIdentity identity, string? persistedMemo, string? packageRevision) => inner.Track(identity, persistedMemo, packageRevision);
        public Task<MemoDraftSnapshot> TrackAsync(PackageMemoIdentity identity, string? persistedMemo, CancellationToken cancellationToken = default) => inner.TrackAsync(identity, persistedMemo, cancellationToken);
        public MemoDraftSnapshot Edit(PackageMemoIdentity identity, string? memo) => inner.Edit(identity, memo);
        public MemoDraftSnapshot GetSnapshot(PackageMemoIdentity? identity) => inner.GetSnapshot(identity);
        public Task<MemoDraftSnapshot> SaveNowAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) => inner.SaveNowAsync(identity, cancellationToken);
        public Task<MemoDraftSnapshot> RetryAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) => inner.RetryAsync(identity, cancellationToken);
        public Task<MemoDraftSnapshot> FlushForNavigationAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) => inner.FlushForNavigationAsync(identity, cancellationToken);
        public Task<MemoDraftDrainResult> DrainForShutdownAsync(CancellationToken cancellationToken = default) => inner.DrainForShutdownAsync(cancellationToken);
    }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), $"uabs-stage6-{Guid.NewGuid():N}");
        public string PackagePath => Path.Combine(Root, "Package");
        public string SourcePath => Path.Combine(Root, "source.assets");
        public PackageService Packages { get; } = new(TestPaths.ClassData);
        public MainWindowViewModel App { get; private set; } = null!;
        public AssetEntry[] Assets { get; private set; } = [];
        public ManualDelay Delay { get; } = new();
        public ControlledPersistence Persistence { get; private set; } = null!;

        public static async Task<Fixture> CreateAsync(bool useApplicationCoordinator = false, string? firstMemo = "old one")
        {
            Fixture fixture = new();
            try
            {
                Directory.CreateDirectory(fixture.Root);
                PackageDocument document = new([new PackageIndex(string.Empty, [new PackageBundleReference
                {
                    Name = "source.assets", Path = fixture.SourcePath, CabCode = "CAB-SOURCE",
                    AssetInfos = [
                        new() { Name = "Hero", PathId = 1, Memo = firstMemo },
                        new() { Name = "Hero", PathId = 2, Memo = "old two" }
                    ]
                }])]);
                await fixture.Packages.SaveAsync(document, fixture.PackagePath);
                fixture.Assets = [
                    new("Hero", "TextAsset", 49, 1, 10, "main.assets", fixture.SourcePath),
                    new("Hero", "AudioClip", 83, 2, 20, "main.assets", fixture.SourcePath)
                ];
                IMemoDraftCoordinator? drafts = null;
                if (!useApplicationCoordinator)
                {
                    fixture.Persistence = new ControlledPersistence(new PackageMemoPersistenceService(
                        fixture.Packages, new PackageWriteCoordinator()));
                    drafts = new MemoDraftCoordinator(fixture.Persistence, fixture.Delay);
                }
                fixture.App = new MainWindowViewModel(new NullPicker(), new SessionFactory(fixture.Assets),
                    packageService: fixture.Packages, memoDraftCoordinator: drafts);
                await fixture.App.Packages.LoadCatalogAsync(fixture.Root);
                await fixture.App.LoadFileAsync(fixture.SourcePath);
                return fixture;
            }
            catch
            {
                fixture.Dispose();
                throw;
            }
        }

        public async Task SelectAsync(long pathId)
        {
            App.AssetList.SelectedAsset = Assets.Single(asset => asset.PathId == pathId);
            await App.AssetList.MemoTransitionTask;
        }

        public async Task SearchAsync()
        {
            App.Search.IncludeText = "Hero";
            App.Search.IncludeSourceResults = false;
            App.Search.IncludePackageResults = false;
            await App.Search.SearchAsync();
        }

        public void Dispose()
        {
            App?.Dispose();
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }

    private sealed class ControlledPersistence(IPackageMemoPersistenceService inner) : IPackageMemoPersistenceService
    {
        public int Calls { get; private set; }
        public bool FailNext { get; set; }
        public bool BlockNext { get; set; }
        public bool BlockCaptureNext { get; set; }
        public TaskCompletionSource<PackageMemoPersistenceRequest> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CaptureStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseCapture { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<string?> CaptureRevisionAsync(string packagePath, CancellationToken cancellationToken = default)
        {
            if (BlockCaptureNext)
            {
                BlockCaptureNext = false;
                CaptureStarted.TrySetResult();
                await ReleaseCapture.Task.WaitAsync(cancellationToken);
            }
            return await inner.CaptureRevisionAsync(packagePath, cancellationToken);
        }

        public async Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Started.TrySetResult(request);
            if (FailNext)
            {
                FailNext = false;
                return new(PackageMemoPersistenceStatus.WriteFailure, request, "Simulated write failure.");
            }
            if (BlockNext)
            {
                BlockNext = false;
                await Release.Task.WaitAsync(cancellationToken);
            }
            return await inner.PersistAsync(request, cancellationToken);
        }
    }

    private sealed class ManualDelay : IMemoDraftDelay
    {
        private readonly Channel<Pending> _pending = Channel.CreateUnbounded<Pending>();
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Pending pending = new(delay);
            _pending.Writer.TryWrite(pending);
            return pending.Completion.Task.WaitAsync(cancellationToken);
        }
        public async Task<Pending> NextAsync() => await _pending.Reader.ReadAsync();
        public sealed class Pending(TimeSpan duration)
        {
            public TimeSpan Duration { get; } = duration;
            public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public void Complete() => Completion.TrySetResult();
        }
    }

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }

    private sealed class SessionFactory(IReadOnlyList<AssetEntry> assets) : IAssetSessionFactory
    {
        public Task<IAssetSession> OpenAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult<IAssetSession>(new Session(assets));
    }

    private sealed class Session(IReadOnlyList<AssetEntry> assets) : IAssetSession
    {
        public AssetSessionMetadata Metadata { get; } = new("fixture.assets", "fixture", UnityFileKind.SerializedFile, "2021.3", 1);
        public IReadOnlyList<AssetEntry> Assets { get; } = assets;
        public IReadOnlyList<string> Warnings { get; } = [];
        public Task<AssetDetails> GetDetailsAsync(AssetEntry asset, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AssetDetails(asset.Name, asset.Type, asset.ClassId, asset.PathId, asset.ByteSize,
                asset.LogicalFile, asset.SourcePath, Metadata.Kind, Metadata.UnityVersion, 1));
        public void Dispose() { }
    }
}
