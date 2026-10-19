using System.Text.Json;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.UnityAssets.Tests;

public sealed class PackageMemoServiceTests
{
    private readonly PackageMemoService _service = new();

    [Theory]
    [InlineData(null, PackageMemoStorageState.Missing)]
    [InlineData("", PackageMemoStorageState.Empty)]
    [InlineData("notes", PackageMemoStorageState.Value)]
    public void Read_DistinguishesMissingEmptyAndStoredValues(string? memo, PackageMemoStorageState expected)
    {
        PackageDocument package = CreatePackage("pkg", "folder", "C:\\game\\a.bundle", "asset", 7, memo);

        PackageMemoReadResult result = _service.Read(package, Identity(package, "folder", "C:\\game\\a.bundle", "asset", 7));

        Assert.Equal(PackageMemoLookupStatus.Found, result.Status);
        Assert.Equal(expected, result.StorageState);
        Assert.Equal(memo, result.Value);
    }

    [Fact]
    public void SetLocalMemo_UsesSourceAndIndexInAdditionToPathId()
    {
        PackageDocument package = new(
        [
            new PackageIndex("one",
            [
                Bundle("C:\\game\\a.bundle", "first", 42, null),
                Bundle("C:\\game\\b.bundle", "second", 42, null)
            ]),
            new PackageIndex("two", [Bundle("C:\\game\\a.bundle", "first", 42, null)])
        ],
        "C:\\packages\\pkg");

        bool changed = _service.SetLocalMemo(
            package,
            Identity(package, "one", "C:\\game\\b.bundle", "second", 42),
            "target only");

        Assert.True(changed);
        Assert.Null(package.Indexes[0].Bundles[0].AssetInfos[0].Memo);
        Assert.Equal("target only", package.Indexes[0].Bundles[1].AssetInfos[0].Memo);
        Assert.Null(package.Indexes[1].Bundles[0].AssetInfos[0].Memo);
    }

    [Fact]
    public void SetLocalMemo_RejectsAmbiguousCompleteIdentity()
    {
        PackageBundleReference duplicate = Bundle("C:\\game\\a.bundle", "same", 9, null);
        PackageDocument package = new(
        [new PackageIndex("", [duplicate, duplicate.Clone()])],
        "C:\\packages\\pkg");

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            _service.SetLocalMemo(package, Identity(package, "", "C:\\game\\a.bundle", "same", 9), "memo"));

        Assert.Contains("ambiguous", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.All(package.Bundles, bundle => Assert.Null(bundle.AssetInfos[0].Memo));
    }

    [Fact]
    public void SetLocalMemo_PreservesExplicitEmptyAsDifferentFromMissing()
    {
        PackageDocument package = CreatePackage("pkg", "", "C:\\game\\a.bundle", "asset", 7, null);
        PackageMemoIdentity identity = Identity(package, "", "C:\\game\\a.bundle", "asset", 7);

        Assert.True(_service.SetLocalMemo(package, identity, ""));

        PackageMemoReadResult result = _service.Read(package, identity);
        Assert.Equal(PackageMemoStorageState.Empty, result.StorageState);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void Inherit_SafeCopiesOnlyNonWhitespaceMemoIntoEmptyTarget()
    {
        PackageDocument source = PackageWithAssets(("copy", "source"), ("blank", " "), ("occupied", "replacement"));
        PackageDocument target = PackageWithAssets(("copy", null), ("blank", null), ("occupied", "keep"));

        PackageMemoInheritanceResult result = _service.Inherit(source, target, PackageMemoInheritanceMode.Safe);

        Assert.Equal(1, result.ChangedCount);
        Assert.Equal(2, result.SkippedByModeCount);
        Assert.Equal("source", Find(result.Document, "copy").Memo);
        Assert.Null(Find(result.Document, "blank").Memo);
        Assert.Equal("keep", Find(result.Document, "occupied").Memo);
        Assert.Null(Find(target, "copy").Memo);
    }

    [Fact]
    public void Inherit_OverwriteIgnoresBlankSourceButReplacesExistingTarget()
    {
        PackageDocument source = PackageWithAssets(("copy", "source"), ("blank", ""));
        PackageDocument target = PackageWithAssets(("copy", "old"), ("blank", "old"));

        PackageMemoInheritanceResult result = _service.Inherit(source, target, PackageMemoInheritanceMode.Overwrite);

        Assert.Equal(1, result.ChangedCount);
        Assert.Equal(1, result.SkippedByModeCount);
        Assert.Equal("source", Find(result.Document, "copy").Memo);
        Assert.Equal("old", Find(result.Document, "blank").Memo);
    }

    [Fact]
    public void Inherit_ForceCopiesEmptyAndMissingValues()
    {
        PackageDocument source = PackageWithAssets(("empty", ""), ("missing", null));
        PackageDocument target = PackageWithAssets(("empty", "old"), ("missing", "old"));

        PackageMemoInheritanceResult result = _service.Inherit(source, target, PackageMemoInheritanceMode.Force);

        Assert.Equal(2, result.ChangedCount);
        Assert.Equal(string.Empty, Find(result.Document, "empty").Memo);
        Assert.Null(Find(result.Document, "missing").Memo);
    }

    [Fact]
    public void Inherit_UsesSameRelativeIndexAndPreservesEmptyIndexesAndUnknownFields()
    {
        using JsonDocument json = JsonDocument.Parse("\"kept\"");
        PackageDocument source = CreatePackage("source", "matching", "C:\\old\\atlas_hash.bundle", "asset", 1, "copied");
        PackageDocument target = new(
        [
            new PackageIndex("matching", [Bundle("D:\\new\\atlas_other.bundle", "asset", 2, null)]),
            new PackageIndex("other", [Bundle("D:\\new\\atlas_other.bundle", "asset", 2, "untouched")]),
            new PackageIndex("empty")
        ],
        "C:\\packages\\target");
        target.Indexes[0].Bundles[0].AssetInfos[0].ExtensionData = new() { ["Future"] = json.RootElement.Clone() };

        PackageMemoInheritanceResult result = _service.Inherit(source, target, PackageMemoInheritanceMode.Safe);

        Assert.Equal("copied", result.Document.Indexes[0].Bundles[0].AssetInfos[0].Memo);
        Assert.Equal("untouched", result.Document.Indexes[1].Bundles[0].AssetInfos[0].Memo);
        Assert.Empty(result.Document.Indexes[2].Bundles);
        Assert.Equal("kept", result.Document.Indexes[0].Bundles[0].AssetInfos[0].ExtensionData!["Future"].GetString());
    }

    [Fact]
    public void Inherit_ReportsAmbiguousBundleAndAssetNamesInsteadOfSelectingFirst()
    {
        PackageDocument source = new(
        [new PackageIndex("", [
            Bundle("C:\\old\\atlas_a.bundle", "duplicate", 1, "one"),
            Bundle("C:\\old\\atlas_b.bundle", "duplicate", 2, "two")])]);
        PackageDocument target = new(
        [new PackageIndex("", [Bundle("D:\\new\\atlas_c.bundle", "duplicate", 3, null)])]);

        PackageMemoInheritanceResult result = _service.Inherit(source, target, PackageMemoInheritanceMode.Force);

        Assert.Equal(0, result.ChangedCount);
        Assert.Equal(1, result.AmbiguousMatchCount);
        Assert.Single(result.Issues);
        Assert.Null(Find(result.Document, "duplicate").Memo);
    }

    [Fact]
    public void Inherit_HandlesBundleNamesWithoutUnderscores()
    {
        PackageDocument source = CreatePackage("source", "", "C:\\old\\plain.bundle", "asset", 1, "memo");
        PackageDocument target = CreatePackage("target", "", "D:\\new\\plain.bundle", "asset", 2, null);

        PackageMemoInheritanceResult result = _service.Inherit(source, target, PackageMemoInheritanceMode.Safe);

        Assert.Equal(1, result.ChangedCount);
        Assert.Equal("memo", Find(result.Document, "asset").Memo);
    }

    [Fact]
    public void Inherit_CancellationDoesNotMutateTarget()
    {
        PackageDocument source = PackageWithAssets(("asset", "memo"));
        PackageDocument target = PackageWithAssets(("asset", null));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            _service.Inherit(source, target, PackageMemoInheritanceMode.Safe, cancellation.Token));
        Assert.Null(Find(target, "asset").Memo);
    }

    [Fact]
    public async Task VerifiedLegacyMemoRepresentation_SaveAndFreshReopenPreservesMemoAndMissingField()
    {
        PackageService packages = new();
        PackageDocument legacy = await packages.LoadAsync(TestPaths.LegacyMemoPackageFixture);
        PackageBundleReference source = Assert.Single(legacy.Bundles);
        Assert.Equal("钩爪", source.AssetInfos.Single(asset => asset.Name == "hook").Memo);
        Assert.Null(source.AssetInfos.Single(asset => asset.Name == "octopus_tentacles_59").Memo);

        using TemporaryDirectory temporary = new();
        string output = Path.Combine(temporary.Path, "roundtrip");
        await packages.SaveAsync(legacy, output);
        PackageDocument reopened = await packages.LoadAsync(output);

        PackageBundleReference reopenedSource = Assert.Single(reopened.Bundles);
        Assert.Equal("钩爪", reopenedSource.AssetInfos.Single(asset => asset.Name == "hook").Memo);
        Assert.Null(reopenedSource.AssetInfos.Single(asset => asset.Name == "octopus_tentacles_59").Memo);
        Assert.Single(reopened.Indexes);
        Assert.Equal(Path.Combine("graphiceffecttextureseparatelygroup_assets_assets", "sprites"), reopened.Indexes[0].RelativeDirectory);
    }

    private static PackageDocument PackageWithAssets(params (string Name, string? Memo)[] assets) => new(
    [
        new PackageIndex("", [new PackageBundleReference
        {
            Name = "atlas_version.bundle",
            Path = "C:\\game\\atlas_version.bundle",
            AssetInfos = assets.Select((asset, index) => new PackageAssetReference
            {
                Name = asset.Name,
                PathId = index + 1,
                Memo = asset.Memo
            }).ToList()
        }])
    ],
    "C:\\packages\\pkg");

    private static PackageDocument CreatePackage(
        string packageName,
        string index,
        string sourcePath,
        string assetName,
        long pathId,
        string? memo) => new(
        [new PackageIndex(index, [Bundle(sourcePath, assetName, pathId, memo)])],
        $"C:\\packages\\{packageName}");

    private static PackageBundleReference Bundle(string sourcePath, string assetName, long pathId, string? memo) => new()
    {
        Name = Path.GetFileName(sourcePath),
        Path = sourcePath,
        AssetInfos = [new PackageAssetReference { Name = assetName, PathId = pathId, Memo = memo }]
    };

    private static PackageMemoIdentity Identity(
        PackageDocument package,
        string index,
        string sourcePath,
        string assetName,
        long pathId) => new(package.PackagePath, index, sourcePath, assetName, pathId);

    private static PackageAssetReference Find(PackageDocument package, string name) =>
        package.Bundles.SelectMany(bundle => bundle.AssetInfos).Single(asset => asset.Name == name);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"uabs-memo-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
