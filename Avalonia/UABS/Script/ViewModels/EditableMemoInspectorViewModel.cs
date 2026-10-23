using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.ViewModels;

public sealed class EditableMemoInspectorViewModel : ViewModelBase, IDisposable
{
    private readonly IMemoDraftCoordinator _drafts;
    private readonly IPackageEditorSessionGuard _editorGuard;
    private readonly CancellationTokenSource _lifetime = new();
    private PackageMemoResolution? _resolution;
    private PackageMemoIdentity? _identity;
    private string _memoText = string.Empty;
    private string _packageContext = string.Empty;
    private string _explanation = "Select an asset result to edit its package memo.";
    private string? _error;
    private MemoDraftSaveState _saveState = MemoDraftSaveState.Unavailable;
    private PackageMemoResolutionStatus? _status;
    private PackageMemoStorageState? _storageState;
    private bool _isApplyingSnapshot;
    private int _presentationVersion;
    private bool _disposed;
    private PackageMemoPersistenceStatus? _lastPersistenceStatus;
    private CancellationTokenSource? _revisionLoad;

    public EditableMemoInspectorViewModel(
        IMemoDraftCoordinator drafts,
        IPackageEditorSessionGuard editorGuard)
    {
        _drafts = drafts ?? throw new ArgumentNullException(nameof(drafts));
        _editorGuard = editorGuard ?? throw new ArgumentNullException(nameof(editorGuard));
        SaveNowCommand = new AsyncCommand(SaveNowAsync, () => CanSaveNow);
        RetryCommand = new AsyncCommand(RetryAsync, () => CanRetry);
        _drafts.DraftChanged += DraftsOnDraftChanged;
    }

    public AsyncCommand SaveNowCommand { get; }
    public AsyncCommand RetryCommand { get; }

    public PackageMemoIdentity? Identity => _identity;
    public PackageMemoResolutionStatus? Status => _status;
    public PackageMemoStorageState? StorageState => _storageState;
    public bool IsEditable => Identity is not null && SaveState != MemoDraftSaveState.Unavailable;
    public bool IsUnavailable => !IsEditable;
    public bool HasPackageContext => PackageContext.Length > 0;
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool CanSaveNow => IsEditable && SaveState == MemoDraftSaveState.Unsaved;
    public bool CanRetry => IsEditable && SaveState == MemoDraftSaveState.Failed;

    public string MemoText
    {
        get => _memoText;
        set
        {
            value ??= string.Empty;
            if (!SetField(ref _memoText, value) || _isApplyingSnapshot || !IsEditable || Identity is null)
                return;
            ApplySnapshot(_drafts.Edit(Identity, value));
        }
    }

    public string PackageContext
    {
        get => _packageContext;
        private set => SetField(ref _packageContext, value);
    }

    public string Explanation
    {
        get => _explanation;
        private set => SetField(ref _explanation, value);
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetField(ref _error, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public MemoDraftSaveState SaveState
    {
        get => _saveState;
        private set
        {
            if (!SetField(ref _saveState, value))
                return;
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(IsUnavailable));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(CanSaveNow));
            OnPropertyChanged(nameof(CanRetry));
            SaveNowCommand.NotifyCanExecuteChanged();
            RetryCommand.NotifyCanExecuteChanged();
        }
    }

    public string StatusText => SaveState switch
    {
        MemoDraftSaveState.Saved => "Saved",
        MemoDraftSaveState.Unsaved => "Unsaved changes",
        MemoDraftSaveState.Saving => "Saving…",
        MemoDraftSaveState.Failed when _lastPersistenceStatus is PackageMemoPersistenceStatus.RevisionConflict or PackageMemoPersistenceStatus.CommittedButSuperseded => "Conflict — draft retained",
        MemoDraftSaveState.Failed => "Save failed",
        _ => "Unavailable"
    };

    public async Task ShowAsync(PackageMemoResolution resolution)
    {
        using var phase = MemoTrace.Start("inspector-selection");
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(resolution);
        int version = ++_presentationVersion;
        _revisionLoad?.Cancel(); _revisionLoad?.Dispose();
        var revisionLoad = _revisionLoad = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _resolution = resolution;
        PresentResolutionContext(resolution);

        if (!resolution.IsAvailable || resolution.Identity is null)
        {
            ShowUnavailable(resolution.Diagnostic);
            return;
        }

        if (_editorGuard.IsPackageOpen(resolution.Identity.PackagePath!))
        {
            ShowUnavailable("Memo editing is unavailable while this package is open in Package Editor.");
            return;
        }

        ShowUnavailable("Loading the current memo revision…");
        try
        {
            MemoDraftSnapshot snapshot = await _drafts.TrackAsync(
                resolution.Identity,
                resolution.Value,
                revisionLoad.Token);
            if (version != _presentationVersion || _disposed)
                return;
            if (_editorGuard.IsPackageOpen(resolution.Identity.PackagePath!))
            {
                ShowUnavailable("Memo editing is unavailable while this package is open in Package Editor.");
                return;
            }
            if (snapshot.State == MemoDraftSaveState.Unavailable)
            {
                ShowUnavailable("The package is missing or no longer available for memo editing.");
                return;
            }

            _identity = resolution.Identity;
            OnPropertyChanged(nameof(Identity));
            Explanation = resolution.StorageState switch
            {
                PackageMemoStorageState.Missing => "No memo is currently stored for this asset.",
                PackageMemoStorageState.Empty => "This asset stores an explicit empty memo.",
                _ => "Edit the indexed package memo below."
            };
            ApplySnapshot(_drafts.GetSnapshot(resolution.Identity));
        }
        catch (OperationCanceledException) when (revisionLoad.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (version == _presentationVersion)
                ShowUnavailable($"The memo could not be prepared for editing: {exception.Message}");
        }
    }

    public Task RefreshEligibilityAsync() => _resolution is null
        ? Task.CompletedTask
        : ShowAsync(_resolution);

    public Task<MemoDraftSnapshot> FlushForNavigationAsync(CancellationToken cancellationToken = default)
    {
        PackageMemoIdentity? identity = Identity;
        return identity is null
            ? Task.FromResult(MemoDraftSnapshot.Unavailable)
            : _drafts.FlushForNavigationAsync(identity, cancellationToken);
    }

    internal Task<MemoDraftSnapshot> FlushIdentityForNavigationAsync(
        PackageMemoIdentity identity,
        CancellationToken cancellationToken = default) =>
        _drafts.FlushForNavigationAsync(identity, cancellationToken);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _presentationVersion++;
        _revisionLoad?.Cancel(); _revisionLoad?.Dispose();
        _drafts.DraftChanged -= DraftsOnDraftChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private async Task SaveNowAsync()
    {
        if (Identity is not null)
            ApplySnapshot(await _drafts.SaveNowAsync(Identity, _lifetime.Token));
    }

    private async Task RetryAsync()
    {
        if (Identity is not null)
            ApplySnapshot(await _drafts.RetryAsync(Identity, _lifetime.Token));
    }

    private void DraftsOnDraftChanged(object? sender, MemoDraftChangedEventArgs eventArgs)
    {
        if (!SameIdentity(Identity, eventArgs.Draft.Identity))
            return;
        if (Dispatcher.UIThread.CheckAccess())
            ApplySnapshot(eventArgs.Draft);
        else
            Dispatcher.UIThread.Post(() => ApplySnapshot(eventArgs.Draft));
    }

    private void ApplySnapshot(MemoDraftSnapshot snapshot)
    {
        if (_disposed || !SameIdentity(Identity, snapshot.Identity))
            return;
        // Queued notifications and awaited saves may carry an older draft revision.
        // Read the coordinator's current state at application time in either Inspector.
        snapshot = _drafts.GetSnapshot(snapshot.Identity);
        _isApplyingSnapshot = true;
        try
        {
            MemoText = snapshot.DraftMemo ?? string.Empty;
        }
        finally
        {
            _isApplyingSnapshot = false;
        }
        Error = snapshot.Error;
        _lastPersistenceStatus = snapshot.LastPersistenceStatus;
        OnPropertyChanged(nameof(StatusText));
        SaveState = snapshot.State;
    }

    private void PresentResolutionContext(PackageMemoResolution resolution)
    {
        _status = resolution.Status;
        _storageState = resolution.StorageState;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StorageState));
        PackageContext = resolution.PackageName is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(resolution.PackagePath)
                ? resolution.PackageName
                : $"{resolution.PackageName} — {resolution.PackagePath}";
        OnPropertyChanged(nameof(HasPackageContext));
    }

    private void ShowUnavailable(string explanation)
    {
        _identity = null;
        OnPropertyChanged(nameof(Identity));
        _isApplyingSnapshot = true;
        try
        {
            var retained = _drafts.GetSnapshot(_resolution?.Identity);
            MemoText = retained.Identity is not null ? retained.DraftMemo ?? string.Empty : _resolution?.Value ?? string.Empty;
        }
        finally
        {
            _isApplyingSnapshot = false;
        }
        Explanation = explanation;
        Error = null;
        SaveState = MemoDraftSaveState.Unavailable;
    }

    private static bool SameIdentity(PackageMemoIdentity? left, PackageMemoIdentity? right) =>
        PackageMemoIdentityComparer.Same(left, right);
}
