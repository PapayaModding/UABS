namespace UABS.UnityAssets.Packages.Dependencies;

public interface IPackageDependencyResolver
{
    IReadOnlyList<PackageDependencyResult> Resolve(
        IReadOnlyList<PackageDependencySource> sources,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageDependencyResult>> ResolveAsync(
        IReadOnlyList<PackageDependencySource> sources,
        CancellationToken cancellationToken = default);

    IReadOnlyList<PackageDependencyResult> ResolveForPackage(
        IReadOnlyList<PackageDependencySource> catalog,
        PackageDependencySource requester,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageDependencyResult>> ResolveForPackageAsync(
        IReadOnlyList<PackageDependencySource> catalog,
        PackageDependencySource requester,
        CancellationToken cancellationToken = default);
}
