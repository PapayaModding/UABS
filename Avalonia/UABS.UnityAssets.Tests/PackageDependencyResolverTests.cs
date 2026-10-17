using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Dependencies;

namespace UABS.UnityAssets.Tests;

public sealed class PackageDependencyResolverTests
{
    private readonly PackageDependencyResolver _resolver = new();

    [Fact]
    public void Resolve_FindsSamePackageDependency()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", @"C:\game\a.ab", "CAB-A", "CAB-B"),
                Bundle("target", @"C:\game\b.ab", "CAB-B")));

        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
        Assert.Equal(@"C:\game\b.ab", Assert.Single(result.Candidates).SourcePath);
    }

    [Fact]
    public void Resolve_FindsCrossPackageDependency()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", @"C:\game\a.ab", "CAB-A", "CAB-B")),
            Source("two", Bundle("target", @"D:\game\b.ab", "CAB-B")));

        Assert.Equal("two", Assert.Single(result.Candidates).Occurrences.Single().PackageName);
    }

    [Fact]
    public void Resolve_MatchesCabCodesCaseInsensitively()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "cab-target"),
                Bundle("target", "b.ab", "CAB-TARGET")));

        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
    }

    [Fact]
    public void Resolve_TrimsPermittedOuterWhitespace()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "  CAB-B  "),
                Bundle("target", "b.ab", " CAB-B ")));

        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
        Assert.Equal("  CAB-B  ", result.RequestedCabCode);
    }

    [Fact]
    public void Resolve_MatchesCandidateMissingOneLegacyCharacter()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-1234"),
                Bundle("target", "b.ab", "CAB-123")));

        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
    }

    [Fact]
    public void Resolve_MatchesRequestMissingOneLegacyCharacter()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-123"),
                Bundle("target", "b.ab", "CAB-1234")));

        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
    }

    [Fact]
    public void Resolve_DoesNotUseArbitraryPrefixMatching()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-1"),
                Bundle("target", "b.ab", "CAB-1234")));

        Assert.Equal(PackageDependencyStatus.Unresolved, result.Status);
    }

    [Fact]
    public void Resolve_ReportsNoMatch()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-MISSING")));

        Assert.Equal(PackageDependencyStatus.Unresolved, result.Status);
        Assert.Empty(result.Candidates);
        Assert.Contains("No loaded", result.Diagnostic);
    }

    [Fact]
    public void Resolve_ReportsDistinctMatchingPathsAsAmbiguous()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-SAME"),
                Bundle("first", "first.ab", "CAB-SAME")),
            Source("two", Bundle("second", "second.ab", "CAB-SAME")));

        Assert.Equal(PackageDependencyStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void Resolve_LegacyTruncationCollisionRemainsAmbiguous()
    {
        PackageDependencyResult result = Resolve(
            Source("request", Bundle("requester", "request.ab", "CAB-REQUEST", "CAB-COLLISION")),
            Source("one", Bundle("first", "first.ab", "CAB-COLLISIONA")),
            Source("two", Bundle("second", "second.ab", "CAB-COLLISIONB")));

        Assert.Equal(PackageDependencyStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void Resolve_DeduplicatesSamePhysicalPathAcrossPackages()
    {
        PackageDependencyResult result = Resolve(
            Source("request", Bundle("requester", "a.ab", "CAB-A", "CAB-SAME")),
            Source("one", Bundle("first", @"C:\game\same.ab", "CAB-SAME")),
            Source("two", Bundle("second", @"C:\game\same.ab", "CAB-SAME")));

        PackageDependencyCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
        Assert.Equal(2, candidate.Occurrences.Count);
    }

    [Fact]
    public void Resolve_NormalizesPathSeparatorsAndCaseForDeduplication()
    {
        PackageDependencyResult result = Resolve(
            Source("request", Bundle("requester", "a.ab", "CAB-A", "CAB-SAME")),
            Source("one", Bundle("first", @"C:\Game\same.ab", "CAB-SAME")),
            Source("two", Bundle("second", "c:/game/same.ab/", "CAB-SAME")));

        Assert.Single(result.Candidates);
    }

    [Fact]
    public void Resolve_DeduplicatesOrdinaryAndExtendedWindowsPaths()
    {
        PackageDependencyResult result = Resolve(
            Source("request", Bundle("requester", "request.ab", "CAB-A", "CAB-SAME")),
            Source("ordinary", Bundle("first", @"C:\Game\same.ab", "CAB-SAME")),
            Source("extended", Bundle("second", @"\\?\c:\game\same.ab", "CAB-SAME")));

        PackageDependencyCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(2, candidate.Occurrences.Count);
    }

    [Fact]
    public void Resolve_PreservesAllDuplicateImportOccurrences()
    {
        PackageDependencyCandidate candidate = Assert.Single(Resolve(
            Source("request", Bundle("requester", "a.ab", "CAB-A", "CAB-SAME")),
            Source("one", Bundle("first", "same.ab", "CAB-SAME")),
            Source("two", Bundle("second", "same.ab", "cab-same"))).Candidates);

        Assert.Equal(["one", "two"], candidate.Occurrences.Select(item => item.PackageName));
        Assert.Equal(["CAB-SAME"], candidate.CabCodes);
    }

    [Fact]
    public void Resolve_ReturnsNullDependencyAsMalformedUnresolved()
    {
        PackageBundleReference requester = Bundle("requester", "a.ab", "CAB-A");
        requester.Dependencies = [null!];

        PackageDependencyResult result = Resolve(Source("one", requester));

        Assert.Equal(PackageDependencyStatus.Unresolved, result.Status);
        Assert.Null(result.RequestedCabCode);
        Assert.Contains("malformed", result.Diagnostic);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-cab")]
    [InlineData("CAB-")]
    [InlineData("CAB-bad value")]
    public void Resolve_ReturnsInvalidDependencyAsMalformedUnresolved(string dependency)
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", dependency)));

        Assert.Equal(PackageDependencyStatus.Unresolved, result.Status);
        Assert.Empty(result.Candidates);
        Assert.Contains("malformed", result.Diagnostic);
    }

    [Fact]
    public void Resolve_IgnoresMalformedCandidateCabCode()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-GOOD"),
                Bundle("target", "b.ab", "bad")));

        Assert.Equal(PackageDependencyStatus.Unresolved, result.Status);
    }

    [Fact]
    public void Resolve_ReturnsNoResultsForSourceWithoutDependencies()
    {
        IReadOnlyList<PackageDependencyResult> results = _resolver.Resolve(
            [Source("one", Bundle("only", "a.ab", "CAB-A"))]);

        Assert.Empty(results);
    }

    [Fact]
    public void Resolve_ReturnsNoResultsForEmptyPackageCollection()
    {
        Assert.Empty(_resolver.Resolve([]));
    }

    [Fact]
    public void Resolve_ReturnsOneResultPerDirectDeclarationInStoredOrder()
    {
        IReadOnlyList<PackageDependencyResult> results = _resolver.Resolve(
            [Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-C", "CAB-B"),
                Bundle("b", "b.ab", "CAB-B"), Bundle("c", "c.ab", "CAB-C"))]);

        Assert.Equal(["CAB-C", "CAB-B"], results.Select(result => result.RequestedCabCode));
    }

    [Fact]
    public void Resolve_OrdersRequestersDeterministically()
    {
        IReadOnlyList<PackageDependencyResult> results = _resolver.Resolve(
            [Source("zeta", Bundle("z", "z.ab", "CAB-Z", "CAB-MISSING-Z")),
             Source("alpha", Bundle("a", "a.ab", "CAB-A", "CAB-MISSING-A"))]);

        Assert.Equal(["alpha", "zeta"], results.Select(result => result.Requester.PackageName));
    }

    [Fact]
    public void Resolve_OrdersAmbiguousCandidatesDeterministically()
    {
        PackageDependencyResult result = Resolve(
            Source("request", Bundle("requester", "request.ab", "CAB-A", "CAB-SAME")),
            Source("zeta", Bundle("z", "z.ab", "CAB-SAME")),
            Source("alpha", Bundle("a", "a.ab", "CAB-SAME")));

        Assert.Equal(["a.ab", "z.ab"], result.Candidates.Select(candidate => candidate.SourcePath));
    }

    [Fact]
    public void Resolve_MarksCandidateWithMissingPathAsIncomplete()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-B"),
                Bundle("target", null!, "CAB-B")));

        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
        Assert.False(Assert.Single(result.Candidates).HasCompleteProvenance);
    }

    [Fact]
    public void Resolve_DoesNotDeduplicateCandidatesWithoutPhysicalPaths()
    {
        PackageDependencyResult result = Resolve(
            Source("request", Bundle("requester", "a.ab", "CAB-A", "CAB-B")),
            Source("one", Bundle("first", null!, "CAB-B")),
            Source("two", Bundle("second", "", "CAB-B")));

        Assert.Equal(PackageDependencyStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void Resolve_DoesNotRequireSourceFileToExist()
    {
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ab");
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "requester.ab", "CAB-A", "CAB-B"),
                Bundle("target", missing, "CAB-B")));

        Assert.False(File.Exists(missing));
        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
    }

    [Fact]
    public void Resolve_DoesNotCopyRewriteOrMutateSourceMetadata()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string sourcePath = Path.Combine(directory, "target.ab");
        byte[] originalBytes = [1, 2, 3, 4, 5];
        File.WriteAllBytes(sourcePath, originalBytes);
        PackageBundleReference target = Bundle("target", sourcePath, "CAB-B");
        PackageDocument package = new(
            [new PackageIndex(string.Empty,
                [Bundle("requester", "requester.ab", "CAB-A", "CAB-B"), target])],
            @"C:\packages\one");

        try
        {
            PackageDependencyResult result = Resolve(new PackageDependencySource("one", package));

            Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
            Assert.Equal(originalBytes, File.ReadAllBytes(sourcePath));
            Assert.Equal([sourcePath], Directory.GetFiles(directory));
            Assert.Equal("target", target.Name);
            Assert.Equal(sourcePath, target.Path);
            Assert.Equal("CAB-B", target.CabCode);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSameStructuredResult()
    {
        PackageDependencySource source = Source("one",
            Bundle("requester", "a.ab", "CAB-A", "CAB-B"),
            Bundle("target", "b.ab", "CAB-B"));

        PackageDependencyResult result = Assert.Single(await _resolver.ResolveAsync([source]));

        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
    }

    [Fact]
    public void Resolve_HonorsPreCanceledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => _resolver.Resolve([], cancellation.Token));
    }

    [Fact]
    public async Task ResolveAsync_HonorsPreCanceledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _resolver.ResolveAsync([], cancellation.Token));
    }

    [Fact]
    public void Resolve_HonorsCancellationDuringSnapshotWithoutReturningPartialResults()
    {
        using CancellationTokenSource cancellation = new();
        PackageDependencySource first = Source("first",
            Bundle("requester", "a.ab", "CAB-A", "CAB-B"));
        PackageDependencySource second = Source("second",
            Bundle("target", "b.ab", "CAB-B"));
        CancelingSourceList sources = new([first, second], cancellation, cancelBeforeIndex: 1);

        Assert.Throws<OperationCanceledException>(() => _resolver.Resolve(sources, cancellation.Token));
    }

    [Fact]
    public void Resolve_ReturnsReadOnlyCollections()
    {
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-B"),
                Bundle("target", "b.ab", "CAB-B")));
        PackageDependencyCandidate candidate = Assert.Single(result.Candidates);

        Assert.Throws<NotSupportedException>(() => ((IList<PackageDependencyCandidate>)result.Candidates).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<PackageDependencyOccurrence>)candidate.Occurrences).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)candidate.CabCodes).Clear());
    }

    [Fact]
    public void Resolve_ResultsRemainStableAfterPackageModelMutation()
    {
        PackageBundleReference target = Bundle("target", "b.ab", "CAB-B");
        PackageDependencyResult result = Resolve(
            Source("one", Bundle("requester", "a.ab", "CAB-A", "CAB-B"), target));

        target.Name = "changed";
        target.Path = "changed.ab";
        target.CabCode = "CAB-CHANGED";

        PackageDependencyCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal("b.ab", candidate.SourcePath);
        Assert.Equal("target", candidate.Occurrences.Single().SourceName);
        Assert.Equal(["CAB-B"], candidate.CabCodes);
    }

    [Fact]
    public void Resolve_ReportsDirectEdgesWithoutTraversingThem()
    {
        IReadOnlyList<PackageDependencyResult> results = _resolver.Resolve(
            [Source("one", Bundle("a", "a.ab", "CAB-A", "CAB-B"),
                Bundle("b", "b.ab", "CAB-B", "CAB-C"),
                Bundle("c", "c.ab", "CAB-C"))]);

        Assert.Equal(2, results.Count);
        Assert.Equal(["CAB-B", "CAB-C"], results.Select(result => result.RequestedCabCode));
        Assert.All(results, result => Assert.Single(result.Candidates));
    }

    [Fact]
    public void ResolveForPackage_UsesWholeCatalogButReturnsOnlySelectedRequesterEdges()
    {
        PackageDependencySource selected = Source("selected",
            Bundle("a", "a.ab", "CAB-A", "CAB-B"));
        PackageDependencySource other = Source("other",
            Bundle("b", "b.ab", "CAB-B", "CAB-C"),
            Bundle("c", "c.ab", "CAB-C"));

        PackageDependencyResult result = Assert.Single(
            _resolver.ResolveForPackage([other, selected], selected));

        Assert.Equal("selected", result.Requester.PackageName);
        Assert.Equal("b.ab", Assert.Single(result.Candidates).SourcePath);
    }

    [Fact]
    public void Resolve_PreservesRequesterAndCandidateProvenance()
    {
        PackageDependencyResult result = Resolve(
            Source("request-package", @"C:\packages\request-package",
                new PackageIndex("nested", [Bundle("requester", "a.ab", "CAB-A", "CAB-B")])),
            Source("target-package", @"C:\packages\target-package",
                new PackageIndex("content", [Bundle("target", "b.ab", "CAB-B")])));

        Assert.Equal("request-package", result.Requester.PackageName);
        Assert.Equal(@"C:\packages\request-package", result.Requester.PackagePath);
        Assert.Equal("nested", result.Requester.IndexDirectory);
        PackageDependencyOccurrence candidate = Assert.Single(result.Candidates).Occurrences.Single();
        Assert.Equal("target-package", candidate.PackageName);
        Assert.Equal("content", candidate.IndexDirectory);
        Assert.Equal("CAB-B", candidate.CabCode);
    }

    [Fact]
    public void Resolve_DeduplicatesConflictingMetadataForOnePhysicalSource()
    {
        PackageDependencyResult result = Resolve(
            Source("request", Bundle("requester", "a.ab", "CAB-A", "CAB-NEW")),
            Source("old", Bundle("target-old", "same.ab", "CAB-OLD")),
            Source("new", Bundle("target-new", "same.ab", "CAB-NEW")));

        PackageDependencyCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(PackageDependencyStatus.Resolved, result.Status);
        Assert.Equal(["CAB-NEW", "CAB-OLD"], candidate.CabCodes);
        Assert.Equal(2, candidate.Occurrences.Count);
    }

    private PackageDependencyResult Resolve(params PackageDependencySource[] sources) =>
        Assert.Single(_resolver.Resolve(sources));

    private static PackageDependencySource Source(string packageName, params PackageBundleReference[] bundles) =>
        Source(packageName, $@"C:\packages\{packageName}", new PackageIndex(string.Empty, bundles));

    private static PackageDependencySource Source(
        string packageName,
        string packagePath,
        params PackageIndex[] indexes) =>
        new(packageName, new PackageDocument(indexes, packagePath));

    private static PackageBundleReference Bundle(
        string name,
        string path,
        string cabCode,
        params string[] dependencies) =>
        new()
        {
            Name = name,
            Path = path,
            CabCode = cabCode,
            Dependencies = dependencies.ToList()
        };

    private sealed class CancelingSourceList(
        IReadOnlyList<PackageDependencySource> items,
        CancellationTokenSource cancellation,
        int cancelBeforeIndex) : IReadOnlyList<PackageDependencySource>
    {
        public int Count => items.Count;

        public PackageDependencySource this[int index] => items[index];

        public IEnumerator<PackageDependencySource> GetEnumerator()
        {
            for (int index = 0; index < items.Count; index++)
            {
                if (index == cancelBeforeIndex)
                    cancellation.Cancel();
                yield return items[index];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
