using UABS.Services;
using UABS.UnityAssets.Packages;
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

    private static PackageEditorViewModel CreateEditor(
        IFilePickerService? picker = null,
        IPackageService? service = null,
        QueueConfirmation? confirmations = null,
        Func<string, PackageDocument, Task>? saved = null) => new(
            picker ?? new QueuePicker(),
            confirmations ?? new QueueConfirmation(true),
            service ?? new PackageService(),
            saved);

    private static PackageDocument Document(string name, params PackageAssetReference[] assets) => new([
        new PackageIndex(string.Empty, [new PackageBundleReference
        {
            Name = "source.ab",
            Path = $@"C:\game\{name}.ab",
            AssetInfos = assets.ToList()
        }])
    ], $@"C:\packages\{name}");

    private static PackageAssetReference Asset(string name, long pathId) => new() { Name = name, PathId = pathId };

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

    private sealed class ControlledPackageService(
        bool blockSave = false,
        Exception? saveError = null,
        bool blockReference = false,
        bool blockFolder = false,
        PackageFolderImportResult? folderResult = null,
        Exception? folderError = null) : IPackageService
    {
        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
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
