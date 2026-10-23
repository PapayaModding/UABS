using Microsoft.Data.Sqlite;
using UABS.PackageV2;
using UABS.PackageV2.Search;
using UABS.UnityAssets.Metadata;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

public sealed class V2BundleSearchTests
{
    private sealed record Asset(string? Name, long Path = 1, int Class = 28, string? Type = "Texture2D", string? Memo = null);
    private sealed record Source(string Name, Asset[] Assets, UnityFileKind Kind = UnityFileKind.Bundle,
        bool Partial = false, bool Verified = true);

    private sealed class Fixture : IDisposable
    {
        internal string Root { get; } = Directory.CreateDirectory(Path.Combine(
            Environment.GetEnvironmentVariable("UABS_V2_TEST_ROOT") ?? Path.GetTempPath(),
            "uabs-bundle-search-" + Guid.NewGuid().ToString("N"))).FullName;

        internal async Task<string> Build(string package, params Source[] sources)
        {
            string path = Path.Combine(Root, Guid.NewGuid().ToString("N") + ".uabs.sqlite");
            Dictionary<string, Source> specs = [];
            var inputs = sources.Select((s, i) => {
                string relative = Path.Combine(Path.GetFileNameWithoutExtension(path), i.ToString(), s.Name);
                string original = Path.Combine(Root, relative); specs.Add(original, s);
                return new BuildSource(0, relative, "indexed", new(original, Guid.NewGuid(), Guid.NewGuid()));
            }).ToArray();
            var result = await new PackageBuilder(new Extractor(specs)).BuildAsync(new(path,
                Guid.NewGuid(), Guid.NewGuid(), package,
                [new("fixture", Root, OperatingSystem.IsWindows() ? "windows-lexical-v1" : "posix-lexical-v1")], inputs,
                AcknowledgePartial: true));
            Assert.True(result.Succeeded, result.Message);
            // Only disposable fixture memos are seeded; metadata is produced through the builder.
            using var c = V2SearchTests.Connect(path);
            c.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
            using var tx = c.BeginTransaction();
            foreach (var entry in specs.Where(p => p.Value.Verified))
                for (int i = 0; i < entry.Value.Assets.Length; i++)
                {
                    using var cmd = c.CreateCommand(); cmd.Transaction = tx;
                    cmd.CommandText = "UPDATE asset SET memo=@memo WHERE table_ordinal=@ordinal AND file_id IN "
                        + "(SELECT f.file_id FROM logical_file f JOIN source s ON s.source_id=f.source_id WHERE s.original_path=@path)";
                    cmd.Parameters.AddWithValue("@memo", (object?)entry.Value.Assets[i].Memo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ordinal", i); cmd.Parameters.AddWithValue("@path", entry.Key);
                    cmd.ExecuteNonQuery();
                }
            tx.Commit(); return path;
        }
        public void Dispose() => Directory.Delete(Root, true);
    }

    private sealed class Extractor(Dictionary<string, Source> specs) : IV2MetadataExtractor
    {
        public async Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
            Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive,
            V2ExtractionOptions? options = null, IProgress<V2ExtractionProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Source source = specs[request.Path];
            for (int i = 0; i < source.Assets.Length; i++)
            {
                Asset a = source.Assets[i];
                await receive([new V2AssetType(0, i, a.Class, null, a.Type),
                    new V2Asset(0, i, a.Path, a.Class, i, null, i * 32, 16,
                        V2IdentityStatus.Addressable, a.Name,
                        a.Name is null ? V2NameStatus.Absent : V2NameStatus.Present, a.Type)], cancellationToken);
            }
            var status = source.Partial ? V2ExtractionStatus.Partial : V2ExtractionStatus.Complete;
            await receive([new V2LogicalFile(0, "CAB", "CAB", "2022.3.1f1", 22, true,
                source.Assets.Length, source.Assets.Length, status, V2DependencyStatus.Complete)], cancellationToken);
            return new(new(request.SourceId, request.RevisionId, request.Path, request.Path, source.Kind,
                1024, DateTime.UnixEpoch, new string('A', 64), source.Verified), status, 1, source.Assets.Length,
                new(default, default, default, default, default, 0, 0, 2));
        }
    }

    private static async Task<SearchRegistration> Register(V2SearchService service, string path)
    {
        var result = await service.RegisterAsync(path, 19);
        Assert.Equal(SearchStatus.Success, result.Status); return result.Registration!;
    }
    private static BundleSearchSession Session(V2SearchService service, BundleSearchRequest request,
        params SearchRegistration[] scope)
    {
        var created = service.CreateBundleSession(request, scope.Select(r => r.RegistrationId).ToArray());
        Assert.True(created.Status == SearchStatus.Success, created.Message); return created.Session!;
    }
    private static async Task<List<BundleSearchResult>> Exhaust(V2SearchService service, BundleSearchSession session, int size = 2)
    {
        List<BundleSearchResult> rows = []; BundleSearchCursor? cursor = null;
        do
        {
            var page = await service.ReadBundlePageAsync(session, cursor, size);
            Assert.True(page.Status == SearchStatus.Success, string.Join(";", page.Diagnostics));
            Assert.InRange(page.Rows.Count, 0, size); rows.AddRange(page.Rows); cursor = page.NextCursor;
            Assert.Equal(cursor is not null, page.HasMore); Assert.Equal(rows.Count, page.Count.Value);
            Assert.True(rows.Count < 5000, "Unexpected cursor loop");
        } while (cursor is not null);
        Assert.Equal(rows.Count, rows.Select(r => r.Key).Distinct().Count()); return rows;
    }

    [Theory]
    [InlineData("hero_bundle", PackageSearchFields.SourceName)]
    [InlineData("hero_asset", PackageSearchFields.AssetName)]
    [InlineData("Attack", PackageSearchFields.Memo)]
    [InlineData("absent，HERO_ASSET", PackageSearchFields.AssetName)]
    public async Task PositiveFieldsAndCommaOr_AggregateBeforePages(string query, PackageSearchFields fields)
    {
        using Fixture f = new(); string path = await f.Build("Package", new Source("hero_bundle",
            [new("hero_asset", Memo: "Attack"), new("hero_asset", 2, Memo: "Attack")]));
        V2SearchService service = new(); var reg = await Register(service, path);
        using var session = Session(service, new(query, Fields: fields), reg);
        var rows = await Exhaust(service, session, 1); Assert.Single(rows);
        Assert.Equal("hero_bundle", rows[0].Name); Assert.Equal(reg.Package.PackageId, rows[0].Key.PackageId);
        Assert.Equal(19, Assert.Single(session.Revisions).LocationEpoch);
    }

    [Fact]
    public async Task MandatoryFilters_HaveSeparateWitnesses_AndCannotBeBypassedBySourceName()
    {
        using Fixture f = new(); string path = await f.Build("P", new Source("hero.bundle",
            [new("hero", -77, 999, "Unknown"), new("texture", 2), new(null, 3, 83, "AudioClip", "Attack")]));
        V2SearchService service = new(); var reg = await Register(service, path);
        var request = new BundleSearchRequest("hero", RequireNonEmptyMemo: true, PathId: -77, ClassId: 83, TypeName: "texture2d");
        using var session = Session(service, request, reg); Assert.Single(await Exhaust(service, session));
        using var absent = Session(service, request with { PathId = long.MinValue }, reg);
        Assert.Empty(await Exhaust(service, absent));
        using var numeric = Session(service, new(PathId: -77), reg); Assert.Single(await Exhaust(service, numeric));
        using var textAndId = Session(service, request with { Query = "missing" }, reg);
        Assert.Empty(await Exhaust(service, textAndId));
    }

    [Theory]
    [InlineData(PackageSearchFields.AssetName, "debug")]
    [InlineData(PackageSearchFields.Memo, "blocked")]
    public async Task AnyExcludedChild_VetoesSourceNameAndOtherPositiveChildren(PackageSearchFields fields, string term)
    {
        using Fixture f = new(); string path = await f.Build("P", new Source("hero.bundle",
            [new("hero"), new("debug", 2, Memo: "blocked")]));
        V2SearchService service = new(); var reg = await Register(service, path);
        using var session = Session(service, new("hero", term, ExclusionFields: fields), reg);
        Assert.Empty(await Exhaust(service, session));
        Assert.Equal(0, (await service.CountBundlesAsync(session)).Count.Value);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnicodeAndExactExclusions_UseExistingOrdinalMatching(bool exact)
    {
        using Fixture f = new(); string path = await f.Build("P", new Source("源", [new("Éclair", Memo: "钩爪"), new("debug", 2)]));
        V2SearchService service = new(); var reg = await Register(service, path);
        using var session = Session(service, new(exact ? "ÉCLAIR" : "écl", Fields: PackageSearchFields.AssetName, ExactText: exact), reg);
        Assert.Single(await Exhaust(service, session));
        using var absent = Session(service, new("e\u0301clair", Fields: PackageSearchFields.AssetName, ExactText: true), reg);
        Assert.Empty(await Exhaust(service, absent));
        using var memo = Session(service, new("钩爪", Fields: PackageSearchFields.Memo), reg);
        Assert.Single(await Exhaust(service, memo));
        using var veto = Session(service, new("Éclair", exact ? "DEBUG" : "deb", ExclusionFields: PackageSearchFields.AssetName, ExactText: exact), reg);
        Assert.Empty(await Exhaust(service, veto));
    }

    [Fact]
    public async Task Eligibility_UsesVerifiedIndexedKind_NotNamesOrExtensions()
    {
        using Fixture f = new(); string path = await f.Build("P", new("actual.data", [new("hero")]),
            new("fake.bundle", [new("hero")], UnityFileKind.SerializedFile),
            new("unverified.bundle", [new("hero")], Verified: false));
        V2SearchService service = new(); var reg = await Register(service, path);
        using var session = Session(service, new("hero"), reg);
        Assert.Equal("actual.data", Assert.Single(await Exhaust(service, session)).Name);
        Assert.Contains(session.Diagnostics, d => d.Code == "UnavailableMetadata");
    }

    [Fact]
    public async Task EmptyAndPartialSources_PreserveEvidence_WithoutInventingNegativeCoverage()
    {
        using Fixture f = new(); string path = await f.Build("P", new("empty", []), new("partial", [new(null, Memo: "")], Partial: true));
        V2SearchService service = new(); var reg = await Register(service, path);
        using var session = Session(service, new("empty,partial"), reg);
        var rows = await Exhaust(service, session); Assert.Equal(2, rows.Count);
        Assert.Equal("partial", rows.Single(r => r.Name == "partial").IndexStatus);
        Assert.Contains(session.Diagnostics, d => d.Code == "PartialIndex");
        using var memos = Session(service, new("empty,partial", RequireNonEmptyMemo: true), reg);
        Assert.Empty(await Exhaust(service, memos));
        var blocked = service.CreateBundleSession(new("partial", "bad", ExclusionFields: PackageSearchFields.AssetName), [reg.RegistrationId]);
        Assert.Equal(SearchStatus.InvalidRequest, blocked.Status); Assert.Null(blocked.Session);
    }

    [Theory]
    [InlineData(BundleSearchSort.Name, PackageSearchSortDirection.Ascending)]
    [InlineData(BundleSearchSort.Name, PackageSearchSortDirection.Descending)]
    [InlineData(BundleSearchSort.Package, PackageSearchSortDirection.Ascending)]
    [InlineData(BundleSearchSort.Package, PackageSearchSortDirection.Descending)]
    public async Task GlobalOrdering_Scope_DuplicateNamesAndCursorIdentity(BundleSearchSort sort, PackageSearchSortDirection direction)
    {
        using Fixture f = new(); V2SearchService service = new();
        var a = await Register(service, await f.Build("é", new("same", [new("hero")]), new("same", [new("hero")])));
        var b = await Register(service, await f.Build("A", new("same", [new("hero")]), new("钩爪", [new("hero")])));
        var unselected = await Register(service, await f.Build("unselected", new Source("same", [new("hero")])));
        using var session = Session(service, new("hero", SortBy: sort, SortDirection: direction), b, a);
        var rows = await Exhaust(service, session, 1); Assert.Equal(4, rows.Count);
        Assert.DoesNotContain(rows, r => r.Key.PackageId == unselected.Package.PackageId);
        Func<BundleSearchResult, string> key = sort == BundleSearchSort.Name ? r => r.Name : r => r.Package;
        var expected = direction == PackageSearchSortDirection.Ascending
            ? rows.OrderBy(key, Comparer<string>.Create(CompareText))
            : rows.OrderByDescending(key, Comparer<string>.Create(CompareText));
        Assert.Equal(expected.Select(key), rows.Select(key));
        using var reverseScope = Session(service, session.Criteria, a, b);
        Assert.Equal(rows.Select(r => r.Key), (await Exhaust(service, reverseScope, 3)).Select(r => r.Key));
        var first = await service.ReadBundlePageAsync(session, pageSize: 1);
        Assert.Equal(SearchStatus.StaleCursor, (await service.ReadBundlePageAsync(reverseScope, first.NextCursor)).Status);
        Assert.Equal(4, (await service.CountBundlesAsync(session)).Count.Value);
    }

    [Fact]
    public async Task MoreThan600Sources_PageGlobally_WithBoundedCandidates()
    {
        using Fixture f = new(); V2SearchService service = new();
        var reg = await Register(service, await f.Build("P", Enumerable.Range(0, 605)
            .Select(i => new Source($"bundle-{604 - i:D4}", [new("hero"), new("hero", 2)])).ToArray()));
        using var session = Session(service, new("hero"), reg);
        var rows = await Exhaust(service, session, 100); Assert.Equal(605, rows.Count);
        Assert.Equal("bundle-0000", rows[0].Name); Assert.Equal("bundle-0604", rows[^1].Name);
        Assert.InRange(service.PeakBufferedCandidates, 1, 101);
    }

    [Fact]
    public void UnresolvedFieldsKindsAndExclusionScope_AreRejected_NotReinterpreted()
    {
        V2SearchService service = new();
        BundleSearchRequest[] blocked = [new("hero", Fields: PackageSearchFields.PackageName),
            new("hero", ResultKinds: PackageSearchResultKinds.Asset), new("hero", "bad"),
            new("hero", "bad", ExclusionFields: PackageSearchFields.SourceName), new(RequireNonEmptyMemo: true)];
        foreach (var request in blocked)
        {
            var result = service.CreateBundleSession(request, []);
            Assert.Equal(SearchStatus.InvalidRequest, result.Status); Assert.Null(result.Session);
        }
    }

    [Fact]
    public async Task ExistingAssetQueriesAndLifecycle_RemainIndependent()
    {
        using Fixture f = new(); string path = await f.Build("P", new Source("hero", [new("hero"), new("debug", 2)]));
        V2SearchService service = new(); var reg = await Register(service, path);
        using var bundle = Session(service, new("hero", "debug", ExclusionFields: PackageSearchFields.AssetName), reg);
        Assert.Empty(await Exhaust(service, bundle));
        using var asset = V2SearchTests.Session(service, new("hero", "debug", PackageSearchFields.AssetName, PackageSearchResultKinds.Asset), reg);
        Assert.Single(await V2SearchTests.Exhaust(service, asset));
        using var cancelled = Session(service, new("hero"), reg); cancelled.Cancel();
        Assert.Equal(SearchStatus.Cancelled, (await service.ReadBundlePageAsync(cancelled)).Status);
        using var unloaded = Session(service, new("hero"), reg); await service.UnloadAsync(reg.RegistrationId);
        Assert.Equal(SearchStatus.StaleCursor, (await service.ReadBundlePageAsync(unloaded)).Status);
    }

    [Fact]
    public async Task CatalogChangeBeforePublication_RejectsEntireBundlePage()
    {
        using Fixture f = new(); string path = await f.Build("P", new Source("hero", [new("hero")]));
        bool changed = false;
        V2SearchService service = new(observe: (boundary, _) => {
            if (boundary != SearchBoundary.BeforePublication || changed) return;
            changed = true;
            using var c = V2SearchTests.Connect(path);
            c.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
            using var cmd = c.CreateCommand(); cmd.CommandText = "UPDATE asset SET memo='external revision'";
            cmd.ExecuteNonQuery();
        });
        var reg = await Register(service, path);
        using var session = Session(service, new("hero"), reg);
        var page = await service.ReadBundlePageAsync(session);
        Assert.True(changed); Assert.Equal(SearchStatus.StaleCursor, page.Status);
        Assert.Empty(page.Rows); Assert.Null(page.NextCursor); Assert.True(session.IsStale);
    }

    [Fact]
    public async Task PredicateBudget_IsExplicitFailure_NotPartialSuccessfulResults()
    {
        using Fixture f = new(); string path = await f.Build("P", new Source("hero", [new("hero"), new("hero", 2)]));
        V2SearchService service = new(new SearchOptions(MaxPredicateEvaluations: 1));
        var reg = await Register(service, path);
        using var session = Session(service, new("hero"), reg);
        var page = await service.ReadBundlePageAsync(session);
        Assert.Equal(SearchStatus.BudgetExceeded, page.Status); Assert.Empty(page.Rows); Assert.Null(page.NextCursor);
    }

    private static int CompareText(string a, string b)
    {
        int value = StringComparer.OrdinalIgnoreCase.Compare(a, b);
        return value != 0 ? value : StringComparer.Ordinal.Compare(a, b);
    }
}
