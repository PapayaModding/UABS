using System.Text.Json;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class PackageMemoPersistenceServiceTests
{
    [Fact]
    public async Task PersistAsync_SavesExactMemoPreservesMetadataAndPublishesFreshDocumentInsideLock()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "target", "old", "other");
        PackageDocument before = await packages.LoadAsync(path);
        PackageMemoIdentity identity = Identity(before, "Hero", 7);
        TrackingCoordinator coordinator = new();
        PackageDocument? published = null;
        bool publishedInsideLock = false;
        PackageMemoPersistenceService service = new(
            packages,
            coordinator,
            catalogPublisher: (document, _) =>
            {
                published = document;
                publishedInsideLock = coordinator.IsExecuting;
                return Task.CompletedTask;
            });
        string revision = (await service.CaptureRevisionAsync(path))!;
        PackageMemoPersistenceRequest request = new(identity, "new", "old", revision);

        PackageMemoPersistenceResult result = await service.PersistAsync(request);

        Assert.Equal(PackageMemoPersistenceStatus.Saved, result.Status);
        Assert.Same(request, result.Request);
        Assert.Equal("new", request.DesiredMemo);
        Assert.True(result.ChangedDisk);
        Assert.NotNull(result.PackageRevision);
        Assert.True(publishedInsideLock);
        Assert.Equal("new", Find(published!, "Hero").Memo);

        PackageDocument reopened = await packages.LoadAsync(path);
        Assert.Equal("new", Find(reopened, "Hero").Memo);
        Assert.Equal("other", Find(reopened, "Other").Memo);
        Assert.Equal("bundle-extra", reopened.Bundles.Single().ExtensionData!["CustomBundle"].GetString());
        Assert.Equal(42, Find(reopened, "Hero").ExtensionData!["CustomAsset"].GetInt32());
        Assert.Equal(Path.Combine("nested", "index"), reopened.Indexes.Single().RelativeDirectory);
    }

    [Fact]
    public async Task PersistAsync_PreservesExplicitEmptyAndMissingMemoStates()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "states", null, "other");
        PackageMemoPersistenceService service = CreateService(packages);
        PackageMemoIdentity identity = Identity(await packages.LoadAsync(path), "Hero", 7);

        PackageMemoPersistenceResult empty = await service.PersistAsync(new(identity, "", null));
        Assert.Equal(PackageMemoPersistenceStatus.Saved, empty.Status);
        Assert.Equal(string.Empty, Find(await packages.LoadAsync(path), "Hero").Memo);

        PackageMemoPersistenceResult missing = await service.PersistAsync(new(identity, null, ""));
        Assert.Equal(PackageMemoPersistenceStatus.Saved, missing.Status);
        Assert.Null(Find(await packages.LoadAsync(path), "Hero").Memo);
        string json = await File.ReadAllTextAsync(Path.Combine(path, "nested", "index", PackageService.IndexFileName));
        Assert.DoesNotContain("\"Memo\": null", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PersistAsync_TargetsExactPackageAndIdentityDespiteDuplicateNames()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string firstPath = await SavePackageAsync(packages, temporary.Path, "first", "first", "other");
        string secondPath = await SavePackageAsync(packages, temporary.Path, "second", "second", "other");
        PackageDocument first = await packages.LoadAsync(firstPath);
        PackageMemoPersistenceService service = CreateService(packages);

        PackageMemoPersistenceResult result = await service.PersistAsync(
            new PackageMemoPersistenceRequest(Identity(first, "Hero", 7), "changed", "first"));

        Assert.Equal(PackageMemoPersistenceStatus.Saved, result.Status);
        Assert.Equal("changed", Find(await packages.LoadAsync(firstPath), "Hero").Memo);
        Assert.Equal("second", Find(await packages.LoadAsync(secondPath), "Hero").Memo);
    }

    [Fact]
    public async Task PersistAsync_RejectsMissingPackageMissingAssetAndAmbiguousIdentityWithoutWriting()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "target", "old", "other", duplicateHero: true);
        PackageDocument package = await packages.LoadAsync(path);
        PackageMemoPersistenceService service = CreateService(packages);
        string before = await service.CaptureRevisionAsync(path) ?? throw new InvalidOperationException();

        PackageMemoPersistenceResult ambiguous = await service.PersistAsync(
            new(Identity(package, "Hero", 7), "new", "old"));
        PackageMemoPersistenceResult missingAsset = await service.PersistAsync(
            new(Identity(package, "Missing", 999), "new", null));
        PackageMemoIdentity missingPackageIdentity = Identity(package, "Hero", 7) with
        {
            PackagePath = Path.Combine(temporary.Path, "missing")
        };
        PackageMemoPersistenceResult missingPackage = await service.PersistAsync(
            new(missingPackageIdentity, "new", "old"));

        Assert.Equal(PackageMemoPersistenceStatus.AmbiguousIdentity, ambiguous.Status);
        Assert.Equal(PackageMemoPersistenceStatus.AssetNotFound, missingAsset.Status);
        Assert.Equal(PackageMemoPersistenceStatus.PackageMissing, missingPackage.Status);
        Assert.Equal(before, await service.CaptureRevisionAsync(path));
    }

    [Fact]
    public async Task PersistAsync_RejectsExpectedRevisionAndExpectedMemoConflicts()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "target", "old", "other");
        PackageDocument package = await packages.LoadAsync(path);
        PackageMemoPersistenceService service = CreateService(packages);
        string staleRevision = (await service.CaptureRevisionAsync(path))!;
        await File.WriteAllTextAsync(Path.Combine(path, PackageService.ValidationFileName), "external change");

        PackageMemoPersistenceResult revisionConflict = await service.PersistAsync(
            new(Identity(package, "Hero", 7), "new", "old", staleRevision));
        PackageMemoPersistenceResult memoConflict = await service.PersistAsync(
            new(Identity(package, "Hero", 7), "new", "not-old"));

        Assert.Equal(PackageMemoPersistenceStatus.RevisionConflict, revisionConflict.Status);
        Assert.Equal(PackageMemoPersistenceStatus.RevisionConflict, memoConflict.Status);
        Assert.Equal("old", Find(await packages.LoadAsync(path), "Hero").Memo);
    }

    [Fact]
    public async Task PersistAsync_DetectsExternalModificationDuringLatestLoad()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "target", "old", "other");
        PackageDocument package = await packages.LoadAsync(path);
        MutatingLoadPackageService mutating = new(packages, path);
        PackageMemoPersistenceService service = CreateService(mutating);

        PackageMemoPersistenceResult result = await service.PersistAsync(
            new(Identity(package, "Hero", 7), "new", "old"));

        Assert.Equal(PackageMemoPersistenceStatus.RevisionConflict, result.Status);
        Assert.Equal(0, mutating.SaveCalls);
        Assert.Equal("old", Find(await packages.LoadAsync(path), "Hero").Memo);
    }

    [Fact]
    public async Task PersistAsync_SerializesSamePackageAndAllowsDifferentPackagesToProceedIndependently()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string firstPath = await SavePackageAsync(packages, temporary.Path, "first", "old", "other");
        string secondPath = await SavePackageAsync(packages, temporary.Path, "second", "old", "other");
        PackageMemoPersistenceService service = CreateService(packages);
        PackageMemoIdentity firstIdentity = Identity(await packages.LoadAsync(firstPath), "Hero", 7);
        PackageMemoPersistenceRequest firstRequest = new(firstIdentity, "one", "old");
        PackageMemoPersistenceRequest staleRequest = new(firstIdentity, "two", "old");

        PackageMemoPersistenceResult[] samePackage = await Task.WhenAll(
            service.PersistAsync(firstRequest),
            service.PersistAsync(staleRequest));
        Assert.Single(samePackage, result => result.Status == PackageMemoPersistenceStatus.Saved);
        Assert.Single(samePackage, result => result.Status == PackageMemoPersistenceStatus.RevisionConflict);

        ConcurrentSavePackageService concurrent = new(packages);
        PackageMemoPersistenceService parallelService = CreateService(concurrent);
        PackageMemoIdentity secondIdentity = Identity(await packages.LoadAsync(secondPath), "Hero", 7);
        await Task.WhenAll(
            parallelService.PersistAsync(new(firstIdentity, "parallel-one", Find(await packages.LoadAsync(firstPath), "Hero").Memo)),
            parallelService.PersistAsync(new(secondIdentity, "parallel-two", "old")));
        Assert.True(concurrent.MaximumConcurrentSaves >= 2);
    }

    [Fact]
    public async Task PersistAsync_CancellationWhileWaitingOrBeforeWorkLeavesPackageUnchanged()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "target", "old", "other");
        PackageDocument package = await packages.LoadAsync(path);
        PackageWriteCoordinator coordinator = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task holder = coordinator.ExecutePackageWriteAsync(path, async _ =>
        {
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;
        PackageMemoPersistenceService service = new(packages, coordinator);
        using CancellationTokenSource waitingCancellation = new();
        Task<PackageMemoPersistenceResult> waiting = service.PersistAsync(
            new(Identity(package, "Hero", 7), "new", "old"),
            waitingCancellation.Token);
        waitingCancellation.Cancel();

        PackageMemoPersistenceResult waitingResult = await waiting;
        release.SetResult();
        await holder;
        using CancellationTokenSource beforeWorkCancellation = new();
        beforeWorkCancellation.Cancel();
        PackageMemoPersistenceResult beforeWork = await service.PersistAsync(
            new(Identity(package, "Hero", 7), "new", "old"),
            beforeWorkCancellation.Token);

        Assert.Equal(PackageMemoPersistenceStatus.CancelledBeforeCommit, waitingResult.Status);
        Assert.Equal(PackageMemoPersistenceStatus.CancelledBeforeCommit, beforeWork.Status);
        Assert.Equal("old", Find(await packages.LoadAsync(path), "Hero").Memo);
    }

    [Fact]
    public async Task PersistAsync_WriterAndVerificationFailuresAreNotReportedAsSuccess()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "target", "old", "other");
        PackageDocument package = await packages.LoadAsync(path);
        PackageMemoPersistenceRequest request = new(Identity(package, "Hero", 7), "new", "old");
        PackageMemoPersistenceService writerFailure = CreateService(new FailingSavePackageService(packages));

        PackageMemoPersistenceResult failedWrite = await writerFailure.PersistAsync(request);
        Assert.Equal(PackageMemoPersistenceStatus.WriteFailure, failedWrite.Status);
        Assert.Equal("old", Find(await packages.LoadAsync(path), "Hero").Memo);

        PackageMemoPersistenceService failedVerification = CreateService(new TamperingVerificationPackageService(packages));
        PackageMemoPersistenceResult verification = await failedVerification.PersistAsync(request);
        Assert.Equal(PackageMemoPersistenceStatus.VerificationFailure, verification.Status);
        Assert.False(verification.IsSuccess);
    }

    [Fact]
    public async Task PersistAsync_ActiveEditorBlocksSamePackageButDoesNotMutateOtherDirtyEditor()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string editedPath = await SavePackageAsync(packages, temporary.Path, "edited", "old", "other");
        string otherPath = await SavePackageAsync(packages, temporary.Path, "other", "old", "other");
        PackageEditorSessionGuard guard = new();
        using PackageEditorViewModel editor = new(
            new NullPicker(),
            new AlwaysConfirm(),
            packages,
            editorSessionController: guard);
        await editor.StartEditAsync("edited", await packages.LoadAsync(editedPath));
        editor.PackageName = "dirty-name";
        PackageMemoPersistenceService service = new(packages, new PackageWriteCoordinator(), editorSessionGuard: guard);

        PackageMemoPersistenceResult blocked = await service.PersistAsync(
            new(Identity(await packages.LoadAsync(editedPath), "Hero", 7), "blocked", "old"));
        PackageMemoPersistenceResult saved = await service.PersistAsync(
            new(Identity(await packages.LoadAsync(otherPath), "Hero", 7), "saved", "old"));

        Assert.Equal(PackageMemoPersistenceStatus.PackageEditorActive, blocked.Status);
        Assert.Equal(PackageMemoPersistenceStatus.Saved, saved.Status);
        Assert.True(editor.IsDirty);
        Assert.Equal("dirty-name", editor.PackageName);
        Assert.Equal("old", Find(await packages.LoadAsync(editedPath), "Hero").Memo);
        Assert.Equal("saved", Find(await packages.LoadAsync(otherPath), "Hero").Memo);
    }

    [Fact]
    public async Task PersistAsync_CatalogPublicationFailureRetainsSavedDraftAndReportsDiskChange()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "Package", "old", "other");
        PackageDocument loaded = await packages.LoadAsync(path);
        PackageMemoPersistenceService service = new(
            packages,
            new PackageWriteCoordinator(),
            catalogPublisher: (_, _) => throw new OperationCanceledException("Publisher stopped."));

        PackageMemoPersistenceRequest request = new(Identity(loaded, "Hero", 7), "draft retained", "old");
        PackageMemoPersistenceResult result = await service.PersistAsync(request);

        Assert.Equal(PackageMemoPersistenceStatus.CatalogPublicationFailure, result.Status);
        Assert.True(result.ChangedDisk);
        Assert.Same(request, result.Request);
        Assert.Equal("draft retained", result.Request.DesiredMemo);
        Assert.Equal("draft retained", Find(await packages.LoadAsync(path), "Hero").Memo);
    }

    [Fact]
    public async Task PersistAsync_RetryAfterPublicationFailureRepublishesAlreadyPersistedMemo()
    {
        using TemporaryDirectory temporary = new();
        PackageService packages = CreatePackageService();
        string path = await SavePackageAsync(packages, temporary.Path, "Package", "old", "other");
        PackageDocument loaded = await packages.LoadAsync(path);
        int publications = 0;
        PackageMemoPersistenceService service = new(
            packages,
            new PackageWriteCoordinator(),
            catalogPublisher: (_, _) => ++publications == 1
                ? throw new IOException("Catalog busy.")
                : Task.CompletedTask);
        PackageMemoPersistenceRequest initial = new(Identity(loaded, "Hero", 7), "saved memo", "old");

        PackageMemoPersistenceResult failedPublication = await service.PersistAsync(initial);
        PackageMemoPersistenceRequest retry = initial with
        {
            ExpectedOriginalMemo = "saved memo",
            ExpectedPackageRevision = failedPublication.PackageRevision
        };
        PackageMemoPersistenceResult retried = await service.PersistAsync(retry);

        Assert.Equal(PackageMemoPersistenceStatus.CatalogPublicationFailure, failedPublication.Status);
        Assert.Equal(PackageMemoPersistenceStatus.Unchanged, retried.Status);
        Assert.Equal(2, publications);
        Assert.Equal("saved memo", Find(await packages.LoadAsync(path), "Hero").Memo);
    }

    private static PackageService CreatePackageService() => new(TestPaths.ClassData);

    private static PackageMemoPersistenceService CreateService(IPackageService packages) =>
        new(packages, new PackageWriteCoordinator());

    private static async Task<string> SavePackageAsync(
        PackageService packages,
        string parent,
        string name,
        string? heroMemo,
        string? otherMemo,
        bool duplicateHero = false)
    {
        string path = Path.Combine(parent, name);
        await packages.SaveAsync(CreateDocument(path, heroMemo, otherMemo, duplicateHero), path);
        return path;
    }

    private static PackageDocument CreateDocument(
        string packagePath,
        string? heroMemo,
        string? otherMemo,
        bool duplicateHero) => new(
        [
            new PackageIndex(Path.Combine("nested", "index"),
            [
                new PackageBundleReference
                {
                    Name = "source.bundle",
                    Path = Path.Combine(Path.GetDirectoryName(packagePath)!, "game", "source.bundle"),
                    CabCode = "CAB-source",
                    Dependencies = ["CAB-dependency"],
                    ExtensionData = new Dictionary<string, JsonElement>
                    {
                        ["CustomBundle"] = JsonSerializer.Deserialize<JsonElement>("\"bundle-extra\"")
                    },
                    AssetInfos =
                    [
                        Asset("Hero", 7, heroMemo, 42),
                        Asset("Other", 8, otherMemo, 84),
                        .. duplicateHero ? [Asset("Hero", 7, heroMemo, 126)] : Array.Empty<PackageAssetReference>()
                    ]
                }
            ])
        ],
        packagePath);

    private static PackageAssetReference Asset(string name, long pathId, string? memo, int custom) => new()
    {
        Name = name,
        PathId = pathId,
        Memo = memo,
        ExtensionData = new Dictionary<string, JsonElement>
        {
            ["CustomAsset"] = JsonSerializer.Deserialize<JsonElement>(custom.ToString())
        }
    };

    private static PackageMemoIdentity Identity(PackageDocument package, string name, long pathId)
    {
        PackageIndex index = package.Indexes.Single();
        PackageBundleReference source = index.Bundles.Single();
        return new(package.PackagePath, index.RelativeDirectory, source.Path, name, pathId);
    }

    private static PackageAssetReference Find(PackageDocument package, string name) =>
        package.Bundles.SelectMany(source => source.AssetInfos).Single(asset => asset.Name == name);

    private sealed class TrackingCoordinator : IPackageWriteCoordinator
    {
        private readonly PackageWriteCoordinator _inner = new();
        public bool IsExecuting { get; private set; }

        public Task ExecutePackageWriteAsync(string packagePath, Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            ExecutePackageWriteAsync(packagePath, async token => { await operation(token); return true; }, cancellationToken);

        public Task<TResult> ExecutePackageWriteAsync<TResult>(string packagePath, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) =>
            _inner.ExecutePackageWriteAsync(packagePath, async token =>
            {
                IsExecuting = true;
                try { return await operation(token); }
                finally { IsExecuting = false; }
            }, cancellationToken);
    }

    private class DelegatingPackageService(IPackageService inner) : IPackageService
    {
        protected IPackageService Inner { get; } = inner;
        public virtual Task<PackageDocument> LoadAsync(string packagePath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => Inner.LoadAsync(packagePath, progress, cancellationToken);
        public virtual Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => Inner.SaveAsync(package, targetPath, overwrite, progress, cancellationToken);
        public Task<PackageDocument> CreateAsync(string sourceRoot, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => Inner.CreateAsync(sourceRoot, progress, cancellationToken);
        public Task<PackageBundleReference> CreateReferenceAsync(string sourcePath, IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null, CancellationToken cancellationToken = default) => Inner.CreateReferenceAsync(sourcePath, selectedAssets, cancellationToken);
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => Inner.CreateReferencesFromFolderAsync(folderPath, progress, cancellationToken);
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, PackageFolderSearchMode searchMode, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => Inner.CreateReferencesFromFolderAsync(folderPath, searchMode, progress, cancellationToken);
        public Task<PackageDocument> BuildAndSaveAsync(string sourceRoot, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => Inner.BuildAndSaveAsync(sourceRoot, targetPath, overwrite, progress, cancellationToken);
        public Task<IReadOnlyList<PackageSummary>> ListAsync(string packageParentPath, CancellationToken cancellationToken = default) => Inner.ListAsync(packageParentPath, cancellationToken);
        public Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(string packageParentPath, CancellationToken cancellationToken = default) => Inner.ReadCatalogAsync(packageParentPath, cancellationToken);
    }

    private sealed class MutatingLoadPackageService(IPackageService inner, string packagePath) : DelegatingPackageService(inner)
    {
        private int _loads;
        public int SaveCalls { get; private set; }
        public override async Task<PackageDocument> LoadAsync(string path, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            PackageDocument document = await base.LoadAsync(path, progress, cancellationToken);
            if (Interlocked.Increment(ref _loads) == 1)
                await File.WriteAllTextAsync(Path.Combine(packagePath, PackageService.ValidationFileName), "external", cancellationToken);
            return document;
        }
        public override Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return base.SaveAsync(package, targetPath, overwrite, progress, cancellationToken);
        }
    }

    private sealed class FailingSavePackageService(IPackageService inner) : DelegatingPackageService(inner)
    {
        public override Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) =>
            throw new IOException("simulated writer failure");
    }

    private sealed class TamperingVerificationPackageService(IPackageService inner) : DelegatingPackageService(inner)
    {
        private int _loads;
        public override async Task<PackageDocument> LoadAsync(string packagePath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            PackageDocument document = await base.LoadAsync(packagePath, progress, cancellationToken);
            if (Interlocked.Increment(ref _loads) > 1)
                Find(document, "Hero").Memo = "tampered verification";
            return document;
        }
    }

    private sealed class ConcurrentSavePackageService(IPackageService inner) : DelegatingPackageService(inner)
    {
        private readonly TaskCompletionSource _twoEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _active;
        private int _entered;
        public int MaximumConcurrentSaves { get; private set; }
        public override async Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            int active = Interlocked.Increment(ref _active);
            MaximumConcurrentSaves = Math.Max(MaximumConcurrentSaves, active);
            if (Interlocked.Increment(ref _entered) == 2)
                _twoEntered.SetResult();
            await _twoEntered.Task.WaitAsync(cancellationToken);
            try { await base.SaveAsync(package, targetPath, overwrite, progress, cancellationToken); }
            finally { Interlocked.Decrement(ref _active); }
        }
    }

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }

    private sealed class AlwaysConfirm : IUserConfirmationService
    {
        public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"uabs-memo-persistence-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
