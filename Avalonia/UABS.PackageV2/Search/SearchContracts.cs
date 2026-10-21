using UABS.UnityAssets.Packages.Search;

namespace UABS.PackageV2.Search;

public enum SearchStatus { Success, InvalidRequest, InvalidPackage, RegistrationConflict, InvalidCursor,
    StaleCursor, Cancelled, BudgetExceeded, Unavailable, Failed }
public enum SearchCountKind { Unknown, LoadedAtLeast, Exact, Stale }
public sealed record SearchCount(SearchCountKind Kind, long Value);
public sealed record SearchDiagnostic(string Code, string Message, Guid? RegistrationId = null);
public sealed record SearchOptions(int Workers = 2, TimeSpan? OperationTimeout = null,
    long MaxPredicateEvaluations = 20_000_000, int MaxSelectedPackages = 128);
public sealed record SearchRequest(string? Query = null, string? ExcludedQuery = null,
    PackageSearchFields Fields = PackageSearchFields.All, PackageSearchResultKinds ResultKinds = PackageSearchResultKinds.All,
    bool ExactText = false, bool RequireNonEmptyMemo = false, long? PathId = null,
    PackageSearchSortField SortBy = PackageSearchSortField.PackageName,
    PackageSearchSortDirection SortDirection = PackageSearchSortDirection.Ascending,
    int? ClassId = null, string? TypeName = null, AssetKey? AssetIdentity = null);
public sealed record SearchRegistration(Guid RegistrationId, string Path, long LocationEpoch, PackageInfo Package);
public sealed record SearchRevision(Guid RegistrationId, long LocationEpoch, Guid PackageId,
    Guid GenerationId, long CatalogRevision);
public sealed record SearchRegistrationResult(SearchStatus Status, SearchRegistration? Registration, string Message);
public sealed record SearchSessionResult(SearchStatus Status, SearchSession? Session, string Message);
public sealed record SearchRowKey(Guid PackageId, Guid GenerationId, PackageSearchResultKind Kind,
    Guid? SourceId, Guid? SourceRevision, int? DirectoryOrdinal, int? TableOrdinal);
public sealed record SearchResult(SearchRowKey Key, PackageSearchMatchCategory MatchCategories,
    string PackageName, string PackagePath, string? IndexDirectory, string? SourceName, string? SourcePath,
    string? AssetName, long? PathId, int? ClassId, string? TypeName, string? Memo, long MemoRevision,
    string? IdentityStatus)
{
    public AssetKey? AssetIdentity => Key.Kind == PackageSearchResultKind.Asset
        ? new(Key.PackageId, Key.GenerationId, Key.SourceId!.Value, Key.SourceRevision!.Value,
            Key.DirectoryOrdinal!.Value, PathId, Key.TableOrdinal!.Value) : null;
}
/// <summary>Opaque, process/session-scoped boundary. No live connection or unconsumed buffer is retained.</summary>
public sealed class SearchCursor
{
    internal SearchCursor(Guid sessionId, SearchResult last, long loaded)
    { SessionId = sessionId; Last = last with { Memo = null }; Loaded = loaded; }
    internal Guid SessionId { get; }
    internal SearchResult Last { get; }
    internal long Loaded { get; }
}
public sealed record SearchPage(SearchStatus Status, IReadOnlyList<SearchResult> Rows, SearchCursor? NextCursor,
    bool HasMore, SearchCount Count, IReadOnlyList<SearchDiagnostic> Diagnostics,
    IReadOnlyList<SearchRevision> Revisions);
public sealed record SearchCountResult(SearchStatus Status, SearchCount Count, IReadOnlyList<SearchDiagnostic> Diagnostics);
public enum SearchBoundary { WorkerStarted, Predicate, SortComparison, BeforePublication }
