using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UABS.Models;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.ViewModels;

/// <summary>One retained metadata browsing state shared by Packages and Assets; exact memo edits use the shared coordinator.</summary>
public sealed class V2BrowseViewModel : ViewModelBase, IDisposable
{
    private readonly V2BrowseCatalog _catalog;
    private readonly IFilePickerService _picker;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _pages, _details, _metadataLoad;
    private CancellationTokenSource _navigation = new();
    private int _version, _detailVersion, _metadataVersion, _openVersion;
    private V2BrowseRegistration? _selected;
    private IReadOnlyList<V2AssetRow> _assets = Array.Empty<V2AssetRow>();
    private IReadOnlyList<V2MetadataItem> _metadata = Array.Empty<V2MetadataItem>();
    private V2AssetRow? _asset;
    private V2MetadataItem? _metadataItem;
    private string _status = "Register a .uabs.sqlite package to browse metadata and edit exact asset memos.";
    private string _inspector = "Select indexed metadata. Eligible asset memos are editable below; content preview and structural saving are unavailable.";
    private string _metadataStatus = "Choose an evidence table.";
    private bool _busy, _hasNext, _hasMetadataNext;
    private long _after, _next, _metadataNext;
    private readonly List<long> _history = new();
    private V2MetadataKind _kind;
    private InspectorMemoService? _memos;
    public EditableMemoInspectorViewModel MemoInspector { get; private set; } = new(new UnavailableMemoDraftCoordinator(), new PackageEditorSessionGuard());
    public Task MemoTransitionTask { get; private set; } = Task.CompletedTask;
    public void ConfigureMemos(InspectorMemoService memos, IMemoDraftCoordinator drafts)
    {
        _memos = memos; MemoInspector.Dispose(); MemoInspector = new(drafts, memos);
        memos.Published += ApplyMemoPublication; OnPropertyChanged(nameof(MemoInspector)); RefreshMemoEligibility();
    }
    public void RefreshMemoEligibility() => QueueMemo(_asset);
    private void QueueMemo(V2AssetRow? asset)
    {
        var previous = MemoInspector.Identity;
        PackageMemoResolution resolution = asset is not null && _selected is { } entry && _memos is not null
            ? _memos.ResolveIndexed(entry, asset.Identity.PackageId, asset.Identity.GenerationId, asset.Identity.SourceId,
                asset.Identity.SourceRevision, asset.Identity.DirectoryOrdinal, asset.Identity.PathId, asset.IdentityStatus, asset.Memo)
            : new(PackageMemoResolutionStatus.Unsupported, null, null, null, null, null, "Select an indexed asset in a loaded V2 package.");
        MemoTransitionTask = PresentMemoAsync(previous, resolution);
    }
    private async Task PresentMemoAsync(PackageMemoIdentity? previous, PackageMemoResolution resolution)
    {
        try { await Task.WhenAll(previous is null ? Task.CompletedTask : MemoInspector.FlushIdentityForNavigationAsync(previous, _lifetime.Token), MemoInspector.ShowAsync(resolution)); }
        catch (OperationCanceledException) { }
        catch (Exception e) { Printer.PrintException("V2 asset memo transition failed.", e); }
    }
    private void ApplyMemoPublication(V2MemoPublication publication)
    {
        if (publication.Registration != _selected) return;
        var target = publication.Change.Target;
        foreach (var row in Assets)
            if (row.Identity.PackageId == target.PackageId && row.Identity.GenerationId == target.GenerationId
                && row.Identity.SourceId == target.SourceId && row.Identity.SourceRevision == target.SourceRevision
                && row.Identity.DirectoryOrdinal == target.DirectoryOrdinal && row.Identity.PathId == target.PathId)
                row.ApplyVerifiedMemo(publication.Change.Memo, publication.Change.MemoRevision);
        if (_asset is not null) Inspector = _asset.Details + "\n" + _asset.ValueStates;
        OnPropertyChanged(nameof(Summary));
    }
    public V2BrowseViewModel(IFilePickerService picker, V2BrowseCatalog? catalog = null)
    {
        _picker = picker; _catalog = catalog ?? new();
        OpenCommand = new AsyncCommand(async () => { var path = await _picker.PickV2PackageAsync(_lifetime.Token); if (path is not null) await OpenAsync(path); });
        NextCommand = new AsyncCommand(async () => { _history.Add(_after); if (_history.Count > 128) _history.RemoveAt(0); await LoadAsync(_next); }, () => _hasNext && !_busy);
        PreviousCommand = new AsyncCommand(async () => { long cursor = _history[^1]; _history.RemoveAt(_history.Count - 1); await LoadAsync(cursor); }, () => _history.Count > 0 && !_busy);
        FirstCommand = new AsyncCommand(async () => { _history.Clear(); await LoadAsync(0); }, () => _selected is not null && !_busy);
        NextMetadataCommand = new AsyncCommand(() => LoadMetadataAsync(_metadataNext), () => _hasMetadataNext && !_busy);
        FirstMetadataCommand = new AsyncCommand(() => LoadMetadataAsync(0), () => _selected is not null && !_busy);
        RemoveCommand = new AsyncCommand(RemoveAsync, () => _selected is not null);
    }
    public ObservableCollection<V2BrowseRegistration> Registrations { get; } = new();
    public AsyncCommand OpenCommand { get; }
    public AsyncCommand NextCommand { get; }
    public AsyncCommand PreviousCommand { get; }
    public AsyncCommand FirstCommand { get; }
    public AsyncCommand NextMetadataCommand { get; }
    public AsyncCommand FirstMetadataCommand { get; }
    public AsyncCommand RemoveCommand { get; }
    public V2MetadataKind[] MetadataKinds { get; } = Enum.GetValues<V2MetadataKind>();
    public V2MetadataKind MetadataKind { get => _kind; set { if (SetField(ref _kind, value)) { Metadata = Array.Empty<V2MetadataItem>(); SelectedMetadata = null; MetadataTask = LoadMetadataAsync(0); } } }
    public Task SelectionTask { get; private set; } = Task.CompletedTask;
    public Task MetadataTask { get; private set; } = Task.CompletedTask;
    public Task InspectorTask { get; private set; } = Task.CompletedTask;
    public V2BrowseRegistration? SelectedPackage
    {
        get => _selected;
        set
        {
            if (value is null || ReferenceEquals(value, _selected)) return; // detaching a workspace must not clear retained selection
            ++_openVersion;
            _navigation.Cancel(); _navigation.Dispose(); _navigation = new();
            Metadata = Array.Empty<V2MetadataItem>(); SelectedMetadata = null; _hasMetadataNext = false;
            _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary));
            _history.Clear(); SelectionTask = SelectAsync();
        }
    }
    public IReadOnlyList<V2AssetRow> Assets { get => _assets; private set => SetField(ref _assets, value); }
    public IReadOnlyList<V2MetadataItem> Metadata { get => _metadata; private set => SetField(ref _metadata, value); }
    public V2AssetRow? SelectedAsset
    {
        get => _asset;
        set
        {
            if (value is null || !Assets.Any(a => a.Identity == value.Identity)) return;
            if (!SetField(ref _asset, value)) return;
            Inspector = value.Details; InspectorTask = LoadInspectorAsync(value); QueueMemo(value);
        }
    }
    public V2MetadataItem? SelectedMetadata { get => _metadataItem; set => SetField(ref _metadataItem, value); }
    public string Inspector { get => _inspector; private set => SetField(ref _inspector, value); }
    public string MetadataStatus { get => _metadataStatus; private set => SetField(ref _metadataStatus, value); }
    public string Summary => _selected?.Summary ?? "No V2 package selected.";
    public string Status { get => _status; private set => SetField(ref _status, value); }
    public bool IsLoading { get => _busy; private set { SetField(ref _busy, value); NotifyCommands(); } }
    public async Task<V2BrowseRegistration?> OpenAsync(string path, CancellationToken token = default, bool select = true)
    {
        int opening = select ? ++_openVersion : _openVersion;
        if (select) { Status = "Validating read-only V2 package..."; IsLoading = true; }
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
            var entry = await _catalog.RegisterAsync(path, linked.Token);
            // Admission may have completed just before cancellation. Publish ownership before
            // honoring selection cancellation, so its reader/pin is never invisible to Unload.
            if (!Registrations.Contains(entry)) Registrations.Add(entry);
            linked.Token.ThrowIfCancellationRequested();
            if (!select) return entry;
            if (opening != _openVersion) return entry;
            SelectedPackage = entry; opening = _openVersion; await SelectionTask; return entry;
        }
        catch (OperationCanceledException) { if (opening == _openVersion) Status = "V2 selection cancelled; any completed admission remains listed for explicit browsing/unload."; return null; }
        catch (Exception e) { if (opening == _openVersion) Status = "V2 registration failed: " + e.Message; return null; }
        finally { if (select && opening == _openVersion) IsLoading = false; }
    }
    private async Task SelectAsync()
    {
        var selected = _selected;
        await LoadAsync(0);
        if (selected == _selected) await LoadMetadataAsync(0);
    }
    public async Task LoadAsync(long after = 0)
    {
        var retained = after == _after ? _asset?.Identity : null;
        _pages?.Cancel(); _details?.Cancel(); ++_detailVersion; _pages = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, _navigation.Token);
        var owned = _pages; int generation = ++_version; var entry = _selected;
        Assets = Array.Empty<V2AssetRow>(); _asset = null; OnPropertyChanged(nameof(SelectedAsset)); QueueMemo(null);
        Inspector = "Select an indexed asset. Metadata is read-only; eligible memos are editable below. Preview is unavailable.";
        _hasNext = false; IsLoading = true;
        try
        {
            if (entry is null) return;
            var page = await entry.AssetsAsync(after, owned.Token);
            if (generation != _version || entry != _selected) return;
            Assets = page.Rows; _after = after; _next = page.After; _hasNext = page.HasMore;
            var selection = page.Rows.FirstOrDefault(a => a.Identity == retained); if (selection is not null) SelectedAsset = selection;
            Status = $"{entry.Status} index · {Assets.Count} visible / {entry.AssetCount:N0} indexed assets · deterministic occurrence order; column sorts affect this page only.";
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { if (generation == _version) { Assets = Array.Empty<V2AssetRow>(); Metadata = Array.Empty<V2MetadataItem>(); SelectedMetadata = null; Status = "Unavailable/stale V2 metadata: " + e.Message; OnPropertyChanged(nameof(Summary)); } }
        finally { if (generation == _version) IsLoading = false; if (ReferenceEquals(_pages, owned)) _pages = null; owned.Dispose(); }
    }
    public async Task LoadMetadataAsync(long after = 0)
    {
        var entry = _selected; int version = _version; var kind = _kind;
        _metadataLoad?.Cancel(); var owned = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, _navigation.Token); _metadataLoad = owned;
        int metadataVersion = ++_metadataVersion;
        MetadataStatus = $"Loading {kind}...";
        try
        {
            if (entry is null) return;
            var page = await entry.MetadataAsync(kind, after, owned.Token);
            if (entry != _selected || version != _version || kind != _kind || metadataVersion != _metadataVersion) return;
            Metadata = page.Rows; SelectedMetadata = page.Rows.FirstOrDefault();
            _metadataNext = page.After; _hasMetadataNext = page.HasMore; NotifyCommands();
            MetadataStatus = page.Rows.Count == 0 ? $"{kind}: no records." : $"{kind}: {page.Rows.Count} records on this page.";
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { if (entry == _selected && metadataVersion == _metadataVersion) { Metadata = Array.Empty<V2MetadataItem>(); SelectedMetadata = null; _hasMetadataNext = false; NotifyCommands(); Status = "Unavailable/stale V2 metadata: " + e.Message; MetadataStatus = Status; } }
        finally { if (ReferenceEquals(_metadataLoad, owned)) _metadataLoad = null; owned.Dispose(); }
    }
    private async Task LoadInspectorAsync(V2AssetRow asset)
    {
        _details?.Cancel(); _details = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, _navigation.Token);
        var owned = _details; var entry = _selected; int version = ++_detailVersion;
        try
        {
            var evidence = entry is null ? "" : await entry.AssetEvidenceAsync(asset, owned.Token);
            if (version == _detailVersion && entry == _selected && _asset?.Identity == asset.Identity) Inspector = asset.Details + "\n" + asset.ValueStates + "\n\n" + evidence;
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { if (version == _detailVersion) Inspector = asset.Details + "\nEvidence unavailable/stale: " + e.Message; }
        finally { if (ReferenceEquals(_details, owned)) _details = null; owned.Dispose(); }
    }
    public async Task RemoveAsync()
    {
        var entry = _selected; if (entry is null) return;
        int navigation = _openVersion;
        await MemoInspector.FlushForNavigationAsync(_lifetime.Token);
        // Flush may await a transaction while the user selects another package. Unload
        // only the captured owner; never clear the newer package's rows/selection.
        if (_selected != entry || navigation != _openVersion)
        {
            Registrations.Remove(entry); await _catalog.UnloadAsync(entry); return;
        }
        _navigation.Cancel(); _pages?.Cancel(); _details?.Cancel(); ++_version; ++_detailVersion; ++_openVersion; ++_metadataVersion;
        _selected = null; _asset = null; Assets = Array.Empty<V2AssetRow>(); Metadata = Array.Empty<V2MetadataItem>();
        QueueMemo(null);
        SelectedMetadata = null; _hasNext = false; _hasMetadataNext = false; _history.Clear();
        Inspector = "Package unloaded."; Status = "V2 package unloaded; no disk data removed."; IsLoading = false;
        OnPropertyChanged(nameof(SelectedPackage)); OnPropertyChanged(nameof(SelectedAsset)); OnPropertyChanged(nameof(Summary));
        Registrations.Remove(entry); await _catalog.UnloadAsync(entry); NotifyCommands();
    }
    public async Task VerifyCurrentAsync()
    {
        var entry = _selected; int version = _version;
        if (entry is null) return;
        try { await entry.CheckAsync(_navigation.Token); }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            if (entry != _selected || version != _version) return;
            Assets = Array.Empty<V2AssetRow>(); Metadata = Array.Empty<V2MetadataItem>(); SelectedMetadata = null;
            _asset = null; OnPropertyChanged(nameof(SelectedAsset)); _hasNext = false; _hasMetadataNext = false;
            QueueMemo(null);
            Inspector = Status = MetadataStatus = "Unavailable/stale V2 metadata: " + e.Message;
            OnPropertyChanged(nameof(Summary)); NotifyCommands();
        }
    }
    private void NotifyCommands() { NextCommand.NotifyCanExecuteChanged(); PreviousCommand.NotifyCanExecuteChanged(); FirstCommand.NotifyCanExecuteChanged(); NextMetadataCommand.NotifyCanExecuteChanged(); FirstMetadataCommand.NotifyCanExecuteChanged(); RemoveCommand.NotifyCanExecuteChanged(); }
    public void Dispose() { if (_memos is not null) _memos.Published -= ApplyMemoPublication; MemoInspector.Dispose(); _lifetime.Cancel(); _navigation.Cancel(); ++_openVersion; ++_version; ++_detailVersion; _pages?.Cancel(); _details?.Cancel(); _catalog.Dispose(); _lifetime.Dispose(); }
}
