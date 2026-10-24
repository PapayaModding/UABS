using UABS.UnityAssets.Packages.Search;

namespace UABS.PackageV2.Search;

/// <summary>Immutable criteria/scope, cancellation lifetime and revision vector; owns no SQLite handles.</summary>
public sealed class SearchSession : IDisposable
{
    internal readonly Guid Id = Guid.NewGuid();
    internal readonly Guid Owner;
    internal readonly Registration[] Registrations;
    internal readonly string[] Include;
    internal readonly string[] Exclude;
    internal readonly SearchRequest Request;
    internal BundleSearchRequest? Bundle { get; }
    private readonly CancellationTokenSource _stop = new();
    internal CancellationToken Token { get; }
    private int _stale;
    public bool IsStale => Volatile.Read(ref _stale) != 0 || Registrations.Any(r => !r.Active);
    public string ComparerProfile { get; } = StorageSchema.TextProfile;
    public IReadOnlyList<SearchRevision> Revisions { get; }
    public IReadOnlyList<SearchDiagnostic> Diagnostics { get; }
    public SearchRequest Criteria => Request;
    internal bool Empty => (Include.Length == 0 && Request.PathId is null && Request.AssetIdentity is null)
        || (Request.Fields == PackageSearchFields.None && Request.PathId is null && Request.AssetIdentity is null)
        || Request.ResultKinds == PackageSearchResultKinds.None || Registrations.Length == 0;
    internal SearchSession(Guid owner, SearchRequest request, Registration[] registrations, IEnumerable<SearchDiagnostic> diagnostics,
        BundleSearchRequest? bundle = null)
    {
        Owner = owner; Request = request; Bundle = bundle; Registrations = registrations; Token = _stop.Token;
        static string[] Terms(string? text) => new PackageSearchRequest(text).IncludeTerms
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Include = Terms(request.Query); Exclude = Terms(request.ExcludedQuery);
        Revisions = Array.AsReadOnly(registrations.Select(r => new SearchRevision(r.Value.RegistrationId,
            r.Value.LocationEpoch, r.Value.Package.PackageId, r.Value.Package.GenerationId, r.Value.Package.CatalogRevision)).ToArray());
        Diagnostics = Array.AsReadOnly((bundle is null ? diagnostics : diagnostics.Append(new SearchDiagnostic(
            "BundleIndexEvidence", "Only sources with indexed bundle kind, complete/partial status and verified revision evidence qualify. "
                + "Other/insufficient source evidence is excluded; this does not verify original files are currently available."))).ToArray());
    }
    public void Cancel() { lock (_stop) { if (!_stop.IsCancellationRequested) _stop.Cancel(); } }
    public void Invalidate() { Interlocked.Exchange(ref _stale, 1); Cancel(); }
    // Token source remains available to already-scheduled operations; it holds no native resources/timers.
    public void Dispose() => Cancel();
}

internal sealed class Registration(SearchRegistration value, IReadOnlyList<SearchDiagnostic> diagnostics)
{
    internal SearchRegistration Value { get; } = value;
    internal IReadOnlyList<SearchDiagnostic> Diagnostics { get; } = diagnostics;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _unloaded = new();
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _active = true;
    private int _users;
    internal long SchemaVersion { get; init; }
    internal PackageSearchFields IndeterminateBundleExclusions { get; init; }
    internal IReadOnlyDictionary<string, string?> ConstantSortValues { get; init; } = new Dictionary<string, string?>();
    internal bool Active { get { lock (_gate) return _active; } }
    internal IDisposable Lease()
    {
        lock (_gate) { if (!_active) throw new StaleSearchException(); _users++; }
        return new LeaseHandle(this);
    }
    internal Task Unload()
    {
        lock (_gate) { _active = false; if (_users == 0) _drained.TrySetResult(); }
        _unloaded.Cancel();
        return _drained.Task;
    }
    internal CancellationToken UnloadToken => _unloaded.Token;
    private sealed class LeaseHandle(Registration owner) : IDisposable
    {
        public void Dispose() { lock (owner._gate) { if (--owner._users == 0 && !owner._active) owner._drained.TrySetResult(); } }
    }
}
internal sealed class StaleSearchException : Exception;
