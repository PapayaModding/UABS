using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using UABS.Models;
using UABS.Page;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class V2BrowseIntegrationTests
{
    private sealed class Picker(string? path = null) : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult(path);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
    private sealed class NoUnityFactory : IAssetSessionFactory
    {
        public int Calls;
        public Task<IAssetSession> OpenAsync(string path, CancellationToken token = default) { Calls++; throw new InvalidOperationException("V2 must not open original Unity content."); }
    }
    private static void Edit(string path, string sql)
    {
        using var c = V2SearchTests.Connect(path); c.CreateFunction<string?, int>("uabs_has_text_v1", s => string.IsNullOrWhiteSpace(s) ? 0 : 1);
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery();
    }
    private static byte[] Hash(string path) => System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path));

    [Fact]
    public async Task Registration_IsReadOnly_DeduplicatesPathAndRefusesUuidAlias()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        byte[] original = Hash(path); using var catalog = new V2BrowseCatalog();
        var first = await catalog.RegisterAsync(path); var second = await catalog.RegisterAsync(Path.Combine(corpus.DirectoryPath, ".", Path.GetFileName(path)));
        Assert.Same(first, second); Assert.Equal(original, Hash(path));
        string copy = path + ".uabs.sqlite"; File.Copy(path, copy);
        await Assert.ThrowsAsync<InvalidDataException>(() => catalog.RegisterAsync(copy));
        await catalog.UnloadAsync(first);
        var moved = await catalog.RegisterAsync(copy); Assert.NotEqual(first.Epoch, moved.Epoch); Assert.Equal(first.PackageId, moved.PackageId);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("version")]
    [InlineData("schema")]
    [InlineData("corrupt")]
    [InlineData("wal")]
    public async Task InvalidAdmission_DoesNotInitializeRepairOrModify(string failure)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        if (failure == "corrupt") File.WriteAllText(path, "Not a SQLite database");
        else Edit(path, failure == "application" ? "PRAGMA application_id=0" : failure == "version" ? "PRAGMA user_version=1" : failure == "wal" ? "PRAGMA journal_mode=WAL" : "DROP TRIGGER asset_memo_changed");
        byte[] before = Hash(path); using var catalog = new V2BrowseCatalog();
        await Assert.ThrowsAsync<InvalidDataException>(() => catalog.RegisterAsync(path)); Assert.Equal(before, Hash(path));
    }

    [Fact]
    public async Task MissingAndNonPackageFiles_AreRefusedWithoutCreation()
    {
        using var corpus = new V2SearchTests.Corpus(); using var catalog = new V2BrowseCatalog();
        string missing = Path.Combine(corpus.DirectoryPath, "missing.uabs.sqlite");
        await Assert.ThrowsAnyAsync<IOException>(() => catalog.RegisterAsync(missing)); Assert.False(File.Exists(missing));
        await Assert.ThrowsAsync<InvalidDataException>(() => catalog.RegisterAsync(missing + ".db"));
    }

    [Fact]
    public async Task AllTypesNullNamesMemoAndOriginalOrdinals_AreRetained()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(36, partial: true, sourceCount: 2);
        string path = corpus.Packages[0].Path;
        Edit(path, "UPDATE asset_type SET type_name=NULL; UPDATE asset SET memo='full memo 中文' WHERE table_ordinal=0; INSERT INTO indexing_issue(source_id,phase,code,severity,message) SELECT source_id,'validation','fixture','warning','Synthetic partial evidence' FROM source;");
        using var catalog = new V2BrowseCatalog(); var reg = await catalog.RegisterAsync(path);
        Assert.Equal("partial", reg.Status); Assert.Contains("partial", reg.Summary);
        var page = await reg.AssetsAsync(0); Assert.Equal(72, page.Rows.Count);
        var first = page.Rows[0]; Assert.Null(page.Rows[9].Name); Assert.Equal("<NULL>", page.Rows[9].NameLabel);
        Assert.Equal("", page.Rows[10].Name); Assert.Contains("empty", page.Rows[10].NameLabel);
        Assert.Equal(1234567, first.ClassId); Assert.Contains("unresolved", first.TypeLabel);
        Assert.Equal("full memo 中文", first.Memo); Assert.Equal(-77, first.Identity.PathId);
        Assert.Equal(3, first.Identity.DirectoryOrdinal); Assert.Equal(0, first.Identity.TableOrdinal);
        Assert.NotEqual(first.Identity.SourceId, page.Rows[36].Identity.SourceId);
        Assert.Equal(first.Identity.PathId, page.Rows[36].Identity.PathId);
        string evidence = await reg.AssetEvidenceAsync(first); Assert.Contains("CAB-shared", evidence); Assert.Contains("original_path", evidence);
        var files = await reg.MetadataAsync(V2MetadataKind.LogicalFiles, 0); Assert.Equal(2, files.Rows.Count);
        Assert.Contains("CAB-shared", files.Rows[0].Details); Assert.Contains("CAB-shared", files.Rows[1].Details);
        var dependencies = await reg.MetadataAsync(V2MetadataKind.Dependencies, 0);
        Assert.Contains("raw_path: archive:/dep/CAB-raw", dependencies.Rows[0].Details);
        Assert.Contains("raw_virtual_path: virtual:/资源", dependencies.Rows[0].Details);
        Assert.Contains("declaration_ordinal: 0", dependencies.Rows[0].Details);
        Assert.NotEmpty((await reg.MetadataAsync(V2MetadataKind.Issues, 0)).Rows);
    }

    [Fact]
    public async Task KeysetWindowExhaustion_HasNoOmittedOrDuplicateOccurrences()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(1253);
        using var catalog = new V2BrowseCatalog(); var reg = await catalog.RegisterAsync(corpus.Packages[0].Path);
        var seen = new HashSet<V2AssetIdentity>(); long after = 0; int pages = 0;
        while (true)
        {
            var page = await reg.AssetsAsync(after); Assert.InRange(page.Rows.Count, 1, 100);
            Assert.All(page.Rows, row => { Assert.True(row.Cursor > after); Assert.True(seen.Add(row.Identity)); });
            pages++; Assert.InRange(pages, 1, 13); after = page.After; if (!page.HasMore) break;
        }
        Assert.Equal(1253, seen.Count); Assert.Equal(13, pages);
    }

    [Theory]
    [InlineData("memo")]
    [InlineData("generation")]
    [InlineData("replacement")]
    public async Task ExternalChanges_InvalidateRatherThanPublishCachedRows(string change)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(200);
        using var catalog = new V2BrowseCatalog(); string path = corpus.Packages[0].Path; var reg = await catalog.RegisterAsync(path);
        await reg.AssetsAsync(0);
        if (change == "replacement")
        {
            using var other = new V2SearchTests.Corpus(); await other.Add(6);
            File.Copy(other.Packages[0].Path, path, true); // in-place overwrite: no delete/rename workaround
        }
        else Edit(path, change == "memo" ? "UPDATE asset SET memo='external' WHERE table_ordinal=0" : "UPDATE package SET generation_uuid=randomblob(16)");
        await Assert.ThrowsAsync<InvalidDataException>(() => reg.AssetsAsync(100)); Assert.Contains("stale", reg.Status);
        await Assert.ThrowsAsync<InvalidDataException>(() => catalog.RegisterAsync(path));
        await catalog.UnloadAsync(reg); var fresh = await catalog.RegisterAsync(path); Assert.NotEqual(reg.Epoch, fresh.Epoch);
    }

    [Fact]
    public async Task ReadOnlyFileAndRawProfileFallback_RemainBrowsable()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        Edit(path, "UPDATE package SET derived_text_profile='unsupported'"); byte[] before = Hash(path);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try { using var catalog = new V2BrowseCatalog(); var reg = await catalog.RegisterAsync(path); Assert.Contains("raw metadata", reg.Summary); Assert.NotEmpty((await reg.AssetsAsync(0)).Rows); }
        finally { File.SetAttributes(path, FileAttributes.Normal); }
        Assert.Equal(before, Hash(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InFlightCancellationOrUnload_DrainsAndPublishesNoPage(bool unload)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var catalog = new V2BrowseCatalog(b => { if (b == V2BrowseBoundary.BeforeRead) { entered.TrySetResult(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); } });
        var reg = await catalog.RegisterAsync(corpus.Packages[0].Path); using var cancel = new CancellationTokenSource();
        var read = reg.AssetsAsync(0, cancel.Token); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Task? closing = null;
        try { if (unload) closing = catalog.UnloadAsync(reg); else cancel.Cancel(); }
        finally { release.Set(); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read); if (closing is not null) await closing;
    }

    [Fact]
    public async Task ViewModelNavigationAndPaging_PreserveExactSelectionAndWindowBounds()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250); await corpus.Add(6);
        using var vm = new V2BrowseViewModel(new Picker());
        await vm.OpenAsync(corpus.Packages[0].Path); Assert.Equal(100, vm.Assets.Count);
        vm.SelectedAsset = vm.Assets[0]; await vm.InspectorTask; var key = vm.SelectedAsset!.Identity;
        vm.SelectedAsset = null; Assert.Equal(key, vm.SelectedAsset!.Identity);
        await vm.FirstCommand.ExecuteAsync(); await vm.InspectorTask; Assert.Equal(key, vm.SelectedAsset!.Identity);
        await vm.NextCommand.ExecuteAsync(); Assert.Equal(100, vm.Assets.Count); Assert.NotEqual(key, vm.Assets[0].Identity);
        await vm.PreviousCommand.ExecuteAsync(); Assert.Equal(key, vm.Assets[0].Identity);
        await vm.OpenAsync(corpus.Packages[1].Path); Assert.Equal(6, vm.Assets.Count); Assert.All(vm.Assets, row => Assert.Equal(vm.SelectedPackage!.PackageId, row.Identity.PackageId));
        await vm.RemoveAsync(); Assert.Empty(vm.Assets); Assert.Empty(vm.Metadata); Assert.Null(vm.SelectedAsset);
    }

    [Fact]
    public async Task NavigationSupersedesBlockedRead_WithoutLateRows()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250); await corpus.Add(6);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); using var release = new ManualResetEventSlim(); int block = 0;
        var catalog = new V2BrowseCatalog(b => { if (b == V2BrowseBoundary.BeforeRead && Interlocked.Exchange(ref block, 0) == 1) { entered.TrySetResult(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); } });
        using var vm = new V2BrowseViewModel(new Picker(), catalog);
        var first = await vm.OpenAsync(corpus.Packages[0].Path); var second = await vm.OpenAsync(corpus.Packages[1].Path, select: false);
        block = 1; var old = vm.LoadAsync(100); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try { vm.SelectedPackage = second; } finally { release.Set(); }
        await old; await vm.SelectionTask;
        Assert.Equal(6, vm.Assets.Count); Assert.All(vm.Assets, row => Assert.Equal(second!.PackageId, row.Identity.PackageId));
    }

    [Fact]
    public async Task ProductionCatalogNavigatorAndInspector_NeverRouteV2ToV1Writers()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(125);
        var factory = new NoUnityFactory(); using var app = new MainWindowViewModel(new Picker(corpus.Packages[0].Path), factory);
        await app.LoadFileAsync(corpus.Packages[0].Path); Assert.Equal(0, factory.Calls); Assert.True(app.IsV2Assets);
        Assert.False(app.AddSelectedAssetToPackageCommand.CanExecute(null)); await app.AddSelectedAssetToPackageAsync(); Assert.False(app.Packages.Editor.IsEditing);
        var catalogItem = Assert.Single(app.Packages.Packages); await app.Packages.SelectPackageAsync(catalogItem);
        Assert.True(app.Packages.IsV2OverviewVisible); Assert.False(app.Packages.EditPackageCommand.CanExecute(null)); Assert.False(await app.Packages.EditSelectedPackageAsync());
        app.V2.SelectedAsset = app.V2.Assets[0]; await app.V2.InspectorTask; Assert.Contains("Read-only", app.V2.Inspector); Assert.Contains("Memo:", app.V2.Inspector);
        var selected = app.V2.SelectedAsset; app.SelectedWorkspaceIndex = 1; app.SelectedWorkspaceIndex = 0; Assert.Same(selected, app.V2.SelectedAsset);
        Assert.Empty(app.Search.PackageScopes); // not added to V1 Search providers
        var directory = await new DirectoryBrowserService().ReadAsync(corpus.DirectoryPath);
        Assert.Contains(directory.Entries, e => e.FullPath == corpus.Packages[0].Path);
    }

    [Fact]
    public async Task CatalogDiscovery_IncludesV2AndRetainsV1Editing()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        var legacy = new PackageDocument([new PackageIndex("", [new PackageBundleReference { Name = "bundle", Path = "missing.bundle", CabCode = "CAB-legacy", AssetInfos = [new() { Name = "Hero", PathId = 1 }] }])]);
        await new PackageService().SaveAsync(legacy, Path.Combine(corpus.DirectoryPath, "legacy"));
        using var app = new MainWindowViewModel(new Picker(), new NoUnityFactory());
        await app.Packages.LoadCatalogAsync(corpus.DirectoryPath); Assert.Equal(2, app.Packages.Packages.Count);
        var v1 = app.Packages.Packages.Single(p => p.V2 is null); await app.Packages.SelectPackageAsync(v1);
        Assert.True(await app.Packages.EditSelectedPackageAsync());
        app.Packages.Editor.PackageName = "dirty V1 name";
        await app.V2.OpenAsync(corpus.Packages[0].Path); Assert.True(app.Packages.Editor.IsDirty);
        await app.V2.RemoveAsync(); Assert.True(app.Packages.Editor.IsEditing); Assert.True(app.Packages.Editor.IsDirty);
    }

    [Fact]
    public async Task ReturningToWorkspaceRevalidatesWithoutReplacingValidSelection_AndFlagsExternalChange()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(150);
        using var app = new MainWindowViewModel(new Picker(), new NoUnityFactory()); await app.LoadFileAsync(corpus.Packages[0].Path);
        var rows = app.V2.Assets; app.V2.SelectedAsset = rows[0]; await app.V2.InspectorTask;
        app.SelectedWorkspaceIndex = 1; app.SelectedWorkspaceIndex = 0; await app.V2NavigationTask;
        Assert.Same(rows, app.V2.Assets); Assert.Same(rows[0], app.V2.SelectedAsset);
        app.SelectedWorkspaceIndex = 1; Edit(corpus.Packages[0].Path, "UPDATE asset SET memo='external' WHERE table_ordinal=0");
        app.SelectedWorkspaceIndex = 0; await app.V2NavigationTask;
        Assert.Empty(app.V2.Assets); Assert.Null(app.V2.SelectedAsset); Assert.Contains("stale", app.V2.Status);
    }

    [Fact]
    public async Task CancelledAdmissionDoesNotPublishOrLeakARegistration()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); using var release = new ManualResetEventSlim();
        using var catalog = new V2BrowseCatalog(b => { if (b == V2BrowseBoundary.BeforeAdmission) { entered.SetResult(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); } });
        using var cancel = new CancellationTokenSource(); var registration = catalog.RegisterAsync(corpus.Packages[0].Path, cancel.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)); cancel.Cancel(); release.Set();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => registration);
        string renamed = corpus.Packages[0].Path + ".moved"; File.Move(corpus.Packages[0].Path, renamed); Assert.True(File.Exists(renamed));
    }

    [Fact]
    public async Task CancellationAfterAdmissionCommit_RetainsVisibleOwnershipWithoutLateSelection()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); using var cancel = new CancellationTokenSource();
        var catalog = new V2BrowseCatalog(b => { if (b == V2BrowseBoundary.BeforeAdmissionCommit) cancel.Cancel(); });
        using var vm = new V2BrowseViewModel(new Picker(), catalog);
        Assert.Null(await vm.OpenAsync(corpus.Packages[0].Path, cancel.Token));
        Assert.Null(vm.SelectedPackage); Assert.Empty(vm.Assets); Assert.Contains("cancelled", vm.Status);
        vm.SelectedPackage = Assert.Single(vm.Registrations); await vm.SelectionTask; await vm.RemoveAsync();
        File.Move(corpus.Packages[0].Path, corpus.Packages[0].Path + ".moved");
    }

    [Fact]
    public async Task EmptyAssetsAndEvidence_AreExplicitSuccessfulEmptyStates()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(0);
        using var vm = new V2BrowseViewModel(new Picker()); Assert.NotNull(await vm.OpenAsync(corpus.Packages[0].Path));
        Assert.Empty(vm.Assets); Assert.Contains("0 visible", vm.Status); Assert.False(vm.NextCommand.CanExecute(null));
        vm.MetadataKind = V2MetadataKind.Issues; await vm.MetadataTask; Assert.Empty(vm.Metadata); Assert.Contains("no records", vm.MetadataStatus);
    }

    [Fact]
    public async Task MixedAudioImageUnknownAndIncompleteAssets_AreNotFilteredOrGuessed()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(6); string path = corpus.Packages[0].Path;
        Edit(path, "INSERT INTO asset_type(file_id,local_type_index,class_id,type_name,resolution_status) SELECT file_id,1,83,'AudioClip','resolved' FROM logical_file;"
            + "INSERT INTO asset_type(file_id,local_type_index,class_id,type_name,resolution_status) SELECT file_id,2,28,'Texture2D','resolved' FROM logical_file;"
            + "UPDATE asset SET class_id=83,type_id=(SELECT type_id FROM asset_type WHERE file_id=asset.file_id AND local_type_index=1) WHERE table_ordinal=0;"
            + "UPDATE asset SET class_id=28,type_id=(SELECT type_id FROM asset_type WHERE file_id=asset.file_id AND local_type_index=2) WHERE table_ordinal=1;"
            + "UPDATE asset SET class_id=NULL,type_id=NULL WHERE table_ordinal=3; UPDATE asset SET path_id=NULL,identity_status='incomplete' WHERE table_ordinal=5;");
        using var vm = new V2BrowseViewModel(new Picker()); await vm.OpenAsync(path);
        Assert.Equal(6, vm.Assets.Count); Assert.Equal("AudioClip", vm.Assets[0].IconType); Assert.Equal("Texture2D", vm.Assets[1].IconType);
        Assert.Equal(1234567, vm.Assets[2].ClassId); Assert.Null(vm.Assets[3].ClassId);
        Assert.Equal("Asset", vm.Assets[3].IconType); Assert.Null(vm.Assets[5].Identity.PathId);
        vm.SelectedAsset = vm.Assets[5]; await vm.InspectorTask; Assert.Contains("incomplete", vm.Inspector); Assert.Contains("Path ID: <NULL>", vm.Inspector);
    }

    [AvaloniaFact]
    public async Task ActualWorkspaceContainers_DisplayV2InsteadOfLegacyEditorOrInspector()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(6);
        using var app = new MainWindowViewModel(new Picker(), new NoUnityFactory()); await app.LoadFileAsync(corpus.Packages[0].Path);
        var assets = new AssetsWorkspaceView { DataContext = app }; var window = new Window { Content = assets, Width = 1200, Height = 750 }; window.Show();
        try
        {
            assets.UpdateLayout(); Assert.True(Assert.Single(assets.GetVisualDescendants().OfType<V2AssetListView>()).IsEffectivelyVisible);
            Assert.False(Assert.Single(assets.GetVisualDescendants().OfType<FolderView>()).IsEffectivelyVisible);
            Assert.True(Assert.Single(assets.GetVisualDescendants().OfType<V2InspectorView>()).IsEffectivelyVisible);
            await app.Packages.SelectPackageAsync(Assert.Single(app.Packages.Packages));
            var packages = new PackageManagementView { DataContext = app.Packages }; window.Content = packages; packages.UpdateLayout();
            Assert.True(Assert.Single(packages.GetVisualDescendants().OfType<V2PackageView>()).IsEffectivelyVisible);
            Assert.False(packages.FindControl<TabControl>("PackageOverviewTabs")!.IsVisible);
            Assert.False(app.Packages.EditPackageCommand.CanExecute(null));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task ReadOnlyViews_UseSharedGreenRowsAndBoundedKindColumn()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250);
        using var vm = new V2BrowseViewModel(new Picker()); await vm.OpenAsync(corpus.Packages[0].Path);
        var view = new V2AssetListView { DataContext = vm }; var window = new Window { Content = view, Width = 1000, Height = 500 };
        window.Show();
        try
        {
            var grid = view.FindControl<DataGrid>("V2AssetsGrid")!; view.UpdateLayout(); var kind = grid.Columns[0];
            Assert.Equal(52, kind.Width.Value); Assert.Equal(0, kind.MinWidth); Assert.Equal(80, kind.MaxWidth); Assert.True(kind.CanUserResize);
            Assert.Equal("", Assert.IsType<TextBlock>(kind.Header).Text); Assert.True(grid.IsReadOnly);
            Assert.All(grid.Columns.Skip(1), c => Assert.True(c.CanUserResize));
            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
            {
                window.RequestedThemeVariant = theme; view.UpdateLayout(); var rows = grid.GetVisualDescendants().OfType<DataGridRow>().Where(r => r.IsVisible).ToArray(); Assert.NotEmpty(rows);
                Assert.All(rows, r => Assert.Contains(r.Index % 2 == 0 ? "assetRowEven" : "assetRowOdd", r.Classes));
            }
            grid.SelectedItem = vm.Assets[0]; await vm.InspectorTask;
            var inspector = new V2InspectorView { DataContext = vm }; window.Content = inspector; inspector.UpdateLayout();
            var box = inspector.FindControl<TextBox>("V2ReadOnlyInspector")!; Assert.True(box.IsReadOnly); Assert.Contains("Memo:", box.Text);
            Assert.Empty(inspector.GetVisualDescendants().OfType<Button>());
        }
        finally { window.Close(); }
    }
}
