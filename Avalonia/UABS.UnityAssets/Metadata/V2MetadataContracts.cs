namespace UABS.UnityAssets.Metadata;

public enum V2ExtractionStatus { Complete, Partial, Failed, Unsupported, RevisionMismatch }
public enum V2IdentityStatus { Addressable, Ambiguous, Incomplete }
public enum V2NameStatus { Present, Absent, Failed, Budgeted, Unattempted }
public enum V2DependencyStatus { Complete, Partial, Unavailable }

/// <summary>IDs are assigned by the caller. An existing revision may only be reused with its expected SHA-256.</summary>
public sealed record V2ExtractionRequest(string Path, Guid SourceId, Guid RevisionId, string? ExpectedSha256 = null);
public sealed record V2ExtractionOptions(
    int BatchSize = 256, int MaxNameBytes = 16384, bool ReadNames = true,
    bool ReadDependencies = true, int MaxDependencyBytes = 1048576, int MaxDependencyItems = 16384);

public abstract record V2MetadataRecord(int DirectoryOrdinal);
public sealed record V2LogicalFile(int DirectoryOrdinal, string? Name, string? CabEvidence,
    string? UnityVersion, uint? SerializedVersion, bool? HasTypeTree, int? DeclaredAssetCount,
    int CapturedAssetCount, V2ExtractionStatus Status, V2DependencyStatus DependencyStatus)
    : V2MetadataRecord(DirectoryOrdinal);
public sealed record V2AssetType(int DirectoryOrdinal, int LocalTypeIndex, int? ClassId,
    int? ScriptTypeIndex, string? TypeName) : V2MetadataRecord(DirectoryOrdinal);
public sealed record V2Asset(int DirectoryOrdinal, int TableOrdinal, long? PathId, int? ClassId,
    int? LocalTypeIndex, int? ScriptTypeIndex, long? ByteOffset, long? ByteSize,
    V2IdentityStatus IdentityStatus, string? Name, V2NameStatus NameStatus, string? TypeName)
    : V2MetadataRecord(DirectoryOrdinal);
public sealed record V2Dependency(int DirectoryOrdinal, string Kind, int DeclarationOrdinal,
    long? DeclaringAssetPathId, string? Path, string? OriginalPath, string? VirtualPath,
    string? CabEvidence, string? GuidHex, int? ExternalType) : V2MetadataRecord(DirectoryOrdinal);
public sealed record V2Diagnostic(int DirectoryOrdinal, int? TableOrdinal, string Phase,
    string Code, string Message) : V2MetadataRecord(DirectoryOrdinal);

public sealed record V2SourceEvidence(Guid SourceId, Guid RevisionId, string OriginalPath, string FullPath,
    UnityFileKind Kind, long? ByteSize, DateTime? LastWriteUtc, string? Sha256, bool Verified);
public sealed record V2ExtractionMetrics(TimeSpan Fingerprinting, TimeSpan Structure,
    TimeSpan Names, TimeSpan Dependencies, TimeSpan Total, long ManagedAllocatedBytes,
    long ProcessPeakWorkingSetBytes, int MaxEmittedBatch);
public sealed record V2ExtractionResult(V2SourceEvidence Source, V2ExtractionStatus Status,
    int LogicalFiles, long CapturedAssets, V2ExtractionMetrics Metrics)
{
    // All callback records are provisional until this final source verdict. Gate 2 must stage/discard accordingly.
    public bool CommitEligible => Source.Verified && Status is V2ExtractionStatus.Complete or V2ExtractionStatus.Partial;
}
public sealed record V2ExtractionProgress(string Phase, int DirectoryOrdinal, long Assets);

public interface IV2MetadataExtractor
{
    /// <summary>Runs off the caller thread; awaits each bounded batch (backpressure). No UI dispatcher.
    /// Callback records belong to request.SourceId/RevisionId and remain provisional until the returned verdict.
    /// Cancellation or callback exceptions abort extraction and require discarding that invocation's staged data.</summary>
    Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
        Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive,
        V2ExtractionOptions? options = null, IProgress<V2ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
