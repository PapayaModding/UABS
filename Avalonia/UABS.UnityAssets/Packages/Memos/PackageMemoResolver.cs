namespace UABS.UnityAssets.Packages.Memos;

public enum PackageMemoResolutionStatus
{
    Available,
    NoMatch,
    Ambiguous,
    Unsupported,
    MissingOrStale
}

public sealed record PackageMemoCatalogEntry(
    string Name,
    string Path,
    PackageDocument Package);

public sealed record AssetMemoLookup(
    string SourcePath,
    string AssetName,
    long PathId,
    bool HasAmbiguousLogicalFileIdentity = false,
    string? LogicalFile = null,
    string? SourceSha256 = null);

public sealed record PackageMemoResolution(
    PackageMemoResolutionStatus Status,
    PackageMemoIdentity? Identity,
    string? PackageName,
    string? PackagePath,
    string? Value,
    PackageMemoStorageState? StorageState,
    string Diagnostic)
{
    public bool IsAvailable => Status == PackageMemoResolutionStatus.Available;
}

public interface IPackageMemoResolver
{
    Task<PackageMemoResolution> ResolveAssetAsync(IReadOnlyList<PackageMemoCatalogEntry> packages,
        AssetMemoLookup lookup, CancellationToken cancellationToken = default) => Task.FromResult(ResolveAsset(packages, lookup));
    PackageMemoResolution ResolveExact(
        IReadOnlyList<PackageMemoCatalogEntry> packages,
        PackageMemoIdentity identity);

    PackageMemoResolution ResolveAsset(
        IReadOnlyList<PackageMemoCatalogEntry> packages,
        AssetMemoLookup lookup);
}

/// <summary>
/// Resolves read-only memo identities against already loaded package metadata.
/// This service never edits a package document and never performs disk I/O.
/// </summary>
public sealed class PackageMemoResolver : IPackageMemoResolver
{
    private readonly IPackageMemoService _memoService;

    public PackageMemoResolver(IPackageMemoService? memoService = null) =>
        _memoService = memoService ?? new PackageMemoService();

    public PackageMemoResolution ResolveExact(
        IReadOnlyList<PackageMemoCatalogEntry> packages,
        PackageMemoIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.V2 is not null)
            return new(PackageMemoResolutionStatus.Unsupported, identity, null, identity.PackagePath, null, null,
                "V2 identity must be resolved against its registered SQLite package.");

        if (string.IsNullOrWhiteSpace(identity.PackagePath)
            || string.IsNullOrWhiteSpace(identity.SourcePath)
            || string.IsNullOrWhiteSpace(identity.AssetName))
        {
            return MissingOrStale("The selected result does not contain a complete package memo identity.");
        }

        PackageMemoCatalogEntry[] packageMatches = packages
            .Where(package => PackagePathComparer.Equals(package.Path, identity.PackagePath))
            .Take(2)
            .ToArray();
        if (packageMatches.Length == 0)
            return MissingOrStale("The package that produced this result is no longer loaded.");
        if (packageMatches.Length > 1)
            return Ambiguous("More than one loaded package has this package path.");

        PackageMemoCatalogEntry package = packageMatches[0];
        PackageMemoReadResult read = _memoService.Read(package.Package, identity);
        return read.Status switch
        {
            PackageMemoLookupStatus.Found => Available(package, read),
            PackageMemoLookupStatus.Ambiguous => Ambiguous(read.Diagnostic),
            _ => MissingOrStale(read.Diagnostic)
        };
    }

    public PackageMemoResolution ResolveAsset(
        IReadOnlyList<PackageMemoCatalogEntry> packages,
        AssetMemoLookup lookup)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(lookup);

        if (lookup.HasAmbiguousLogicalFileIdentity)
        {
            return Ambiguous(
                "This bundle contains more than one logical-file asset with the same package identity. " +
                "The legacy package format cannot identify one safely.");
        }

        if (string.IsNullOrWhiteSpace(lookup.SourcePath) || string.IsNullOrWhiteSpace(lookup.AssetName))
            return MissingOrStale("The selected asset does not contain a complete package memo identity.");

        List<(PackageMemoCatalogEntry Package, PackageMemoIdentity Identity)> matches = [];
        foreach (PackageMemoCatalogEntry package in packages)
        {
            foreach (PackageIndex index in package.Package.Indexes)
            {
                foreach (PackageBundleReference source in index.Bundles.Where(source =>
                             PackagePathComparer.Equals(source.Path, lookup.SourcePath)))
                {
                    foreach (PackageAssetReference asset in source.AssetInfos.Where(asset =>
                                 asset.PathId == lookup.PathId
                                 && string.Equals(asset.Name, lookup.AssetName, StringComparison.Ordinal)))
                    {
                        matches.Add((package, new PackageMemoIdentity(
                            package.Path,
                            index.RelativeDirectory,
                            source.Path,
                            asset.Name,
                            asset.PathId)));
                        if (matches.Count > 1)
                            return Ambiguous("More than one loaded package asset matches this asset identity.");
                    }
                }
            }
        }

        if (matches.Count == 0)
        {
            return new PackageMemoResolution(
                PackageMemoResolutionStatus.NoMatch,
                null,
                null,
                null,
                null,
                null,
                "No loaded package contains this asset identity.");
        }

        (PackageMemoCatalogEntry matchedPackage, PackageMemoIdentity matchedIdentity) = matches[0];
        PackageMemoReadResult read = _memoService.Read(matchedPackage.Package, matchedIdentity);
        return read.Status switch
        {
            PackageMemoLookupStatus.Found => Available(matchedPackage, read),
            PackageMemoLookupStatus.Ambiguous => Ambiguous(read.Diagnostic),
            _ => MissingOrStale(read.Diagnostic)
        };
    }

    private static PackageMemoResolution Available(
        PackageMemoCatalogEntry package,
        PackageMemoReadResult read) => new(
            PackageMemoResolutionStatus.Available,
            read.Identity,
            package.Name,
            package.Path,
            read.Value,
            read.StorageState,
            read.Diagnostic);

    private static PackageMemoResolution Ambiguous(string diagnostic) => new(
        PackageMemoResolutionStatus.Ambiguous,
        null,
        null,
        null,
        null,
        null,
        diagnostic);

    private static PackageMemoResolution MissingOrStale(string diagnostic) => new(
        PackageMemoResolutionStatus.MissingOrStale,
        null,
        null,
        null,
        null,
        null,
        diagnostic);
}
