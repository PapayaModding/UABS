namespace UABS.UnityAssets.Packages.Search;

public interface IPackageSearchService
{
    IReadOnlyList<PackageSearchResult> Search(
        IReadOnlyList<PackageSearchSource> sources,
        PackageSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageSearchResult>> SearchAsync(
        IReadOnlyList<PackageSearchSource> sources,
        PackageSearchRequest request,
        CancellationToken cancellationToken = default);
}
