using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.UnityAssets.Tests;

public sealed class PackageMemoResolverTests
{
    private readonly PackageMemoResolver _resolver = new();

    [Theory]
    [InlineData(null, PackageMemoStorageState.Missing)]
    [InlineData("", PackageMemoStorageState.Empty)]
    [InlineData("a memo that must not be truncated", PackageMemoStorageState.Value)]
    public void ResolveExact_PreservesFullMemoAndStorageState(string? memo, PackageMemoStorageState state)
    {
        PackageDocument package = Document("alpha", Bundle("source.ab", Asset("Hero", 7, memo)));

        PackageMemoResolution result = _resolver.ResolveExact(
            [Catalog("Alpha", package)],
            Identity(package, "Hero", 7));

        Assert.Equal(PackageMemoResolutionStatus.Available, result.Status);
        Assert.Equal(state, result.StorageState);
        Assert.Equal(memo, result.Value);
        Assert.Equal("Alpha", result.PackageName);
    }

    [Fact]
    public void ResolveExact_ReportsUnloadedAndStaleProvenanceWithoutFallingBack()
    {
        PackageDocument package = Document("alpha", Bundle("source.ab", Asset("Hero", 7, "memo")));
        PackageMemoIdentity stale = Identity(package, "Hero", 99);

        PackageMemoResolution unloaded = _resolver.ResolveExact([], Identity(package, "Hero", 7));
        PackageMemoResolution missingAsset = _resolver.ResolveExact([Catalog("Alpha", package)], stale);

        Assert.Equal(PackageMemoResolutionStatus.MissingOrStale, unloaded.Status);
        Assert.Equal(PackageMemoResolutionStatus.MissingOrStale, missingAsset.Status);
    }

    [Fact]
    public void ResolveExact_ReportsDuplicateCompleteIdentityAsAmbiguous()
    {
        PackageBundleReference source = Bundle("source.ab", Asset("Hero", 7, "first"), Asset("Hero", 7, "second"));
        PackageDocument package = Document("alpha", source);

        PackageMemoResolution result = _resolver.ResolveExact(
            [Catalog("Alpha", package)],
            Identity(package, "Hero", 7));

        Assert.Equal(PackageMemoResolutionStatus.Ambiguous, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ResolveAsset_FindsExactlyOneTextureAcrossLoadedCatalog()
    {
        PackageDocument package = Document("alpha", Bundle("source.ab", Asset("Hero", 7, "complete memo")));

        PackageMemoResolution result = _resolver.ResolveAsset(
            [Catalog("Alpha", package)],
            new AssetMemoLookup(@"C:\game\source.ab", "Hero", 7));

        Assert.Equal(PackageMemoResolutionStatus.Available, result.Status);
        Assert.Equal("complete memo", result.Value);
        Assert.NotNull(result.Identity);
    }

    [Fact]
    public void ResolveAsset_DistinguishesNoMatchAndLogicalFileAmbiguity()
    {
        PackageDocument package = Document("alpha", Bundle("source.ab", Asset("Hero", 7, "memo")));
        PackageMemoCatalogEntry[] catalog = [Catalog("Alpha", package)];

        PackageMemoResolution noMatch = _resolver.ResolveAsset(
            catalog,
            new AssetMemoLookup(@"C:\game\other.ab", "Hero", 7));
        PackageMemoResolution multiFile = _resolver.ResolveAsset(
            catalog,
            new AssetMemoLookup(@"C:\game\source.ab", "Hero", 7, true));

        Assert.Equal(PackageMemoResolutionStatus.NoMatch, noMatch.Status);
        Assert.Equal(PackageMemoResolutionStatus.Ambiguous, multiFile.Status);
    }

    [Theory]
    [InlineData("TextAsset")]
    [InlineData("AudioClip")]
    [InlineData("MonoBehaviour")]
    public void ResolveAsset_IsTypeAgnosticWhenPackageRecordExists(string assetName)
    {
        PackageDocument package = Document("alpha", Bundle("source.ab", Asset(assetName, 7, "memo")));

        PackageMemoResolution result = _resolver.ResolveAsset(
            [Catalog("Alpha", package)],
            new AssetMemoLookup(@"C:\game\source.ab", assetName, 7));

        Assert.Equal(PackageMemoResolutionStatus.Available, result.Status);
        Assert.Equal("memo", result.Value);
    }

    [Fact]
    public void ResolveAsset_MultiplePackagesNeverSelectsFirst()
    {
        PackageDocument first = Document("first", Bundle("source.ab", Asset("Hero", 7, "first")));
        PackageDocument second = Document("second", Bundle("source.ab", Asset("Hero", 7, "second")));

        PackageMemoResolution result = _resolver.ResolveAsset(
            [Catalog("First", first), Catalog("Second", second)],
            new AssetMemoLookup(@"C:\game\source.ab", "Hero", 7));

        Assert.Equal(PackageMemoResolutionStatus.Ambiguous, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ResolutionIsReadOnly()
    {
        PackageDocument package = Document("alpha", Bundle("source.ab", Asset("Hero", 7, "memo")));
        string? before = package.Bundles.Single().AssetInfos.Single().Memo;

        _resolver.ResolveExact([Catalog("Alpha", package)], Identity(package, "Hero", 7));
        _resolver.ResolveAsset(
            [Catalog("Alpha", package)],
            new AssetMemoLookup(@"C:\game\source.ab", "Hero", 7));

        Assert.Equal(before, package.Bundles.Single().AssetInfos.Single().Memo);
    }

    private static PackageMemoCatalogEntry Catalog(string name, PackageDocument package) =>
        new(name, package.PackagePath!, package);

    private static PackageMemoIdentity Identity(PackageDocument package, string assetName, long pathId) =>
        new(package.PackagePath, string.Empty, @"C:\game\source.ab", assetName, pathId);

    private static PackageDocument Document(string name, params PackageBundleReference[] bundles) =>
        new([new PackageIndex(string.Empty, bundles)], $@"C:\packages\{name}");

    private static PackageBundleReference Bundle(string name, params PackageAssetReference[] assets) => new()
    {
        Name = name,
        Path = $@"C:\game\{name}",
        AssetInfos = assets.ToList()
    };

    private static PackageAssetReference Asset(string name, long pathId, string? memo) =>
        new() { Name = name, PathId = pathId, Memo = memo };
}
