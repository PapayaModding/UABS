using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using UABS.PackageV2.Memos;
using UABS.PackageV2.Search;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.Services;

public sealed record V2MemoPublication(V2BrowseRegistration Registration, VerifiedMemoChange Change);

/// <summary>Format boundary only: one shared draft coordinator calls this facade. No draft map/timer,
/// guessed identity, V1 conversion, validation cache, or independent persistence algorithm.</summary>
public sealed class InspectorMemoService : IPackageMemoPersistenceService, IPackageMemoResolver, IPackageEditorSessionGuard
{
    private readonly IPackageMemoPersistenceService _legacy;
    private readonly IPackageMemoResolver _resolver;
    private readonly IPackageEditorSessionGuard _legacyEditors;
    private readonly Func<IReadOnlyList<V2BrowseRegistration>> _loaded;
    private readonly Func<Action, Task> _dispatch;
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Context> _contexts = new();
    public V2PackageEditorGuard V2Editors { get; }
    public V2MemoPersistenceService V2Writer { get; }
    public event Action<V2MemoPublication>? Published;
    public Task CleanupTask { get; private set; } = Task.CompletedTask;

    public InspectorMemoService(IPackageMemoPersistenceService legacy, IPackageMemoResolver resolver,
        IPackageWriteCoordinator writes, IPackageEditorSessionGuard legacyEditors,
        Func<IReadOnlyList<V2BrowseRegistration>> loaded, V2PackageEditorGuard? editors = null,
        Action<MemoBoundary>? boundary = null, Func<Action, Task>? dispatch = null)
    {
        _legacy = legacy; _resolver = resolver; _legacyEditors = legacyEditors; _loaded = loaded;
        _dispatch = dispatch ?? DispatchAsync;
        V2Editors = editors ?? new(); V2Writer = new(writes, V2Editors, PublishAsync, boundary, reuseAdmission: true);
    }
    private static async Task DispatchAsync(Action action)
    { if (Dispatcher.UIThread.CheckAccess()) action(); else await Dispatcher.UIThread.InvokeAsync(action); }
    public bool IsPackageOpen(string path)
    {
        if (_legacyEditors.IsPackageOpen(path)) return true;
        lock (_gate) return _contexts.Values.Any(c => PackagePathComparer.Equals(c.Entry.Path, path) && V2Editors.IsOpen(c.Entry.PackageId));
    }
    public static PackageMemoIdentity Identity(V2BrowseRegistration entry, MemoTarget target) => new(entry.Path, "", "", "", target.PathId,
        new(entry.Epoch, target.PackageId, target.GenerationId, target.SourceId, target.SourceRevision, target.DirectoryOrdinal, target.PathId));
    private static MemoTarget Target(V2MemoIdentity v) => new(v.PackageId, v.GenerationId, v.SourceId, v.SourceRevision, v.DirectoryOrdinal, v.PathId);
    private static string Stamp(long revision, long catalog) => $"v2:{revision.ToString(CultureInfo.InvariantCulture)}:{catalog.ToString(CultureInfo.InvariantCulture)}";
    private Context ContextFor(V2BrowseRegistration entry)
    {
        lock (_gate)
        {
            if (_contexts.TryGetValue(entry.Epoch, out var known)) return known;
            var context = new Context(entry); _contexts.Add(entry.Epoch, context);
            context.Unload = entry.Lifetime.Register(() => { CleanupTask = CleanupAsync(context); });
            return context;
        }
    }
    private async Task CleanupAsync(Context context)
    {
        try
        {
            await context.Admission.WaitAsync().ConfigureAwait(false);
            try { if (context.Writer is { } writer) await V2Writer.UnloadAsync(writer).ConfigureAwait(false); }
            finally { context.Admission.Release(); }
        }
        catch (Exception e) { Printer.PrintException("V2 memo registration cleanup failed.", e); }
    }
    public async Task<V2MemoRegistration> GetRegistrationAsync(V2BrowseRegistration entry, CancellationToken token = default)
    {
        using var phase = MemoTrace.Start("writer-registration");
        var context = ContextFor(entry);
        await context.Admission.WaitAsync(token).ConfigureAwait(false);
        try
        {
            entry.Lifetime.ThrowIfCancellationRequested(); await entry.RequireMemoLocationAsync(token).ConfigureAwait(false);
            if (context.Writer is null)
            {
                var admitted = await V2Writer.RegisterAsync(entry.Path, token).ConfigureAwait(false);
                context.Writer = admitted.Registration ?? throw new InvalidDataException($"{admitted.Failure}: {admitted.Message}");
            }
            if (context.Writer.Info.PackageId != entry.PackageId || context.Writer.Info.GenerationId != entry.GenerationId)
                throw new InvalidDataException("Registered package generation changed; no memo draft retargeting.");
            entry.Lifetime.ThrowIfCancellationRequested(); return context.Writer;
        }
        finally { context.Admission.Release(); }
    }
    private Context RequireContext(PackageMemoIdentity identity)
    {
        var v = identity.V2 ?? throw new InvalidDataException("Not a V2 memo target.");
        lock (_gate)
        {
            if (!_contexts.TryGetValue(v.RegistrationEpoch, out var c) || c.Entry.PackageId != v.PackageId
                || c.Entry.GenerationId != v.GenerationId || !PackagePathComparer.Equals(c.Entry.Path, identity.PackagePath)
                || c.Entry.Lifetime.IsCancellationRequested) throw new InvalidDataException("Captured V2 registration is unloaded/stale; draft retained, never retargeted.");
            return c;
        }
    }
    public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => _legacy.CaptureRevisionAsync(path, token);
    public async Task<MemoPersistenceBasis> CaptureBasisAsync(PackageMemoIdentity identity, string? displayedMemo, CancellationToken token = default)
    {
        using var phase = MemoTrace.Start("memo-basis");
        if (identity.V2 is not { } v) return await _legacy.CaptureBasisAsync(identity, displayedMemo, token).ConfigureAwait(false);
        var c = RequireContext(identity);
        var snapshot = await c.Entry.ReadCurrentMemoAsync(r => r.ReadMemoAsync(Target(v), token, refreshAdmission: true), token).ConfigureAwait(false);
        return new(snapshot.Memo, Stamp(snapshot.MemoRevision, snapshot.CatalogRevision));
    }
    public async Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default)
    {
        if (request.Identity.V2 is not { } v) return await _legacy.PersistAsync(request, token).ConfigureAwait(false);
        bool invoked = false;
        try
        {
            var c = RequireContext(request.Identity);
            string[] revision = (request.ExpectedPackageRevision ?? "").Split(':');
            if (revision.Length != 3 || revision[0] != "v2" || !long.TryParse(revision[1], NumberStyles.None, CultureInfo.InvariantCulture, out long memoRevision))
                return new(PackageMemoPersistenceStatus.RevisionConflict, request, "Missing V2 memo revision precondition.");
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, c.Entry.Lifetime);
            await c.Entry.RequireMemoLocationAsync(linked.Token).ConfigureAwait(false);
            var registration = await GetRegistrationAsync(c.Entry, linked.Token).ConfigureAwait(false);
            invoked = true;
            var result = await V2Writer.PersistAsync(new(registration, Target(v), request.DesiredMemo, request.ExpectedOriginalMemo, memoRevision), linked.Token).ConfigureAwait(false);
            bool verified = result.Succeeded && result.Change is { Verified: true } change && change.Target == Target(v)
                && string.Equals(change.Memo, request.DesiredMemo, StringComparison.Ordinal) && result.CommitState != MemoCommitState.Uncertain;
            var status = result.Status switch
            {
                MemoStatus.Saved when verified => PackageMemoPersistenceStatus.Saved,
                MemoStatus.Unchanged when verified => PackageMemoPersistenceStatus.Unchanged,
                MemoStatus.RevisionConflict or MemoStatus.GenerationChanged => PackageMemoPersistenceStatus.RevisionConflict,
                MemoStatus.CommittedButSuperseded => PackageMemoPersistenceStatus.CommittedButSuperseded,
                MemoStatus.EditorActive => PackageMemoPersistenceStatus.PackageEditorActive,
                MemoStatus.MissingAsset => PackageMemoPersistenceStatus.AssetNotFound,
                MemoStatus.AmbiguousIdentity => PackageMemoPersistenceStatus.AmbiguousIdentity,
                MemoStatus.PackageMissing => PackageMemoPersistenceStatus.PackageMissing,
                MemoStatus.RegistrationChanged => PackageMemoPersistenceStatus.RegistrationChanged,
                MemoStatus.CancelledBeforeCommit => PackageMemoPersistenceStatus.CancelledBeforeCommit,
                MemoStatus.PublicationFailure => PackageMemoPersistenceStatus.CatalogPublicationFailure,
                MemoStatus.VerificationFailure or MemoStatus.Saved or MemoStatus.Unchanged => PackageMemoPersistenceStatus.VerificationFailure,
                _ => PackageMemoPersistenceStatus.WriteFailure
            };
            return new(status, request, $"{result.Status} / {result.CommitState}: {result.Message}",
                (verified || (result.Status == MemoStatus.PublicationFailure && result.Change is { Verified: true } delta
                    && delta.Target == Target(v) && delta.Memo == request.DesiredMemo))
                    ? Stamp(result.Change!.MemoRevision, result.Change.CatalogRevision) : null,
                CommitState: result.CommitState switch { MemoCommitState.Committed => PackageMemoCommitState.Committed,
                    MemoCommitState.Uncertain => PackageMemoCommitState.Uncertain, _ => PackageMemoCommitState.NotCommitted });
        }
        catch (OperationCanceledException) { return new(invoked ? PackageMemoPersistenceStatus.WriteFailure : PackageMemoPersistenceStatus.CancelledBeforeCommit,
            request, invoked ? "Writer completion is uncertain; retain draft and reconcile." : "Cancelled before calling the SQLite writer.",
            CommitState: invoked ? PackageMemoCommitState.Uncertain : PackageMemoCommitState.NotCommitted); }
        catch (Exception e) { return new(invoked ? PackageMemoPersistenceStatus.WriteFailure : PackageMemoPersistenceStatus.RegistrationChanged,
            request, e.Message, CommitState: invoked ? PackageMemoCommitState.Uncertain : PackageMemoCommitState.NotCommitted); }
    }
    private async Task PublishAsync(VerifiedMemoChange change)
    {
        using var phase = MemoTrace.Start("publication");
        Context c;
        lock (_gate) c = _contexts.Values.Single(x => x.Writer is not null && x.Entry.PackageId == change.Target.PackageId
            && x.Entry.GenerationId == change.Target.GenerationId && !x.Entry.Lifetime.IsCancellationRequested);
        // A writer-created attempt capability can reuse proven admission. External
        // commits still force full validation on a fresh publication connection.
        var snapshot = await c.Entry.ReadmitForMemoAsync(async reader =>
        {
            var current = await reader.ReadMemoAsync(change.Target).ConfigureAwait(false);
            if (current.MemoRevision != change.MemoRevision || current.Memo != change.Memo || current.CatalogRevision < change.CatalogRevision)
                throw new InvalidDataException("Verified memo was externally superseded before publication; draft retained.");
            return current;
        }, CancellationToken.None, change).ConfigureAwait(false);
        var publication = new V2MemoPublication(c.Entry, change with { CatalogRevision = snapshot.CatalogRevision });
        using var ui = MemoTrace.Start("ui-publication");
        await _dispatch(() => { c.Entry.Lifetime.ThrowIfCancellationRequested(); Published?.Invoke(publication); }).ConfigureAwait(false);
    }
    public PackageMemoResolution ResolveExact(IReadOnlyList<PackageMemoCatalogEntry> packages, PackageMemoIdentity identity) => _resolver.ResolveExact(packages, identity);
    public PackageMemoResolution ResolveAsset(IReadOnlyList<PackageMemoCatalogEntry> packages, AssetMemoLookup lookup) => _resolver.ResolveAsset(packages, lookup);
    public async Task<PackageMemoResolution> ResolveAssetAsync(IReadOnlyList<PackageMemoCatalogEntry> packages, AssetMemoLookup lookup, CancellationToken token = default)
    {
        var legacy = _resolver.ResolveAsset(packages, lookup); var loaded = _loaded().ToArray();
        if (loaded.Length == 0 || lookup.SourceSha256 is null || lookup.LogicalFile is null) return legacy;
        List<(V2BrowseRegistration entry, MemoTarget target)> matches = new();
        try
        {
            foreach (var entry in loaded)
            {
                var found = await entry.ReadCurrentMemoAsync(r => r.FindMemoTargetsAsync(lookup.SourcePath, lookup.SourceSha256, lookup.LogicalFile, lookup.PathId, token, refreshAdmission: true), token).ConfigureAwait(false);
                matches.AddRange(found.Select(target => (entry, target)));
                if (matches.Count > 1) break;
            }
            if (matches.Count > 1 || (matches.Count > 0 && (legacy.IsAvailable || legacy.Status == PackageMemoResolutionStatus.Ambiguous)))
                return Unavailable("Multiple indexed occurrences/packages match this verified native session; select the exact indexed V2 asset instead.", PackageMemoResolutionStatus.Ambiguous);
            if (matches.Count == 1)
            {
                var match = matches[0]; ContextFor(match.entry);
                return Available(match.entry, match.target, null);
            }
            return legacy.IsAvailable ? legacy : Unavailable("No loaded index matches this session's verified source bytes, path, logical file and asset identity.", PackageMemoResolutionStatus.NoMatch);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e) { return Unavailable("Cannot establish an unambiguous V2 memo target: " + e.Message); }
    }
    public PackageMemoResolution ResolveIndexed(V2BrowseRegistration entry, Guid package, Guid generation, Guid source,
        Guid revision, int ordinal, long? pathId, string? identityStatus, string? memo)
    {
        using var phase = MemoTrace.Start("indexed-identity");
        if (entry.PackageId != package || entry.GenerationId != generation)
            return Unavailable("Indexed package registration/generation is stale.");
        if (pathId is not long path || identityStatus != "addressable") return Unavailable("Asset identity is ambiguous/incomplete; no memo write is eligible.", PackageMemoResolutionStatus.Ambiguous);
        var target = new MemoTarget(package, generation, source, revision, ordinal, path);
        if (entry.Lifetime.IsCancellationRequested)
            return Available(entry, target, memo) with { Status = PackageMemoResolutionStatus.MissingOrStale, Diagnostic = "Package unloaded; any existing draft is retained under its original registration." };
        ContextFor(entry); return Available(entry, target, memo);
    }
    public PackageMemoResolution ResolveSearch(SearchResult row, SearchRevision revision)
    {
        var entry = _loaded().SingleOrDefault(e => e.LocationEpoch == revision.LocationEpoch);
        if (entry is null) lock (_gate) entry = _contexts.Values.Select(c => c.Entry).SingleOrDefault(e => e.LocationEpoch == revision.LocationEpoch);
        if (entry is null || row.AssetIdentity is not { } key) return Unavailable("This result has no loaded exact V2 asset registration.");
        return ResolveIndexed(entry, key.PackageId, key.GenerationId, key.SourceId, key.SourceRevision, key.DirectoryOrdinal,
            key.PathId, row.IdentityStatus, row.Memo);
    }
    private static PackageMemoResolution Available(V2BrowseRegistration entry, MemoTarget target, string? memo) => new(
        PackageMemoResolutionStatus.Available, Identity(entry, target), entry.Name, entry.Path, memo,
        memo is null ? PackageMemoStorageState.Missing : memo.Length == 0 ? PackageMemoStorageState.Empty : PackageMemoStorageState.Value,
        "Exact V2 registration/generation/source/logical asset identity; fresh revision validation required.");
    private static PackageMemoResolution Unavailable(string message, PackageMemoResolutionStatus status = PackageMemoResolutionStatus.MissingOrStale) => new(status, null, null, null, null, null, message);
    private sealed class Context(V2BrowseRegistration entry)
    {
        public V2BrowseRegistration Entry { get; } = entry;
        public SemaphoreSlim Admission { get; } = new(1, 1);
        public V2MemoRegistration? Writer;
        public CancellationTokenRegistration Unload;
    }
}
