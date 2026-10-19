using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class PackageEditorViewModelTests
{
    [Fact]
    public async Task NewPackage_WithoutCatalog_SavesEmptyAndReopens()
    {
        using TemporaryDirectory temporary = new();
        PackageService service = new();
        PackageDocument? saved = null;
        using PackageEditorViewModel editor = CreateEditor(
            service: service,
            saved: (_, document) => { saved = document; return Task.CompletedTask; });

        Assert.True(await editor.StartNewAsync());
        editor.PackageName = "First Package";
        editor.DestinationDirectory = temporary.Path;
        await editor.SaveAsync();

        Assert.False(editor.IsDirty);
        Assert.NotNull(saved);
        Assert.Empty(saved!.Bundles);
        Assert.True(File.Exists(Path.Combine(temporary.Path, "First Package", PackageService.IndexFileName)));
    }

    [Fact]
    public async Task AddSource_FromRealBundle_DeduplicatesSourceAndCanRemoveIt()
    {
        QueuePicker picker = new(files: [TestPaths.BundleFixture, TestPaths.BundleFixture]);
        using PackageEditorViewModel editor = CreateEditor(picker);
        await editor.StartNewAsync();

        await editor.AddSourceAsync();
        int firstCount = editor.Items.Count;
        await editor.AddSourceAsync();

        Assert.True(firstCount > 1);
        Assert.Equal(firstCount, editor.Items.Count);
        editor.SelectedItem = editor.Items.First(item => item.Kind == PackageEditorItemKind.Source);
        editor.RemoveSelected();
        Assert.Empty(editor.Items);
    }

    [Fact]
    public async Task AddSelectedAsset_FromRealSerializedFile_PreservesProvenanceAndRejectsDuplicate()
    {
        using IAssetSession session = await new AssetsToolsAssetSessionFactory().OpenAsync(TestPaths.SerializedFixture);
        AssetEntry selected = session.Assets.First(asset => asset.ClassId is 28 or 213);
        using PackageEditorViewModel editor = CreateEditor();

        Assert.True(await editor.AddSelectedAssetAsync(selected));
        Assert.False(await editor.AddSelectedAssetAsync(selected));

        PackageEditorItemViewModel asset = Assert.Single(editor.Items, item => item.Kind == PackageEditorItemKind.Asset);
        Assert.Equal(selected.PathId, asset.PathId);
        Assert.Equal(Path.GetFullPath(TestPaths.SerializedFixture), asset.SourcePath);
    }

    [Fact]
    public async Task Save_InvalidNameOrMissingDestination_RetainsDirtyEditor()
    {
        using PackageEditorViewModel editor = CreateEditor();
        await editor.StartNewAsync();
        editor.PackageName = "bad/name";

        await editor.SaveAsync();
        Assert.True(editor.HasError);
        Assert.True(editor.IsDirty);

        editor.PackageName = "valid";
        await editor.SaveAsync();
        Assert.Contains("destination", editor.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.True(editor.IsDirty);
    }

    [Fact]
    public async Task EditAndDiscard_UsesIsolatedCopyAndRequiresConfirmation()
    {
        PackageDocument original = Document("original", Asset("Hero", 10));
        QueueConfirmation confirmations = new(false, true);
        using PackageEditorViewModel editor = CreateEditor(confirmations: confirmations);
        await editor.StartEditAsync("original", original);
        editor.SelectedItem = editor.Items.Single(item => item.Kind == PackageEditorItemKind.Asset);
        editor.RemoveSelected();

        await editor.DiscardAsync();
        Assert.True(editor.IsEditing);
        Assert.Single(original.Bundles.Single().AssetInfos);

        await editor.DiscardAsync();
        Assert.False(editor.IsEditing);
        Assert.Equal(2, confirmations.Calls);
        Assert.Single(original.Bundles.Single().AssetInfos);
    }

    [Fact]
    public async Task Save_ExistingTargetRequiresExplicitOverwriteConfirmation()
    {
        using TemporaryDirectory temporary = new();
        string target = temporary.CreateDirectory("existing");
        await new PackageService().SaveAsync(new PackageDocument([new PackageIndex(string.Empty)]), target, overwrite: true);
        QueueConfirmation confirmations = new(false, true);
        using PackageEditorViewModel editor = CreateEditor(confirmations: confirmations);
        await editor.StartNewAsync();
        editor.PackageName = "existing";
        editor.DestinationDirectory = temporary.Path;

        await editor.SaveAsync();
        Assert.True(editor.IsDirty);
        Assert.Contains("cancelled", editor.Status, StringComparison.OrdinalIgnoreCase);

        await editor.SaveAsync();
        Assert.False(editor.IsDirty);
        Assert.Equal(2, confirmations.Calls);
    }

    [Fact]
    public async Task CloseWithUnsavedChanges_UsesDiscardConfirmation()
    {
        QueueConfirmation confirmations = new(false, true);
        using PackageEditorViewModel editor = CreateEditor(confirmations: confirmations);
        await editor.StartNewAsync();

        Assert.False(await editor.ConfirmCloseAsync());
        Assert.True(await editor.ConfirmCloseAsync());
        Assert.True(editor.IsDirty);
    }

    [Fact]
    public async Task CloseEditor_CleanSessionClosesAndReleasesGuardWithoutPrompt()
    {
        PackageEditorSessionGuard guard = new();
        QueueCloseConfirmation confirmations = new();
        PackageDocument package = Document("clean", Asset("Hero", 10));
        using PackageEditorViewModel editor = CreateEditor(
            confirmations: confirmations,
            editorSessionController: guard);
        await editor.StartEditAsync("clean", package);
        Assert.True(guard.IsPackageOpen(package.PackagePath!));

        await editor.CloseEditorAsync();

        Assert.False(editor.IsEditing);
        Assert.False(guard.IsPackageOpen(package.PackagePath!));
        Assert.Equal(0, confirmations.CloseCalls);
    }

    [Fact]
    public async Task CloseEditor_DirtySavePersistsThenClosesAndReleasesGuard()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "memo");
        PackageService service = new();
        await service.SaveAsync(Document("memo", Asset("Hero", 10, "old")), target, overwrite: true);
        PackageDocument package = await service.LoadAsync(target);
        PackageEditorSessionGuard guard = new();
        QueueCloseConfirmation confirmations = new(UnsavedChangesChoice.Save) { ConfirmResult = true };
        using PackageEditorViewModel editor = CreateEditor(
            service: service,
            confirmations: confirmations,
            editorSessionController: guard);
        await editor.StartEditAsync("memo", package);
        editor.SelectedItem = editor.Items.Single(item => item.Kind == PackageEditorItemKind.Asset);
        editor.MemoText = "saved while closing";

        await editor.CloseEditorAsync();

        PackageDocument reopened = await service.LoadAsync(target);
        Assert.False(editor.IsEditing);
        Assert.False(guard.IsPackageOpen(target));
        Assert.Equal("saved while closing", reopened.Bundles.Single().AssetInfos.Single().Memo);
    }

    [Fact]
    public async Task CloseEditor_DirtySaveFailureKeepsEditorDirtyAndGuardActive()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "failure");
        ControlledPackageService service = new(saveError: new IOException("disk full"));
        PackageEditorSessionGuard guard = new();
        using PackageEditorViewModel editor = CreateEditor(
            service: service,
            confirmations: new QueueCloseConfirmation(UnsavedChangesChoice.Save),
            editorSessionController: guard);
        await editor.StartEditAsync("failure", new PackageDocument([new PackageIndex(string.Empty)], target));
        editor.PackageName = "changed";

        await editor.CloseEditorAsync();

        Assert.True(editor.IsEditing);
        Assert.True(editor.IsDirty);
        Assert.Contains("disk full", editor.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.True(guard.IsPackageOpen(Path.Combine(temporary.Path, "changed")));
    }

    [Theory]
    [InlineData(UnsavedChangesChoice.Discard, false)]
    [InlineData(UnsavedChangesChoice.Cancel, true)]
    public async Task CloseEditor_DirtyDiscardOrCancelUsesExplicitChoice(
        UnsavedChangesChoice choice,
        bool remainsOpen)
    {
        PackageEditorSessionGuard guard = new();
        PackageDocument package = Document("choice", Asset("Hero", 10));
        using PackageEditorViewModel editor = CreateEditor(
            confirmations: new QueueCloseConfirmation(choice),
            editorSessionController: guard);
        await editor.StartEditAsync("choice", package);
        editor.PackageName = "changed";

        await editor.CloseEditorAsync();

        Assert.Equal(remainsOpen, editor.IsEditing);
        Assert.Equal(remainsOpen, editor.IsDirty);
        Assert.Equal(remainsOpen, guard.IsPackageOpen(package.PackagePath!));
    }

    [Fact]
    public async Task AddMissingSource_ShowsErrorAndKeepsEditorConsistent()
    {
        QueuePicker picker = new(files: [Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.assets")]);
        using PackageEditorViewModel editor = CreateEditor(picker);
        await editor.StartNewAsync();

        await editor.AddSourceAsync();

        Assert.True(editor.HasError);
        Assert.Empty(editor.Items);
        Assert.True(editor.IsDirty);
    }

    [Fact]
    public async Task SaveFailure_RetainsUnsavedChanges()
    {
        using TemporaryDirectory temporary = new();
        ControlledPackageService service = new(saveError: new IOException("disk full"));
        using PackageEditorViewModel editor = CreateEditor(service: service);
        await editor.StartNewAsync();
        editor.PackageName = "failure";
        editor.DestinationDirectory = temporary.Path;

        await editor.SaveAsync();

        Assert.True(editor.IsDirty);
        Assert.Contains("disk full", editor.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplicitSaveUsesCoordinatorThroughVerificationAndPublication()
    {
        using TemporaryDirectory temporary = new();
        ControlledPackageService service = new();
        TrackingWriteCoordinator coordinator = new();
        bool callbackObservedLock = false;
        using PackageEditorViewModel editor = CreateEditor(
            service: service,
            saved: (_, _) =>
            {
                callbackObservedLock = coordinator.IsExecuting;
                return Task.CompletedTask;
            },
            writeCoordinator: coordinator);
        await editor.StartNewAsync();
        editor.PackageName = "coordinated";
        editor.DestinationDirectory = temporary.Path;

        Assert.Equal(0, coordinator.Calls);
        Assert.Equal(0, service.SaveCalls);

        await editor.SaveAsync();

        Assert.Equal(1, coordinator.Calls);
        Assert.Equal(Path.Combine(temporary.Path, "coordinated"), coordinator.LastPath);
        Assert.Equal(1, service.SaveCalls);
        Assert.True(callbackObservedLock);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public async Task CancelSave_CancelsOneOperationAndRetainsChanges()
    {
        using TemporaryDirectory temporary = new();
        ControlledPackageService service = new(blockSave: true);
        using PackageEditorViewModel editor = CreateEditor(service: service);
        await editor.StartNewAsync();
        editor.PackageName = "cancelled";
        editor.DestinationDirectory = temporary.Path;

        Task save = editor.SaveAsync();
        await service.SaveStarted.Task;
        editor.CancelSave();
        await save;

        Assert.True(service.SaveToken.IsCancellationRequested);
        Assert.True(editor.IsDirty);
        Assert.Contains("cancelled", editor.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelWhileWaitingForPackageLockDoesNotStartWriter()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "waiting");
        PackageWriteCoordinator coordinator = new();
        TaskCompletionSource holderEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseHolder = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task holder = coordinator.ExecutePackageWriteAsync(
            target,
            async _ =>
            {
                holderEntered.SetResult();
                await releaseHolder.Task;
            });
        await holderEntered.Task;

        ControlledPackageService service = new();
        using PackageEditorViewModel editor = CreateEditor(service: service, writeCoordinator: coordinator);
        await editor.StartNewAsync();
        editor.PackageName = "waiting";
        editor.DestinationDirectory = temporary.Path;

        Task save = editor.SaveAsync();
        editor.CancelSave();
        await save;

        Assert.Equal(0, service.SaveCalls);
        Assert.True(editor.IsDirty);
        Assert.Contains("cancelled", editor.Status, StringComparison.OrdinalIgnoreCase);
        releaseHolder.SetResult();
        await holder;
        Assert.Equal(0, coordinator.TrackedPathCount);
    }

    [Fact]
    public async Task MultipleRapidSaveAttempts_StartOnlyOneWrite()
    {
        using TemporaryDirectory temporary = new();
        ControlledPackageService service = new(blockSave: true);
        using PackageEditorViewModel editor = CreateEditor(service: service);
        await editor.StartNewAsync();
        editor.PackageName = "single-write";
        editor.DestinationDirectory = temporary.Path;

        Task first = editor.SaveAsync();
        await service.SaveStarted.Task;
        await editor.SaveAsync();
        editor.CancelSave();
        await first;

        Assert.Equal(1, service.SaveCalls);
    }

    [Fact]
    public async Task EditArrivingDuringCoordinatedSaveRemainsDirtyAfterVerification()
    {
        using TemporaryDirectory temporary = new();
        ControlledPackageService service = new(blockSave: true);
        using PackageEditorViewModel editor = CreateEditor(service: service);
        await editor.StartNewAsync();
        editor.PackageName = "snapshot";
        editor.DestinationDirectory = temporary.Path;

        Task save = editor.SaveAsync();
        await service.SaveStarted.Task;
        editor.PackageName = "newer edit";
        service.ReleaseSave.SetResult();
        await save;

        Assert.True(editor.IsDirty);
        Assert.Equal("newer edit", editor.PackageName);
        Assert.Contains("newer unsaved edits", editor.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaleAddResult_DoesNotModifyReplacementEditor()
    {
        ControlledPackageService service = new(blockReference: true);
        using PackageEditorViewModel editor = CreateEditor(service: service, confirmations: new QueueConfirmation(true));
        await editor.StartNewAsync();
        AssetEntry selected = new("Hero", "Texture2D", 28, 7, 10, "main.assets", @"C:\game\source.assets");

        Task<bool> add = editor.AddSelectedAssetAsync(selected);
        await service.ReferenceStarted.Task;
        await editor.StartNewAsync();
        service.ReleaseReference.SetResult();

        Assert.False(await add);
        Assert.Empty(editor.Items);
    }

    [Fact]
    public async Task AddFolder_MixedFilesImportsSupportedHeadersPreservesPathsAndDeduplicates()
    {
        using TemporaryDirectory temporary = new();
        string arbitrary = Path.Combine(temporary.Path, "first.custom");
        string extensionless = Path.Combine(temporary.Path, "second");
        File.Copy(TestPaths.SerializedFixture, arbitrary);
        File.Copy(TestPaths.SerializedFixture, extensionless);
        await File.WriteAllTextAsync(Path.Combine(temporary.Path, "notes.bundle"), "not a Unity file");
        QueuePicker picker = new(folders: [temporary.Path, temporary.Path]);
        using PackageEditorViewModel editor = CreateEditor(picker);
        await editor.StartNewAsync();

        await editor.AddFolderAsync();

        Assert.Equal(2, editor.Items.Count(item => item.Kind == PackageEditorItemKind.Source));
        Assert.Contains(editor.Items, item => item.SourcePath == Path.GetFullPath(arbitrary));
        Assert.Contains(editor.Items, item => item.SourcePath == Path.GetFullPath(extensionless));
        Assert.Contains("2 supported", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 unsupported", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("asset reference(s) added", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.True(editor.IsDirty);

        int itemCount = editor.Items.Count;
        await editor.AddFolderAsync();

        Assert.Equal(itemCount, editor.Items.Count);
        Assert.Contains("0 source(s)", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("already present", editor.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFolder_RecursiveModeImportsNestedFilesUpdatesUiAndSurvivesSaveReopen()
    {
        using TemporaryDirectory temporary = new();
        string source = temporary.CreateDirectory("source");
        string nested = temporary.CreateDirectory(Path.Combine("source", "nested"));
        string rootFile = Path.Combine(source, "root.custom");
        string nestedFile = Path.Combine(nested, "nested-file");
        File.Copy(TestPaths.SerializedFixture, rootFile);
        File.Copy(TestPaths.BundleFixture, nestedFile);
        await File.WriteAllTextAsync(Path.Combine(nested, "notes.assets"), "ordinary text");
        string destination = temporary.CreateDirectory("packages");
        QueuePicker picker = new(folders: [source]);
        PackageDocument? saved = null;
        using PackageEditorViewModel editor = CreateEditor(
            picker,
            new PackageService(),
            saved: (_, document) => { saved = document; return Task.CompletedTask; });
        await editor.StartNewAsync();
        editor.IncludeSubfolders = true;

        await editor.AddFolderAsync();

        Assert.Equal(2, editor.Items.Count(item => item.Kind == PackageEditorItemKind.Source));
        Assert.Contains(editor.Items, item => item.SourcePath == Path.GetFullPath(rootFile));
        Assert.Contains(editor.Items, item => item.SourcePath == Path.GetFullPath(nestedFile));
        Assert.Contains("including subfolders", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("3 file(s) scanned", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 unsupported", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not in the package on disk until Save", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, editor.EditorSourceCount);
        Assert.True(editor.EditorAssetReferenceCount > 0);
        Assert.Contains("not saved", editor.EditorContentsSummary, StringComparison.OrdinalIgnoreCase);
        Assert.True(editor.IsDirty);

        editor.PackageName = "recursive-package";
        editor.DestinationDirectory = destination;
        await editor.SaveAsync();

        Assert.False(editor.IsDirty);
        Assert.NotNull(saved);
        PackageDocument reopened = await new PackageService().LoadAsync(Path.Combine(destination, "recursive-package"));
        Assert.Equal(2, reopened.Bundles.Count());
        Assert.Equal(2, reopened.Indexes.Count);
        Assert.Contains(reopened.Indexes, index => index.RelativeDirectory == string.Empty
            && index.Bundles.Any(reference => reference.Path == Path.GetFullPath(rootFile)));
        Assert.Contains(reopened.Indexes, index => index.RelativeDirectory == "nested"
            && index.Bundles.Any(reference => reference.Path == Path.GetFullPath(nestedFile)));
        Assert.Contains(reopened.Bundles, reference => reference.Path == Path.GetFullPath(rootFile));
        Assert.Contains(reopened.Bundles, reference => reference.Path == Path.GetFullPath(nestedFile));
    }

    [Fact]
    public async Task AddFolder_MergesMappedSourcesWithoutFlatteningExistingIndexes()
    {
        PackageBundleReference imported = new()
        {
            Name = "new.assets",
            Path = @"C:\game\ui\new.assets",
            CabCode = "CAB-NEW",
            AssetInfos = [new PackageAssetReference { Name = "New", PathId = 2 }]
        };
        PackageFolderImportResult result = new(
            1,
            1,
            [imported],
            0,
            [],
            0,
            PackageFolderSearchMode.IncludeSubfolders,
            MappedReferences: [new PackageFolderImportReference(Path.Combine("ui", "icons"), imported)]);
        ControlledPackageService service = new(folderResult: result);
        QueuePicker picker = new(folders: [@"C:\game"]);
        PackageDocument existing = new([
            new PackageIndex(string.Empty),
            new PackageIndex(Path.Combine("characters", "hero"), [new PackageBundleReference
            {
                Name = "existing.assets",
                Path = @"C:\game\characters\hero\existing.assets",
                AssetInfos = [new PackageAssetReference { Name = "Existing", PathId = 1 }]
            }]),
            new PackageIndex(Path.Combine("characters", "empty"))
        ], @"C:\packages\existing");
        using PackageEditorViewModel editor = CreateEditor(picker, service);
        await editor.StartEditAsync("existing", existing);
        editor.IncludeSubfolders = true;

        await editor.AddFolderAsync();

        Assert.Equal(3, existing.Indexes.Count);
        Assert.Equal(4, editor.Items.Count);
        Assert.Contains(editor.Items, item => item.IndexDirectory == Path.Combine("characters", "hero"));
        Assert.Contains(editor.Items, item => item.IndexDirectory == Path.Combine("ui", "icons"));
        Assert.True(editor.IsDirty);
    }

    [Fact]
    public async Task AddFolder_CurrentFolderOnlyDoesNotImportNestedFileAndExplainsZeroResult()
    {
        using TemporaryDirectory temporary = new();
        string nested = temporary.CreateDirectory("nested");
        File.Copy(TestPaths.SerializedFixture, Path.Combine(nested, "nested.assets"));
        QueuePicker picker = new(folders: [temporary.Path]);
        using PackageEditorViewModel editor = CreateEditor(picker);
        await editor.StartNewAsync();

        await editor.AddFolderAsync();

        Assert.Empty(editor.Items);
        Assert.False(editor.IncludeSubfolders);
        Assert.Contains("current folder only", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0 file(s) scanned", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 subdirector", editor.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("enable Include subfolders", editor.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFolder_NestedOnlyFolderRequiresRecursiveScopeAndMarksImportedRowsUnsaved()
    {
        using TemporaryDirectory temporary = new();
        string nested = temporary.CreateDirectory(Path.Combine("nested", "deeper"));
        string nestedFile = Path.Combine(nested, "extensionless");
        File.Copy(TestPaths.BundleFixture, nestedFile);
        QueuePicker picker = new(folders: [temporary.Path, temporary.Path]);
        using PackageEditorViewModel editor = CreateEditor(picker);
        await editor.StartEditAsync(
            "existing",
            new PackageDocument([new PackageIndex(string.Empty)], @"C:\packages\existing"));

        await editor.AddFolderAsync();

        Assert.Empty(editor.Items);
        Assert.False(editor.IsDirty);
        Assert.Contains("enable Include subfolders", editor.Error!, StringComparison.OrdinalIgnoreCase);

        editor.IncludeSubfolders = true;
        await editor.AddFolderAsync();

        Assert.Contains(editor.Items, item =>
            item.Kind == PackageEditorItemKind.Source && item.SourcePath == Path.GetFullPath(nestedFile));
        Assert.True(editor.Items.Count(item => item.Kind == PackageEditorItemKind.Asset) > 0);
        Assert.True(editor.IsDirty);
        Assert.Contains("not in the package on disk until Save", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not saved", editor.EditorContentsSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFolder_RecursiveChoiceIsPassedToServiceAndZeroFailuresAreVisible()
    {
        PackageFolderImportResult result = new(
            2,
            1,
            [],
            1,
            [new PackageFolderImportFailure(@"C:\game\nested\bad.assets", "enumeration failed")],
            0,
            PackageFolderSearchMode.IncludeSubfolders);
        ControlledPackageService service = new(folderResult: result);
        QueuePicker picker = new(folders: [@"C:\game"]);
        using PackageEditorViewModel editor = CreateEditor(picker, service);
        await editor.StartNewAsync();
        editor.IncludeSubfolders = true;

        await editor.AddFolderAsync();

        Assert.Equal(PackageFolderSearchMode.IncludeSubfolders, service.FolderSearchMode);
        Assert.Empty(editor.Items);
        Assert.Contains("0 imported", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 file failure", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("enumeration failed", editor.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelFolderImport_AppliesNoPartialResultsAndPreservesCleanEditor()
    {
        ControlledPackageService service = new(blockFolder: true);
        QueuePicker picker = new(folders: [@"C:\game\sources"]);
        using PackageEditorViewModel editor = CreateEditor(picker, service);
        await editor.StartEditAsync("existing", new PackageDocument([new PackageIndex(string.Empty)], @"C:\packages\existing"));

        Task import = editor.AddFolderAsync();
        await service.FolderStarted.Task;
        editor.CancelCurrentOperation();
        await import;

        Assert.True(service.FolderToken.IsCancellationRequested);
        Assert.Empty(editor.Items);
        Assert.False(editor.IsDirty);
        Assert.Contains("no folder results", editor.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFolderFailure_LeavesExistingEditorUnchanged()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"missing-folder-{Guid.NewGuid():N}");
        QueuePicker picker = new(folders: [missing]);
        using PackageEditorViewModel editor = CreateEditor(picker);
        await editor.StartEditAsync("existing", new PackageDocument([new PackageIndex(string.Empty)], @"C:\packages\existing"));

        await editor.AddFolderAsync();

        Assert.True(editor.HasError);
        Assert.Empty(editor.Items);
        Assert.False(editor.IsDirty);
        Assert.Contains("no folder results", editor.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FolderApplicationFailure_RollsBackEntireBatch()
    {
        PackageBundleReference valid = new()
        {
            Name = "new.assets",
            Path = @"C:\game\new.assets",
            AssetInfos = [new PackageAssetReference { Name = "New", PathId = 2 }]
        };
        PackageFolderImportResult result = new(
            2,
            2,
            [valid, null!],
            0,
            [],
            0,
            PackageFolderSearchMode.CurrentFolderOnly);
        ControlledPackageService service = new(folderResult: result);
        QueuePicker picker = new(folders: [@"C:\game\sources"]);
        PackageDocument original = Document("existing", Asset("Original", 1));
        using PackageEditorViewModel editor = CreateEditor(picker, service);
        await editor.StartEditAsync("existing", original);

        await editor.AddFolderAsync();

        Assert.True(editor.HasError);
        Assert.False(editor.IsDirty);
        Assert.Equal(["source.ab", "Original"], editor.Items.Select(item => item.Name));
        Assert.Contains("no folder results", editor.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InaccessibleFolder_ReportsFailureWithoutChangingEditor()
    {
        ControlledPackageService service = new(folderError: new UnauthorizedAccessException("access denied"));
        QueuePicker picker = new(folders: [@"C:\restricted"]);
        using PackageEditorViewModel editor = CreateEditor(picker, service);
        await editor.StartEditAsync("existing", new PackageDocument([new PackageIndex(string.Empty)], @"C:\packages\existing"));

        await editor.AddFolderAsync();

        Assert.Contains("access denied", editor.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.False(editor.IsDirty);
        Assert.Empty(editor.Items);
    }

    [Fact]
    public async Task MemoEdit_SurvivesSelectionChangesAndDoesNotMutateLoadedPackage()
    {
        PackageDocument original = Document("memo", Asset("First", 1, "old"), Asset("Second", 2));
        using PackageEditorViewModel editor = CreateEditor();
        await editor.StartEditAsync("memo", original);
        PackageEditorItemViewModel first = editor.Items.Single(item => item.Name == "First");
        PackageEditorItemViewModel second = editor.Items.Single(item => item.Name == "Second");

        editor.SelectedItem = first;
        editor.MemoText = "unsaved text";
        editor.SelectedItem = second;
        editor.SelectedItem = first;

        Assert.Equal("unsaved text", editor.MemoText);
        Assert.Equal("unsaved text", first.Memo);
        Assert.True(editor.IsDirty);
        Assert.Equal("old", original.Bundles.Single().AssetInfos[0].Memo);
    }

    [Fact]
    public async Task MemoEditor_ExplainsUnsupportedSourceRowsWithoutChangingState()
    {
        PackageDocument original = Document("memo", Asset("Asset", 1, "kept"));
        using PackageEditorViewModel editor = CreateEditor();
        await editor.StartEditAsync("memo", original);

        editor.SelectedItem = editor.Items.Single(item => item.Kind == PackageEditorItemKind.Source);
        editor.MemoText = "ignored";

        Assert.False(editor.IsMemoEditable);
        Assert.Contains("no package, directory, or source-level", editor.MemoTargetDescription, StringComparison.OrdinalIgnoreCase);
        Assert.False(editor.IsDirty);
        Assert.Equal("kept", original.Bundles.Single().AssetInfos[0].Memo);
    }

    [Fact]
    public async Task ClearMemo_WritesExplicitEmptyValue()
    {
        PackageDocument original = Document("memo", Asset("Asset", 1));
        using PackageEditorViewModel editor = CreateEditor();
        await editor.StartEditAsync("memo", original);
        editor.SelectedItem = editor.Items.Single(item => item.Kind == PackageEditorItemKind.Asset);

        editor.ClearMemoCommand.Execute(null);

        Assert.Equal(string.Empty, editor.SelectedItem.Memo);
        Assert.Contains("explicit empty", editor.MemoStorageDescription, StringComparison.OrdinalIgnoreCase);
        Assert.True(editor.IsDirty);
    }

    [Fact]
    public async Task MemoEdit_SaveAndFreshReopen_PreservesValue()
    {
        using TemporaryDirectory temporary = new();
        PackageService service = new();
        PackageDocument original = Document("memo", Asset("Asset", 1));
        using PackageEditorViewModel editor = CreateEditor(service: service);
        await editor.StartEditAsync("memo", original);
        editor.DestinationDirectory = temporary.Path;
        editor.SelectedItem = editor.Items.Single(item => item.Kind == PackageEditorItemKind.Asset);
        editor.MemoText = "persisted";

        await editor.SaveAsync();
        PackageDocument reopened = await service.LoadAsync(Path.Combine(temporary.Path, "memo"));

        Assert.False(editor.IsDirty);
        Assert.Equal("persisted", reopened.Bundles.Single().AssetInfos.Single().Memo);
    }

    [Fact]
    public async Task MemoEdit_SaveFailureRetainsTextAndDirtyState()
    {
        using TemporaryDirectory temporary = new();
        ControlledPackageService service = new(saveError: new IOException("disk full"));
        using PackageEditorViewModel editor = CreateEditor(service: service);
        await editor.StartEditAsync("memo", Document("memo", Asset("Asset", 1)));
        editor.DestinationDirectory = temporary.Path;
        editor.SelectedItem = editor.Items.Single(item => item.Kind == PackageEditorItemKind.Asset);
        editor.MemoText = "keep after failure";

        await editor.SaveAsync();

        Assert.True(editor.IsDirty);
        Assert.Equal("keep after failure", editor.MemoText);
        Assert.Equal("keep after failure", editor.SelectedItem.Memo);
    }

    [Fact]
    public async Task MemoInheritance_PublishesTransactionalResultAndKeepsSelection()
    {
        PackageDocument target = new([
            new PackageIndex("", [new PackageBundleReference
            {
                Name = "atlas_new.bundle",
                Path = @"C:\game\new.bundle",
                AssetInfos = [Asset("Asset", 2)]
            }])
        ], @"C:\packages\target");
        PackageDocument source = new([
            new PackageIndex("", [new PackageBundleReference
            {
                Name = "atlas_old.bundle",
                Path = @"C:\game\old.bundle",
                AssetInfos = [Asset("Asset", 1, "copied")]
            }])
        ], @"C:\packages\source");
        using PackageEditorViewModel editor = CreateEditor();
        await editor.StartEditAsync("target", target);
        editor.SelectedItem = editor.Items.Single(item => item.Kind == PackageEditorItemKind.Asset);

        await editor.ApplyMemoInheritanceAsync("source", source, PackageMemoInheritanceMode.Safe);

        Assert.True(editor.IsDirty);
        Assert.NotNull(editor.SelectedItem);
        Assert.Equal("copied", editor.SelectedItem!.Memo);
        Assert.Equal("copied", editor.MemoText);
        Assert.Contains("1 changed", editor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Null(target.Bundles.Single().AssetInfos.Single().Memo);
    }

    [Fact]
    public async Task MemoInheritance_CancelOperationDoesNotPublishPartialResult()
    {
        BlockingMemoService memos = new();
        PackageDocument target = Document("target", Asset("Asset", 1));
        PackageDocument source = Document("source", Asset("Asset", 2, "source memo"));
        using PackageEditorViewModel editor = CreateEditor(memoService: memos);
        await editor.StartEditAsync("target", target);
        editor.SelectedItem = editor.Items.Single(item => item.Kind == PackageEditorItemKind.Asset);

        Task inheritance = editor.ApplyMemoInheritanceAsync("source", source, PackageMemoInheritanceMode.Safe);
        await memos.Started.Task;
        editor.CancelCurrentOperation();
        await inheritance;

        Assert.False(editor.IsApplyingMemos);
        Assert.False(editor.IsDirty);
        Assert.Null(editor.SelectedItem!.Memo);
        Assert.Contains("cancelled", editor.Status, StringComparison.OrdinalIgnoreCase);
    }

    private static PackageEditorViewModel CreateEditor(
        IFilePickerService? picker = null,
        IPackageService? service = null,
        IUserConfirmationService? confirmations = null,
        Func<string, PackageDocument, Task>? saved = null,
        IPackageMemoService? memoService = null,
        IPackageWriteCoordinator? writeCoordinator = null,
        IPackageEditorSessionController? editorSessionController = null) => new(
            picker ?? new QueuePicker(),
            confirmations ?? new QueueConfirmation(true),
            service ?? new PackageService(),
            saved,
            memoService,
            writeCoordinator,
            editorSessionController);

    private static PackageDocument Document(string name, params PackageAssetReference[] assets) => new([
        new PackageIndex(string.Empty, [new PackageBundleReference
        {
            Name = "source.ab",
            Path = $@"C:\game\{name}.ab",
            AssetInfos = assets.ToList()
        }])
    ], $@"C:\packages\{name}");

    private static PackageAssetReference Asset(string name, long pathId, string? memo = null) => new()
    {
        Name = name,
        PathId = pathId,
        Memo = memo
    };

    private sealed class QueuePicker(IEnumerable<string>? files = null, IEnumerable<string>? folders = null) : IFilePickerService
    {
        private readonly Queue<string> _files = new(files ?? []);
        private readonly Queue<string> _folders = new(folders ?? []);
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(_files.Count == 0 ? null : _files.Dequeue());
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(_folders.Count == 0 ? null : _folders.Dequeue());
    }

    private sealed class QueueConfirmation(params bool[] answers) : IUserConfirmationService
    {
        private readonly Queue<bool> _answers = new(answers);
        public int Calls { get; private set; }
        public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_answers.Count > 0 && _answers.Dequeue());
        }
    }

    private sealed class QueueCloseConfirmation(params UnsavedChangesChoice[] choices) : IUserConfirmationService
    {
        private readonly Queue<UnsavedChangesChoice> _choices = new(choices);
        public bool ConfirmResult { get; init; }
        public int CloseCalls { get; private set; }

        public Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmText,
            string cancelText,
            CancellationToken cancellationToken = default) => Task.FromResult(ConfirmResult);

        public Task<UnsavedChangesChoice> ConfirmSaveDiscardCancelAsync(
            string title,
            string message,
            CancellationToken cancellationToken = default)
        {
            CloseCalls++;
            return Task.FromResult(_choices.Count > 0 ? _choices.Dequeue() : UnsavedChangesChoice.Cancel);
        }
    }

    private sealed class ControlledPackageService(
        bool blockSave = false,
        Exception? saveError = null,
        bool blockReference = false,
        bool blockFolder = false,
        PackageFolderImportResult? folderResult = null,
        Exception? folderError = null) : IPackageService
    {
        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReferenceStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseReference { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FolderStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken SaveToken { get; private set; }
        public int SaveCalls { get; private set; }
        public CancellationToken FolderToken { get; private set; }
        public PackageFolderSearchMode FolderSearchMode { get; private set; }

        public async Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            SaveToken = cancellationToken;
            SaveStarted.TrySetResult();
            if (saveError is not null)
                throw saveError;
            if (blockSave)
                await ReleaseSave.Task.WaitAsync(cancellationToken);
        }

        public Task<PackageDocument> LoadAsync(string packagePath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PackageDocument([new PackageIndex(string.Empty)], packagePath));
        public async Task<PackageBundleReference> CreateReferenceAsync(string sourcePath, IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null, CancellationToken cancellationToken = default)
        {
            ReferenceStarted.TrySetResult();
            if (blockReference)
                await ReleaseReference.Task;
            PackageAssetSelection selection = selectedAssets?.Single() ?? new PackageAssetSelection("Hero", 7, "main.assets");
            return new PackageBundleReference
            {
                Name = Path.GetFileName(sourcePath),
                Path = sourcePath,
                AssetInfos = [new PackageAssetReference { Name = selection.Name, PathId = selection.PathId }]
            };
        }
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) =>
            CreateReferencesFromFolderAsync(folderPath, PackageFolderSearchMode.CurrentFolderOnly, progress, cancellationToken);

        public async Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, PackageFolderSearchMode searchMode, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            FolderToken = cancellationToken;
            FolderSearchMode = searchMode;
            FolderStarted.TrySetResult();
            if (folderError is not null)
                throw folderError;
            if (blockFolder)
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return folderResult ?? new PackageFolderImportResult(0, 0, [], 0, [], 0, searchMode);
        }
        public Task<PackageDocument> CreateAsync(string sourceRoot, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> BuildAndSaveAsync(string sourceRoot, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PackageSummary>> ListAsync(string packageParentPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(string packageParentPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TrackingWriteCoordinator : IPackageWriteCoordinator
    {
        public int Calls { get; private set; }
        public string? LastPath { get; private set; }
        public bool IsExecuting { get; private set; }

        public async Task ExecutePackageWriteAsync(
            string packagePath,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastPath = packagePath;
            IsExecuting = true;
            try
            {
                await operation(cancellationToken);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public async Task<TResult> ExecutePackageWriteAsync<TResult>(
            string packagePath,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastPath = packagePath;
            IsExecuting = true;
            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }

    private sealed class BlockingMemoService : IPackageMemoService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PackageMemoReadResult Read(PackageDocument package, PackageMemoIdentity identity) =>
            throw new NotSupportedException();

        public bool SetLocalMemo(PackageDocument package, PackageMemoIdentity identity, string? value) =>
            throw new NotSupportedException();

        public PackageMemoInheritanceResult Inherit(
            PackageDocument source,
            PackageDocument target,
            PackageMemoInheritanceMode mode,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            cancellationToken.WaitHandle.WaitOne();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Expected cancellation.");
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"uabs-editor-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }
        public string CreateDirectory(string relative) => Directory.CreateDirectory(System.IO.Path.Combine(Path, relative)).FullName;
        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
