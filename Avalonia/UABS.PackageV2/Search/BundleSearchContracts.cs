using UABS.UnityAssets.Packages.Search;

namespace UABS.PackageV2.Search;

public enum BundleSearchSort { Name, Package }

/// <summary>
/// Explicit bundle projection, not a reinterpretation of the UI's result-kind controls.
/// SourceName/AssetName/Memo are supported positive fields. Exclusions require an explicit
/// AssetName/Memo field mask; source metadata exclusion and other result kinds are unresolved.
/// </summary>
public sealed record BundleSearchRequest(string? Query = null, string? ExcludedQuery = null,
    PackageSearchFields Fields = PackageSearchFields.SourceName | PackageSearchFields.AssetName,
    PackageSearchFields? ExclusionFields = null, bool ExactText = false,
    bool RequireNonEmptyMemo = false, long? PathId = null, int? ClassId = null, string? TypeName = null,
    BundleSearchSort SortBy = BundleSearchSort.Name,
    PackageSearchSortDirection SortDirection = PackageSearchSortDirection.Ascending,
    PackageSearchResultKinds ResultKinds = PackageSearchResultKinds.SourceFile);

public sealed record BundleSearchKey(Guid PackageId, Guid GenerationId, Guid SourceId, Guid SourceRevision);
public sealed record BundleSearchResult(BundleSearchKey Key, string Name, string Package,
    string PackagePath, string OriginalSourcePath, string IndexDirectory, string IndexStatus,
    PackageSearchMatchCategory MatchEvidence);
public sealed record BundleSearchSessionResult(SearchStatus Status, BundleSearchSession? Session, string Message);
public sealed record BundleSearchPage(SearchStatus Status, IReadOnlyList<BundleSearchResult> Rows,
    BundleSearchCursor? NextCursor, bool HasMore, SearchCount Count,
    IReadOnlyList<SearchDiagnostic> Diagnostics, IReadOnlyList<SearchRevision> Revisions);

/// <summary>No connection or result universe retained; lifecycle and cursors reuse the existing engine.</summary>
public sealed class BundleSearchSession : IDisposable
{
    internal SearchSession Inner { get; }
    internal BundleSearchSession(SearchSession inner) => Inner = inner;
    public BundleSearchRequest Criteria => Inner.Bundle!;
    public IReadOnlyList<SearchRevision> Revisions => Inner.Revisions;
    public IReadOnlyList<SearchDiagnostic> Diagnostics => Inner.Diagnostics;
    public bool IsStale => Inner.IsStale;
    public void Cancel() => Inner.Cancel();
    public void Invalidate() => Inner.Invalidate();
    public void Dispose() => Inner.Dispose();
}

public sealed class BundleSearchCursor
{
    internal SearchCursor Inner { get; }
    internal BundleSearchCursor(SearchCursor inner) => Inner = inner;
}

public sealed partial class V2SearchService
{
    public BundleSearchSessionResult CreateBundleSession(BundleSearchRequest request, IReadOnlyList<Guid> selectedRegistrationIds)
    {
        ArgumentNullException.ThrowIfNull(request);
        const PackageSearchFields supported = PackageSearchFields.SourceName | PackageSearchFields.AssetName | PackageSearchFields.Memo;
        const PackageSearchFields assetFields = PackageSearchFields.AssetName | PackageSearchFields.Memo;
        bool excludes = new PackageSearchRequest(request.ExcludedQuery).IncludeTerms.Count > 0;
        if ((request.Fields & ~supported) != 0 || request.ResultKinds != PackageSearchResultKinds.SourceFile
            || !Enum.IsDefined(request.SortBy) || !Enum.IsDefined(request.SortDirection)
            || (request.ExclusionFields is { } fields && (fields & ~assetFields) != 0)
            || (excludes && (request.ExclusionFields is null or PackageSearchFields.None)))
            return new(SearchStatus.InvalidRequest, null,
                "Bundle projection supports SourceName/AssetName/Memo and SourceFile results only. "
                + "Exclusions require explicit AssetName/Memo scope. Other field/result-kind mappings remain unresolved.");
        if (new PackageSearchRequest(request.Query).IncludeTerms.Count == 0 && request.PathId is null)
            return new(SearchStatus.InvalidRequest, null, "An include keyword or signed Path ID is required.");
        if (request.Fields == PackageSearchFields.None && request.PathId is null)
            return new(SearchStatus.InvalidRequest, null, "At least one positive text field is required without a Path ID.");
        SearchRequest inner = new(request.Query, request.ExcludedQuery, request.Fields, PackageSearchResultKinds.SourceFile,
            request.ExactText, request.RequireNonEmptyMemo, request.PathId,
            request.SortBy == BundleSearchSort.Name ? PackageSearchSortField.SourceName : PackageSearchSortField.PackageName,
            request.SortDirection, request.ClassId, request.TypeName);
        SearchSessionResult created = CreateSessionCore(inner, selectedRegistrationIds, request);
        return new(created.Status, created.Session is null ? null : new(created.Session), created.Message);
    }

    public async Task<BundleSearchPage> ReadBundlePageAsync(BundleSearchSession session, BundleSearchCursor? cursor = null,
        int pageSize = 100, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        SearchPage page = await ReadPageAsync(session.Inner, cursor?.Inner, pageSize, token).ConfigureAwait(false);
        var rows = page.Rows.Select(row => new BundleSearchResult(
            new(row.Key.PackageId, row.Key.GenerationId, row.Key.SourceId!.Value, row.Key.SourceRevision!.Value),
            row.SourceName!, row.PackageName, row.PackagePath, row.SourcePath!, row.IndexDirectory!,
            row.IdentityStatus!, row.MatchCategories)).ToArray();
        return new(page.Status, Array.AsReadOnly(rows), page.NextCursor is null ? null : new(page.NextCursor),
            page.HasMore, page.Count, page.Diagnostics, page.Revisions);
    }

    public Task<SearchCountResult> CountBundlesAsync(BundleSearchSession session, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CountAsync(session.Inner, token);
    }
}
