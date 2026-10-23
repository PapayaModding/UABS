using UABS.UnityAssets.Metadata;

namespace UABS.PackageV2;

public enum StorageStatus { Success, Cancelled, DestinationExists, InvalidInput, ExtractionFailed,
    PartialAcknowledgementRequired, InvalidPackage, UnsupportedVersion, IoFailure, ValidationFailure }
public enum BuildBoundary { BeforeSchema, AfterSchema, BeforeBatch, BeforeValidation, AfterValidation, BeforePublication }
public sealed record PackageBuildProgress(string Stage, int SourceNumber, int SourceCount, long Assets);
public sealed record PartialBuildSummary(long Assets, long Issues, IReadOnlyList<string> Details);
public sealed record SourceRoot(string Label, string OriginalBasePath, string PathPolicy);
public sealed record BuildSource(int RootIndex, string RelativePath, string IndexDirectory, V2ExtractionRequest Request);
public sealed record PackageBuildRequest(string Destination, Guid PackageId, Guid GenerationId, string Name,
    IReadOnlyList<SourceRoot> Roots, IReadOnlyList<BuildSource> Sources, bool AcknowledgePartial = false);
public sealed record BuildMetrics(TimeSpan Schema, TimeSpan Ingestion, TimeSpan Validation, TimeSpan Publication,
    TimeSpan Total, long FileBytes, long ManagedAllocatedBytes, long ProcessPeakWorkingSetBytes, int MaxBatch);
public sealed record PackageBuildResult(StorageStatus Status, string? PublishedPath, string Message, BuildMetrics Metrics,
    string? RetainedStagingPath = null) { public bool Succeeded => Status == StorageStatus.Success; }
public sealed record AdmissionResult(StorageStatus Status, PackageReader? Reader, string Message);
public sealed record PackageInfo(Guid PackageId, Guid GenerationId, string Name, long CatalogRevision,
    string IndexStatus, string TextProfile, bool DerivedFieldsCompatible, long Sources, long Assets, long Issues,
    string CreatedUtc, string IndexerVersion, string TextContract, int SchemaMajor, int SchemaMinor);
public sealed record AssetKey(Guid PackageId, Guid GenerationId, Guid SourceId, Guid SourceRevision,
    int DirectoryOrdinal, long? PathId, int TableOrdinal);
public sealed record StoredAsset(long RowId, AssetKey Key, int? ClassId, long? TypeId, string? TypeName,
    long? ByteOffset, long? ByteSize, string IdentityStatus, string? Name, string NameStatus,
    string? Memo, long MemoRevision);
/// <summary>Typed raw database values; paging ordinals/row IDs are storage-local, not asset identities.</summary>
public sealed record MetadataRow(long RowId, IReadOnlyDictionary<string, object?> Values);
public enum MetadataTable { SourceRoots, Sources, LogicalFiles, Types, Dependencies, Issues }
