using System.Threading.Channels;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.UnityAssets.Tests;

public sealed class MemoDraftCoordinatorTests
{
    [Fact]
    public async Task Debounce_UsesTransactionalPersistenceAndReloadsFromDisposablePackage()
    {
        string root = Path.Combine(Path.GetTempPath(), $"uabs-memo-draft-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            PackageService packages = new(TestPaths.ClassData);
            string packagePath = Path.Combine(root, "Package");
            PackageDocument document = new([new PackageIndex(string.Empty, [new PackageBundleReference
            {
                Name = "source.bundle",
                Path = Path.Combine(root, "source.bundle"),
                CabCode = "CAB-TEST",
                AssetInfos = [new PackageAssetReference { Name = "Hero", PathId = 1, Memo = "old" }]
            }])]);
            await packages.SaveAsync(document, packagePath);
            PackageDocument loaded = await packages.LoadAsync(packagePath);
            PackageMemoIdentity identity = new(packagePath, string.Empty, Path.Combine(root, "source.bundle"), "Hero", 1);
            PackageMemoPersistenceService persistence = new(packages, new PackageWriteCoordinator());
            string revision = (await persistence.CaptureRevisionAsync(packagePath))!;
            ManualDelay delay = new();
            await using MemoDraftCoordinator coordinator = new(persistence, delay);
            TaskCompletionSource<MemoDraftSnapshot> saved = new(TaskCreationOptions.RunContinuationsAsynchronously);
            coordinator.DraftChanged += (_, args) =>
            {
                if (args.Draft.State == MemoDraftSaveState.Saved && args.Draft.DraftRevision > 0)
                    saved.TrySetResult(args.Draft);
            };
            coordinator.Track(identity, "old", revision);

            coordinator.Edit(identity, "persisted through Stage 3");
            (await delay.NextAsync()).Complete();
            MemoDraftSnapshot completed = await saved.Task.WaitAsync(TimeSpan.FromSeconds(5));
            PackageDocument reopened = await packages.LoadAsync(packagePath);

            Assert.Equal(MemoDraftSaveState.Saved, completed.State);
            Assert.Equal(
                "persisted through Stage 3",
                reopened.Bundles.Single().AssetInfos.Single().Memo);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Edit_SavesAfterExactDebounceAndPublishesObservableStates()
    {
        RecordingPersistence persistence = new();
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity identity = Identity("Package", "Hero", 1);
        List<MemoDraftSaveState> states = [];
        coordinator.DraftChanged += (_, args) => states.Add(args.Draft.State);
        coordinator.Track(identity, "old", "revision-0");

        MemoDraftSnapshot edited = coordinator.Edit(identity, "new");
        ManualDelay.Pending pending = await delay.NextAsync();

        Assert.Equal(MemoDraftCoordinator.DefaultDebounceDelay, pending.Delay);
        Assert.Equal(MemoDraftSaveState.Unsaved, edited.State);
        Assert.Equal(0, persistence.CallCount);

        pending.Complete();
        PackageMemoPersistenceRequest request = await persistence.NextRequestAsync();
        await persistence.WaitForCompletionAsync(1);

        Assert.Equal(identity, request.Identity);
        Assert.Equal("new", request.DesiredMemo);
        Assert.Equal("old", request.ExpectedOriginalMemo);
        Assert.Equal("revision-0", request.ExpectedPackageRevision);
        Assert.Equal(MemoDraftSaveState.Saved, coordinator.GetSnapshot(identity).State);
        Assert.Contains(MemoDraftSaveState.Unsaved, states);
        Assert.Contains(MemoDraftSaveState.Saving, states);
        Assert.Equal(MemoDraftSaveState.Saved, states[^1]);
    }

    [Fact]
    public async Task RepeatedTyping_ResetsDelayAndCoalescesToLatestRevision()
    {
        RecordingPersistence persistence = new();
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity identity = Identity("Package", "Hero", 1);
        coordinator.Track(identity, "old", "revision-0");

        coordinator.Edit(identity, "n");
        ManualDelay.Pending first = await delay.NextAsync();
        coordinator.Edit(identity, "ne");
        ManualDelay.Pending second = await delay.NextAsync();
        coordinator.Edit(identity, "new");
        ManualDelay.Pending third = await delay.NextAsync();

        Assert.True(first.IsCancelled);
        Assert.True(second.IsCancelled);
        third.Complete();
        PackageMemoPersistenceRequest request = await persistence.NextRequestAsync();
        await persistence.WaitForCompletionAsync(1);

        Assert.Equal("new", request.DesiredMemo);
        Assert.Equal(1, persistence.CallCount);
        Assert.Equal(3, coordinator.GetSnapshot(identity).DraftRevision);
    }

    [Fact]
    public async Task NoOpEdit_DoesNotScheduleOrWrite()
    {
        RecordingPersistence persistence = new();
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity identity = Identity("Package", "Hero", 1);
        coordinator.Track(identity, null, "revision-0");

        MemoDraftSnapshot snapshot = coordinator.Edit(identity, null);
        MemoDraftSnapshot flushed = await coordinator.FlushForNavigationAsync(identity);

        Assert.Equal(MemoDraftSaveState.Saved, snapshot.State);
        Assert.Equal(MemoDraftSaveState.Saved, flushed.State);
        Assert.Equal(0, delay.RequestCount);
        Assert.Equal(0, persistence.CallCount);
    }

    [Fact]
    public async Task SaveNow_BypassesDelayAndEquivalentOverlapWritesOnce()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RecordingPersistence persistence = new(async (request, _) =>
        {
            await release.Task;
            return Saved(request, "revision-1");
        });
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity identity = Identity("Package", "Hero", 1);
        coordinator.Track(identity, "old", "revision-0");
        coordinator.Edit(identity, "new");
        ManualDelay.Pending pending = await delay.NextAsync();

        Task<MemoDraftSnapshot> first = coordinator.SaveNowAsync(identity);
        await persistence.NextRequestAsync();
        Task<MemoDraftSnapshot> overlapping = coordinator.SaveNowAsync(identity);
        release.SetResult();

        MemoDraftSnapshot[] results = await Task.WhenAll(first, overlapping);
        Assert.True(pending.IsCancelled);
        Assert.Equal(1, persistence.CallCount);
        Assert.All(results, result => Assert.Equal(MemoDraftSaveState.Saved, result.State));
    }

    [Fact]
    public async Task FailureAndConflict_PreserveDraftUntilExplicitRetry()
    {
        int attempt = 0;
        RecordingPersistence persistence = new((request, _) => Task.FromResult(
            ++attempt == 1
                ? Failure(request, PackageMemoPersistenceStatus.RevisionConflict, "Package changed.")
                : Saved(request, "revision-2")));
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity identity = Identity("Package", "Hero", 1);
        coordinator.Track(identity, "old", "revision-0");
        coordinator.Edit(identity, "draft");

        MemoDraftSnapshot failed = await coordinator.SaveNowAsync(identity);
        MemoDraftSnapshot retainedAfterNavigation = await coordinator.FlushForNavigationAsync(identity);

        Assert.Equal(MemoDraftSaveState.Failed, failed.State);
        Assert.Equal(PackageMemoPersistenceStatus.RevisionConflict, failed.LastPersistenceStatus);
        Assert.Equal("draft", failed.DraftMemo);
        Assert.Equal(MemoDraftSaveState.Failed, retainedAfterNavigation.State);
        Assert.Equal(1, persistence.CallCount);

        MemoDraftSnapshot retried = await coordinator.RetryAsync(identity);
        Assert.Equal(MemoDraftSaveState.Saved, retried.State);
        Assert.Equal("draft", retried.ConfirmedMemo);
        Assert.Equal(2, persistence.CallCount);
    }

    [Fact]
    public async Task NewEditDuringSave_RemainsUnsavedAndOldCompletionCannotClearIt()
    {
        TaskCompletionSource<PackageMemoPersistenceResult> firstResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int call = 0;
        RecordingPersistence persistence = new((request, _) =>
        {
            if (Interlocked.Increment(ref call) == 1)
                return firstResult.Task;
            return Task.FromResult(Saved(request, "revision-2"));
        });
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity identity = Identity("Package", "Hero", 1);
        coordinator.Track(identity, "old", "revision-0");
        coordinator.Edit(identity, "first");
        ManualDelay.Pending firstDelay = await delay.NextAsync();
        Task<MemoDraftSnapshot> saving = coordinator.SaveNowAsync(identity);
        PackageMemoPersistenceRequest firstRequest = await persistence.NextRequestAsync();
        Assert.True(firstDelay.IsCancelled);

        coordinator.Edit(identity, "second");
        ManualDelay.Pending secondDelay = await delay.NextAsync();
        firstResult.SetResult(Saved(firstRequest, "revision-1"));
        await saving;

        MemoDraftSnapshot afterOldCompletion = coordinator.GetSnapshot(identity);
        Assert.Equal(MemoDraftSaveState.Unsaved, afterOldCompletion.State);
        Assert.Equal("second", afterOldCompletion.DraftMemo);
        Assert.Equal("first", afterOldCompletion.ConfirmedMemo);

        secondDelay.Complete();
        await persistence.NextRequestAsync();
        await persistence.WaitForCompletionAsync(2);
        Assert.Equal(MemoDraftSaveState.Saved, coordinator.GetSnapshot(identity).State);
        Assert.Equal("second", coordinator.GetSnapshot(identity).ConfirmedMemo);
    }

    [Fact]
    public async Task NavigationFlush_UsesPreviousExactIdentityAndDoesNotRetargetNewSelection()
    {
        RecordingPersistence persistence = new();
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity first = Identity("Package", "Hero", 1);
        PackageMemoIdentity second = Identity("Package", "Hero", 2);
        coordinator.Track(first, "one", "revision-0");
        coordinator.Track(second, "two", "revision-0");
        coordinator.Edit(first, "changed one");

        Task<MemoDraftSnapshot> flush = coordinator.FlushForNavigationAsync(first);
        coordinator.Edit(second, "changed two");
        PackageMemoPersistenceRequest request = await persistence.NextRequestAsync();
        await flush;

        Assert.Equal(first, request.Identity);
        Assert.Equal("changed one", request.DesiredMemo);
        Assert.Equal(MemoDraftSaveState.Saved, coordinator.GetSnapshot(first).State);
        Assert.Equal(MemoDraftSaveState.Unsaved, coordinator.GetSnapshot(second).State);
    }

    [Fact]
    public async Task MultipleDrafts_SamePackageKeepIdentityAndDifferentPackagesCanPersistConcurrently()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int active = 0;
        int maximumActive = 0;
        RecordingPersistence persistence = new(async (request, _) =>
        {
            int now = Interlocked.Increment(ref active);
            maximumActive = Math.Max(maximumActive, now);
            await release.Task;
            Interlocked.Decrement(ref active);
            return Saved(request, $"revision-{request.Identity.PathId}");
        });
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity first = Identity("Package-A", "Hero", 1);
        PackageMemoIdentity samePackage = Identity("Package-A", "Villain", 2);
        PackageMemoIdentity otherPackage = Identity("Package-B", "Hero", 1);
        foreach (PackageMemoIdentity identity in new[] { first, samePackage, otherPackage })
        {
            coordinator.Track(identity, "old", "revision-0");
            coordinator.Edit(identity, $"new-{identity.AssetName}-{identity.PathId}");
        }

        Task<MemoDraftSnapshot>[] saves =
        [
            coordinator.SaveNowAsync(first),
            coordinator.SaveNowAsync(samePackage),
            coordinator.SaveNowAsync(otherPackage)
        ];
        PackageMemoPersistenceRequest[] requests =
        [
            await persistence.NextRequestAsync(),
            await persistence.NextRequestAsync(),
            await persistence.NextRequestAsync()
        ];
        release.SetResult();
        await Task.WhenAll(saves);

        Assert.Equal(3, requests.Select(request => request.Identity).Distinct().Count());
        Assert.True(maximumActive >= 2);
        Assert.All(saves, task => Assert.Equal(MemoDraftSaveState.Saved, task.Result.State));
    }

    [Fact]
    public async Task EditorBlockedOutcome_IsActionableAndEditingFailedDraftReschedules()
    {
        int attempt = 0;
        RecordingPersistence persistence = new((request, _) => Task.FromResult(
            ++attempt == 1
                ? Failure(request, PackageMemoPersistenceStatus.PackageEditorActive, "Close Package Editor first.")
                : Saved(request, "revision-1")));
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity identity = Identity("Package", "Hero", 1);
        coordinator.Track(identity, "old", "revision-0");
        coordinator.Edit(identity, "blocked");
        ManualDelay.Pending blockedDelay = await delay.NextAsync();
        MemoDraftSnapshot blocked = await coordinator.SaveNowAsync(identity);

        Assert.True(blockedDelay.IsCancelled);
        Assert.Equal(MemoDraftSaveState.Failed, blocked.State);
        Assert.Equal(PackageMemoPersistenceStatus.PackageEditorActive, blocked.LastPersistenceStatus);
        Assert.Contains("Close Package Editor", blocked.Error);

        MemoDraftSnapshot edited = coordinator.Edit(identity, "retry draft");
        ManualDelay.Pending pending = await delay.NextAsync();
        Assert.Equal(MemoDraftSaveState.Unsaved, edited.State);
        Assert.Null(edited.Error);
        pending.Complete();
        await persistence.NextRequestAsync();
        await persistence.WaitForCompletionAsync(2);
        Assert.Equal(MemoDraftSaveState.Saved, coordinator.GetSnapshot(identity).State);
    }

    [Fact]
    public async Task ShutdownDrain_WaitsForInflightFlushesPendingAndReportsFailures()
    {
        TaskCompletionSource releaseActive = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PackageMemoIdentity first = Identity("Package-A", "Hero", 1);
        PackageMemoIdentity second = Identity("Package-B", "Hero", 2);
        RecordingPersistence persistence = new(async (request, _) =>
        {
            if (request.Identity == first)
            {
                await releaseActive.Task;
                return Saved(request, "revision-1");
            }
            return Failure(request, PackageMemoPersistenceStatus.PackageMissing, "Package removed.");
        });
        ManualDelay delay = new();
        await using MemoDraftCoordinator coordinator = new(persistence, delay);
        coordinator.Track(first, "old", "revision-0");
        coordinator.Track(second, "old", "revision-0");
        coordinator.Edit(first, "first draft");
        coordinator.Edit(second, "second draft");
        Task<MemoDraftSnapshot> active = coordinator.SaveNowAsync(first);
        await persistence.NextRequestAsync();

        Task<MemoDraftDrainResult> drain = coordinator.DrainForShutdownAsync();
        PackageMemoPersistenceRequest drainedRequest = await persistence.NextRequestAsync();
        Assert.Equal(second, drainedRequest.Identity);
        Assert.False(drain.IsCompleted);
        releaseActive.SetResult();

        MemoDraftDrainResult result = await drain;
        await active;
        Assert.False(result.AllSaved);
        Assert.Single(result.Failures);
        Assert.Equal(second, result.Failures[0].Identity);
        Assert.Equal("second draft", result.Failures[0].DraftMemo);
        Assert.Throws<InvalidOperationException>(() => coordinator.Edit(first, "late edit"));
    }

    [Fact]
    public async Task DisposeAsync_CancelsPendingTimerAndDoesNotWrite()
    {
        RecordingPersistence persistence = new();
        ManualDelay delay = new();
        MemoDraftCoordinator coordinator = new(persistence, delay);
        PackageMemoIdentity identity = Identity("Package", "Hero", 1);
        coordinator.Track(identity, "old", "revision-0");
        coordinator.Edit(identity, "new");
        ManualDelay.Pending pending = await delay.NextAsync();

        await coordinator.DisposeAsync();

        Assert.True(pending.IsCancelled);
        Assert.Equal(0, persistence.CallCount);
        Assert.Throws<ObjectDisposedException>(() => coordinator.Edit(identity, "later"));
    }

    private static PackageMemoIdentity Identity(string package, string asset, long pathId) => new(
        Path.Combine(Path.GetTempPath(), package),
        "index",
        Path.Combine(Path.GetTempPath(), package, "source.bundle"),
        asset,
        pathId);

    private static PackageMemoPersistenceResult Saved(
        PackageMemoPersistenceRequest request,
        string revision) => new(
        PackageMemoPersistenceStatus.Saved,
        request,
        "Saved.",
        revision);

    private static PackageMemoPersistenceResult Failure(
        PackageMemoPersistenceRequest request,
        PackageMemoPersistenceStatus status,
        string diagnostic) => new(status, request, diagnostic);

    private sealed class RecordingPersistence : IPackageMemoPersistenceService
    {
        private readonly Func<PackageMemoPersistenceRequest, CancellationToken, Task<PackageMemoPersistenceResult>> _handler;
        private readonly Channel<PackageMemoPersistenceRequest> _requests = Channel.CreateUnbounded<PackageMemoPersistenceRequest>();
        private readonly object _sync = new();
        private readonly List<TaskCompletionSource> _completionWaiters = [];
        private int _callCount;
        private int _completionCount;

        public RecordingPersistence(
            Func<PackageMemoPersistenceRequest, CancellationToken, Task<PackageMemoPersistenceResult>>? handler = null)
        {
            _handler = handler ?? ((request, _) => Task.FromResult(Saved(request, $"revision-{CallCount}")));
        }

        public int CallCount => Volatile.Read(ref _callCount);

        public Task<string?> CaptureRevisionAsync(string packagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("revision-0");

        public async Task<PackageMemoPersistenceResult> PersistAsync(
            PackageMemoPersistenceRequest request,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            _requests.Writer.TryWrite(request);
            try
            {
                return await _handler(request, cancellationToken);
            }
            finally
            {
                List<TaskCompletionSource> ready;
                lock (_sync)
                {
                    _completionCount++;
                    ready = _completionWaiters.ToList();
                    _completionWaiters.Clear();
                }
                foreach (TaskCompletionSource waiter in ready)
                    waiter.TrySetResult();
            }
        }

        public ValueTask<PackageMemoPersistenceRequest> NextRequestAsync() =>
            _requests.Reader.ReadAsync();

        public Task WaitForCompletionAsync(int count)
        {
            lock (_sync)
            {
                if (_completionCount >= count)
                    return Task.CompletedTask;
                TaskCompletionSource waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _completionWaiters.Add(waiter);
                return WaitUntilCountAsync(waiter.Task, count);
            }
        }

        private async Task WaitUntilCountAsync(Task signal, int count)
        {
            while (true)
            {
                await signal;
                lock (_sync)
                {
                    if (_completionCount >= count)
                        return;
                    TaskCompletionSource waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    _completionWaiters.Add(waiter);
                    signal = waiter.Task;
                }
            }
        }
    }

    private sealed class ManualDelay : IMemoDraftDelay
    {
        private readonly Channel<Pending> _requests = Channel.CreateUnbounded<Pending>();
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Pending pending = new(delay, cancellationToken);
            Interlocked.Increment(ref _requestCount);
            _requests.Writer.TryWrite(pending);
            return pending.Task;
        }

        public ValueTask<Pending> NextAsync() => _requests.Reader.ReadAsync();

        public sealed class Pending
        {
            private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly CancellationTokenRegistration _registration;

            public Pending(TimeSpan delay, CancellationToken cancellationToken)
            {
                Delay = delay;
                _registration = cancellationToken.Register(() => _completion.TrySetCanceled(cancellationToken));
            }

            public TimeSpan Delay { get; }
            public Task Task => _completion.Task;
            public bool IsCancelled => Task.IsCanceled;

            public void Complete()
            {
                _registration.Dispose();
                _completion.TrySetResult();
            }
        }
    }
}
