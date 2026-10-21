using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using UABS.PackageV2;
using UABS.PackageV2.Search;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

/// <summary>Explicit opt-in, reusable disposable corpora; never a production package/registry entry.</summary>
public static class V2SearchCalibration
{
    public sealed record Study(string Directory, string Label, int Total, int Packages, bool NegativePackages, string[] Hashes, bool LongMemo = false);
    public static string Name(int i) => i % 100 == 0 ? "HeroIcon" : i % 10 < 3 ? "钩爪_" + i.ToString("D8")
        : i % 10 < 5 ? "混合_asset_" + i.ToString("D8") : "asset_" + i.ToString("D8");
    public static async Task Prepare(string label, int total, int packages, bool negatives, bool longMemo = false)
    {
        Assert.True(total % packages == 0); Assert.InRange(packages, 1, 128);
        string parent = Environment.GetEnvironmentVariable("UABS_V2_TEST_ROOT") ?? throw new InvalidOperationException("Explicit disposable root required.");
        string directory = System.IO.Directory.CreateDirectory(Path.Combine(parent, "calibration-" + Guid.NewGuid().ToString("N"))).FullName;
        List<string> hashes = [];
        using V2SearchTests.Corpus corpus = new();
        for (int p = 0; p < packages; p++)
        {
            int number = p;
            string? originalMemo = V2SearchTests.Corpus.Memos[3];
            try
            {
                if (longMemo) V2SearchTests.Corpus.Memos[3] = originalMemo + " " + new string('长', 8192);
                await corpus.Add(total / packages, "Package " + p,
                    nameFactory: i => negatives && number % 4 == 0 ? "negative_" + i.ToString("D8") : Name(i));
            }
            finally { V2SearchTests.Corpus.Memos[3] = originalMemo; }
            string destination = Path.Combine(directory, $"package-{p}.uabs.sqlite");
            File.Copy(corpus.Packages[^1].Path, destination, false); // closed, owned synthetic fixture only
            hashes.Add(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(destination))));
            corpus.Packages[^1].Legacy.Package.Indexes.Clear();
        }
        Study study = new(directory, label, total, packages, negatives, hashes.ToArray(), longMemo);
        File.WriteAllText(Path.Combine(directory, "study.json"), JsonSerializer.Serialize(study));
        Console.WriteLine("CALIBRATION_STUDY " + JsonSerializer.Serialize(study));
    }
    private static Study Load(string directory)
    {
        string root = Path.GetFullPath(Environment.GetEnvironmentVariable("UABS_V2_TEST_ROOT") ?? throw new InvalidOperationException("Disposable root required."));
        string full = Path.GetFullPath(directory);
        if (!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(full).StartsWith("calibration-", StringComparison.Ordinal)) throw new InvalidOperationException("Not an owned calibration directory.");
        Study study = JsonSerializer.Deserialize<Study>(File.ReadAllText(Path.Combine(full, "study.json")))!;
        Assert.Equal(full, study.Directory);
        for (int p = 0; p < study.Packages; p++) Assert.Equal(study.Hashes[p], Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(full, $"package-{p}.uabs.sqlite")))));
        return study;
    }
    public static async Task Run(string directory, string tag, int repetitions, bool deep)
    {
        Study study = Load(directory); Assert.InRange(repetitions, 3, 20);
        V2SearchService service = new(new(OperationTimeout: TimeSpan.FromSeconds(15)));
        List<SearchRegistration> registrations = [];
        var admission = Stopwatch.StartNew();
        for (int p = 0; p < study.Packages; p++)
        {
            var registered = await service.RegisterAsync(Path.Combine(directory, $"package-{p}.uabs.sqlite"));
            Assert.Equal(SearchStatus.Success, registered.Status); registrations.Add(registered.Registration!);
        }
        Emit(new { Type = "admission", tag, study.Label, study.Total, study.Packages, Milliseconds = admission.Elapsed.TotalMilliseconds,
            Runtime = Environment.Version.ToString(), Pid = Environment.ProcessId });
        var assets = PackageSearchResultKinds.Asset; var name = PackageSearchFields.AssetName;
        var both = name | PackageSearchFields.Memo;
        (string Label, SearchRequest Query)[] workloads = study.NegativePackages
            ? [("broad", new("asset", Fields: name, ResultKinds: assets)), ("sparse", new("HeroIcon", Fields: name, ResultKinds: assets, ExactText: true)),
                ("empty", new("not_present", Fields: name, ResultKinds: assets))]
            : [("exact", new("HeroIcon", Fields: name, ResultKinds: assets, ExactText: true)), ("pathid", new(PathId: -77, Fields: PackageSearchFields.None, ResultKinds: assets)),
                ("ascii", new("Hero", Fields: name, ResultKinds: assets)), ("cjk2", new("钩爪", Fields: name, ResultKinds: assets)),
                ("mixed-memo", new("混合,备注", Fields: both, ResultKinds: assets)), ("include-exclude", new("asset,钩爪", "00001", both, assets)),
                ("memo-only", new("memo", Fields: both, ResultKinds: assets, RequireNonEmptyMemo: true)), ("broad", new("asset", Fields: name, ResultKinds: assets)),
                ("text-sort", new("asset", Fields: name, ResultKinds: assets, SortBy: PackageSearchSortField.AssetName)),
                ("class-text", new("钩爪", Fields: name, ResultKinds: assets, ClassId: 1234567))];
        string? selected = Environment.GetEnvironmentVariable("UABS_V2_CALIBRATION_WORKLOADS");
        if (selected is not null) workloads = workloads.Where(w => selected.Split(',').Contains(w.Label, StringComparer.Ordinal)).ToArray();
        foreach (var workload in workloads)
        {
            var creation = Stopwatch.StartNew(); using var session = V2SearchTests.Session(service, workload.Query, registrations.ToArray());
            double creationMs = creation.Elapsed.TotalMilliseconds;
            List<double> first = [], next = []; List<long> allocated = [];
            var timer = Stopwatch.StartNew(); var initial = await service.ReadPageAsync(session);
            AssertPage(initial); double firstAttempt = timer.Elapsed.TotalMilliseconds;
            await service.ReadPageAsync(session); // second warmup; first is separately labelled first attempt, not disk-cold
            GC.Collect(); GC.WaitForPendingFinalizers();
            SQLitePCL.raw.sqlite3_memory_highwater(1);
            using Process process = Process.GetCurrentProcess(); process.Refresh();
            long workingBefore = process.WorkingSet64, heapBefore = GC.GetGCMemoryInfo().HeapSizeBytes;
            for (int i = 0; i < repetitions; i++)
            {
                long before = GC.GetTotalAllocatedBytes(); timer.Restart(); var page = await service.ReadPageAsync(session);
                AssertPage(page); first.Add(timer.Elapsed.TotalMilliseconds);
                if (page.NextCursor is not null)
                {
                    timer.Restart(); var following = await service.ReadPageAsync(session, page.NextCursor);
                    AssertPage(following); next.Add(timer.Elapsed.TotalMilliseconds);
                    Assert.DoesNotContain(following.Rows, r => page.Rows.Any(prior => prior.Key == r.Key));
                }
                allocated.Add(GC.GetTotalAllocatedBytes() - before);
            }
            long expected = Expected(study, workload.Query);
            var countTimer = Stopwatch.StartNew(); var count = await service.CountAsync(session);
            Assert.Equal(SearchStatus.Success, count.Status); Assert.Equal(expected, count.Count.Value);
            process.Refresh();
            Emit(new { Type = "measure", tag, study.Label, study.Total, study.Packages, Workload = workload.Label, repetitions,
                CreationMs = creationMs, FirstAttemptMs = firstAttempt, FirstMedianMs = Median(first), NextMedianMs = next.Count == 0 ? (double?)null : Median(next),
                P95FirstMs = repetitions >= 20 ? Percentile(first, 0.95) : (double?)null, P95NextMs = repetitions >= 20 && next.Count != 0 ? Percentile(next, 0.95) : (double?)null,
                FirstMinMs = first.Min(), FirstMaxMs = first.Max(), MedianAllocatedBytesPerPair = allocated.Order().ElementAt(allocated.Count / 2),
                WorkingBefore = workingBefore, WorkingAfter = process.WorkingSet64, ProcessLifetimePeak = process.PeakWorkingSet64,
                ManagedHeapBefore = heapBefore, ManagedHeapAfterLastGc = GC.GetGCMemoryInfo().HeapSizeBytes,
                NativeSqliteHighWater = SQLitePCL.raw.sqlite3_memory_highwater(0), NativeSqliteAfterClose = SQLitePCL.raw.sqlite3_memory_used(),
                CountMs = countTimer.Elapsed.TotalMilliseconds, Matches = expected, Buffer = service.PeakBufferedCandidates, Workers = service.PeakWorkers });
            if (deep && workload.Label == "broad") await Deep(service, session, expected, tag, study);
        }
        Assert.Equal(0, service.ActiveWorkers);
        foreach (var registration in registrations) Assert.True(await service.UnloadAsync(registration.RegistrationId));
        Load(directory); // byte-for-byte unchanged synthetic data after read-only run
    }
    private static long Expected(Study study, SearchRequest request)
    {
        var terms = new PackageSearchRequest(request.Query, request.ExcludedQuery);
        bool Match(string? value, string term) => !string.IsNullOrEmpty(value) && (request.ExactText
            ? value.Equals(term, StringComparison.OrdinalIgnoreCase) : value.Contains(term, StringComparison.OrdinalIgnoreCase));
        long count = 0;
        for (int p = 0; p < study.Packages; p++)
        for (int i = 0; i < study.Total / study.Packages; i++)
        {
            if (request.PathId is not null && V2SearchTests.Corpus.PathId(i) != request.PathId) continue;
            string name = study.NegativePackages && p % 4 == 0 ? "negative_" + i.ToString("D8") : Name(i);
            string? memo = V2SearchTests.Corpus.Memos[i % 8];
            if (request.RequireNonEmptyMemo && string.IsNullOrWhiteSpace(memo)) continue;
            string?[] values = request.Fields.HasFlag(PackageSearchFields.Memo) ? [name, memo] : [name];
            if (terms.ExcludeTerms.Any(t => values.Any(v => Match(v, t)))) continue;
            if (terms.IncludeTerms.Count > 0 && !terms.IncludeTerms.Any(t => values.Any(v => Match(v, t)))) continue;
            count++;
        }
        return count;
    }
    private static async Task Deep(V2SearchService service, SearchSession session, long expected, string tag, Study study)
    {
        HashSet<SearchRowKey> keys = []; SearchCursor? cursor = null; int index = 0; SearchResult? previous = null;
        do
        {
            var timer = Stopwatch.StartNew(); var page = await service.ReadPageAsync(session, cursor);
            AssertPage(page);
            foreach (var row in page.Rows)
            {
                Assert.True(keys.Add(row.Key), "Duplicate continuation row.");
                int p = int.Parse(row.PackageName["Package ".Length..], System.Globalization.CultureInfo.InvariantCulture);
                int i = row.Key.TableOrdinal!.Value;
                Assert.InRange(p, 0, study.Packages - 1); Assert.InRange(i, 0, study.Total / study.Packages - 1);
                string expectedName = study.NegativePackages && p % 4 == 0 ? "negative_" + i.ToString("D8") : Name(i);
                Assert.Equal(expectedName, row.AssetName); Assert.True(row.AssetName?.Contains("asset", StringComparison.OrdinalIgnoreCase) == true);
                Assert.Equal(V2SearchTests.Corpus.PathId(i), row.PathId);
                if (previous is not null) Assert.True(CompareReference(previous, row) < 0, "Global order inversion across continuation.");
                previous = row;
            }
            if (index is 0 or 5 or 20 or 40) Emit(new { Type = "deep", tag, study.Label, PageIndex = index, Milliseconds = timer.Elapsed.TotalMilliseconds, Loaded = keys.Count });
            cursor = page.NextCursor; index++;
            Assert.True(index <= expected / 100 + 2, "Infinite continuation loop.");
        } while (cursor is not null);
        Assert.Equal(expected, keys.Count); // correctness job only, excluded from measured retained-memory samples
        Emit(new { Type = "exhaustion", tag, study.Label, UniqueRows = keys.Count, Pages = index });
    }
    private static int CompareReference(SearchResult a, SearchResult b)
    {
        static int Text(string? x, string? y) { int c = StringComparer.OrdinalIgnoreCase.Compare(x, y); return c != 0 ? c : StringComparer.Ordinal.Compare(x, y); }
        foreach (int comparison in new[] { Text(a.PackageName, b.PackageName), Text(a.PackagePath, b.PackagePath),
            Text(a.IndexDirectory, b.IndexDirectory), Text(a.SourcePath, b.SourcePath), Text(a.AssetName, b.AssetName),
            Nullable.Compare(a.PathId, b.PathId), a.Key.Kind.CompareTo(b.Key.Kind),
            StringComparer.Ordinal.Compare(a.Key.PackageId.ToString("N"), b.Key.PackageId.ToString("N")),
            StringComparer.Ordinal.Compare(a.Key.GenerationId.ToString("N"), b.Key.GenerationId.ToString("N")),
            StringComparer.Ordinal.Compare(a.Key.SourceId?.ToString("N"), b.Key.SourceId?.ToString("N")),
            StringComparer.Ordinal.Compare(a.Key.SourceRevision?.ToString("N"), b.Key.SourceRevision?.ToString("N")),
            Nullable.Compare(a.Key.DirectoryOrdinal, b.Key.DirectoryOrdinal), Nullable.Compare(a.Key.TableOrdinal, b.Key.TableOrdinal) })
            if (comparison != 0) return comparison;
        return 0;
    }
    public static async Task Resources(string directory)
    {
        Study study = Load(directory); using CancellationTokenSource stop = new();
        long cancelAt = 0; int hits = 0; bool armed = false;
        V2SearchService service = new(observe: (point, _) => {
            if (armed && point == SearchBoundary.Predicate && Interlocked.Increment(ref hits) == 100)
            { cancelAt = Stopwatch.GetTimestamp(); stop.Cancel(); }
        });
        List<SearchRegistration> registrations = [];
        for (int p = 0; p < study.Packages; p++) registrations.Add((await service.RegisterAsync(Path.Combine(directory, $"package-{p}.uabs.sqlite"))).Registration!);
        using var session = V2SearchTests.Session(service, new("asset", Fields: PackageSearchFields.AssetName, ResultKinds: PackageSearchResultKinds.Asset), registrations.ToArray());
        // Controlled live-heap probe, separate from latency samples and allocation churn. Retain one page only.
        await service.ReadPageAsync(session);
        long liveBefore = GC.GetTotalMemory(true); SearchPage? retained = null;
        for (int repeat = 0; repeat < 20; repeat++) { retained = await service.ReadPageAsync(session); AssertPage(retained); }
        long liveAfter = GC.GetTotalMemory(true);
        Emit(new { Type = "live-heap", study.Label, study.Packages, ManagedLiveBefore = liveBefore, ManagedLiveAfter = liveAfter,
            Delta = liveAfter - liveBefore, RetainedPages = 1, RetainedRows = retained!.Rows.Count,
            NativeAfterClose = SQLitePCL.raw.sqlite3_memory_used(), Buffer = service.PeakBufferedCandidates, Workers = service.PeakWorkers });
        GC.KeepAlive(retained);
        armed = true; var cancelled = await service.ReadPageAsync(session, token: stop.Token);
        Assert.Equal(SearchStatus.Cancelled, cancelled.Status); Assert.Empty(cancelled.Rows); Assert.Null(cancelled.NextCursor);
        double cancelMs = Stopwatch.GetElapsedTime(cancelAt).TotalMilliseconds; Assert.Equal(0, service.ActiveWorkers);
        armed = false;
        Task<bool>? unload = null; long unloadAt = 0; V2SearchService? unloading = null;
        unloading = new(observe: (point, _) => {
            if (armed && point == SearchBoundary.WorkerStarted && Interlocked.Increment(ref hits) == 1)
            { unloadAt = Stopwatch.GetTimestamp(); unload = unloading!.UnloadAsync(registrations[0].RegistrationId); }
        });
        registrations.Clear();
        for (int p = 0; p < study.Packages; p++) registrations.Add((await unloading.RegisterAsync(Path.Combine(directory, $"package-{p}.uabs.sqlite"))).Registration!);
        using var unloadSession = V2SearchTests.Session(unloading, session.Criteria, registrations.ToArray()); hits = 0; armed = true;
        var stale = await unloading.ReadPageAsync(unloadSession); Assert.Equal(SearchStatus.StaleCursor, stale.Status);
        Assert.Empty(stale.Rows); Assert.True(await unload!); Assert.Equal(0, unloading.ActiveWorkers);
        Emit(new { Type = "resources", study.Label, study.Packages, CancellationResponseMs = cancelMs,
            UnloadDrainAndPageMs = Stopwatch.GetElapsedTime(unloadAt).TotalMilliseconds, NoPartialRows = true, ActiveWorkers = unloading.ActiveWorkers });
    }
    public static async Task Profile(string directory, string query = "asset")
    {
        Study study = Load(directory); long predicate = 0, sort = 0, connections = 0;
        V2SearchService service = new(observe: (point, _) => {
            if (point == SearchBoundary.Predicate) Interlocked.Increment(ref predicate);
            if (point == SearchBoundary.SortComparison) Interlocked.Increment(ref sort);
            if (point == SearchBoundary.WorkerStarted) Interlocked.Increment(ref connections);
        });
        List<SearchRegistration> registrations = [];
        for (int p = 0; p < study.Packages; p++) registrations.Add((await service.RegisterAsync(Path.Combine(directory, $"package-{p}.uabs.sqlite"))).Registration!);
        using var session = V2SearchTests.Session(service, new(query, Fields: PackageSearchFields.AssetName,
            ResultKinds: PackageSearchResultKinds.Asset, ExactText: query == "HeroIcon"), registrations.ToArray());
        long before = GC.GetTotalAllocatedBytes(); var timer = Stopwatch.StartNew(); var page = await service.ReadPageAsync(session); AssertPage(page);
        Emit(new { Type = "profile", study.Label, Query = query, Milliseconds = timer.Elapsed.TotalMilliseconds, TotalAllocated = GC.GetTotalAllocatedBytes() - before,
            ManagedPredicateCalls = predicate - sort, ManagedSortCalls = sort, Connections = connections, Emitted = page.Rows.Count, Buffer = service.PeakBufferedCandidates });
        if (study.NegativePackages) return;
        string path = Path.Combine(directory, "package-0.uabs.sqlite");
        using SqliteConnection c = new(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()); c.Open();
        long sortCalls = 0, predicateCalls = 0;
        c.CreateCollation("uabs_order", (a, b) => { sortCalls++; int comparison = StringComparer.OrdinalIgnoreCase.Compare(a, b); return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(a, b); });
        c.CreateFunction<string?, int>("matches", text => { predicateCalls++; return text?.Contains("asset", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0; });
        using (var only = c.CreateCommand()) { only.CommandText = "SELECT COUNT(*) FROM source"; Assert.Equal(1L, (long)only.ExecuteScalar()!); }
        long[]? reference = null;
        foreach (bool constants in new[] { true, false })
        {
            using var command = c.CreateCommand();
            command.CommandText = "SELECT a.asset_id FROM asset a JOIN logical_file f ON f.file_id=a.file_id JOIN source s ON s.source_id=f.source_id WHERE matches(a.name)=1 ORDER BY "
                + (constants ? "s.index_directory COLLATE uabs_order,s.original_path COLLATE uabs_order," : "")
                + "a.name COLLATE uabs_order,a.path_id,a.table_ordinal LIMIT 101";
            // Warm the identical command before taking controlled thread-local allocation counters.
            using (var warm = command.ExecuteReader()) while (warm.Read()) _ = warm.GetInt64(0);
            SQLitePCL.raw.sqlite3_db_status(c.Handle, SQLitePCL.raw.SQLITE_DBSTATUS_CACHE_HIT, out _, out _, 1);
            SQLitePCL.raw.sqlite3_db_status(c.Handle, SQLitePCL.raw.SQLITE_DBSTATUS_CACHE_MISS, out _, out _, 1);
            sortCalls = 0; predicateCalls = 0; long allocated = GC.GetAllocatedBytesForCurrentThread(); timer.Restart();
            List<long> ids = []; using (var reader = command.ExecuteReader()) while (reader.Read()) ids.Add(reader.GetInt64(0));
            long churn = GC.GetAllocatedBytesForCurrentThread() - allocated;
            SQLitePCL.raw.sqlite3_db_status(c.Handle, SQLitePCL.raw.SQLITE_DBSTATUS_CACHE_HIT, out int cacheHits, out _, 0);
            SQLitePCL.raw.sqlite3_db_status(c.Handle, SQLitePCL.raw.SQLITE_DBSTATUS_CACHE_MISS, out int cacheMisses, out _, 0);
            if (reference is null) reference = ids.ToArray(); else Assert.Equal(reference, ids);
            Emit(new { Type = "controlled-sort", study.Label, constants, Milliseconds = timer.Elapsed.TotalMilliseconds,
                Allocated = churn, sortCalls, predicateCalls, Returned = ids.Count, cacheHits, cacheMisses });
        }
    }
    private static void AssertPage(SearchPage page) => Assert.True(page.Status == SearchStatus.Success, string.Join(';', page.Diagnostics.Select(d => d.Message)));
    private static double Median(List<double> values) { var v = values.Order().ToArray(); return (v[(v.Length - 1) / 2] + v[v.Length / 2]) / 2; }
    private static double Percentile(List<double> values, double quantile) => values.Order().ElementAt((int)Math.Ceiling(values.Count * quantile) - 1);
    private static void Emit(object value) => Console.WriteLine("CALIBRATION " + JsonSerializer.Serialize(value));
}
