namespace UABS.UnityAssets.Packages.Memos;

public enum MemoDraftSaveState
{
    Unavailable,
    Saved,
    Unsaved,
    Saving,
    Failed
}

public sealed record MemoDraftSnapshot(
    PackageMemoIdentity? Identity,
    string? DraftMemo,
    string? ConfirmedMemo,
    string? ConfirmedPackageRevision,
    long DraftRevision,
    MemoDraftSaveState State,
    PackageMemoPersistenceStatus? LastPersistenceStatus = null,
    string? Error = null)
{
    public static MemoDraftSnapshot Unavailable { get; } = new(
        null,
        null,
        null,
        null,
        0,
        MemoDraftSaveState.Unavailable);

    public bool HasPendingChanges => State is MemoDraftSaveState.Unsaved
        or MemoDraftSaveState.Saving
        or MemoDraftSaveState.Failed;
}

public sealed class MemoDraftChangedEventArgs(MemoDraftSnapshot draft) : EventArgs
{
    public MemoDraftSnapshot Draft { get; } = draft;
}

public sealed record MemoDraftDrainResult(IReadOnlyList<MemoDraftSnapshot> Drafts)
{
    public bool AllSaved => Drafts.All(draft => draft.State == MemoDraftSaveState.Saved);

    public IReadOnlyList<MemoDraftSnapshot> Failures => Drafts
        .Where(draft => draft.State == MemoDraftSaveState.Failed)
        .ToArray();
}

public interface IMemoDraftDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public interface IMemoDraftCoordinator
{
    event EventHandler<MemoDraftChangedEventArgs>? DraftChanged;

    MemoDraftSnapshot Track(
        PackageMemoIdentity identity,
        string? persistedMemo,
        string? packageRevision);

    Task<MemoDraftSnapshot> TrackAsync(
        PackageMemoIdentity identity,
        string? persistedMemo,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Track(identity, persistedMemo, packageRevision: null));

    MemoDraftSnapshot Edit(PackageMemoIdentity identity, string? memo);

    MemoDraftSnapshot GetSnapshot(PackageMemoIdentity? identity);

    Task<MemoDraftSnapshot> SaveNowAsync(
        PackageMemoIdentity identity,
        CancellationToken cancellationToken = default);

    Task<MemoDraftSnapshot> RetryAsync(
        PackageMemoIdentity identity,
        CancellationToken cancellationToken = default);

    Task<MemoDraftSnapshot> FlushForNavigationAsync(
        PackageMemoIdentity identity,
        CancellationToken cancellationToken = default);

    Task<MemoDraftDrainResult> DrainForShutdownAsync(CancellationToken cancellationToken = default);
}

public sealed class MemoDraftCoordinator : IMemoDraftCoordinator, IDisposable, IAsyncDisposable
{
    public static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromSeconds(1.5);

    private readonly object _sync = new();
    private readonly IPackageMemoPersistenceService _persistence;
    private readonly IMemoDraftDelay _delay;
    private readonly TimeSpan _debounceDelay;
    private readonly Dictionary<MemoDraftKey, DraftEntry> _drafts = [];
    private readonly HashSet<Task> _activeOperations = [];
    private bool _isShuttingDown;
    private bool _disposed;

    public MemoDraftCoordinator(
        IPackageMemoPersistenceService persistence,
        IMemoDraftDelay? delay = null,
        TimeSpan? debounceDelay = null)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _delay = delay ?? new SystemMemoDraftDelay();
        _debounceDelay = debounceDelay ?? DefaultDebounceDelay;
        if (_debounceDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(debounceDelay));
    }

    public event EventHandler<MemoDraftChangedEventArgs>? DraftChanged;

    public async Task<MemoDraftSnapshot> TrackAsync(
        PackageMemoIdentity identity,
        string? persistedMemo,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);
        MemoDraftSnapshot observed = GetSnapshot(identity);
        string? packageRevision = await _persistence.CaptureRevisionAsync(
            identity.PackagePath!, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return packageRevision is null
            ? MemoDraftSnapshot.Unavailable
            : TrackCore(identity, persistedMemo, packageRevision, observed);
    }

    public MemoDraftSnapshot Track(
        PackageMemoIdentity identity,
        string? persistedMemo,
        string? packageRevision) => TrackCore(identity, persistedMemo, packageRevision);

    private MemoDraftSnapshot TrackCore(
        PackageMemoIdentity identity,
        string? persistedMemo,
        string? packageRevision,
        MemoDraftSnapshot? observed = null)
    {
        ValidateIdentity(identity);
        MemoDraftSnapshot snapshot;
        bool changed = false;
        lock (_sync)
        {
            ThrowIfUnavailableForEdits();
            MemoDraftKey key = MemoDraftKey.Create(identity);
            if (!_drafts.TryGetValue(key, out DraftEntry? entry))
            {
                entry = new DraftEntry(identity, persistedMemo, packageRevision);
                _drafts.Add(key, entry);
                changed = true;
            }
            else if (observed is not null && entry.ToSnapshot() != observed)
            {
                // A save/edit or another Inspector advanced this identity while revision
                // capture awaited I/O. Its older catalog value must not reset that state.
                return entry.ToSnapshot();
            }
            else if (entry.State == MemoDraftSaveState.Saved
                     && (!string.Equals(entry.ConfirmedMemo, persistedMemo, StringComparison.Ordinal)
                         || !string.Equals(entry.ConfirmedPackageRevision, packageRevision, StringComparison.Ordinal)))
            {
                entry.Identity = identity;
                entry.DraftMemo = persistedMemo;
                entry.ConfirmedMemo = persistedMemo;
                entry.ConfirmedPackageRevision = packageRevision;
                entry.DraftRevision++;
                entry.LastPersistenceStatus = null;
                entry.Error = null;
                changed = true;
            }

            snapshot = entry.ToSnapshot();
        }

        if (changed)
            Publish(snapshot);
        return snapshot;
    }

    public MemoDraftSnapshot Edit(PackageMemoIdentity identity, string? memo)
    {
        ValidateIdentity(identity);
        MemoDraftSnapshot snapshot;
        lock (_sync)
        {
            ThrowIfUnavailableForEdits();
            DraftEntry entry = GetRequiredEntry(identity);
            if (string.Equals(entry.DraftMemo, memo, StringComparison.Ordinal))
                return entry.ToSnapshot();

            entry.DraftMemo = memo;
            entry.DraftRevision++;
            entry.LastPersistenceStatus = null;
            entry.Error = null;
            CancelDebounce(entry);
            if (string.Equals(entry.DraftMemo, entry.ConfirmedMemo, StringComparison.Ordinal))
            {
                entry.State = MemoDraftSaveState.Saved;
            }
            else
            {
                entry.State = MemoDraftSaveState.Unsaved;
                ScheduleDebounce(MemoDraftKey.Create(identity), entry);
            }

            snapshot = entry.ToSnapshot();
        }

        Publish(snapshot);
        return snapshot;
    }

    public MemoDraftSnapshot GetSnapshot(PackageMemoIdentity? identity)
    {
        if (identity is null)
            return MemoDraftSnapshot.Unavailable;

        ValidateIdentity(identity);
        lock (_sync)
        {
            return _drafts.TryGetValue(MemoDraftKey.Create(identity), out DraftEntry? entry)
                ? entry.ToSnapshot()
                : MemoDraftSnapshot.Unavailable;
        }
    }

    public Task<MemoDraftSnapshot> SaveNowAsync(
        PackageMemoIdentity identity,
        CancellationToken cancellationToken = default) =>
        StartSave(identity, cancelDebounce: true, onlyIfFailed: false, saveFailed: true, cancellationToken);

    public Task<MemoDraftSnapshot> RetryAsync(
        PackageMemoIdentity identity,
        CancellationToken cancellationToken = default) =>
        StartSave(identity, cancelDebounce: true, onlyIfFailed: true, saveFailed: true, cancellationToken);

    public Task<MemoDraftSnapshot> FlushForNavigationAsync(
        PackageMemoIdentity identity,
        CancellationToken cancellationToken = default) =>
        StartSave(identity, cancelDebounce: true, onlyIfFailed: false, saveFailed: false, cancellationToken);

    public async Task<MemoDraftDrainResult> DrainForShutdownAsync(CancellationToken cancellationToken = default)
    {
        PackageMemoIdentity[] identities;
        Task[] workToDrain;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _isShuttingDown = true;
            foreach (DraftEntry entry in _drafts.Values)
                CancelDebounce(entry);
            identities = _drafts.Values
                .Where(entry => !string.Equals(entry.DraftMemo, entry.ConfirmedMemo, StringComparison.Ordinal))
                .Select(entry => entry.Identity)
                .ToArray();
            workToDrain = _activeOperations
                .Concat(_drafts.Values
                    .Select(entry => entry.DebounceTask)
                    .Where(task => task is not null)
                    .Cast<Task>())
                .Distinct()
                .ToArray();
        }

        Task<MemoDraftSnapshot>[] flushes = identities
            .Select(identity => StartSave(
                identity,
                cancelDebounce: false,
                onlyIfFailed: false,
                saveFailed: true,
                cancellationToken,
                allowDuringShutdown: true))
            .ToArray();

        await AwaitAllWithoutShortCircuiting(workToDrain, cancellationToken).ConfigureAwait(false);
        await AwaitAllWithoutShortCircuiting(flushes, cancellationToken).ConfigureAwait(false);

        lock (_sync)
        {
            return new MemoDraftDrainResult(_drafts.Values
                .Select(entry => entry.ToSnapshot())
                .ToArray());
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            _isShuttingDown = true;
            foreach (DraftEntry entry in _drafts.Values)
                CancelDebounce(entry);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task[] active;
        lock (_sync)
        {
            if (!_disposed)
            {
                _disposed = true;
                _isShuttingDown = true;
                foreach (DraftEntry entry in _drafts.Values)
                    CancelDebounce(entry);
            }
            active = _activeOperations
                .Concat(_drafts.Values
                    .Select(entry => entry.DebounceTask)
                    .Where(task => task is not null)
                    .Cast<Task>())
                .Distinct()
                .ToArray();
        }

        await AwaitAllWithoutShortCircuiting(active, CancellationToken.None).ConfigureAwait(false);
    }

    private Task<MemoDraftSnapshot> StartSave(
        PackageMemoIdentity identity,
        bool cancelDebounce,
        bool onlyIfFailed,
        bool saveFailed,
        CancellationToken cancellationToken,
        bool allowDuringShutdown = false)
    {
        ValidateIdentity(identity);
        Task<MemoDraftSnapshot> operation;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_isShuttingDown && !allowDuringShutdown)
                throw new InvalidOperationException("Memo draft shutdown draining has started.");

            DraftEntry entry = GetRequiredEntry(identity);
            if (onlyIfFailed && entry.State != MemoDraftSaveState.Failed)
                return Task.FromResult(entry.ToSnapshot());
            if (!saveFailed && entry.State == MemoDraftSaveState.Failed)
                return Task.FromResult(entry.ToSnapshot());
            if (cancelDebounce)
                CancelDebounce(entry);

            operation = SaveCoreAsync(entry, cancellationToken);
            TrackOperation(operation);
        }

        return operation;
    }

    private async Task<MemoDraftSnapshot> SaveCoreAsync(
        DraftEntry entry,
        CancellationToken cancellationToken)
    {
        await entry.SaveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            PackageMemoPersistenceRequest request;
            long savingRevision;
            MemoDraftSnapshot? savingSnapshot = null;
            lock (_sync)
            {
                if (string.Equals(entry.DraftMemo, entry.ConfirmedMemo, StringComparison.Ordinal))
                {
                    if (entry.State != MemoDraftSaveState.Saving)
                        entry.State = MemoDraftSaveState.Saved;
                    return entry.ToSnapshot();
                }

                savingRevision = entry.DraftRevision;
                request = new PackageMemoPersistenceRequest(
                    entry.Identity,
                    entry.DraftMemo,
                    entry.ConfirmedMemo,
                    entry.ConfirmedPackageRevision);
                entry.State = MemoDraftSaveState.Saving;
                entry.LastPersistenceStatus = null;
                entry.Error = null;
                savingSnapshot = entry.ToSnapshot();
            }

            Publish(savingSnapshot);
            PackageMemoPersistenceResult result;
            try
            {
                result = await _persistence.PersistAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                MemoDraftSnapshot cancelled;
                lock (_sync)
                {
                    if (entry.DraftRevision == savingRevision)
                        entry.State = MemoDraftSaveState.Unsaved;
                    cancelled = entry.ToSnapshot();
                }
                Publish(cancelled);
                return cancelled;
            }
            catch (Exception exception)
            {
                result = new PackageMemoPersistenceResult(
                    PackageMemoPersistenceStatus.WriteFailure,
                    request,
                    $"Memo persistence failed unexpectedly: {exception.Message}");
            }

            MemoDraftSnapshot completed;
            lock (_sync)
            {
                if (result.IsSuccess)
                {
                    entry.ConfirmedMemo = request.DesiredMemo;
                    entry.ConfirmedPackageRevision = result.PackageRevision ?? entry.ConfirmedPackageRevision;
                    entry.LastPersistenceStatus = result.Status;
                    entry.Error = null;
                    entry.State = entry.DraftRevision == savingRevision
                                  && string.Equals(entry.DraftMemo, request.DesiredMemo, StringComparison.Ordinal)
                        ? MemoDraftSaveState.Saved
                        : MemoDraftSaveState.Unsaved;
                }
                else if (entry.DraftRevision == savingRevision)
                {
                    if (result.Status == PackageMemoPersistenceStatus.CatalogPublicationFailure
                        && result.PersistedPackage is not null)
                    {
                        entry.ConfirmedMemo = request.DesiredMemo;
                        entry.ConfirmedPackageRevision = result.PackageRevision ?? entry.ConfirmedPackageRevision;
                    }

                    entry.State = MemoDraftSaveState.Failed;
                    entry.LastPersistenceStatus = result.Status;
                    entry.Error = result.Diagnostic;
                }

                completed = entry.ToSnapshot();
            }

            Publish(completed);
            return completed;
        }
        finally
        {
            entry.SaveGate.Release();
        }
    }

    private void ScheduleDebounce(MemoDraftKey key, DraftEntry entry)
    {
        if (_isShuttingDown || _disposed)
            return;

        CancellationTokenSource cancellation = new();
        entry.DebounceCancellation = cancellation;
        long scheduledRevision = entry.DraftRevision;
        entry.DebounceTask = RunDebounceAsync(key, entry, scheduledRevision, cancellation);
    }

    private async Task RunDebounceAsync(
        MemoDraftKey key,
        DraftEntry entry,
        long scheduledRevision,
        CancellationTokenSource cancellation)
    {
        try
        {
            await _delay.DelayAsync(_debounceDelay, cancellation.Token).ConfigureAwait(false);
            cancellation.Token.ThrowIfCancellationRequested();
            PackageMemoIdentity? identity = null;
            lock (_sync)
            {
                if (!_disposed
                    && !_isShuttingDown
                    && entry.DraftRevision == scheduledRevision
                    && entry.State == MemoDraftSaveState.Unsaved
                    && _drafts.TryGetValue(key, out DraftEntry? current)
                    && ReferenceEquals(current, entry))
                {
                    identity = entry.Identity;
                }
            }

            if (identity is not null)
                await StartSave(identity, false, false, false, CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(entry.DebounceCancellation, cancellation))
                {
                    entry.DebounceCancellation = null;
                    entry.DebounceTask = null;
                }
            }
            cancellation.Dispose();
        }
    }

    private void TrackOperation(Task operation)
    {
        _activeOperations.Add(operation);
        _ = operation.ContinueWith(
            completed =>
            {
                lock (_sync)
                    _activeOperations.Remove(completed);
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static async Task AwaitAllWithoutShortCircuiting(
        IEnumerable<Task> tasks,
        CancellationToken cancellationToken)
    {
        foreach (Task task in tasks)
        {
            try
            {
                await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private DraftEntry GetRequiredEntry(PackageMemoIdentity identity)
    {
        if (_drafts.TryGetValue(MemoDraftKey.Create(identity), out DraftEntry? entry))
            return entry;
        throw new InvalidOperationException("The memo target must be tracked before it can be edited or saved.");
    }

    private static void ValidateIdentity(PackageMemoIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.PackagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.SourcePath);
        ArgumentNullException.ThrowIfNull(identity.IndexDirectory);
        ArgumentNullException.ThrowIfNull(identity.AssetName);
    }

    private void ThrowIfUnavailableForEdits()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_isShuttingDown)
            throw new InvalidOperationException("Memo draft shutdown draining has started.");
    }

    private static void CancelDebounce(DraftEntry entry)
    {
        CancellationTokenSource? cancellation = entry.DebounceCancellation;
        if (cancellation is null)
            return;
        cancellation.Cancel();
    }

    private void Publish(MemoDraftSnapshot snapshot) =>
        DraftChanged?.Invoke(this, new MemoDraftChangedEventArgs(snapshot));

    private sealed class DraftEntry(
        PackageMemoIdentity identity,
        string? persistedMemo,
        string? packageRevision)
    {
        public PackageMemoIdentity Identity { get; set; } = identity;
        public string? DraftMemo { get; set; } = persistedMemo;
        public string? ConfirmedMemo { get; set; } = persistedMemo;
        public string? ConfirmedPackageRevision { get; set; } = packageRevision;
        public long DraftRevision { get; set; }
        public MemoDraftSaveState State { get; set; } = MemoDraftSaveState.Saved;
        public PackageMemoPersistenceStatus? LastPersistenceStatus { get; set; }
        public string? Error { get; set; }
        public SemaphoreSlim SaveGate { get; } = new(1, 1);
        public CancellationTokenSource? DebounceCancellation { get; set; }
        public Task? DebounceTask { get; set; }

        public MemoDraftSnapshot ToSnapshot() => new(
            Identity,
            DraftMemo,
            ConfirmedMemo,
            ConfirmedPackageRevision,
            DraftRevision,
            State,
            LastPersistenceStatus,
            Error);
    }

    private readonly record struct MemoDraftKey(
        string Package,
        string Index,
        string Source,
        string Asset,
        long PathId)
    {
        public static MemoDraftKey Create(PackageMemoIdentity identity) => new(
            PackagePathComparer.GetIdentity(identity.PackagePath!),
            identity.IndexDirectory.ToUpperInvariant(),
            PackagePathComparer.GetIdentity(identity.SourcePath),
            identity.AssetName,
            identity.PathId);
    }

    private sealed class SystemMemoDraftDelay : IMemoDraftDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
            Task.Delay(delay, cancellationToken);
    }
}
