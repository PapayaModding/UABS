using System.Diagnostics;
using Microsoft.Data.Sqlite;
using UABS.UnityAssets.Packages.Search;

namespace UABS.PackageV2.Search;

/// <summary>Isolated read-only federation. No application registration, UI state or writable package API.</summary>
public sealed class V2SearchService
{
    private readonly Guid _owner = Guid.NewGuid();
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Registration> _registrations = [];
    private readonly SemaphoreSlim _workers;
    private readonly SearchOptions _options;
    private readonly Action<SearchBoundary, string>? _observe;
    private int _activeWorkers, _peakWorkers;
    private int _peakBufferedCandidates;
    public int ActiveWorkers => Volatile.Read(ref _activeWorkers);
    public int PeakWorkers => Volatile.Read(ref _peakWorkers);
    public int PeakBufferedCandidates => Volatile.Read(ref _peakBufferedCandidates);
    public V2SearchService(SearchOptions? options = null, Action<SearchBoundary, string>? observe = null)
    {
        _options = options ?? new(); _observe = observe;
        if (_options.Workers is < 1 or > 4 || _options.MaxSelectedPackages is < 1 or > 128
            || _options.MaxPredicateEvaluations < 1 || (_options.OperationTimeout is TimeSpan t && (t <= TimeSpan.Zero || t > TimeSpan.FromMinutes(10))))
            throw new ArgumentOutOfRangeException(nameof(options));
        _workers = new(_options.Workers, _options.Workers);
    }

    public async Task<SearchRegistrationResult> RegisterAsync(string path, long locationEpoch = 0, CancellationToken token = default)
    {
        try
        {
            if (locationEpoch < 0) return new(SearchStatus.InvalidRequest, null, "Location epoch must be nonnegative.");
            string fullPath = Path.GetFullPath(path);
            return await Worker(token, () => {
                using SqliteConnection c = StorageSchema.Open(fullPath, true);
                using var interrupt = token.Register(() => SQLitePCL.raw.sqlite3_interrupt(c.Handle));
                using StorageSql sql = new(c); sql.Exec("BEGIN");
                PackageInfo info = StorageSchema.Validate(c, false, token);
                List<SearchDiagnostic> warnings = [];
                Guid id = Guid.NewGuid();
                if (info.IndexStatus != "complete") warnings.Add(new("PartialIndex", "Matches cover indexed data only; package coverage is partial.", id));
                if (sql.Number("SELECT EXISTS(SELECT 1 FROM source WHERE index_status IN('failed','unsupported'))") != 0)
                    warnings.Add(new("UnavailableMetadata", "One or more sources have unavailable metadata.", id));
                if (sql.Number("SELECT EXISTS(SELECT 1 FROM asset WHERE name_status<>'present')") != 0)
                    warnings.Add(new("OptionalNamesUnavailable", "Some assets lack indexed names; numeric/memo matching remains available.", id));
                if (sql.Number("SELECT EXISTS(SELECT 1 FROM dependency) OR EXISTS(SELECT 1 FROM source WHERE dependency_status<>'complete')") != 0)
                    warnings.Add(new("DependencyEvidence", "Raw dependency declarations are not resolved targets; dependency enrichment may be incomplete.", id));
                if (!info.DerivedFieldsCompatible) warnings.Add(new("RawTextFallback", "Derived text profile is unsupported; use authoritative raw .NET predicates.", id));
                long schemaVersion = sql.Number("PRAGMA schema_version");
                sql.Exec("COMMIT"); token.ThrowIfCancellationRequested();
                SearchRegistration value = new(id, fullPath, locationEpoch, info);
                lock (_gate)
                {
                    if (_registrations.Values.Any(r => r.Value.Package.PackageId == info.PackageId))
                        return new SearchRegistrationResult(SearchStatus.RegistrationConflict, null, "A package UUID is already registered; unload it before registering an alias/copy.");
                    _registrations.Add(id, new(value, warnings.AsReadOnly()) { SchemaVersion = schemaVersion });
                }
                return new SearchRegistrationResult(SearchStatus.Success, value, "Read-only package registered.");
            }).ConfigureAwait(false);
        }
        catch (Exception e) { return new(token.IsCancellationRequested ? SearchStatus.Cancelled : SearchStatus.InvalidPackage, null, e.Message); }
    }

    public async Task<bool> UnloadAsync(Guid registrationId)
    {
        Registration? registration;
        lock (_gate) { if (!_registrations.Remove(registrationId, out registration)) return false; }
        await registration.Unload().ConfigureAwait(false);
        return true;
    }

    public SearchSessionResult CreateSession(SearchRequest request, IReadOnlyList<Guid> selectedRegistrationIds)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(selectedRegistrationIds);
        if (!Enum.IsDefined(request.SortBy) || !Enum.IsDefined(request.SortDirection)
            || (request.Fields & ~PackageSearchFields.All) != 0 || (request.ResultKinds & ~PackageSearchResultKinds.All) != 0
            || ((request.ClassId is not null || request.TypeName is not null || request.AssetIdentity is not null) && request.ResultKinds != PackageSearchResultKinds.Asset)
            || selectedRegistrationIds.Count > _options.MaxSelectedPackages)
            return new(SearchStatus.InvalidRequest, null, "Invalid criteria, scope budget, or non-Asset result kinds with asset-only filters.");
        lock (_gate)
        {
            List<Registration> registrations = [];
            foreach (Guid id in selectedRegistrationIds.Distinct())
            {
                if (!_registrations.TryGetValue(id, out var registration) || !registration.Active)
                    return new(SearchStatus.StaleCursor, null, "Selected registration is no longer loaded; explicitly select/search again.");
                registrations.Add(registration);
            }
            return new(SearchStatus.Success, new(_owner, request, registrations.ToArray(), registrations.SelectMany(r => r.Diagnostics)), "Criteria and scope captured.");
        }
    }

    public async Task<SearchPage> ReadPageAsync(SearchSession session, SearchCursor? cursor = null,
        int pageSize = 100, CancellationToken token = default)
    {
        if (pageSize is < 1 or > 500 || session.Owner != _owner)
            return Failure(session, SearchStatus.InvalidRequest, cursor, "Invalid page size or session owner.");
        if (cursor is not null && cursor.SessionId != session.Id)
            return Failure(session, SearchStatus.StaleCursor, null, "Cursor scope/criteria differ from this immutable search session.");
        using Operation operation = new(session, token, _options);
        try
        {
            CheckSession(session); operation.Token.ThrowIfCancellationRequested();
            SortedSet<Candidate> best = new(Comparer<Candidate>.Create((a, b) => SearchQuery.Compare(a.Row, b.Row, session.Request)));
            object merge = new();
            if (!session.Empty)
                await Task.WhenAll(session.Registrations.Select(registration => WorkRegistration(registration, operation.Token, c => {
                    Configure(c, session, operation, registration.Value.Path);
                    using StorageSql sql = new(c); sql.Exec("BEGIN"); Verify(c, registration);
                    using (SqliteCommand command = SearchQuery.Command(c, session, registration, cursor?.Last, pageSize + 1, false))
                    using (SqliteDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            operation.Token.ThrowIfCancellationRequested();
                            Candidate candidate = new(SearchQuery.Row(reader, registration), reader.GetInt64(18), registration);
                            lock (merge)
                            {
                                best.Add(candidate); if (best.Count > pageSize + 1) best.Remove(best.Max!);
                                int peak; do { peak = PeakBufferedCandidates; if (best.Count <= peak) break; }
                                while (Interlocked.CompareExchange(ref _peakBufferedCandidates, best.Count, peak) != peak);
                            }
                        }
                    Verify(c, registration); sql.Exec("COMMIT");
                }))).ConfigureAwait(false);
            bool more = best.Count > pageSize;
            Candidate[] emitted = best.Take(pageSize).ToArray();
            // Only emitted assets hydrate memo contents. No prefetched memo or full result universe is retained.
            await Task.WhenAll(emitted.Where(c => c.Row.Key.Kind == PackageSearchResultKind.Asset).GroupBy(c => c.Registration)
                .Select(group => WorkRegistration(group.Key, operation.Token, c => {
                    using StorageSql sql = new(c); sql.Exec("BEGIN"); Verify(c, group.Key);
                    foreach (Candidate item in group)
                    {
                        operation.Token.ThrowIfCancellationRequested();
                        using SqliteDataReader reader = sql.Command("SELECT memo,memo_revision FROM asset WHERE asset_id=@p0", item.AssetId).ExecuteReader();
                        if (!reader.Read() || reader.GetInt64(1) != item.Row.MemoRevision) throw new StaleSearchException();
                        item.Row = item.Row with { Memo = reader.IsDBNull(0) ? null : reader.GetString(0) };
                    }
                    Verify(c, group.Key); sql.Exec("COMMIT");
                }))).ConfigureAwait(false);
            _observe?.Invoke(SearchBoundary.BeforePublication, "page");
            await ValidateVector(session, operation.Token).ConfigureAwait(false);
            long loaded = checked((cursor?.Loaded ?? 0) + emitted.Length);
            lock (_gate)
            {
                CheckSession(session); operation.Token.ThrowIfCancellationRequested();
                var rows = Array.AsReadOnly(emitted.Select(c => c.Row).ToArray());
                return new(SearchStatus.Success, rows, more ? new(session.Id, rows[^1], loaded) : null, more,
                    new(more ? SearchCountKind.LoadedAtLeast : SearchCountKind.Exact, loaded), session.Diagnostics, session.Revisions);
            }
        }
        catch (Exception e)
        {
            SearchStatus status = Status(session, operation, token, e);
            return Failure(session, status, cursor, e.Message);
        }
    }

    public async Task<SearchCountResult> CountAsync(SearchSession session, CancellationToken token = default)
    {
        if (session.Owner != _owner) return new(SearchStatus.InvalidRequest, new(SearchCountKind.Unknown, 0),
            [new("InvalidRequest", "Session owner differs.")]);
        using Operation operation = new(session, token, _options);
        try
        {
            CheckSession(session); operation.Token.ThrowIfCancellationRequested(); long total = 0;
            if (!session.Empty)
                await Task.WhenAll(session.Registrations.Select(registration => WorkRegistration(registration, operation.Token, c => {
                    Configure(c, session, operation, registration.Value.Path);
                    using StorageSql sql = new(c); sql.Exec("BEGIN"); Verify(c, registration);
                    using SqliteCommand command = SearchQuery.Command(c, session, registration, null, 0, true);
                    long count = Convert.ToInt64(command.ExecuteScalar());
                    Interlocked.Add(ref total, count); Verify(c, registration); sql.Exec("COMMIT");
                }))).ConfigureAwait(false);
            _observe?.Invoke(SearchBoundary.BeforePublication, "count");
            await ValidateVector(session, operation.Token).ConfigureAwait(false);
            lock (_gate) { CheckSession(session); operation.Token.ThrowIfCancellationRequested(); return new(SearchStatus.Success, new(SearchCountKind.Exact, total), session.Diagnostics); }
        }
        catch (Exception e)
        {
            SearchStatus status = Status(session, operation, token, e);
            return new(status, new(status == SearchStatus.StaleCursor ? SearchCountKind.Stale : SearchCountKind.Unknown, 0),
                Array.AsReadOnly(session.Diagnostics.Append(new SearchDiagnostic(status.ToString(), e.Message)).ToArray()));
        }
    }

    private async Task<T> Worker<T>(CancellationToken token, Func<T> body)
    {
        await _workers.WaitAsync(token).ConfigureAwait(false);
        try { return await Task.Run(() => {
            token.ThrowIfCancellationRequested();
            int active = Interlocked.Increment(ref _activeWorkers);
            int peak; do { peak = PeakWorkers; if (active <= peak) break; } while (Interlocked.CompareExchange(ref _peakWorkers, active, peak) != peak);
            try { return body(); } finally { Interlocked.Decrement(ref _activeWorkers); }
        }).ConfigureAwait(false); }
        finally { _workers.Release(); }
    }
    private async Task WorkRegistration(Registration registration, CancellationToken token, Action<SqliteConnection> body)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, registration.UnloadToken);
        await Worker(linked.Token, () => {
            using IDisposable lease = registration.Lease();
            using SqliteConnection c = StorageSchema.Open(registration.Value.Path, true);
            using var interrupt = linked.Token.Register(() => SQLitePCL.raw.sqlite3_interrupt(c.Handle));
            using (StorageSql sql = new(c)) { sql.Exec("PRAGMA cache_size=-4096"); sql.Exec("PRAGMA temp_store=FILE"); sql.Exec("PRAGMA query_only=ON"); }
            _observe?.Invoke(SearchBoundary.WorkerStarted, registration.Value.Path);
            linked.Token.ThrowIfCancellationRequested(); body(c); linked.Token.ThrowIfCancellationRequested(); return true;
        }).ConfigureAwait(false);
    }
    private void Configure(SqliteConnection c, SearchSession session, Operation operation, string path)
    {
        bool Matches(string? value, string term) => !string.IsNullOrEmpty(value) && (session.Request.ExactText
            ? value.Equals(term, StringComparison.OrdinalIgnoreCase) : value.Contains(term, StringComparison.OrdinalIgnoreCase));
        void Check() { operation.Step(); _observe?.Invoke(SearchBoundary.Predicate, path); operation.Token.ThrowIfCancellationRequested(); }
        c.CreateFunction<string?, int, int>("uabs_match", (value, category) => {
            Check(); foreach (string term in session.Include) if (Matches(value, term)) return category; return 0;
        });
        c.CreateFunction<string?, int>("uabs_exclude", value => {
            Check(); foreach (string term in session.Exclude) if (Matches(value, term)) return 1; return 0;
        });
        c.CreateFunction<string?, int>("uabs_type", value => { Check(); return string.Equals(value, session.Request.TypeName, StringComparison.OrdinalIgnoreCase) ? 1 : 0; });
        c.CreateCollation("uabs_order", (a, b) => { Check(); _observe?.Invoke(SearchBoundary.SortComparison, path); operation.Token.ThrowIfCancellationRequested(); return SearchQuery.Text(a, b); });
    }
    private static void Verify(SqliteConnection c, Registration registration)
    {
        if (!registration.Active) throw new StaleSearchException();
        using StorageSql sql = new(c);
        if (sql.Number("PRAGMA schema_version") != registration.SchemaVersion) throw new StaleSearchException();
        using SqliteDataReader manifest = sql.Command("SELECT package_uuid,generation_uuid,catalog_revision FROM package WHERE singleton=1").ExecuteReader();
        PackageInfo p = registration.Value.Package;
        if (!manifest.Read() || StorageSchema.Uuid(manifest.GetValue(0)) != p.PackageId
            || StorageSchema.Uuid(manifest.GetValue(1)) != p.GenerationId || manifest.GetInt64(2) != p.CatalogRevision)
            throw new StaleSearchException();
    }
    private Task ValidateVector(SearchSession session, CancellationToken token) => Task.WhenAll(session.Registrations
        .Select(r => WorkRegistration(r, token, c => Verify(c, r))));
    private void CheckSession(SearchSession session)
    {
        lock (_gate)
            if (session.IsStale || session.ComparerProfile != StorageSchema.TextProfile || session.Registrations.Any(r =>
                !_registrations.TryGetValue(r.Value.RegistrationId, out var current) || !ReferenceEquals(current, r)))
                throw new StaleSearchException();
    }
    private static SearchStatus Status(SearchSession session, Operation operation, CancellationToken token, Exception error)
    {
        if (session.IsStale || error is StaleSearchException) { session.Invalidate(); return SearchStatus.StaleCursor; }
        if (token.IsCancellationRequested || session.Token.IsCancellationRequested) return SearchStatus.Cancelled;
        if (operation.Token.IsCancellationRequested) return SearchStatus.BudgetExceeded;
        return error is IOException or SqliteException or UnauthorizedAccessException ? SearchStatus.Unavailable : SearchStatus.Failed;
    }
    private static SearchPage Failure(SearchSession session, SearchStatus status, SearchCursor? cursor, string message) =>
        new(status, Array.Empty<SearchResult>(), null, false,
            new(status == SearchStatus.StaleCursor ? SearchCountKind.Stale : SearchCountKind.Unknown, cursor?.Loaded ?? 0),
            Array.AsReadOnly(session.Diagnostics.Append(new SearchDiagnostic(status.ToString(), message)).ToArray()), session.Revisions);
    private sealed class Candidate(SearchResult row, long assetId, Registration registration)
    {
        internal SearchResult Row = row;
        internal readonly long AssetId = assetId;
        internal readonly Registration Registration = registration;
    }
    private sealed class Operation : IDisposable
    {
        private readonly CancellationTokenSource _stop;
        private readonly long _max;
        private long _steps;
        internal CancellationToken Token => _stop.Token;
        internal Operation(SearchSession session, CancellationToken token, SearchOptions options)
        {
            _max = options.MaxPredicateEvaluations;
            _stop = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
            _stop.CancelAfter(options.OperationTimeout ?? TimeSpan.FromSeconds(30));
        }
        internal void Step() { if (Interlocked.Increment(ref _steps) > _max) _stop.Cancel(); Token.ThrowIfCancellationRequested(); }
        public void Dispose() => _stop.Dispose();
    }
}
