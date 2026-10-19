using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UABS.UnityAssets;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.ViewModels;

public enum AssetSortField
{
    Name,
    Type,
    ClassId,
    PathId,
    Size,
    SourceFile
}

public sealed class AssetListViewModel : ViewModelBase, IDisposable
{
    private static readonly IReadOnlyList<AssetSortField> AvailableSortFields = Enum.GetValues<AssetSortField>();
    private IReadOnlyList<AssetEntry> _allAssets = [];
    private IReadOnlyList<AssetEntry> _assets = [];
    private IAssetSession? _session;
    private AssetEntry? _selectedAsset;
    private AssetDetails? _selectedDetails;
    private CancellationTokenSource? _detailsCancellation;
    private string _nameFilter = string.Empty;
    private string _typeFilter = string.Empty;
    private string _pathIdFilter = string.Empty;
    private string? _detailsError;
    private AssetSortField _sortField = AssetSortField.Name;
    private bool _sortDescending;
    private bool _isDetailsLoading;
    private int _detailsVersion;
    private bool _disposed;
    private readonly IPackageMemoResolver _memoResolver;
    private IReadOnlyList<PackageMemoCatalogEntry> _memoCatalog = [];
    private readonly CancellationTokenSource _memoLifetime = new();

    public AssetListViewModel(
        IPackageMemoResolver? memoResolver = null,
        IMemoDraftCoordinator? memoDraftCoordinator = null,
        IPackageEditorSessionGuard? editorSessionGuard = null)
    {
        _memoResolver = memoResolver ?? new PackageMemoResolver();
        MemoInspector = new EditableMemoInspectorViewModel(
            memoDraftCoordinator ?? new UnavailableMemoDraftCoordinator(),
            editorSessionGuard ?? new PackageEditorSessionGuard());
    }

    public EditableMemoInspectorViewModel MemoInspector { get; }
    public Task MemoTransitionTask { get; private set; } = Task.CompletedTask;

    public IReadOnlyList<AssetEntry> Assets
    {
        get => _assets;
        private set => SetField(ref _assets, value);
    }

    public IReadOnlyList<AssetSortField> SortFields => AvailableSortFields;

    public string NameFilter
    {
        get => _nameFilter;
        set
        {
            if (SetField(ref _nameFilter, value ?? string.Empty))
                Rebuild();
        }
    }

    public string TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (SetField(ref _typeFilter, value ?? string.Empty))
                Rebuild();
        }
    }

    public string PathIdFilter
    {
        get => _pathIdFilter;
        set
        {
            if (SetField(ref _pathIdFilter, value ?? string.Empty))
                Rebuild();
        }
    }

    public AssetSortField SortField
    {
        get => _sortField;
        set
        {
            if (SetField(ref _sortField, value))
                Rebuild();
        }
    }

    public bool SortDescending
    {
        get => _sortDescending;
        set
        {
            if (SetField(ref _sortDescending, value))
                Rebuild();
        }
    }

    public AssetEntry? SelectedAsset
    {
        get => _selectedAsset;
        set
        {
            if (!SetField(ref _selectedAsset, value))
                return;

            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(HasNoSelection));
            DetailsLoadingTask = LoadDetailsAsync(value);
            RefreshMemo(flushPrevious: true);
        }
    }

    public bool HasSelection => SelectedAsset is not null;
    public bool HasNoSelection => SelectedAsset is null;

    public AssetDetails? SelectedDetails
    {
        get => _selectedDetails;
        private set
        {
            if (SetField(ref _selectedDetails, value))
                OnPropertyChanged(nameof(HasDetails));
        }
    }

    public bool HasDetails => SelectedDetails is not null;

    public bool IsDetailsLoading
    {
        get => _isDetailsLoading;
        private set => SetField(ref _isDetailsLoading, value);
    }

    public string? DetailsError
    {
        get => _detailsError;
        private set
        {
            if (SetField(ref _detailsError, value))
                OnPropertyChanged(nameof(HasDetailsError));
        }
    }

    public bool HasDetailsError => !string.IsNullOrWhiteSpace(DetailsError);

    public Task DetailsLoadingTask { get; private set; } = Task.CompletedTask;

    public void ReplaceSession(IAssetSession session)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CancelDetails();
        _session = session;
        _allAssets = session.Assets;
        _selectedAsset = null;
        OnPropertyChanged(nameof(SelectedAsset));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasNoSelection));
        SelectedDetails = null;
        DetailsError = null;
        IsDetailsLoading = false;
        Rebuild();
        RefreshMemo(flushPrevious: true);
    }

    public void UpdateMemoCatalog(IReadOnlyList<PackageMemoCatalogEntry> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        _memoCatalog = packages;
        RefreshMemo();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CancelDetails();
        _memoLifetime.Cancel();
        MemoInspector.Dispose();
        _memoLifetime.Dispose();
    }

    private void Rebuild()
    {
        IEnumerable<AssetEntry> query = _allAssets;
        if (!string.IsNullOrWhiteSpace(NameFilter))
            query = query.Where(asset => asset.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(TypeFilter))
            query = query.Where(asset => asset.Type.Contains(TypeFilter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(PathIdFilter))
            query = query.Where(asset => asset.PathId.ToString(CultureInfo.InvariantCulture).Contains(PathIdFilter, StringComparison.Ordinal));

        query = SortField switch
        {
            AssetSortField.Type => query.OrderBy(asset => asset.Type, StringComparer.OrdinalIgnoreCase).ThenBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase),
            AssetSortField.ClassId => query.OrderBy(asset => asset.ClassId),
            AssetSortField.PathId => query.OrderBy(asset => asset.PathId),
            AssetSortField.Size => query.OrderBy(asset => asset.ByteSize),
            AssetSortField.SourceFile => query.OrderBy(asset => asset.LogicalFile, StringComparer.OrdinalIgnoreCase).ThenBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase),
            _ => query.OrderBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
        };

        if (SortDescending)
            query = query.Reverse();

        AssetEntry[] visible = query.ToArray();
        Assets = visible;
        if (SelectedAsset is not null && !visible.Any(asset => ReferenceEquals(asset, SelectedAsset)))
            SelectedAsset = null;
    }

    private async Task LoadDetailsAsync(AssetEntry? asset)
    {
        CancelDetails();
        SelectedDetails = null;
        DetailsError = null;
        IsDetailsLoading = asset is not null && _session is not null;
        if (asset is null || _session is null)
            return;

        var cancellation = new CancellationTokenSource();
        _detailsCancellation = cancellation;
        int version = ++_detailsVersion;
        IAssetSession session = _session;

        try
        {
            AssetDetails details = await session.GetDetailsAsync(asset, cancellation.Token);
            if (!cancellation.IsCancellationRequested && version == _detailsVersion && ReferenceEquals(asset, SelectedAsset))
                SelectedDetails = details;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (version == _detailsVersion)
                DetailsError = exception.Message;
        }
        finally
        {
            if (version == _detailsVersion)
                IsDetailsLoading = false;
        }
    }

    private void CancelDetails()
    {
        _detailsVersion++;
        _detailsCancellation?.Cancel();
        _detailsCancellation?.Dispose();
        _detailsCancellation = null;
    }

    public void RefreshMemoEligibility() => RefreshMemo();

    public Task FlushActiveMemoForNavigationAsync(CancellationToken cancellationToken = default) =>
        MemoInspector.FlushForNavigationAsync(cancellationToken);

    private void RefreshMemo(bool flushPrevious = false)
    {
        PackageMemoResolution resolution = ResolveSelectedMemo();
        PackageMemoIdentity? previousIdentity = flushPrevious || !resolution.IsAvailable
            ? MemoInspector.Identity
            : null;
        MemoTransitionTask = ObserveMemoTransitionAsync(previousIdentity, resolution);
    }

    private async Task ObserveMemoTransitionAsync(PackageMemoIdentity? previousIdentity, PackageMemoResolution resolution)
    {
        try
        {
            Task flush = previousIdentity is null
                ? Task.CompletedTask
                : MemoInspector.FlushIdentityForNavigationAsync(previousIdentity, _memoLifetime.Token);
            await Task.WhenAll(flush, MemoInspector.ShowAsync(resolution)).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (_memoLifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Printer.PrintException("Assets memo transition failed.", exception);
        }
    }

    private PackageMemoResolution ResolveSelectedMemo()
    {
        AssetEntry? selected = SelectedAsset;
        if (selected is null)
        {
            return new PackageMemoResolution(
                PackageMemoResolutionStatus.Unsupported, null, null, null, null, null,
                "Select an asset to edit its indexed package memo.");
        }

        bool ambiguousLogicalFileIdentity = _allAssets.Any(asset =>
            !ReferenceEquals(asset, selected)
            && asset.PathId == selected.PathId
            && string.Equals(asset.Name, selected.Name, StringComparison.Ordinal)
            && PackagePathComparer.Equals(asset.SourcePath, selected.SourcePath)
            && !string.Equals(asset.LogicalFile, selected.LogicalFile, StringComparison.Ordinal));
        return _memoResolver.ResolveAsset(
            _memoCatalog,
            new AssetMemoLookup(
                selected.SourcePath,
                selected.Name,
                selected.PathId,
                ambiguousLogicalFileIdentity));
    }
}
