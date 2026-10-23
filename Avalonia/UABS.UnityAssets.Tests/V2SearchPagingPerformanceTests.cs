using System.Diagnostics;
using UABS.PackageV2;
using UABS.PackageV2.Search;
using UABS.Services;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

public sealed class V2SearchPagingPerformanceTests
{
    private static async Task<List<SearchResult>> Exhaust(V2SearchService engine, V2SearchTests.Corpus corpus, SearchRequest request)
    {
        var registrations = await V2SearchTests.Register(engine, corpus);
        using var session = engine.CreateSession(request, registrations.Select(r => r.RegistrationId).ToArray()).Session!;
        List<SearchResult> rows = []; SearchCursor? cursor = null;
        do
        {
            var page = await engine.ReadPageAsync(session, cursor, 17);
            Assert.Equal(SearchStatus.Success, page.Status); rows.AddRange(page.Rows); cursor = page.NextCursor;
            Assert.InRange(engine.PeakBufferedCandidates, 0, 18); Assert.Equal(0, engine.ActiveWorkers);
        } while (cursor is not null);
        Assert.Equal(rows.Count, rows.Select(r => r.Key).Distinct().Count());
        foreach (var registration in registrations) await engine.UnloadAsync(registration.RegistrationId);
        return rows;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task IndexedAndUncoveredPartitions_AgreeWithGeneralPlan_AllSortsAndFederation(int sourceCount)
    {
        using var corpus = new V2SearchTests.Corpus();
        string?[] names = [null, "", "asset", "ASSET", "Asset", "asset_1", "asset_10", "asset_2", "asset~", "asset!", "钩爪asset", "ÉASSET", "😀asset", "\tasset", "ＡＳＳＥＴ", "assetİı", "assetßẞ"];
        await corpus.Add(90, "same", nameFactory: i => names[i % names.Length], sourceCount: sourceCount);
        await corpus.Add(90, "same", nameFactory: i => names[(i + 7) % names.Length]);
        foreach (var field in Enum.GetValues<PackageSearchSortField>())
        foreach (var direction in Enum.GetValues<PackageSearchSortDirection>())
        {
            var request = new SearchRequest("asset,钩爪", Fields: PackageSearchFields.AssetName | PackageSearchFields.Memo,
                ResultKinds: PackageSearchResultKinds.Asset, SortBy: field, SortDirection: direction);
            var expected = await Exhaust(new(new(UseIndexedAsciiPaging: false)), corpus, request);
            var actual = await Exhaust(new(), corpus, request);
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData("asset", "ASSET_2", false, false)]
    [InlineData("asset", "", true, false)]
    [InlineData("钩爪,memo", "blocked", false, true)]
    [InlineData("", "", false, false)]
    public async Task IndexedPartitions_PreserveCategoriesExclusionsMemosAndNumericCriteria(string query, string excluded, bool exact, bool memoOnly)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(260);
        foreach (long? pathId in new long?[] { null, 5 })
        {
            var request = new SearchRequest(query, excluded, PackageSearchFields.AssetName | PackageSearchFields.Memo,
                PackageSearchResultKinds.Asset, ExactText: exact, RequireNonEmptyMemo: memoOnly, PathId: pathId);
            Assert.Equal(await Exhaust(new(new(UseIndexedAsciiPaging: false)), corpus, request), await Exhaust(new(), corpus, request));
        }
    }

    [Fact]
    public async Task PinnedAdmission_IsImmutableAcrossNewCaptureAndMemoReaderRefresh()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        using var reader = (await PackageReader.OpenAsync(corpus.Packages[0].Path, memoReuse: true)).Reader!;
        var old = await reader.CaptureSearchAdmissionAsync();
        if (!OperatingSystem.IsWindows()) { Assert.Null(old); return; }
        Assert.NotNull(old); await old.CheckAsync();
        Edit(corpus, "UPDATE asset SET memo='外部' WHERE path_id=5");
        await Assert.ThrowsAsync<InvalidDataException>(() => old.CheckAsync());
        var fresh = (await reader.CaptureSearchAdmissionAsync())!; await fresh.CheckAsync();
        Assert.True(fresh.Info.CatalogRevision > old.Info.CatalogRevision);
        await Assert.ThrowsAsync<InvalidDataException>(() => old.CheckAsync()); // never renew an old cursor's evidence
        reader.Dispose(); await Assert.ThrowsAsync<ObjectDisposedException>(() => fresh.CheckAsync());
        File.Move(corpus.Packages[0].Path, corpus.Packages[0].Path + ".closed"); // no leaked pin
    }

    [Theory]
    [InlineData("path")]
    [InlineData("identity")]
    [InlineData("class")]
    [InlineData("type")]
    [InlineData("memo")]
    public async Task SelectiveCriteria_DoNotForceTheAsciiIndex(string kind)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(200);
        using var reader = (await PackageReader.OpenAsync(corpus.Packages[0].Path)).Reader!;
        var key = (await reader.ReadAssetsAsync())[0].Key;
        var request = kind switch {
            "path" => new SearchRequest("asset", PathId: 42, ResultKinds: PackageSearchResultKinds.Asset),
            "identity" => new SearchRequest(AssetIdentity: key, ResultKinds: PackageSearchResultKinds.Asset),
            "class" => new SearchRequest("asset", ClassId: 1234567, ResultKinds: PackageSearchResultKinds.Asset),
            "type" => new SearchRequest("asset", TypeName: "UnknownType", ResultKinds: PackageSearchResultKinds.Asset),
            _ => new SearchRequest("asset", RequireNonEmptyMemo: true, ResultKinds: PackageSearchResultKinds.Asset) };
        var engine = new V2SearchService(); var registrations = await V2SearchTests.Register(engine, corpus);
        using var session = engine.CreateSession(request, registrations.Select(r => r.RegistrationId).ToArray()).Session!;
        int queries = 0;
        using var listener = new ActivityListener { ShouldListenTo = s => s.Name == "UABS.Search",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => { if (a.OperationName == "candidate-query") Interlocked.Increment(ref queries); } };
        ActivitySource.AddActivityListener(listener);
        var page = await engine.ReadPageAsync(session); Assert.Equal(SearchStatus.Success, page.Status); Assert.NotEmpty(page.Rows);
        Assert.Equal(1, queries);
        foreach (var registration in registrations) await engine.UnloadAsync(registration.RegistrationId);
    }

    [Fact]
    public async Task UnpinnedReader_NeverOffersAReusableProof()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        using var reader = (await PackageReader.OpenAsync(corpus.Packages[0].Path)).Reader!;
        Assert.Null(await reader.CaptureSearchAdmissionAsync());
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("derived")]
    [InlineData("generation")]
    [InlineData("journal")]
    public async Task ChangedInvalidAdmission_CannotBeRenewed(string kind)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add();
        using var reader = (await PackageReader.OpenAsync(corpus.Packages[0].Path, memoReuse: true)).Reader!;
        var admission = await reader.CaptureSearchAdmissionAsync();
        if (!OperatingSystem.IsWindows()) { Assert.Null(admission); return; }
        Assert.NotNull(admission);
        Edit(corpus, kind switch {
            "schema" => "CREATE INDEX unexpected ON asset(memo)",
            "derived" => "UPDATE asset SET name_ascii_key='wrong' WHERE path_id=5",
            "generation" => "UPDATE package SET generation_uuid=randomblob(16)",
            _ => "PRAGMA journal_mode=WAL" });
        await Assert.ThrowsAsync<InvalidDataException>(() => admission.CheckAsync());
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.CaptureSearchAdmissionAsync());
    }

    [Fact]
    public async Task WarmOperation_NoFullHashOrAdmission_ExternalNonCatalogWriteStillStales()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(350);
        using var catalog = new V2BrowseCatalog(); var entry = await catalog.RegisterAsync(corpus.Packages[0].Path);
        await using var operation = await V2SearchOperation.OpenAsync([entry], new("asset", ResultKinds: PackageSearchResultKinds.Asset), default);
        var first = await operation.ReadPageAsync(); Assert.Equal(SearchStatus.Success, first.Status);
        List<string> spans = []; object gate = new();
        using var listener = new ActivityListener { ShouldListenTo = s => s.Name is "UABS.Search" or "UABS.Memos",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => { lock (gate) spans.Add(a.OperationName); } };
        ActivitySource.AddActivityListener(listener);
        var next = await operation.ReadPageAsync(first.NextCursor); Assert.Equal(SearchStatus.Success, next.Status);
        if (OperatingSystem.IsWindows()) { Assert.DoesNotContain("package-hash", spans); Assert.DoesNotContain("full-validation", spans); Assert.Contains("read-admission-check", spans); }
        Edit(corpus, "UPDATE source SET original_name='external-no-catalog-change'");
        var stale = await operation.ReadPageAsync(next.NextCursor); Assert.Equal(SearchStatus.StaleCursor, stale.Status); Assert.Empty(stale.Rows);
        await using var refreshed = await V2SearchOperation.OpenAsync([entry], new("asset", ResultKinds: PackageSearchResultKinds.Asset), default);
        Assert.Equal(SearchStatus.Success, (await refreshed.ReadPageAsync()).Status);
    }

    [Fact]
    public async Task ExternalWriteBetweenQueryAndPublication_CannotPublishOldAdmission()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250);
        using var catalog = new V2BrowseCatalog(); var entry = await catalog.RegisterAsync(corpus.Packages[0].Path);
        int once = 0;
        var engine = new V2SearchService(observe: (b, _) => { if (b == SearchBoundary.BeforePublication && Interlocked.Exchange(ref once, 1) == 0)
            Edit(corpus, "UPDATE source SET original_name='external-mid-page'"); });
        await using var operation = await V2SearchOperation.OpenAsync([entry], new("asset", ResultKinds: PackageSearchResultKinds.Asset), default, engine);
        var page = await operation.ReadPageAsync(); Assert.Equal(SearchStatus.StaleCursor, page.Status);
        Assert.Empty(page.Rows); Assert.Null(page.NextCursor); Assert.Equal(0, engine.ActiveWorkers);
    }

    [Fact]
    public async Task PinnedReplacementRejected_UnloadAndNewEpochCannotReuseCursor()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(250);
        using var catalog = new V2BrowseCatalog(); string path = corpus.Packages[0].Path;
        var entry = await catalog.RegisterAsync(path);
        await using var old = await V2SearchOperation.OpenAsync([entry], new("asset", ResultKinds: PackageSearchResultKinds.Asset), default);
        var first = await old.ReadPageAsync();
        if (OperatingSystem.IsWindows()) Assert.ThrowsAny<IOException>(() => File.Move(path, path + ".backup"));
        await catalog.UnloadAsync(entry); Assert.Equal(SearchStatus.StaleCursor, (await old.ReadPageAsync(first.NextCursor)).Status);
        await old.DisposeAsync(); File.Move(path, path + ".backup"); File.Copy(path + ".backup", path);
        var replacement = await catalog.RegisterAsync(path); Assert.NotEqual(entry.LocationEpoch, replacement.LocationEpoch);
        await using var fresh = await V2SearchOperation.OpenAsync([replacement], new("asset", ResultKinds: PackageSearchResultKinds.Asset), default);
        Assert.Equal(SearchStatus.StaleCursor, (await fresh.ReadPageAsync(first.NextCursor)).Status);
        await catalog.UnloadAsync(replacement);
    }

    private static void Edit(V2SearchTests.Corpus corpus, string sql)
    {
        using var c = V2SearchTests.Connect(corpus.Packages[0].Path);
        c.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
        using var command = c.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
    }
}
