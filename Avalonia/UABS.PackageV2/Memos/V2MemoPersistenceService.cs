using System.Text;
using Microsoft.Data.Sqlite;
using UABS.UnityAssets.Packages;

namespace UABS.PackageV2.Memos;

/// <summary>Isolated D6 writer. All instances in one application must share coordinator and editor guard.</summary>
public sealed class V2MemoPersistenceService
{
    private readonly IPackageWriteCoordinator _writes;
    private readonly V2PackageEditorGuard _editors;
    private readonly Func<VerifiedMemoChange, Task>? _publish;
    private readonly Action<MemoBoundary>? _boundary;
    private readonly Guid _owner = Guid.NewGuid();
    private readonly object _registry = new();
    private readonly Dictionary<Guid, V2MemoRegistration> _packages = [];
    private readonly int _busySeconds;

    public V2MemoPersistenceService(IPackageWriteCoordinator writes, V2PackageEditorGuard editors,
        Func<VerifiedMemoChange, Task>? publish = null, Action<MemoBoundary>? boundary = null, int busyTimeoutSeconds = 1)
    {
        _writes = writes ?? throw new ArgumentNullException(nameof(writes));
        _editors = editors ?? throw new ArgumentNullException(nameof(editors)); _publish = publish; _boundary = boundary;
        if (busyTimeoutSeconds is < 0 or > 30) throw new ArgumentOutOfRangeException(nameof(busyTimeoutSeconds));
        _busySeconds = busyTimeoutSeconds;
    }

    public async Task<MemoAdmission> RegisterAsync(string path, CancellationToken token = default)
    {
        try
        {
            string full = Path.GetFullPath(path);
            return await _writes.ExecutePackageWriteAsync(full, ct => Task.Run(() => {
                if (!File.Exists(full)) return new MemoAdmission(MemoStatus.PackageMissing, null, "Package missing.");
                using var c = Open(full, true); using StorageSql sql = new(c); sql.Exec("BEGIN");
                PackageInfo info = StorageSchema.Validate(c, false, ct); sql.Exec("COMMIT");
                if (!info.DerivedFieldsCompatible) return new MemoAdmission(MemoStatus.InvalidPackage, null, "Unsupported derived profile: writable revalidation required.");
                lock (_registry)
                {
                    if (_packages.ContainsKey(info.PackageId) || _packages.Values.Any(r => string.Equals(r.Path, full,
                        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
                        return new MemoAdmission(MemoStatus.RegistrationChanged, null, "UUID/location already registered; unload before moving/reopening.");
                    var reg = new V2MemoRegistration(_owner, full, info); _packages.Add(info.PackageId, reg);
                    return new MemoAdmission(null, reg, "Registered isolated V2 memo target.");
                }
            }, ct), token).ConfigureAwait(false);
        }
        catch (Exception e) { return new(Failure(e, token), null, e.Message); }
    }

    public Task UnloadAsync(V2MemoRegistration registration, CancellationToken token = default) =>
        _writes.ExecutePackageWriteAsync(registration.Path, _ => {
            lock (_registry)
            {
                if (Active(registration)) { registration.Active = false; _packages.Remove(registration.Info.PackageId); }
            }
            return Task.CompletedTask;
        }, token);

    private bool Active(V2MemoRegistration r)
    { lock (_registry) return r.Owner == _owner && r.Active && _packages.TryGetValue(r.Info.PackageId, out var current) && ReferenceEquals(current, r); }

    public async Task<EditorAdmission> OpenEditorAsync(V2MemoRegistration registration, EditorBaseRevision revision,
        CancellationToken token = default)
    {
        try
        {
            return await _writes.ExecutePackageWriteAsync(registration.Path, ct => Task.Run(() => {
                if (!Active(registration)) return new EditorAdmission(MemoStatus.RegistrationChanged, null, "Registration unavailable.");
                using var c = Open(registration.Path, true); using StorageSql sql = new(c); sql.Exec("BEGIN");
                PackageInfo info = StorageSchema.Validate(c, false, ct);
                MemoStatus? failure = info.PackageId != registration.Info.PackageId || info.GenerationId != registration.Info.GenerationId
                    ? MemoStatus.GenerationChanged : revision.Check(info);
                sql.Exec("COMMIT"); ct.ThrowIfCancellationRequested();
                if (failure is not null) return new EditorAdmission(failure, null, "Editor base changed.");
                var lease = _editors.Enter(revision);
                return new EditorAdmission(lease is null ? MemoStatus.EditorActive : null, lease, "Editor eligibility checked; release only on explicit close.");
            }, ct), token).ConfigureAwait(false);
        }
        catch (Exception e) { return new(Failure(e, token), null, e.Message); }
    }

    public async Task<MemoWriteResult> PersistAsync(MemoWriteRequest request, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(request.Registration);
        ArgumentNullException.ThrowIfNull(request.Target);
        try { if (request.DesiredMemo is not null) _ = new UTF8Encoding(false, true).GetByteCount(request.DesiredMemo); }
        catch (EncoderFallbackException e) { return Result(MemoStatus.InvalidText, e.Message); }
        if (request.Target.PackageId == Guid.Empty || request.Target.GenerationId == Guid.Empty || request.Target.SourceId == Guid.Empty
            || request.Target.SourceRevision == Guid.Empty || request.Target.DirectoryOrdinal < 0 || request.ExpectedMemoRevision < 0
            || request.ExpectedCatalogRevision < 0) return Result(MemoStatus.AmbiguousIdentity, "Incomplete target/precondition.");
        try
        {
            using var wait = UABS.UnityAssets.Packages.Memos.MemoTrace.Start("write-lock-wait");
            return await _writes.ExecutePackageWriteAsync(request.Registration.Path,
                ct => { wait?.Dispose(); return SaveCoordinated(request, ct); }, token).ConfigureAwait(false);
        }
        catch (Exception e) { return Result(Failure(e, token), e.Message); }
        MemoWriteResult Result(MemoStatus status, string message) => new(status, request, MemoCommitState.NotCommitted, message);
    }

    private async Task<MemoWriteResult> SaveCoordinated(MemoWriteRequest request, CancellationToken token)
    {
        MemoCommitState committed = MemoCommitState.NotCommitted;
        VerifiedMemoChange? change = null;
        bool unchanged = false;
        SqliteConnection? origin = null;
        FileStream? verificationPin = null;
        long validatedDataVersion = -1;
        MemoWriteResult completed = await AttemptAsync().ConfigureAwait(false);
        try
        {
            try { origin?.Dispose(); }
            finally { verificationPin?.Dispose(); }
        }
        catch (Exception e)
        {
            // Cleanup must not escape to PersistAsync's pre-commit exception wrapper:
            // a confirmed/uncertain commit remains so, even when handle closing fails.
            return Fail(MemoStatus.WriteFailure, "Memo resource cleanup failed: " + e.Message);
        }
        return completed;

        async Task<MemoWriteResult> AttemptAsync()
        {
        try
        {
            var outcome = await Task.Run(() => {
                var r = request.Registration; var target = request.Target;
                if (!Active(r)) return Fail(MemoStatus.RegistrationChanged, "Captured registration was unloaded.");
                if (_editors.IsOpen(target.PackageId)) return Fail(MemoStatus.EditorActive, "Package Editor owns this package, clean or dirty.");
                if (!File.Exists(r.Path)) return Fail(MemoStatus.PackageMissing, "Package missing.");
                if (r.Info.PackageId != target.PackageId || r.Info.GenerationId != target.GenerationId)
                    return Fail(MemoStatus.GenerationChanged, "Request does not belong to this registration generation.");
                // Attempt-local reuse only. Keep this exact connection to observe OTHER
                // commits; its own commit does not change data_version. The Windows pin
                // prevents a different file appearing at this path during verification.
                if (OperatingSystem.IsWindows()) verificationPin = new(r.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var c = origin = Open(r.Path, false); using StorageSql sql = new(c);
                _boundary?.Invoke(MemoBoundary.BeforeTransaction);
                using var interrupt = token.Register(() => SQLitePCL.raw.sqlite3_interrupt(c.Handle));
                bool transaction = false;
                try
                {
                    token.ThrowIfCancellationRequested();
                    using (UABS.UnityAssets.Packages.Memos.MemoTrace.Start("transaction-open")) sql.Exec("BEGIN IMMEDIATE");
                    transaction = true;
                    PackageInfo info = StorageSchema.Validate(c, false, token);
                    validatedDataVersion = sql.Number("PRAGMA data_version");
                    if (info.PackageId != target.PackageId || info.GenerationId != target.GenerationId)
                        return Fail(MemoStatus.GenerationChanged, "Package replaced or generation changed.");
                    if (!info.DerivedFieldsCompatible) return Fail(MemoStatus.InvalidPackage, "Unsupported derived profile; no mixed-profile writes.");
                    if (request.ExpectedCatalogRevision is long expected && expected != info.CatalogRevision)
                        return Fail(MemoStatus.RevisionConflict, "Catalog revision changed.");
                    var current = ReadTarget(sql, target);
                    if (current.Count == 0) return Fail(MemoStatus.MissingAsset, "Exact source/revision/logical/Path ID is absent.");
                    if (current.Count != 1 || current.Status != "addressable") return Fail(MemoStatus.AmbiguousIdentity, "Target is not uniquely addressable.");
                    if (current.Revision != request.ExpectedMemoRevision || !string.Equals(current.Memo, request.ExpectedOriginalMemo, StringComparison.Ordinal))
                        return Fail(MemoStatus.RevisionConflict, "Expected memo revision or original nullable value changed.");
                    unchanged = string.Equals(current.Memo, request.DesiredMemo, StringComparison.Ordinal);
                    if (!unchanged)
                    {
                        using var update = UABS.UnityAssets.Packages.Memos.MemoTrace.Start("memo-update");
                        // Local row ID is used only after full identity resolution inside the locked transaction.
                        if (sql.Exec("UPDATE asset SET memo=@p0 WHERE asset_id=@p1 AND memo_revision=@p2 AND memo IS @p3 AND identity_status='addressable'"
                            + " AND path_id=@p4 AND file_id IN (SELECT f.file_id FROM logical_file f JOIN source s ON s.source_id=f.source_id"
                            + " WHERE s.source_uuid=@p5 AND s.source_revision_uuid=@p6 AND f.directory_ordinal=@p7)"
                            + " AND EXISTS(SELECT 1 FROM package WHERE package_uuid=@p8 AND generation_uuid=@p9)",
                            request.DesiredMemo, current.Id, request.ExpectedMemoRevision, request.ExpectedOriginalMemo,
                            target.PathId, StorageSchema.Bytes(target.SourceId), StorageSchema.Bytes(target.SourceRevision),
                            target.DirectoryOrdinal, StorageSchema.Bytes(target.PackageId), StorageSchema.Bytes(target.GenerationId)) != 1)
                            return Fail(MemoStatus.RevisionConflict, "Conditional target update failed.");
                    }
                    var updated = ReadTarget(sql, target);
                    long catalog = sql.Number("SELECT catalog_revision FROM package");
                    if (updated.Count != 1 || updated.Memo != request.DesiredMemo || updated.Revision != checked(current.Revision + (unchanged ? 0 : 1))
                        || catalog != checked(info.CatalogRevision + (unchanged ? 0 : 1)))
                        return Fail(MemoStatus.WriteFailure, "Trigger-managed counters/value did not match expected update.");
                    change = new(target, updated.Memo, updated.Revision, catalog);
                    _boundary?.Invoke(MemoBoundary.BeforeCommit); token.ThrowIfCancellationRequested();
                    // From here cancellation cannot interrupt commit or verification and claim rollback.
                    interrupt.Dispose(); token.ThrowIfCancellationRequested();
                    committed = unchanged ? MemoCommitState.NotCommitted : MemoCommitState.Uncertain;
                    using (UABS.UnityAssets.Packages.Memos.MemoTrace.Start("memo-commit")) sql.Exec("COMMIT");
                    transaction = false;
                    committed = unchanged ? MemoCommitState.NotCommitted : MemoCommitState.Committed;
                    _boundary?.Invoke(MemoBoundary.AfterCommit);
                    return null;
                }
                finally
                {
                    if (transaction)
                    {
                        // No token-controlled interrupt during cleanup. A failed rollback leaves uncertainty explicit.
                        interrupt.Dispose();
                        try { sql.Exec("ROLLBACK"); if (committed == MemoCommitState.Uncertain) committed = MemoCommitState.NotCommitted; }
                        catch (Exception e) { committed = MemoCommitState.Uncertain; throw new IOException("Rollback could not be confirmed.", e); }
                    }
                }
            }).ConfigureAwait(false);
            if (outcome is not null) return outcome;
            try
            {
                await Task.Run(() => {
                    using var verification = UABS.UnityAssets.Packages.Memos.MemoTrace.Start("fresh-verification");
                    _boundary?.Invoke(MemoBoundary.BeforeVerification);
                    using var fresh = Open(request.Registration.Path, true); using StorageSql sql = new(fresh); sql.Exec("BEGIN");
                    // Pin the FRESH read snapshot before consulting the original connection.
                    // Any intervening external commit falls back to full global validation.
                    _ = sql.Number("SELECT COUNT(*) FROM package");
                    using var observer = new StorageSql(origin!);
                    bool reusable = verificationPin is not null && observer.Number("PRAGMA data_version") == validatedDataVersion;
                    using var reused = reusable ? UABS.UnityAssets.Packages.Memos.MemoTrace.Start("attempt-validation-reuse") : null;
                    PackageInfo info = reusable ? StorageSchema.ReadManifest(fresh, CancellationToken.None)
                        : StorageSchema.Validate(fresh, false, CancellationToken.None);
                    if (info.PackageId != request.Target.PackageId || info.GenerationId != request.Target.GenerationId)
                        throw new VerificationException(MemoStatus.GenerationChanged, "Package replaced after transaction.");
                    var saved = ReadTarget(sql, request.Target);
                    if (saved.Count == 1 && saved.Revision > change!.MemoRevision)
                        throw new VerificationException(MemoStatus.CommittedButSuperseded, "A newer target memo committed before verification.");
                    if (saved.Count != 1 || saved.Status != "addressable" || saved.Memo != change!.Memo || saved.Revision != change.MemoRevision
                        || info.CatalogRevision < change.CatalogRevision)
                        throw new VerificationException(MemoStatus.VerificationFailure, "Fresh-connection verification failed.");
                    change = change with { CatalogRevision = info.CatalogRevision }; sql.Exec("COMMIT");
                }).ConfigureAwait(false);
            }
            catch (VerificationException e) { return Fail(e.Status, e.Message); }
            catch (Exception e) { return Fail(MemoStatus.VerificationFailure, e.Message); }
            try { if (_publish is not null) await _publish(change!).ConfigureAwait(false); }
            catch (Exception e) { return new(MemoStatus.PublicationFailure, request, committed, e.Message, change); }
            return new(unchanged ? MemoStatus.Unchanged : MemoStatus.Saved, request, committed, "Fresh-connection verified.", change);
        }
        catch (Exception e) { return Fail(committed == MemoCommitState.NotCommitted ? Failure(e, token) : MemoStatus.WriteFailure, e.Message); }
        }
        MemoWriteResult Fail(MemoStatus status, string message) => new(status, request, committed, message);
    }

    private SqliteConnection Open(string path, bool readOnly)
    {
        var c = StorageSchema.Open(path, readOnly); c.DefaultTimeout = _busySeconds;
        try
        {
            using StorageSql sql = new(c);
            if (!readOnly)
            {
                if (!string.Equals((string?)sql.Scalar("PRAGMA journal_mode"), "delete", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Writable package requires approved DELETE journal mode.");
                sql.Exec("PRAGMA synchronous=FULL");
            }
            return c;
        }
        catch { c.Dispose(); throw; }
    }

    private sealed record TargetRow(long Count, long Id, string? Memo, long Revision, string? Status);
    private static TargetRow ReadTarget(StorageSql sql, MemoTarget key)
    {
        using var phase = UABS.UnityAssets.Packages.Memos.MemoTrace.Start("target-lookup");
        using var rows = sql.Command("SELECT a.asset_id,a.memo,a.memo_revision,a.identity_status FROM asset a JOIN logical_file f ON f.file_id=a.file_id JOIN source s ON s.source_id=f.source_id WHERE s.source_uuid=@p0 AND s.source_revision_uuid=@p1 AND f.directory_ordinal=@p2 AND a.path_id=@p3 LIMIT 2",
            StorageSchema.Bytes(key.SourceId), StorageSchema.Bytes(key.SourceRevision), key.DirectoryOrdinal, key.PathId).ExecuteReader();
        if (!rows.Read()) return new(0, 0, null, 0, null);
        var row = new TargetRow(1, rows.GetInt64(0), rows.IsDBNull(1) ? null : rows.GetString(1), rows.GetInt64(2), rows.GetString(3));
        return rows.Read() ? row with { Count = 2 } : row;
    }
    private static MemoStatus Failure(Exception e, CancellationToken token) => token.IsCancellationRequested ? MemoStatus.CancelledBeforeCommit
        : e is SqliteException s ? s.SqliteErrorCode switch { 5 or 6 => MemoStatus.Busy, 8 => MemoStatus.ReadOnly, 14 => MemoStatus.PackageMissing, _ => MemoStatus.WriteFailure }
        : e is FileNotFoundException or DirectoryNotFoundException ? MemoStatus.PackageMissing
        : e is UnauthorizedAccessException ? MemoStatus.ReadOnly
        : e is InvalidDataException or StorageSchema.UnsupportedSchemaException ? MemoStatus.InvalidPackage : MemoStatus.WriteFailure;
    private sealed class VerificationException(MemoStatus status, string message) : Exception(message)
    { internal MemoStatus Status { get; } = status; }
}
