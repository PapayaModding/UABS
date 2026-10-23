using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UABS.PackageV2.Search;

namespace UABS.Services;

/// <summary>A single immutable production Search session. Admission is once per explicit Search,
/// never per page. Owns no Unity source and never routes results through V1 persistence.</summary>
public sealed class V2SearchOperation : IAsyncDisposable
{
    private readonly V2SearchService _engine;
    private readonly V2BrowseRegistration[] _scope;
    private readonly string[] _evidence;
    private readonly List<Guid> _registered = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly List<CancellationTokenRegistration> _unloads = new();
    private int _disposed;
    private SearchSession? _session;
    public SearchSession Session => _session ?? throw new InvalidOperationException("Search admission is incomplete.");
    public event Action<V2SearchOperation>? Invalidated;

    private V2SearchOperation(V2BrowseRegistration[] scope, V2SearchService engine)
    { _scope = scope; _engine = engine; _evidence = new string[scope.Length]; }

    public static async Task<V2SearchOperation> OpenAsync(IReadOnlyList<V2BrowseRegistration> scope,
        SearchRequest request, CancellationToken token, V2SearchService? engine = null)
    {
        if (scope.Count is < 1 or > 128 || scope.Distinct().Count() != scope.Count)
            throw new InvalidDataException("Select between 1 and 128 distinct registered V2 packages.");
        var operation = new V2SearchOperation(scope.ToArray(), engine ?? new V2SearchService());
        try
        {
            // Unload cancellation also protects the admission phase, before a session exists.
            foreach (var entry in operation._scope)
                operation._unloads.Add(entry.Lifetime.Register(operation.Invalidate));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, operation._stop.Token);
            for (int i = 0; i < operation._scope.Length; i++)
            {
                var entry = operation._scope[i];
                operation._evidence[i] = await V2BrowseCatalog.EvidenceAsync(entry.Path, linked.Token).ConfigureAwait(false);
                var admitted = await operation._engine.RegisterAsync(entry.Path, entry.LocationEpoch, linked.Token).ConfigureAwait(false);
                linked.Token.ThrowIfCancellationRequested();
                if (admitted.Registration is not { } registration)
                    throw new InvalidDataException($"{admitted.Status}: {admitted.Message}");
                operation._registered.Add(registration.RegistrationId);
                if (registration.Package.PackageId != entry.PackageId || registration.Package.GenerationId != entry.GenerationId)
                    throw new InvalidDataException("Package identity/generation changed; unload and reopen it in Packages.");
            }
            await operation.CheckEvidenceAsync(linked.Token).ConfigureAwait(false);
            var created = operation._engine.CreateSession(request, operation._registered);
            operation._session = created.Session ?? throw new InvalidDataException($"{created.Status}: {created.Message}");
            linked.Token.ThrowIfCancellationRequested();
            return operation;
        }
        catch { await operation.DisposeAsync().ConfigureAwait(false); throw; }
    }

    public void Invalidate()
    {
        if (_stop.IsCancellationRequested) return;
        _session?.Invalidate();
        _stop.Cancel();
        Invalidated?.Invoke(this);
    }

    public async Task<bool> CheckCurrentAsync(CancellationToken token = default)
    {
        try { await CheckEvidenceAsync(token).ConfigureAwait(false); return !Session.IsStale && !_stop.IsCancellationRequested; }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or OperationCanceledException)
        { Invalidate(); return false; }
    }

    private async Task CheckEvidenceAsync(CancellationToken token)
    {
        for (int i = 0; i < _scope.Length; i++)
        {
            token.ThrowIfCancellationRequested(); _scope[i].Lifetime.ThrowIfCancellationRequested();
            if (_evidence[i] != await V2BrowseCatalog.EvidenceAsync(_scope[i].Path, token).ConfigureAwait(false))
                throw new InvalidDataException("Package bytes/location changed; Refresh Search to readmit the current revision.");
        }
    }

    public async Task<SearchPage> ReadPageAsync(SearchCursor? cursor = null, int pageSize = 100,
        CancellationToken token = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _stop.Token);
        try
        {
            await CheckEvidenceAsync(linked.Token).ConfigureAwait(false);
            var page = await _engine.ReadPageAsync(Session, cursor, pageSize, linked.Token).ConfigureAwait(false);
            if (page.Status == SearchStatus.StaleCursor) Invalidate();
            if (page.Status != SearchStatus.Success) return page;
            await CheckEvidenceAsync(linked.Token).ConfigureAwait(false);
            linked.Token.ThrowIfCancellationRequested();
            return page;
        }
        catch (OperationCanceledException)
        { return Failure(_stop.IsCancellationRequested ? SearchStatus.StaleCursor : SearchStatus.Cancelled, "Search cancelled or registration unloaded."); }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        { Invalidate(); return Failure(SearchStatus.StaleCursor, e.Message); }
    }

    private SearchPage Failure(SearchStatus status, string message) => new(status, [], null, false,
        new(SearchCountKind.Stale, 0), [new(status.ToString(), message)], Session.Revisions);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _session?.Cancel(); _stop.Cancel();
        foreach (var unload in _unloads) unload.Dispose();
        foreach (var id in _registered) await _engine.UnloadAsync(id).ConfigureAwait(false);
        _session?.Dispose();
        // Already-scheduled operations may still inspect the cancellation token.
    }
}
