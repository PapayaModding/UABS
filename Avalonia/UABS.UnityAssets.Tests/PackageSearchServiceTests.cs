using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

public sealed class PackageSearchServiceTests
{
    private readonly PackageSearchService _service = new();

    [Fact]
    public void Search_FindsPackageNameCaseInsensitively()
    {
        IReadOnlyList<PackageSearchResult> results = _service.Search(
            CreateSources(),
            new PackageSearchRequest("ALPHA PACKAGE")
            {
                Fields = PackageSearchFields.PackageName,
                ResultKinds = PackageSearchResultKinds.Package
            });

        PackageSearchResult result = Assert.Single(results);
        Assert.Equal(PackageSearchResultKind.Package, result.Kind);
        Assert.Equal("Alpha Package", result.PackageName);
        Assert.Equal(PackageSearchMatchCategory.PackageName, result.MatchCategories);
    }

    [Fact]
    public void Search_FindsPackagePathMetadata()
    {
        IReadOnlyList<PackageSearchResult> results = _service.Search(
            CreateSources(),
            new PackageSearchRequest("packages\\beta")
            {
                Fields = PackageSearchFields.PackagePath,
                ResultKinds = PackageSearchResultKinds.Package
            });

        PackageSearchResult result = Assert.Single(results);
        Assert.Equal("Beta Package", result.PackageName);
        Assert.Equal(PackageSearchMatchCategory.PackagePath, result.MatchCategories);
    }

    [Fact]
    public void Search_FindsAssetNamesAndPreservesOriginalReference()
    {
        IReadOnlyList<PackageSearchResult> results = _service.Search(
            CreateSources(),
            AssetRequest("portrait"));

        Assert.Equal(2, results.Count);
        Assert.All(results, result =>
        {
            Assert.Equal(PackageSearchResultKind.Asset, result.Kind);
            Assert.Contains("portrait", result.AssetName!, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(result.SourcePath);
            Assert.True(result.PathId.HasValue);
        });
        Assert.Contains(results, result => result.SourcePath == MissingPath("alpha.ab") && result.PathId == 7);
        Assert.Contains(results, result => result.SourcePath == MissingPath("beta.ab") && result.PathId == 7);
    }

    [Fact]
    public void Search_FindsMemoWithoutOpeningMissingSource()
    {
        PackageSearchRequest request = new("scarlet")
        {
            Fields = PackageSearchFields.Memo,
            ResultKinds = PackageSearchResultKinds.Asset
        };

        PackageSearchResult result = Assert.Single(_service.Search(CreateSources(), request));

        Assert.Equal("hero scarlet", result.Memo);
        Assert.False(File.Exists(result.SourcePath));
        Assert.Equal(PackageSearchMatchCategory.Memo, result.MatchCategories);
    }

    [Fact]
    public void Search_MemoOnlyFiltersAssetsIndependentlyFromMemoTextMatching()
    {
        PackageSearchRequest withoutMemoText = new("portrait")
        {
            Fields = PackageSearchFields.AssetName,
            ResultKinds = PackageSearchResultKinds.Asset,
            RequireNonEmptyMemo = true
        };
        PackageSearchRequest withMemoText = new("scarlet")
        {
            Fields = PackageSearchFields.AssetName | PackageSearchFields.Memo,
            ResultKinds = PackageSearchResultKinds.Asset,
            RequireNonEmptyMemo = true
        };

        IReadOnlyList<PackageSearchResult> nameResults = _service.Search(CreateSources(), withoutMemoText);
        PackageSearchResult memoResult = Assert.Single(_service.Search(CreateSources(), withMemoText));

        Assert.Equal(2, nameResults.Count);
        Assert.All(nameResults, result => Assert.False(string.IsNullOrWhiteSpace(result.Memo)));
        Assert.Equal("Alpha Portrait", memoResult.AssetName);
        Assert.Equal(PackageSearchMatchCategory.Memo, memoResult.MatchCategories);
    }

    [Fact]
    public void Search_FindsSourcePathWithoutRequiringItToExist()
    {
        PackageSearchRequest request = new("missing-sources\\alpha")
        {
            Fields = PackageSearchFields.SourcePath,
            ResultKinds = PackageSearchResultKinds.SourceFile
        };

        PackageSearchResult result = Assert.Single(_service.Search(CreateSources(), request));

        Assert.Equal(MissingPath("alpha.ab"), result.SourcePath);
        Assert.False(File.Exists(result.SourcePath));
        Assert.Equal(PackageSearchMatchCategory.SourcePath, result.MatchCategories);
    }

    [Fact]
    public void Search_UsesExactNumericPathIdAndKeepsDuplicateIdsDistinctBySource()
    {
        PackageSearchRequest request = new()
        {
            PathId = 7,
            ResultKinds = PackageSearchResultKinds.Asset,
            SortBy = PackageSearchSortField.SourcePath
        };

        IReadOnlyList<PackageSearchResult> results = _service.Search(CreateSources(), request);

        Assert.Equal(2, results.Count);
        Assert.All(results, result =>
        {
            Assert.Equal((long)7, result.PathId);
            Assert.True(result.MatchCategories.HasFlag(PackageSearchMatchCategory.PathId));
        });
        Assert.Equal(2, results.Select(result => result.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Search_EmptyQueryReturnsNoResultsUnlessPathIdIsProvided()
    {
        Assert.Empty(_service.Search(CreateSources(), new PackageSearchRequest()));
        Assert.Empty(_service.Search(CreateSources(), new PackageSearchRequest(" , ， ")));
        Assert.NotEmpty(_service.Search(CreateSources(), new PackageSearchRequest { PathId = 7 }));
    }

    [Fact]
    public void Search_ToleratesNullAndMissingOptionalMetadata()
    {
        PackageBundleReference bundle = new()
        {
            Name = null!,
            Path = MissingPath("null.ab"),
            CabCode = null!,
            Dependencies = null!,
            AssetInfos = [new PackageAssetReference { Name = null!, PathId = 9, Memo = null }]
        };
        PackageDocument package = new([new PackageIndex(string.Empty, [bundle])], packagePath: null);
        PackageSearchSource source = new("Null Package", package);

        IReadOnlyList<PackageSearchResult> results = _service.Search(
            [source],
            new PackageSearchRequest("anything"));

        Assert.Empty(results);
    }

    [Fact]
    public void Search_ParsesEnglishAndChineseCommaTermsAsCaseInsensitiveOr()
    {
        PackageSearchRequest request = new("PORTRAIT，icon")
        {
            Fields = PackageSearchFields.AssetName,
            ResultKinds = PackageSearchResultKinds.Asset
        };

        IReadOnlyList<PackageSearchResult> results = _service.Search(CreateSources(), request);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, result => result.AssetName == "Icon Asset");
    }

    [Fact]
    public void Search_ExclusionsTakePrecedenceWithinCandidateMetadata()
    {
        PackageSearchRequest request = new("portrait", "scarlet")
        {
            Fields = PackageSearchFields.AssetName | PackageSearchFields.Memo,
            ResultKinds = PackageSearchResultKinds.Asset
        };

        IReadOnlyList<PackageSearchResult> results = _service.Search(CreateSources(), request);

        PackageSearchResult result = Assert.Single(results);
        Assert.Equal("Beta Portrait", result.AssetName);
    }

    [Fact]
    public void Search_ExactTextRequiresWholeFieldEquality()
    {
        PackageSearchRequest partial = new("portrait")
        {
            Fields = PackageSearchFields.AssetName,
            ResultKinds = PackageSearchResultKinds.Asset,
            ExactText = true
        };
        PackageSearchRequest exact = new("Alpha Portrait")
        {
            Fields = PackageSearchFields.AssetName,
            ResultKinds = PackageSearchResultKinds.Asset,
            ExactText = true
        };

        Assert.Empty(_service.Search(CreateSources(), partial));
        Assert.Equal("Alpha Portrait", Assert.Single(_service.Search(CreateSources(), exact)).AssetName);
    }

    [Fact]
    public void Search_FindsRepresentedIndexCabAndDependencyMetadata()
    {
        PackageSearchRequest request = new("dependency-shared")
        {
            Fields = PackageSearchFields.Dependency,
            ResultKinds = PackageSearchResultKinds.SourceFile
        };

        IReadOnlyList<PackageSearchResult> results = _service.Search(CreateSources(), request);

        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.True(result.MatchCategories.HasFlag(PackageSearchMatchCategory.Dependency)));
        Assert.All(results, result => Assert.Contains("CAB-dependency-shared", result.Dependencies));
    }

    [Fact]
    public void Search_SortsPathIdsNumericallyAndDeterministically()
    {
        PackageSearchRequest ascending = WithSort(PackageSearchSortDirection.Ascending);
        PackageSearchRequest descending = WithSort(PackageSearchSortDirection.Descending);

        long?[] ascendingIds = _service.Search(CreateSources(reverse: true), ascending).Select(result => result.PathId).ToArray();
        long?[] descendingIds = _service.Search(CreateSources(), descending).Select(result => result.PathId).ToArray();

        Assert.Equal([2L, 10L], ascendingIds);
        Assert.Equal([10L, 2L], descendingIds);

        PackageSearchRequest WithSort(PackageSearchSortDirection direction) => new("asset")
        {
            Fields = PackageSearchFields.AssetName,
            ResultKinds = PackageSearchResultKinds.Asset,
            SortBy = PackageSearchSortField.PathId,
            SortDirection = direction
        };
    }

    [Fact]
    public async Task SearchAsync_HonorsCancellationWithoutReturningPartialResults()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _service.SearchAsync(CreateSources(), AssetRequest("portrait"), cancellation.Token));
    }

    [Fact]
    public void Search_CancellationAfterWorkBeginsDoesNotReturnAccumulatedResults()
    {
        using CancellationTokenSource cancellation = new();
        CancelAfterFirstSource sources = new(CreateSources(), cancellation);

        Assert.Throws<OperationCanceledException>(() =>
            _service.Search(sources, AssetRequest("portrait"), cancellation.Token));
    }

    [Fact]
    public void Search_MultiplePackagesWithSimilarEntriesRemainIdentifiable()
    {
        IReadOnlyList<PackageSearchResult> results = _service.Search(CreateSources(), AssetRequest("portrait"));

        Assert.Equal(["Alpha Package", "Beta Package"], results.Select(result => result.PackageName).ToArray());
        Assert.All(results, result => Assert.False(string.IsNullOrWhiteSpace(result.PackagePath)));
    }

    [Fact]
    public void Search_ReturnsReadOnlySnapshotsAndDoesNotMutatePackageModels()
    {
        IReadOnlyList<PackageSearchSource> sources = CreateSources();
        PackageBundleReference original = sources[0].Package.Bundles.First();
        string originalPath = original.Path;
        string originalMemo = original.AssetInfos[0].Memo!;

        IReadOnlyList<PackageSearchResult> results = _service.Search(sources, AssetRequest("portrait"));
        PackageSearchResult result = results[0];

        Assert.Equal(originalPath, original.Path);
        Assert.Equal(originalMemo, original.AssetInfos[0].Memo);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)result.Dependencies).Add("mutation"));
        Assert.DoesNotContain("mutation", original.Dependencies);
    }

    [Fact]
    public void Search_NoMatchReturnsEmptyReadOnlyResult()
    {
        IReadOnlyList<PackageSearchResult> results = _service.Search(CreateSources(), AssetRequest("not-present"));

        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_WorksWithLoadedLegacyFixtureWhenReferencedSourceIsMissing()
    {
        PackageDocument package = await new PackageService(TestPaths.ClassData)
            .LoadAsync(TestPaths.LegacyPackageFixture);
        PackageSearchSource source = PackageSearchSource.FromDocument(package);

        IReadOnlyList<PackageSearchResult> results = _service.Search(
            [source],
            new PackageSearchRequest("legacy memo")
            {
                Fields = PackageSearchFields.Memo,
                ResultKinds = PackageSearchResultKinds.Asset
            });

        PackageSearchResult result = Assert.Single(results);
        Assert.Equal("portrait", result.AssetName);
        Assert.Equal((long)1001, result.PathId);
        Assert.Equal("C:\\LegacyGame\\Data\\character.ab", result.SourcePath);
        Assert.False(File.Exists(result.SourcePath));
    }

    private static PackageSearchRequest AssetRequest(string query) => new(query)
    {
        Fields = PackageSearchFields.AssetName,
        ResultKinds = PackageSearchResultKinds.Asset,
        SortBy = PackageSearchSortField.PackageName
    };

    private static IReadOnlyList<PackageSearchSource> CreateSources(bool reverse = false)
    {
        PackageBundleReference alpha = new()
        {
            Name = "alpha.ab",
            Path = MissingPath("alpha.ab"),
            CabCode = "CAB-alpha",
            Dependencies = ["CAB-dependency-shared", "CAB-alpha-only"],
            AssetInfos =
            [
                new PackageAssetReference { Name = "Alpha Portrait", PathId = 7, Memo = "hero scarlet" },
                new PackageAssetReference { Name = "Icon Asset", PathId = 10 }
            ]
        };
        PackageBundleReference beta = new()
        {
            Name = "beta.assets",
            Path = MissingPath("beta.ab"),
            CabCode = "CAB-beta",
            Dependencies = ["CAB-dependency-shared"],
            AssetInfos =
            [
                new PackageAssetReference { Name = "Beta Portrait", PathId = 7, Memo = "hero blue" },
                new PackageAssetReference { Name = "Small Asset", PathId = 2 }
            ]
        };

        PackageSearchSource first = new(
            "Alpha Package",
            new PackageDocument([new PackageIndex("characters", [alpha])], packagePath: MissingPath("packages\\alpha")));
        PackageSearchSource second = new(
            "Beta Package",
            new PackageDocument([new PackageIndex("characters", [beta])], packagePath: MissingPath("packages\\beta")));
        return reverse ? [second, first] : [first, second];
    }

    private static string MissingPath(string name)
        => Path.GetFullPath(Path.Combine("missing-sources", name));

    private sealed class CancelAfterFirstSource(
        IReadOnlyList<PackageSearchSource> sources,
        CancellationTokenSource cancellation) : IReadOnlyList<PackageSearchSource>
    {
        public int Count => sources.Count;

        public PackageSearchSource this[int index] => sources[index];

        public IEnumerator<PackageSearchSource> GetEnumerator()
        {
            for (int index = 0; index < sources.Count; index++)
            {
                if (index == 1)
                    cancellation.Cancel();
                yield return sources[index];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
