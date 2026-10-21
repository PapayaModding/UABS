using Microsoft.Data.Sqlite;
using UABS.PackageV2;
using UABS.PackageV2.Search;
using UABS.UnityAssets.Metadata;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

public sealed class V2SearchTests
{
    internal sealed class Corpus : IDisposable
    {
        internal string DirectoryPath { get; } = Directory.CreateDirectory(Path.Combine(
            Environment.GetEnvironmentVariable("UABS_V2_TEST_ROOT") ?? Path.GetTempPath(), "uabs-v2-search-" + Guid.NewGuid().ToString("N"))).FullName;
        internal readonly List<(string Path, PackageSearchSource Legacy)> Packages = [];
        internal static readonly string?[] Names = ["Hero", "hero", "钩爪", "甲钩爪乙", "Éclair", "e\u0301clair", "😀Hero", "İstanbul", "ıstanbul", null, "", "texture blocked", "音声", "duplicate", "duplicate", "\tHero", "ＡＢＣ", "ẞß"];
        internal static readonly string?[] Memos = [null, "", "\u2003", "memo approved", "钩爪 memo", "😀备注", "blocked", " "];
        internal async Task Add(int count = 36, string name = "Package", bool partial = false, bool duplicateNames = false,
            Func<int, string?>? nameFactory = null, int sourceCount = 1)
        {
            int number = Packages.Count;
            string path = Path.Combine(DirectoryPath, $"package-{number}.uabs.sqlite");
            string source = Path.Combine(DirectoryPath, $"source-{number}.bundle");
            PackageBuildRequest request = new(path, Guid.NewGuid(), Guid.NewGuid(), name,
                [new("game", DirectoryPath, OperatingSystem.IsWindows() ? "windows-lexical-v1" : "posix-lexical-v1")],
                Enumerable.Range(0, sourceCount).Select(s => {
                    string file = s == 0 ? source : source + "-" + s;
                    return new BuildSource(0, Path.GetFileName(file), s == 0 ? "characters" : "characters-other",
                        new(file, Guid.NewGuid(), Guid.NewGuid()));
                }).ToArray(), partial);
            var result = await new PackageBuilder(new Extractor(count, partial, duplicateNames, nameFactory)).BuildAsync(request);
            Assert.True(result.Succeeded, result.Message);
            // Fixture-only memo seeding exercises the canonical triggers; no production memo API is introduced.
            using (SqliteConnection c = Connect(path))
            {
                c.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
                using var transaction = c.BeginTransaction();
                using var command = c.CreateCommand(); command.Transaction = transaction;
                command.CommandText = "UPDATE asset SET memo=@memo WHERE table_ordinal % 8=@i";
                command.Parameters.AddWithValue("@memo", DBNull.Value); command.Parameters.AddWithValue("@i", 0);
                for (int i = 0; i < Memos.Length; i++) { command.Parameters[0].Value = (object?)Memos[i] ?? DBNull.Value; command.Parameters[1].Value = i; command.ExecuteNonQuery(); }
                transaction.Commit();
            }
            List<PackageAssetReference> assets = Enumerable.Range(0, count).Select(i => new PackageAssetReference {
                Name = (nameFactory is null ? AssetName(i, duplicateNames) : nameFactory(i))!, PathId = PathId(i), Memo = Memos[i % Memos.Length] }).ToList();
            var indexes = request.Sources.GroupBy(s => s.IndexDirectory).Select(group => new PackageIndex(group.Key,
                group.Select(s => new PackageBundleReference { Name = Path.GetFileName(s.Request.Path), Path = s.Request.Path,
                    CabCode = "CAB-shared", Dependencies = ["archive:/dep/CAB-raw"], AssetInfos = assets.Select(a => a.Clone()).ToList() })));
            var legacy = new PackageSearchSource(name, new PackageDocument(indexes, path));
            Packages.Add((path, legacy));
        }
        internal static string? AssetName(int i, bool duplicateNames) => duplicateNames ? "asset" : i < Names.Length ? Names[i] : "asset_" + i.ToString("D8");
        internal static long PathId(int i) => i == 0 ? -77 : i == 1 ? long.MaxValue : i;
        public void Dispose() => Directory.Delete(DirectoryPath, true);
    }
    private sealed class Extractor(int count, bool partial, bool duplicateNames, Func<int, string?>? nameFactory) : IV2MetadataExtractor
    {
        public async Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
            Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive, V2ExtractionOptions? options = null,
            IProgress<V2ExtractionProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            List<V2MetadataRecord> batch = [];
            async ValueTask Emit(V2MetadataRecord record) { batch.Add(record); if (batch.Count == 128) { await receive(batch.ToArray(), cancellationToken); batch.Clear(); } }
            await Emit(new V2AssetType(3, 0, 1234567, null, "UnknownType"));
            for (int i = 0; i < count; i++)
            {
                string? name = nameFactory is null ? Corpus.AssetName(i, duplicateNames) : nameFactory(i);
                await Emit(new V2Asset(3, i, Corpus.PathId(i), 1234567, 0, null, i * 32L, 16,
                    V2IdentityStatus.Addressable, name, name is null ? V2NameStatus.Absent : V2NameStatus.Present, "UnknownType"));
            }
            await Emit(new V2Dependency(3, "external_file", 0, null, "archive:/dep/CAB-raw", "archive:/dep/CAB-raw", "virtual:/资源", "CAB-raw", new string('0', 32), 0));
            await Emit(new V2LogicalFile(3, "CAB-shared", "CAB-shared", "2022.3.1f1", 22, true, count, count,
                partial ? V2ExtractionStatus.Partial : V2ExtractionStatus.Complete, V2DependencyStatus.Complete));
            if (batch.Count != 0) await receive(batch.ToArray(), cancellationToken);
            return new(new(request.SourceId, request.RevisionId, request.Path, Path.GetFullPath(request.Path), UnityFileKind.Bundle,
                1234, DateTime.UnixEpoch, new string('A', 64), true), partial ? V2ExtractionStatus.Partial : V2ExtractionStatus.Complete,
                1, count, new(default, default, default, default, default, 0, 0, 128));
        }
    }
    internal static SqliteConnection Connect(string path)
    {
        SqliteConnection c = new(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString());
        c.Open(); return c;
    }
    private static void Edit(string path, string sql)
    {
        using var c = Connect(path); c.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
        using var command = c.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
    }
    internal static async Task<SearchRegistration[]> Register(V2SearchService service, Corpus corpus)
    {
        List<SearchRegistration> registrations = [];
        foreach (var package in corpus.Packages)
        {
            var registered = await service.RegisterAsync(package.Path);
            Assert.Equal(SearchStatus.Success, registered.Status); registrations.Add(registered.Registration!);
        }
        return registrations.ToArray();
    }
    internal static SearchSession Session(V2SearchService service, SearchRequest request, params SearchRegistration[] registrations)
    {
        var created = service.CreateSession(request, registrations.Select(r => r.RegistrationId).ToArray());
        Assert.Equal(SearchStatus.Success, created.Status); return created.Session!;
    }
    internal static async Task<List<SearchResult>> Exhaust(V2SearchService service, SearchSession session, int size = 7)
    {
        List<SearchResult> rows = []; SearchCursor? cursor = null;
        do
        {
            var page = await service.ReadPageAsync(session, cursor, size);
            Assert.True(page.Status == SearchStatus.Success, string.Join("; ", page.Diagnostics.Select(d => d.Code + ": " + d.Message))); Assert.InRange(page.Rows.Count, 0, size);
            rows.AddRange(page.Rows); cursor = page.NextCursor;
            Assert.Equal(cursor is not null, page.HasMore);
            Assert.Equal(rows.Count, page.Count.Value);
            Assert.Equal(page.HasMore ? SearchCountKind.LoadedAtLeast : SearchCountKind.Exact, page.Count.Kind);
            Assert.True(rows.Count < 100_000, "Unexpected paging loop.");
        } while (cursor is not null);
        Assert.Equal(rows.Count, rows.Select(r => r.Key).Distinct().Count());
        return rows;
    }
    private static string Signature(PackageSearchResult row) => $"{row.Kind}|{(int)row.MatchCategories}|{row.PackageName}|{row.PackagePath}|{row.IndexDirectory}|{row.SourcePath}|{row.AssetName}|{row.PathId}|{row.Memo}";
    private static string Signature(SearchResult row) => $"{row.Key.Kind}|{(int)row.MatchCategories}|{row.PackageName}|{row.PackagePath}|{row.IndexDirectory}|{row.SourcePath}|{row.AssetName}|{row.PathId}|{row.Memo}";

    [Theory]
    [InlineData("hero", "", false, false)] [InlineData("钩", "", false, false)] [InlineData("钩爪", "", false, false)]
    [InlineData("é", "", false, false)] [InlineData("é", "", false, false)] [InlineData("😀", "", false, false)]
    [InlineData("Hero", "", true, false)] [InlineData("hero，钩爪,音声, HERO", "blocked", false, false)]
    [InlineData("memo,hero", "blocked", false, true)] [InlineData("memo", "", true, false)]
    [InlineData("", "", false, false)] [InlineData("", "hero", false, true)]
    [InlineData("İ", "", false, false)] [InlineData("ß", "", false, false)]
    public async Task DifferentialAssetMatching_AgreesWithV1(string query, string exclude, bool exact, bool memoOnly)
    {
        using Corpus corpus = new(); await corpus.Add(); await corpus.Add(name: "package");
        V2SearchService service = new(); var registrations = await Register(service, corpus);
        SearchRequest request = new(query, exclude, PackageSearchFields.AssetName | PackageSearchFields.Memo,
            PackageSearchResultKinds.Asset, exact, memoOnly, SortBy: PackageSearchSortField.AssetName);
        using var session = Session(service, request, registrations.Reverse().ToArray());
        var actual = await Exhaust(service, session);
        var expected = new PackageSearchService().Search(corpus.Packages.Select(p => p.Legacy).ToArray(), new(query, exclude) {
            Fields = request.Fields, ResultKinds = request.ResultKinds, ExactText = exact, RequireNonEmptyMemo = memoOnly, SortBy = request.SortBy });
        Assert.Equal(expected.Select(Signature), actual.Select(Signature));
    }

    [Theory]
    [InlineData(PackageSearchSortField.PackageName)] [InlineData(PackageSearchSortField.IndexDirectory)]
    [InlineData(PackageSearchSortField.SourceName)] [InlineData(PackageSearchSortField.SourcePath)]
    [InlineData(PackageSearchSortField.AssetName)] [InlineData(PackageSearchSortField.PathId)] [InlineData(PackageSearchSortField.MatchCategory)]
    public async Task EverySortAndDirection_GlobalKeysetsMatchV1(PackageSearchSortField field)
    {
        using Corpus corpus = new(); await corpus.Add(); await corpus.Add(name: "package"); await corpus.Add(name: "Unselected");
        V2SearchService service = new(); var registrations = await Register(service, corpus);
        foreach (PackageSearchSortDirection direction in Enum.GetValues<PackageSearchSortDirection>())
        {
            SearchRequest request = new("Package,source,characters,CAB,hero,memo", SortBy: field, SortDirection: direction);
            using var session = Session(service, request, registrations.Take(2).ToArray());
            var actual = await Exhaust(service, session, 1);
            var expected = new PackageSearchService().Search(corpus.Packages.Take(2).Select(p => p.Legacy).ToArray(), new(request.Query) { SortBy = field, SortDirection = direction });
            Assert.Equal(expected.Select(Signature), actual.Select(Signature));
            Assert.DoesNotContain(actual, row => row.PackageName == "Unselected");
        }
    }

    [Fact]
    public async Task PathIdNullNamesUnknownClassAndTypeFilters_PreserveIdentityAndMemoNullEmpty()
    {
        using Corpus corpus = new(); await corpus.Add();
        V2SearchService service = new(); var registrations = await Register(service, corpus);
        foreach (int ordinal in new[] { 0, 1, 9, 10 })
        {
            using var session = Session(service, new(PathId: Corpus.PathId(ordinal), Fields: PackageSearchFields.None,
                ResultKinds: PackageSearchResultKinds.Asset, ClassId: 1234567, TypeName: "unknowntype"), registrations);
            SearchResult row = Assert.Single(await Exhaust(service, session));
            Assert.Equal(Corpus.Names[ordinal], row.AssetName); Assert.Equal(Corpus.Memos[ordinal % 8], row.Memo);
            Assert.NotNull(row.AssetIdentity); Assert.Equal(3, row.AssetIdentity.DirectoryOrdinal);
            Assert.Equal(ordinal, row.Key.TableOrdinal); Assert.Equal(1234567, row.ClassId);
            using var exact = Session(service, new(Fields: PackageSearchFields.None, ResultKinds: PackageSearchResultKinds.Asset, AssetIdentity: row.AssetIdentity), registrations);
            Assert.Equal(row.Key, Assert.Single(await Exhaust(service, exact)).Key);
        }
        using var mismatch = Session(service, new("hero", ResultKinds: PackageSearchResultKinds.Asset, ClassId: 28), registrations);
        Assert.Empty(await Exhaust(service, mismatch));
        using var excluded = Session(service, new("Hero", "Hero", PackageSearchFields.AssetName, PackageSearchResultKinds.Asset, PathId: -77), registrations);
        Assert.Empty(await Exhaust(service, excluded));
        Assert.Equal(SearchStatus.InvalidRequest, service.CreateSession(new("hero", ClassId: 28), registrations.Select(r => r.RegistrationId).ToArray()).Status);
    }

    [Fact]
    public async Task SourceEvidence_UsesOriginalVirtualAndCabWithoutOpeningOriginalFiles()
    {
        using Corpus corpus = new(); await corpus.Add(); V2SearchService service = new(); var registrations = await Register(service, corpus);
        foreach (var query in new[] { "archive:/dep", "virtual:/资源", "CAB-raw", "CAB-shared" })
        {
            using var session = Session(service, new(query, Fields: PackageSearchFields.Dependency | PackageSearchFields.CabCode,
                ResultKinds: PackageSearchResultKinds.SourceFile), registrations);
            Assert.Equal(PackageSearchResultKind.SourceFile, Assert.Single(await Exhaust(service, session)).Key.Kind);
        }
        using var rejected = Session(service, new("CAB", "资源", PackageSearchFields.Dependency, PackageSearchResultKinds.SourceFile), registrations);
        Assert.Empty(await Exhaust(service, rejected));
    }

    [Fact]
    public async Task LargeDuplicateNames_RemainDistinctWithBoundedPagesAndExactCountOnlyAtEnd()
    {
        using Corpus corpus = new(); await corpus.Add(600, duplicateNames: true); await corpus.Add(600, duplicateNames: true);
        V2SearchService service = new(); var registrations = await Register(service, corpus);
        using var session = Session(service, new("asset", ResultKinds: PackageSearchResultKinds.Asset), registrations);
        var first = await service.ReadPageAsync(session); Assert.Equal(100, first.Rows.Count); Assert.True(first.HasMore);
        Assert.Equal(SearchCountKind.LoadedAtLeast, first.Count.Kind);
        Assert.Equal(1200, (await Exhaust(service, session, 137)).Count);
        var count = await service.CountAsync(session); Assert.Equal(SearchStatus.Success, count.Status); Assert.Equal(new(SearchCountKind.Exact, 1200), count.Count);
        Assert.InRange(service.PeakWorkers, 1, 2); Assert.Equal(0, service.ActiveWorkers);
        Assert.InRange(service.PeakBufferedCandidates, 100, 138);
        Assert.Equal(SearchStatus.InvalidRequest, (await service.ReadPageAsync(session, pageSize: 501)).Status);
        using var other = Session(service, new("asset"), registrations);
        Assert.Equal(SearchStatus.StaleCursor, (await service.ReadPageAsync(other, first.NextCursor)).Status);
    }

    [Fact]
    public async Task PartialCoverageAndEmptyScopes_KeepWarningsEvenAtExactZero()
    {
        using Corpus corpus = new(); await corpus.Add(partial: true); V2SearchService service = new(); var registrations = await Register(service, corpus);
        using var session = Session(service, new("no-hit"), registrations);
        var page = await service.ReadPageAsync(session); Assert.Equal(SearchStatus.Success, page.Status); Assert.Empty(page.Rows);
        Assert.Equal(new(SearchCountKind.Exact, 0), page.Count); Assert.Contains(page.Diagnostics, d => d.Code == "PartialIndex");
        Assert.Contains(page.Diagnostics, d => d.Code == "OptionalNamesUnavailable");
        using var empty = Session(service, new("Hero")); Assert.Empty((await service.ReadPageAsync(empty)).Rows);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RevisionAndGenerationChanges_RejectOldCursorAndDoNotMutatePublishedRows(bool generation)
    {
        using Corpus corpus = new(); await corpus.Add(); V2SearchService service = new(); var registrations = await Register(service, corpus);
        using var session = Session(service, new("memo,hero"), registrations);
        var page = await service.ReadPageAsync(session, pageSize: 1); Assert.True(page.HasMore); var old = page.Rows.ToArray();
        Edit(corpus.Packages[0].Path, generation ? "UPDATE package SET generation_uuid=randomblob(16)" : "UPDATE asset SET memo='new memo' WHERE table_ordinal=0");
        var stale = await service.ReadPageAsync(session, page.NextCursor);
        Assert.Equal(SearchStatus.StaleCursor, stale.Status); Assert.Empty(stale.Rows); Assert.Equal(SearchCountKind.Stale, stale.Count.Kind);
        Assert.Equal(old, page.Rows); Assert.True(session.IsStale);
        Assert.Equal(SearchStatus.StaleCursor, (await service.CountAsync(session)).Status);
    }

    [Fact]
    public async Task UnloadAndReselection_InvalidateCapturedRegistrationEvenAtSameLocationEpoch()
    {
        using Corpus corpus = new(); await corpus.Add(); V2SearchService service = new(); var registrations = await Register(service, corpus);
        using var session = Session(service, new("hero"), registrations);
        Assert.Equal(SearchStatus.RegistrationConflict, (await service.RegisterAsync(corpus.Packages[0].Path)).Status);
        Assert.True(await service.UnloadAsync(registrations[0].RegistrationId));
        Assert.Equal(SearchStatus.StaleCursor, (await service.ReadPageAsync(session)).Status);
        var replacement = await service.RegisterAsync(corpus.Packages[0].Path, locationEpoch: 2);
        using var fresh = Session(service, new("hero"), replacement.Registration!);
        Assert.NotEmpty(await Exhaust(service, fresh)); Assert.Equal(2, fresh.Revisions[0].LocationEpoch);
    }

    [Theory]
    [InlineData(SearchBoundary.Predicate)] [InlineData(SearchBoundary.BeforePublication)]
    [InlineData(SearchBoundary.SortComparison)]
    public async Task CancellationNeverPublishesPartialPages_AndReleasesWorkers(SearchBoundary boundary)
    {
        using Corpus corpus = new(); await corpus.Add(); using CancellationTokenSource stop = new(); bool armed = false;
        V2SearchService service = new(observe: (point, _) => { if (armed && point == boundary) stop.Cancel(); });
        var registrations = await Register(service, corpus); using var session = Session(service, new("hero"), registrations); armed = true;
        var page = await service.ReadPageAsync(session, token: stop.Token);
        Assert.Equal(SearchStatus.Cancelled, page.Status); Assert.Empty(page.Rows); Assert.Null(page.NextCursor); Assert.Equal(0, service.ActiveWorkers);
        armed = false; Assert.NotEmpty(await Exhaust(service, session));
    }

    [Fact]
    public async Task RevisionChangeJustBeforePublication_IsNotPublished()
    {
        using Corpus corpus = new(); await corpus.Add(); bool armed = false;
        V2SearchService service = new(observe: (point, _) => { if (armed && point == SearchBoundary.BeforePublication) { armed = false; Edit(corpus.Packages[0].Path, "UPDATE asset SET memo='changed' WHERE table_ordinal=0"); } });
        var registrations = await Register(service, corpus); using var session = Session(service, new("hero"), registrations); armed = true;
        var page = await service.ReadPageAsync(session); Assert.Equal(SearchStatus.StaleCursor, page.Status); Assert.Empty(page.Rows);
    }

    [Fact]
    public async Task PredicateBudget_IsExplicitAndDoesNotMasqueradeAsZeroResults()
    {
        using Corpus corpus = new(); await corpus.Add(); V2SearchService service = new(new(MaxPredicateEvaluations: 1));
        var registrations = await Register(service, corpus); using var session = Session(service, new("hero"), registrations);
        var page = await service.ReadPageAsync(session); Assert.Equal(SearchStatus.BudgetExceeded, page.Status);
        Assert.Empty(page.Rows); Assert.Equal(SearchCountKind.Unknown, page.Count.Kind);
    }

    [Fact]
    public async Task QueryFailure_IsRecoverableWithoutAdvancingCursor()
    {
        using Corpus corpus = new(); await corpus.Add(); bool fail = false;
        V2SearchService service = new(observe: (boundary, _) => { if (fail && boundary == SearchBoundary.WorkerStarted) throw new IOException("Injected query failure"); });
        var registrations = await Register(service, corpus); using var session = Session(service, new("memo,hero"), registrations);
        var page = await service.ReadPageAsync(session, pageSize: 1); fail = true;
        Assert.Equal(SearchStatus.Unavailable, (await service.ReadPageAsync(session, page.NextCursor)).Status); Assert.Equal(0, service.ActiveWorkers);
        fail = false; Assert.Equal(SearchStatus.Success, (await service.ReadPageAsync(session, page.NextCursor)).Status);
    }

    [Fact]
    public async Task UnsupportedDerivedProfile_UsesRawMemoPredicate()
    {
        using Corpus corpus = new(); await corpus.Add(); Edit(corpus.Packages[0].Path, "UPDATE package SET derived_text_profile='unknown'; UPDATE asset SET memo_nonblank=0");
        V2SearchService service = new(); var registrations = await Register(service, corpus);
        using var session = Session(service, new("memo", Fields: PackageSearchFields.Memo, ResultKinds: PackageSearchResultKinds.Asset, RequireNonEmptyMemo: true), registrations);
        Assert.NotEmpty(await Exhaust(service, session)); Assert.Contains(session.Diagnostics, d => d.Code == "RawTextFallback");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NullPrimaryValuesAndImmutableAmbiguousTies_PageWithoutGaps(bool descending)
    {
        using Corpus corpus = new(); await corpus.Add();
        Edit(corpus.Packages[0].Path, "UPDATE asset SET path_id=NULL,identity_status='incomplete' WHERE table_ordinal IN(0,1); UPDATE asset SET memo='memo' WHERE table_ordinal=9; UPDATE asset SET path_id=13,identity_status='ambiguous' WHERE table_ordinal IN(13,14)");
        V2SearchService service = new(); var registrations = await Register(service, corpus);
        var direction = descending ? PackageSearchSortDirection.Descending : PackageSearchSortDirection.Ascending;
        using var paths = Session(service, new("Hero", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset,
            SortBy: PackageSearchSortField.PathId, SortDirection: direction), registrations);
        var ordered = await Exhaust(service, paths, 1);
        var expectedIds = descending ? ordered.Select(r => r.PathId).OrderByDescending(x => x) : ordered.Select(r => r.PathId).OrderBy(x => x);
        Assert.Equal(expectedIds, ordered.Select(r => r.PathId)); Assert.Contains(ordered, r => r.PathId is null);
        using var names = Session(service, new("memo", Fields: PackageSearchFields.Memo, ResultKinds: PackageSearchResultKinds.Asset,
            SortBy: PackageSearchSortField.AssetName, SortDirection: direction), registrations);
        var memoRows = await Exhaust(service, names, 1);
        Assert.Null((descending ? memoRows[^1] : memoRows[0]).AssetName);
        using var ties = Session(service, new("duplicate", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset), registrations);
        var duplicates = await Exhaust(service, ties, 1); Assert.Equal(2, duplicates.Count);
        Assert.Equal(new int?[] { 13, 14 }, duplicates.Select(r => r.Key.TableOrdinal));
        Assert.All(duplicates, r => Assert.Equal("ambiguous", r.IdentityStatus));
    }

    [Fact]
    public async Task UnloadDrainsActiveLease_AndQueuedCancellationDoesNotOpenConnection()
    {
        using Corpus corpus = new(); await corpus.Add();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim release = new(false); bool armed = false;
        V2SearchService service = new(new(Workers: 1), (point, _) => {
            if (armed && point == SearchBoundary.WorkerStarted) { entered.TrySetResult(); release.Wait(); }
        });
        var registrations = await Register(service, corpus); using var session = Session(service, new("hero"), registrations); armed = true;
        Task<SearchPage> active = service.ReadPageAsync(session);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using CancellationTokenSource queuedStop = new();
            Task<SearchPage> queued = service.ReadPageAsync(session, token: queuedStop.Token); queuedStop.Cancel();
            Assert.Equal(SearchStatus.Cancelled, (await queued.WaitAsync(TimeSpan.FromSeconds(10))).Status);
            Task<bool> unload = service.UnloadAsync(registrations[0].RegistrationId); Assert.False(unload.IsCompleted);
            release.Set(); Assert.Equal(SearchStatus.StaleCursor, (await active.WaitAsync(TimeSpan.FromSeconds(10))).Status);
            Assert.True(await unload); Assert.Equal(0, service.ActiveWorkers); Assert.Equal(1, service.PeakWorkers);
        }
        finally { release.Set(); await active; }
        string original = corpus.Packages[0].Path, moved = original + ".moved";
        File.Move(original, moved); File.Move(moved, original);
    }

    [Fact]
    public async Task ExplicitCountCancellationAndMemoOnlyPackageSemantics_AreIndependent()
    {
        using Corpus corpus = new(); await corpus.Add(); using CancellationTokenSource stop = new(); bool armed = false;
        V2SearchService service = new(observe: (point, _) => { if (armed && point == SearchBoundary.Predicate) stop.Cancel(); });
        var registrations = await Register(service, corpus);
        using var session = Session(service, new("memo"), registrations); armed = true;
        var count = await service.CountAsync(session, stop.Token); Assert.Equal(SearchStatus.Cancelled, count.Status);
        Assert.Equal(SearchCountKind.Unknown, count.Count.Kind); armed = false;
        using var kinds = Session(service, new("Package,source", RequireNonEmptyMemo: true), registrations);
        var rows = await Exhaust(service, kinds);
        Assert.Contains(rows, r => r.Key.Kind == PackageSearchResultKind.Package);
        Assert.Contains(rows, r => r.Key.Kind == PackageSearchResultKind.SourceFile);
    }

    [Fact]
    public async Task ExplicitSessionCancellationAndInvalidation_KeepCompletedRowsImmutable()
    {
        using Corpus corpus = new(); await corpus.Add(); V2SearchService service = new(); var registrations = await Register(service, corpus);
        using var cancelled = Session(service, new("hero"), registrations); var page = await service.ReadPageAsync(cancelled, pageSize: 1);
        cancelled.Cancel(); Assert.Equal(SearchStatus.Cancelled, (await service.ReadPageAsync(cancelled, page.NextCursor)).Status);
        using var invalid = Session(service, new("hero"), registrations); invalid.Invalidate();
        Assert.Equal(SearchStatus.StaleCursor, (await service.ReadPageAsync(invalid)).Status); Assert.Single(page.Rows);
    }

    [Theory]
    [InlineData(PackageSearchSortField.PackageName)] [InlineData(PackageSearchSortField.SourceName)]
    [InlineData(PackageSearchSortField.SourcePath)] [InlineData(PackageSearchSortField.IndexDirectory)]
    [InlineData(PackageSearchSortField.AssetName)] [InlineData(PackageSearchSortField.PathId)] [InlineData(PackageSearchSortField.MatchCategory)]
    public async Task ConstantSortFolding_MixedSourcesAndPackageNullsPreserveV1Order(PackageSearchSortField field)
    {
        using Corpus corpus = new(); await corpus.Add(sourceCount: 3); await corpus.Add(name: "package");
        V2SearchService service = new(); var registrations = await Register(service, corpus);
        foreach (var direction in Enum.GetValues<PackageSearchSortDirection>())
        foreach (var kinds in new[] { PackageSearchResultKinds.Asset, PackageSearchResultKinds.All })
        {
            SearchRequest request = new("hero,memo,Package,characters,source", SortBy: field, SortDirection: direction, ResultKinds: kinds);
            using var session = Session(service, request, registrations.Reverse().ToArray());
            var actual = await Exhaust(service, session, 3);
            var expected = new PackageSearchService().Search(corpus.Packages.Select(p => p.Legacy).ToArray(), new(request.Query) {
                SortBy = field, SortDirection = direction, ResultKinds = kinds });
            Assert.Equal(expected.Select(Signature), actual.Select(Signature));
        }
    }

    [Theory]
    [InlineData(8, 1)] [InlineData(32, 2)] [InlineData(128, 4)]
    public async Task LargeSelections_ExhaustExactlyWithBoundedWorkers(int packages, int workers)
    {
        using Corpus corpus = new();
        for (int p = 0; p < packages; p++)
        { bool matches = p % 4 != 0; await corpus.Add(8, "Package " + p.ToString("D3"), nameFactory: _ => matches ? "asset" : "negative"); }
        V2SearchService service = new(new(Workers: workers)); var registrations = await Register(service, corpus);
        using var session = Session(service, new("asset", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset), registrations.Reverse().ToArray());
        var actual = await Exhaust(service, session, 100);
        var expected = new PackageSearchService().Search(corpus.Packages.Select(p => p.Legacy).ToArray(), new("asset") {
            Fields = PackageSearchFields.AssetName, ResultKinds = PackageSearchResultKinds.Asset });
        Assert.Equal(expected.Select(Signature), actual.Select(Signature));
        Assert.Equal(packages * 3 / 4 * 8, actual.Count); Assert.InRange(service.PeakWorkers, 1, workers);
        Assert.InRange(service.PeakBufferedCandidates, 1, 101); Assert.Equal(0, service.ActiveWorkers);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TextResourceLimits_ReturnBudgetFailureNotTruncatedSuccess(bool nativeLimit)
    {
        using Corpus corpus = new(); await corpus.Add();
        if (nativeLimit) Edit(corpus.Packages[0].Path, "UPDATE asset SET memo=replace(hex(zeroblob(3000)),'0','长') WHERE table_ordinal=0");
        V2SearchService service = new(nativeLimit ? new(MaxSqliteValueBytes: 4096) : new(MaxBufferedTextBytes: 1));
        var registered = await service.RegisterAsync(corpus.Packages[0].Path);
        if (nativeLimit && registered.Status == SearchStatus.BudgetExceeded) { Assert.Null(registered.Registration); return; }
        Assert.Equal(SearchStatus.Success, registered.Status);
        using var session = Session(service, new("hero", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset), registered.Registration!);
        var page = await service.ReadPageAsync(session); Assert.Equal(SearchStatus.BudgetExceeded, page.Status);
        Assert.Empty(page.Rows); Assert.Null(page.NextCursor); Assert.Equal(SearchCountKind.Unknown, page.Count.Kind);
        Assert.Equal(0, service.ActiveWorkers);
    }

    [Fact]
    public async Task EmittedMemoBudget_IsGlobalAndPreservesFullTextWhenWithinBudget()
    {
        using Corpus corpus = new(); await corpus.Add();
        Edit(corpus.Packages[0].Path, "UPDATE asset SET memo=replace(hex(zeroblob(2000)),'0','字') WHERE table_ordinal=0");
        V2SearchService constrained = new(new(MaxBufferedTextBytes: 4096)); var registrations = await Register(constrained, corpus);
        using var session = Session(constrained, new(PathId: -77, Fields: PackageSearchFields.None, ResultKinds: PackageSearchResultKinds.Asset), registrations);
        Assert.Equal(SearchStatus.BudgetExceeded, (await constrained.ReadPageAsync(session)).Status);
        V2SearchService roomy = new(); var room = await Register(roomy, corpus);
        using var valid = Session(roomy, session.Criteria, room);
        Assert.Equal(4000, Assert.Single(await Exhaust(roomy, valid)).Memo!.Length);
    }

    [Fact]
    public async Task OptInSearchMeasurements()
    {
        if (Environment.GetEnvironmentVariable("UABS_V2_SEARCH_BENCHMARK") != "1") return;
        int total = int.Parse(Environment.GetEnvironmentVariable("UABS_V2_SEARCH_SIZE") ?? "10000", System.Globalization.CultureInfo.InvariantCulture);
        int repetitions = int.Parse(Environment.GetEnvironmentVariable("UABS_V2_SEARCH_REPETITIONS") ?? "20", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains(total, new[] { 10_000, 100_000, 1_000_000 }); Assert.InRange(repetitions, 3, 20);
        using Corpus corpus = new();
        static string Name(int i) => i % 100 == 0 ? "HeroIcon" : i % 10 < 3 ? "钩爪_" + i.ToString("D8")
            : i % 10 < 5 ? "混合_asset_" + i.ToString("D8") : "asset_" + i.ToString("D8");
        var setup = System.Diagnostics.Stopwatch.StartNew();
        for (int package = 0; package < 8; package++)
        {
            await corpus.Add(total / 8, "Package " + package, nameFactory: Name);
            corpus.Packages[^1].Legacy.Package.Indexes.Clear(); // no preloaded V1 universe during bounded Search measurements
        }
        long bytes = corpus.Packages.Sum(p => new FileInfo(p.Path).Length);
        Console.WriteLine($"SEARCH_SETUP total={total} packages=8 elapsed_ms={setup.Elapsed.TotalMilliseconds:F2} bytes={bytes} runtime={Environment.Version}");
        V2SearchService service = new(new(OperationTimeout: TimeSpan.FromSeconds(15)));
        var admission = System.Diagnostics.Stopwatch.StartNew(); var registrations = await Register(service, corpus);
        Console.WriteLine($"SEARCH_ADMISSION total={total} ms={admission.Elapsed.TotalMilliseconds:F2}");
        var assets = PackageSearchResultKinds.Asset;
        var fields = PackageSearchFields.AssetName | PackageSearchFields.Memo;
        (string Label, SearchRequest Request)[] workloads = [
            ("exact", new("HeroIcon", Fields: PackageSearchFields.AssetName, ResultKinds: assets, ExactText: true)),
            ("pathid", new(PathId: -77, Fields: PackageSearchFields.None, ResultKinds: assets)),
            ("ascii", new("Hero", Fields: PackageSearchFields.AssetName, ResultKinds: assets)),
            ("cjk2", new("钩爪", Fields: PackageSearchFields.AssetName, ResultKinds: assets)),
            ("mixed-memo", new("混合,备注", Fields: fields, ResultKinds: assets)),
            ("include-exclude", new("asset,钩爪", "00001", fields, assets)),
            ("memo-only", new("memo", Fields: fields, ResultKinds: assets, RequireNonEmptyMemo: true)),
            ("broad", new("asset", Fields: PackageSearchFields.AssetName, ResultKinds: assets)),
            ("text-sort", new("asset", Fields: PackageSearchFields.AssetName, ResultKinds: assets, SortBy: PackageSearchSortField.AssetName)) ];
        string? selectedWorkloads = Environment.GetEnvironmentVariable("UABS_V2_SEARCH_WORKLOADS");
        if (selectedWorkloads is not null) workloads = workloads.Where(w => selectedWorkloads.Split(',').Contains(w.Label, StringComparer.Ordinal)).ToArray();
        foreach (var workload in workloads)
        {
            using var session = Session(service, workload.Request, registrations);
            List<double> first = [], next = []; long allocated = 0; int pageRows = 0; bool limited = false;
            long beforeMemory = 0;
            for (int iteration = -2; iteration < repetitions; iteration++)
            {
                if (iteration == 0) { GC.Collect(); GC.WaitForPendingFinalizers(); allocated = GC.GetTotalAllocatedBytes(); beforeMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64; }
                var timer = System.Diagnostics.Stopwatch.StartNew(); var page = await service.ReadPageAsync(session);
                double firstTime = timer.Elapsed.TotalMilliseconds;
                if (page.Status == SearchStatus.BudgetExceeded)
                { Console.WriteLine($"SEARCH_LIMIT total={total} workload={workload.Label} status={page.Status} ms={firstTime:F2}"); limited = true; break; }
                Assert.True(page.Status == SearchStatus.Success, string.Join(';', page.Diagnostics.Select(d => d.Message)));
                pageRows = page.Rows.Count;
                if (iteration >= 0) first.Add(firstTime);
                if (page.NextCursor is not null)
                {
                    timer.Restart(); var following = await service.ReadPageAsync(session, page.NextCursor);
                    Assert.True(following.Status == SearchStatus.Success, string.Join(';', following.Diagnostics.Select(d => d.Message)));
                    Assert.DoesNotContain(following.Rows, row => page.Rows.Any(previous => previous.Key == row.Key));
                    if (iteration >= 0) next.Add(timer.Elapsed.TotalMilliseconds);
                }
            }
            if (limited) continue;
            double Median(List<double> values) { values.Sort(); return values.Count == 0 ? 0 : values[values.Count / 2]; }
            double firstMedian = Median(first), nextMedian = Median(next);
            long churn = (GC.GetTotalAllocatedBytes() - allocated) / repetitions;
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            // Independent streaming predicate count, outside timing; no full-result list.
            var oracleRequest = workload.Request;
            var includes = new PackageSearchRequest(oracleRequest.Query, oracleRequest.ExcludedQuery);
            bool Match(string? value, string term) => !string.IsNullOrEmpty(value) && (oracleRequest.ExactText
                ? value.Equals(term, StringComparison.OrdinalIgnoreCase) : value.Contains(term, StringComparison.OrdinalIgnoreCase));
            long expected = 0;
            for (int i = 0; i < total / 8; i++)
            {
                if (oracleRequest.PathId is not null && Corpus.PathId(i) != oracleRequest.PathId) continue;
                string? memo = Corpus.Memos[i % 8];
                if (oracleRequest.RequireNonEmptyMemo && string.IsNullOrWhiteSpace(memo)) continue;
                string?[] values = oracleRequest.Fields.HasFlag(PackageSearchFields.Memo) ? [Name(i), memo] : [Name(i)];
                if (includes.ExcludeTerms.Any(t => values.Any(v => Match(v, t)))) continue;
                if (includes.IncludeTerms.Count > 0 && !includes.IncludeTerms.Any(t => values.Any(v => Match(v, t)))) continue;
                expected += 8;
            }
            var count = await service.CountAsync(session); Assert.Equal(SearchStatus.Success, count.Status); Assert.Equal(expected, count.Count.Value);
            Console.WriteLine($"SEARCH_MEASURE total={total} workload={workload.Label} reps={repetitions} matches={expected} page_rows={pageRows} first_median_ms={firstMedian:F2} next_median_ms={nextMedian:F2} p95_first_ms={first[(int)Math.Ceiling(first.Count * 0.95) - 1]:F2} managed_churn_per_two_pages={churn} working_before={beforeMemory} working_after={process.WorkingSet64} process_peak={process.PeakWorkingSet64} buffered={service.PeakBufferedCandidates} workers={service.PeakWorkers}");
        }
    }
}
