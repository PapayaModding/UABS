namespace UABS.UnityAssets.Packages;

public interface IPackageWriteCoordinator
{
    Task ExecutePackageWriteAsync(
        string packagePath,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    Task<TResult> ExecutePackageWriteAsync<TResult>(
        string packagePath,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Serializes complete package write transactions by normalized package path.
/// It provides mutual exclusion only; callers remain responsible for freshness
/// checks and conflict policy.
/// </summary>
public sealed class PackageWriteCoordinator : IPackageWriteCoordinator
{
    private readonly object _sync = new();
    private readonly Dictionary<string, LockEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    internal int TrackedPathCount
    {
        get
        {
            lock (_sync)
                return _entries.Count;
        }
    }

    public Task ExecutePackageWriteAsync(
        string packagePath,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecutePackageWriteAsync(
            packagePath,
            async token =>
            {
                await operation(token);
                return true;
            },
            cancellationToken);
    }

    public async Task<TResult> ExecutePackageWriteAsync<TResult>(
        string packagePath,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        string identity = PackagePathComparer.GetIdentity(packagePath);
        LockEntry entry;
        lock (_sync)
        {
            if (!_entries.TryGetValue(identity, out entry!))
            {
                entry = new LockEntry();
                _entries.Add(identity, entry);
            }
            entry.Users++;
        }

        bool acquired = false;
        try
        {
            await entry.Gate.WaitAsync(cancellationToken);
            acquired = true;
            cancellationToken.ThrowIfCancellationRequested();
            return await operation(cancellationToken);
        }
        finally
        {
            if (acquired)
                entry.Gate.Release();

            bool dispose = false;
            lock (_sync)
            {
                entry.Users--;
                if (entry.Users == 0
                    && _entries.TryGetValue(identity, out LockEntry? current)
                    && ReferenceEquals(current, entry))
                {
                    _entries.Remove(identity);
                    dispose = true;
                }
            }

            if (dispose)
                entry.Gate.Dispose();
        }
    }

    private sealed class LockEntry
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public int Users { get; set; }
    }
}
