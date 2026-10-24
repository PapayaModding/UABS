using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UABS.PackageV2.Search;
using UABS.PackageV2;
using Microsoft.Data.Sqlite;

namespace UABS.Services;

/// <summary>A single immutable production Search session. Admission is once per explicit Search,
/// never per page. Owns no Unity source and never routes results through V1 persistence.</summary>
public sealed class V2SearchOperation : IAsyncDisposable
{
    private readonly V2SearchService _engine;
    private readonly V2BrowseRegistration[] _scope;
    private readonly string[] _evidence;
    private readonly PackageReader.SearchAdmission?[] _admissions;
    private readonly List<Guid> _registered = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly List<CancellationTokenRegistration> _unloads = new();
    private int _disposed;
    private SearchSession? _session;
    public SearchSession Session => _session ?? throw new InvalidOperationException("Search admission is incomplete.");
    public event Action<V2SearchOperation>? Invalidated;

    private V2SearchOperation(V2BrowseRegistration[] scope, V2SearchService engine)
    { _scope = scope; _engine = engine; _evidence = new string[scope.Length]; _admissions = new PackageReader.SearchAdmission?[scope.Length]; }

    public static Task<V2SearchOperation> OpenAsync(IReadOnlyList<V2BrowseRegistration> scope,
        SearchRequest request, CancellationToken token, V2SearchService? engine = null)
        => OpenCoreAsync(scope, request, null, token, engine);

    public static async Task<V2SearchOperation> OpenBundleAsync(IReadOnlyList<V2BrowseRegistration> scope,
        BundleSearchRequest request, CancellationToken token, V2SearchService? engine = null)
    {
        engine ??= new V2SearchService();
        var validation = engine.CreateBundleSession(request, Array.Empty<Guid>());
        using var probe = validation.Session ?? throw new InvalidDataException($"{validation.Status}: {validation.Message}");
        return await OpenCoreAsync(scope, null, request, token, engine).ConfigureAwait(false);
    }

    private static async Task<V2SearchOperation> OpenCoreAsync(IReadOnlyList<V2BrowseRegistration> scope,
        SearchRequest? request, BundleSearchRequest? bundle, CancellationToken token, V2SearchService? engine)
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
                var admission = operation._admissions[i] = await entry.CaptureSearchAdmissionAsync(linked.Token).ConfigureAwait(false);
                if (admission is null)
                    operation._evidence[i] = await V2BrowseCatalog.EvidenceAsync(entry.Path, linked.Token).ConfigureAwait(false);
                var admitted = await operation._engine.RegisterAsync(entry.Path, entry.LocationEpoch, linked.Token).ConfigureAwait(false);
                linked.Token.ThrowIfCancellationRequested();
                if (admitted.Registration is not { } registration)
                    throw new InvalidDataException($"{admitted.Status}: {admitted.Message}");
                operation._registered.Add(registration.RegistrationId);
                if (registration.Package.PackageId != entry.PackageId || registration.Package.GenerationId != entry.GenerationId)
                    throw new InvalidDataException("Package identity/generation changed; unload and reopen it in Packages.");
                if (admission is not null && admission.Info.CatalogRevision != registration.Package.CatalogRevision)
                    throw new InvalidDataException("Search read admission/catalog changed during registration. Refresh explicitly.");
            }
            await operation.CheckEvidenceAsync(linked.Token).ConfigureAwait(false);
            if (bundle is not null)
            {
                var created = operation._engine.CreateBundleSession(bundle, operation._registered);
                operation._session = created.Session?.SourceSession ?? throw new InvalidDataException($"{created.Status}: {created.Message}");
            }
            else
            {
                var created = operation._engine.CreateSession(request!, operation._registered);
                operation._session = created.Session ?? throw new InvalidDataException($"{created.Status}: {created.Message}");
            }
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
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or OperationCanceledException or ObjectDisposedException or SqliteException)
        { Invalidate(); return false; }
    }

    private async Task CheckEvidenceAsync(CancellationToken token)
    {
        for (int i = 0; i < _scope.Length; i++)
        {
            token.ThrowIfCancellationRequested(); _scope[i].Lifetime.ThrowIfCancellationRequested();
            if (_admissions[i] is { } admission)
                await _scope[i].CheckSearchAdmissionAsync(admission, token).ConfigureAwait(false);
            else if (_evidence[i] != await V2BrowseCatalog.EvidenceAsync(_scope[i].Path, token).ConfigureAwait(false))
                throw new InvalidDataException("Package bytes/location changed; Refresh Search to readmit the current revision.");
        }
    }

    public Task<SearchPage> ReadPageAsync(SearchCursor? cursor = null, int pageSize = 100,
        CancellationToken token = default) => ReadPageCoreAsync(Session, cursor, pageSize, token);

    /// <summary>Bounded membership/metadata check for a retained selected occurrence;
    /// reuses admitted registrations and the exact frozen criteria, not a name lookup.</summary>
    public async Task<SearchPage> ProbeAssetAsync(SearchResult asset, CancellationToken token = default)
    {
        if (asset.AssetIdentity is not { } key) throw new ArgumentException("An exact asset occurrence is required.", nameof(asset));
        if (!Session.Criteria.ResultKinds.HasFlag(UABS.UnityAssets.Packages.Search.PackageSearchResultKinds.Asset))
            return new(SearchStatus.Success, [], null, false, new(SearchCountKind.Exact, 0), Session.Diagnostics, Session.Revisions);
        var created = _engine.CreateSession(Session.Criteria with { AssetIdentity = key,
            ResultKinds = UABS.UnityAssets.Packages.Search.PackageSearchResultKinds.Asset }, _registered);
        using var probe = created.Session ?? throw new InvalidDataException(created.Message);
        return await ReadPageCoreAsync(probe, null, 1, token).ConfigureAwait(false);
    }

    private async Task<SearchPage> ReadPageCoreAsync(SearchSession session, SearchCursor? cursor, int pageSize, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _stop.Token);
        try
        {
            await CheckEvidenceAsync(linked.Token).ConfigureAwait(false);
            var page = await _engine.ReadPageAsync(session, cursor, pageSize, linked.Token).ConfigureAwait(false);
            if (page.Status == SearchStatus.StaleCursor) Invalidate();
            if (page.Status != SearchStatus.Success) return page;
            await CheckEvidenceAsync(linked.Token).ConfigureAwait(false);
            linked.Token.ThrowIfCancellationRequested();
            return page;
        }
        catch (OperationCanceledException)
        { return Failure(_stop.IsCancellationRequested ? SearchStatus.StaleCursor : SearchStatus.Cancelled, "Search cancelled or registration unloaded."); }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or ObjectDisposedException or SqliteException)
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
