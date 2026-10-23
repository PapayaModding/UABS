using Microsoft.Data.Sqlite;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.PackageV2.Memos;

/// <summary>Owned by one registration, serialized by the shared path coordinator. No active transaction between calls.</summary>
internal sealed class WriterAdmission(SqliteConnection connection, FileStream pin, PackageInfo info, long version) : IDisposable
{
    internal SqliteConnection Connection { get; } = connection;
    internal FileStream Pin { get; } = pin;
    internal PackageInfo Info { get; set; } = info;
    internal long? Version { get; set; } = version;
    public void Dispose() { try { Connection.Dispose(); } finally { Pin.Dispose(); } }
}

/// <summary>Attempt-local authority to reuse validation, not a transferable data_version token.
/// The origin connection is idle and exclusively owned by the save until publication returns.</summary>
internal sealed class MemoAdmissionProof(string path, SqliteConnection origin, bool pinned, long version,
    PackageInfo admitted, Action invalidate)
{
    private readonly object _gate = new();
    private bool _active = true;
    internal void Expire() { lock (_gate) _active = false; }
    internal bool Matches(string pathToCheck)
    {
        lock (_gate) return _active && string.Equals(Path.GetFullPath(pathToCheck), path,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    internal PackageInfo Read(SqliteConnection fresh, CancellationToken token)
    {
        // Drain any already-started capability use before the writer releases its
        // coordinator or disposes/reuses the origin connection. Detached callers
        // cannot race a later save on that connection after publication returns.
        lock (_gate)
        {
            if (!_active) throw new InvalidDataException("Memo admission capability expired.");
            using StorageSql snapshot = new(fresh);
            if (!string.Equals((string?)snapshot.Scalar("PRAGMA journal_mode"), "delete", StringComparison.OrdinalIgnoreCase))
            { invalidate(); throw new InvalidDataException("Memo publication requires approved DELETE journal mode."); }
            // Establish the fresh read snapshot BEFORE observing the origin connection's token.
            _ = snapshot.Number("SELECT COUNT(*) FROM package");
            using StorageSql observer = new(origin);
            bool reuse = pinned && observer.Number("PRAGMA data_version") == version;
            if (!reuse) { invalidate(); return StorageSchema.Validate(fresh, false, token); }
            using var phase = MemoTrace.Start("attempt-validation-reuse");
            var info = StorageSchema.ReadManifest(fresh, token);
            if (info.PackageId != admitted.PackageId || info.GenerationId != admitted.GenerationId
                || info.CatalogRevision != admitted.CatalogRevision)
            { invalidate(); throw new InvalidDataException("Validated memo snapshot changed."); }
            return info with { Sources = admitted.Sources, Assets = admitted.Assets, Issues = admitted.Issues };
        }
    }
}
