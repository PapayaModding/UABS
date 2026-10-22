namespace UABS.PackageV2.Memos;

/// <summary>Addressable identity, not a diagnostic table occurrence or a database-local row ID.</summary>
public sealed record MemoTarget(Guid PackageId, Guid GenerationId, Guid SourceId, Guid SourceRevision,
    int DirectoryOrdinal, long PathId)
{
    public static MemoTarget? From(StoredAsset asset) => asset.IdentityStatus == "addressable" && asset.Key.PathId is long path
        ? new(asset.Key.PackageId, asset.Key.GenerationId, asset.Key.SourceId, asset.Key.SourceRevision, asset.Key.DirectoryOrdinal, path) : null;
}

public enum MemoStatus { Saved, Unchanged, PackageMissing, GenerationChanged, MissingAsset, AmbiguousIdentity,
    EditorActive, RevisionConflict, Busy, ReadOnly, InvalidText, InvalidPackage, RegistrationChanged,
    WriteFailure, VerificationFailure, PublicationFailure, CancelledBeforeCommit, CommittedButSuperseded }
public enum MemoCommitState { NotCommitted, Committed, Uncertain }
public enum MemoBoundary { BeforeTransaction, BeforeCommit, AfterCommit, BeforeVerification }
public sealed record MemoWriteRequest(V2MemoRegistration Registration, MemoTarget Target, string? DesiredMemo,
    string? ExpectedOriginalMemo, long ExpectedMemoRevision, long? ExpectedCatalogRevision = null);
public sealed record VerifiedMemoChange(MemoTarget Target, string? Memo, long MemoRevision, long CatalogRevision,
    bool Verified = true);
public sealed record MemoWriteResult(MemoStatus Status, MemoWriteRequest Request, MemoCommitState CommitState,
    string Message, VerifiedMemoChange? Change = null)
{
    public bool Succeeded => Status is MemoStatus.Saved or MemoStatus.Unchanged;
}
public sealed record MemoAdmission(MemoStatus? Failure, V2MemoRegistration? Registration, string Message);

/// <summary>Captured location/ownership; no with-expression or mutable path can retarget queued writes.</summary>
public sealed class V2MemoRegistration
{
    internal V2MemoRegistration(Guid owner, string path, PackageInfo info) { Owner = owner; Path = path; Info = info; }
    internal Guid Owner { get; }
    internal bool Active { get; set; } = true; // accessed only under the service registry monitor
    public Guid Id { get; } = Guid.NewGuid();
    public string Path { get; }
    public PackageInfo Info { get; }
}

public sealed record EditorBaseRevision(Guid PackageId, Guid GenerationId, long CatalogRevision)
{
    /// <summary>Future explicit editor Save MUST call this with manifest values read inside its write transaction.</summary>
    public MemoStatus? Check(PackageInfo current) => current.PackageId != PackageId || current.GenerationId != GenerationId
        ? MemoStatus.GenerationChanged : current.CatalogRevision != CatalogRevision ? MemoStatus.RevisionConflict : null;
}
public sealed record EditorAdmission(MemoStatus? Failure, V2EditorSession? Session, string Message);

/// <summary>Stateless bridge only. Gate5 must adapt the existing shared coordinator's keys/revisions,
/// retaining its 1.5s debounce/Retry/navigation algorithms; this is not another draft coordinator.</summary>
public sealed record MemoSaveAttempt(long DraftRevision, MemoWriteRequest Request);
public sealed record MemoDraftCompletion(MemoWriteResult Result, bool MayMarkSaved, string? RetainedDraft);
public sealed class V2MemoDraftAdapter(V2MemoPersistenceService persistence)
{
    public Task<MemoWriteResult> SaveAsync(MemoSaveAttempt attempt, CancellationToken token = default) =>
        persistence.PersistAsync(attempt.Request, token);
    public static MemoDraftCompletion Complete(MemoSaveAttempt attempt, long currentDraftRevision,
        string? currentDraft, MemoWriteResult result) => new(result,
            result.Request == attempt.Request && result.Succeeded && result.Change is { Verified: true }
            && result.Change.Target == attempt.Request.Target
            && string.Equals(result.Change.Memo, attempt.Request.DesiredMemo, StringComparison.Ordinal)
            && currentDraftRevision == attempt.DraftRevision
            && string.Equals(currentDraft, attempt.Request.DesiredMemo, StringComparison.Ordinal), currentDraft);
}
