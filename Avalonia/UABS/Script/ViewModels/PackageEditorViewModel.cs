using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UABS.Services;
using UABS.UnityAssets;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.ViewModels;

public enum PackageEditorItemKind
{
    Source,
    Asset
}

public sealed class PackageEditorItemViewModel : ViewModelBase
{
    internal PackageEditorItemViewModel(
        PackageIndex index,
        PackageBundleReference source,
        PackageAssetReference? asset)
    {
        Index = index;
        Source = source;
        Asset = asset;
    }

    internal PackageIndex Index { get; }
    internal PackageBundleReference Source { get; }
    internal PackageAssetReference? Asset { get; }
    public PackageEditorItemKind Kind => Asset is null ? PackageEditorItemKind.Source : PackageEditorItemKind.Asset;
    public bool IsSource => Kind == PackageEditorItemKind.Source;
    public string TypeLabel => IsSource ? "Source" : "Asset (type unavailable)";
    public string Name => Asset?.Name ?? Source.Name;
    public string SourceName => Source.Name;
    public string SourcePath => Source.Path;
    public string CabCode => Source.CabCode;
    public long? PathId => Asset?.PathId;
    public string? Memo => Asset?.Memo;
    public string IndexDirectory => Index.RelativeDirectory;
    public string TechnicalDetails => IsSource
        ? $"CAB: {CabCode} · Original path: {SourcePath} · Index: {IndexDirectory}"
        : $"Source: {SourceName} · CAB: {CabCode} · Path ID: {PathId} · Original path: {SourcePath} · Index: {IndexDirectory}";

    internal void NotifyMemoChanged() => OnPropertyChanged(nameof(Memo));
}

public sealed class PackageEditorViewModel : ViewModelBase, IDisposable
{
    private readonly IFilePickerService _filePicker;
    private readonly IUserConfirmationService _confirmations;
    private readonly IPackageService _packageService;
    private readonly IPackageMemoService _memoService;
    private readonly IPackageWriteCoordinator _writeCoordinator;
    private readonly IPackageEditorSessionController? _editorSessionController;
    private readonly Func<string, PackageDocument, Task>? _saved;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _saveCancellation;
    private CancellationTokenSource? _importCancellation;
    private CancellationTokenSource? _memoCancellation;
    private PackageDocument? _workingDocument;
    private IReadOnlyList<PackageEditorItemViewModel> _items = [];
    private PackageEditorItemViewModel? _selectedItem;
    private string _packageName = string.Empty;
    private string _destinationDirectory = string.Empty;
    private string _status = "No package is being edited.";
    private string? _error;
    private string _progressText = string.Empty;
    private bool _isEditing;
    private bool _isNew;
    private bool _isDirty;
    private bool _isSaving;
    private bool _isImporting;
    private bool _includeSubfolders;
    private bool _isApplyingMemos;
    private bool _loadingMemo;
    private string _memoText = string.Empty;
    private int _editVersion;
    private bool _disposed;

    public PackageEditorViewModel(
        IFilePickerService filePicker,
        IUserConfirmationService confirmations,
        IPackageService packageService,
        Func<string, PackageDocument, Task>? saved = null,
        IPackageMemoService? memoService = null,
        IPackageWriteCoordinator? writeCoordinator = null,
        IPackageEditorSessionController? editorSessionController = null)
    {
        _filePicker = filePicker;
        _confirmations = confirmations;
        _packageService = packageService;
        _memoService = memoService ?? new PackageMemoService();
        _writeCoordinator = writeCoordinator ?? new PackageWriteCoordinator();
        _editorSessionController = editorSessionController;
        _saved = saved;
        NewCommand = new AsyncCommand(() => StartNewAsync());
        ChooseDestinationCommand = new AsyncCommand(ChooseDestinationAsync, () => IsEditing && !IsOperationRunning);
        AddSourceCommand = new AsyncCommand(AddSourceAsync, () => IsEditing && !IsOperationRunning);
        AddFolderCommand = new AsyncCommand(AddFolderAsync, () => IsEditing && !IsOperationRunning);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => IsEditing && !IsOperationRunning && SelectedItem is not null);
        SaveCommand = new AsyncCommand(SaveAsync, () => IsEditing && !IsOperationRunning);
        CancelSaveCommand = new RelayCommand(CancelCurrentOperation, () => IsOperationRunning);
        DiscardCommand = new AsyncCommand(DiscardAsync, () => IsEditing && !IsOperationRunning);
        CloseEditorCommand = new AsyncCommand(CloseEditorAsync, () => IsEditing && !IsOperationRunning);
        ClearMemoCommand = new RelayCommand(ClearSelectedMemo, () => IsMemoEditable && !IsOperationRunning);
    }

    public AsyncCommand NewCommand { get; }
    public AsyncCommand ChooseDestinationCommand { get; }
    public AsyncCommand AddSourceCommand { get; }
    public AsyncCommand AddFolderCommand { get; }
    public RelayCommand RemoveSelectedCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public RelayCommand CancelSaveCommand { get; }
    public AsyncCommand DiscardCommand { get; }
    public AsyncCommand CloseEditorCommand { get; }
    public RelayCommand ClearMemoCommand { get; }

    public IReadOnlyList<PackageEditorItemViewModel> Items
    {
        get => _items;
        private set
        {
            if (SetField(ref _items, value))
            {
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(EmptyStateText));
                OnPropertyChanged(nameof(EditorSourceCount));
                OnPropertyChanged(nameof(EditorAssetReferenceCount));
                OnPropertyChanged(nameof(EditorContentsSummary));
            }
        }
    }

    public PackageEditorItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetField(ref _selectedItem, value))
            {
                RemoveSelectedCommand.NotifyCanExecuteChanged();
                LoadSelectedMemo();
                OnPropertyChanged(nameof(IsMemoEditable));
                OnPropertyChanged(nameof(MemoTargetDescription));
                OnPropertyChanged(nameof(MemoStorageDescription));
                ClearMemoCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string PackageName
    {
        get => _packageName;
        set
        {
            if (SetField(ref _packageName, value ?? string.Empty))
                MarkDirty();
        }
    }

    public string DestinationDirectory
    {
        get => _destinationDirectory;
        set
        {
            if (SetField(ref _destinationDirectory, value ?? string.Empty))
                MarkDirty();
        }
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
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

    public string ProgressText
    {
        get => _progressText;
        private set => SetField(ref _progressText, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetField(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(IsEditorClosed));
                NotifyCommands();
            }
        }
    }

    public bool IsEditorClosed => !IsEditing;
    public bool IsNew { get => _isNew; private set => SetField(ref _isNew, value); }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetField(ref _isDirty, value))
            {
                OnPropertyChanged(nameof(UnsavedIndicator));
                OnPropertyChanged(nameof(EditorContentsSummary));
            }
        }
    }

    public bool IncludeSubfolders
    {
        get => _includeSubfolders;
        set => SetField(ref _includeSubfolders, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetField(ref _isSaving, value))
            {
                OnPropertyChanged(nameof(IsOperationRunning));
                NotifyCommands();
            }
        }
    }

    public bool IsImporting
    {
        get => _isImporting;
        private set
        {
            if (SetField(ref _isImporting, value))
            {
                OnPropertyChanged(nameof(IsOperationRunning));
                NotifyCommands();
            }
        }
    }

    public bool IsApplyingMemos
    {
        get => _isApplyingMemos;
        private set
        {
            if (SetField(ref _isApplyingMemos, value))
            {
                OnPropertyChanged(nameof(IsOperationRunning));
                NotifyCommands();
            }
        }
    }

    public bool IsOperationRunning => IsSaving || IsImporting || IsApplyingMemos;

    public string MemoText
    {
        get => _memoText;
        set
        {
            value ??= string.Empty;
            if (!SetField(ref _memoText, value) || _loadingMemo || !IsMemoEditable)
                return;

            SetSelectedMemo(value);
        }
    }

    public bool IsMemoEditable => SelectedItem?.Asset is not null;
    public string MemoTargetDescription => SelectedItem switch
    {
        null => "Select an asset row to view or edit its memo.",
        { Asset: null } => "The legacy format has no package, directory, or source-level memo field.",
        var item => $"{item.Name} — {item.SourceName} — Path ID {item.PathId}"
    };
    public string MemoStorageDescription => SelectedItem?.Asset?.Memo switch
    {
        null when SelectedItem?.Asset is not null => "Absent: no Memo field is stored. Typing or Clear writes a local asset memo.",
        "" => "Empty: an explicit empty Memo value is stored on this asset.",
        not null => "Local: this Memo value is stored directly on the asset record.",
        _ => "Memo editing is available only for asset rows."
    };

    public bool HasItems => Items.Count > 0;
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public int EditorSourceCount => Items.Count(item => item.Kind == PackageEditorItemKind.Source);
    public int EditorAssetReferenceCount => Items.Count(item => item.Kind == PackageEditorItemKind.Asset);
    public string UnsavedIndicator => IsDirty ? "Unsaved changes" : "Saved";
    public string EditorContentsSummary =>
        $"{EditorSourceCount:N0} source(s), {EditorAssetReferenceCount:N0} asset reference(s) in editor"
        + (IsDirty ? " — not saved" : " — saved");
    public string EmptyStateText => IsEditing && !HasItems
        ? "This package is empty. Add Unity assets from a file or folder, or add the selected asset from the Assets workspace."
        : string.Empty;

    public async Task<bool> StartNewAsync(string? suggestedDestination = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!await ConfirmAbandonAsync("Start a new package?").ConfigureAwait(true))
            return false;

        _workingDocument = new PackageDocument([new PackageIndex(string.Empty)]);
        _editorSessionController?.SetActivePackage(null);
        SetEditorValues(string.Empty, suggestedDestination ?? string.Empty, isNew: true, isDirty: true);
        Status = "New empty package. Enter a name, choose a destination, and optionally add assets.";
        return true;
    }

    public async Task<bool> TryAbandonAsync(string title)
    {
        if (!await ConfirmAbandonAsync(title).ConfigureAwait(true))
            return false;
        if (IsEditing)
            CloseEditor();
        return true;
    }

    public async Task<bool> StartEditAsync(string packageName, PackageDocument package)
    {
        ArgumentNullException.ThrowIfNull(package);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!await ConfirmAbandonAsync("Edit another package?").ConfigureAwait(true))
            return false;

        _workingDocument = package.CloneForEditing();
        _editorSessionController?.SetActivePackage(package.PackagePath);
        string destination = package.PackagePath is null ? string.Empty : Path.GetDirectoryName(package.PackagePath) ?? string.Empty;
        SetEditorValues(packageName, destination, isNew: false, isDirty: false);
        Status = $"Editing {packageName}. Changes are isolated until Save completes and the package is reopened.";
        return true;
    }

    public async Task<bool> AddSelectedAssetAsync(AssetEntry asset, string? suggestedDestination = null)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsOperationRunning)
            return false;
        if (asset.ClassId is not (28 or 213))
        {
            Error = "The legacy package format stores only Texture2D and Sprite asset references.";
            return false;
        }

        if (!IsEditing && !await StartNewAsync(suggestedDestination).ConfigureAwait(true))
            return false;

        int version = _editVersion;
        Error = null;
        Status = $"Reading {Path.GetFileName(asset.SourcePath)}...";
        try
        {
            PackageBundleReference reference = await _packageService.CreateReferenceAsync(
                asset.SourcePath,
                [new PackageAssetSelection(asset.Name, asset.PathId, asset.LogicalFile)],
                _lifetime.Token);
            if (version != _editVersion)
                return false;

            bool changed = MergeReference(reference);
            Status = changed
                ? $"Added {asset.Name} from {reference.Name}."
                : $"{asset.Name} is already present from this source.";
            return changed;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception) when (exception is PackageException or IOException or UnauthorizedAccessException)
        {
            Error = exception.Message;
            Status = "The selected asset could not be added.";
            return false;
        }
    }

    public async Task ChooseDestinationAsync()
    {
        try
        {
            string? selected = await _filePicker.PickPackageDestinationAsync(_lifetime.Token);
            if (!string.IsNullOrWhiteSpace(selected))
                DestinationDirectory = Path.GetFullPath(selected);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Error = $"The package destination picker could not be opened: {exception.Message}";
        }
    }

    public async Task AddSourceAsync()
    {
        if (!IsEditing || IsOperationRunning)
            return;

        try
        {
            string? path = await _filePicker.PickUnityFileAsync(_lifetime.Token);
            if (string.IsNullOrWhiteSpace(path))
                return;

            int version = _editVersion;
            Error = null;
            Status = $"Reading {Path.GetFileName(path)}...";
            PackageBundleReference reference = await _packageService.CreateReferenceAsync(path, cancellationToken: _lifetime.Token);
            if (version != _editVersion)
                return;
            bool changed = MergeReference(reference);
            Status = changed
                ? $"Added {reference.Name} with {reference.AssetInfos.Count:N0} supported asset reference(s)."
                : $"All supported assets from {reference.Name} are already present.";
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is PackageException or IOException or UnauthorizedAccessException)
        {
            Error = exception.Message;
            Status = "The Unity source file could not be added.";
        }
    }

    public async Task AddFolderAsync()
    {
        if (!IsEditing || IsOperationRunning)
            return;

        string? folder;
        try
        {
            folder = await _filePicker.PickUnityFolderAsync(_lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or ArgumentException
                                           or NotSupportedException)
        {
            Error = $"The asset source folder picker could not be opened: {exception.Message}";
            Status = "The asset source folder could not be selected.";
            return;
        }

        if (string.IsNullOrWhiteSpace(folder))
            return;

        int version = _editVersion;
        _importCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationToken token = _importCancellation.Token;
        var progress = new Progress<PackageProgress>(item =>
        {
            ProgressText = item.Total == 0 && item.Phase == PackageProgressPhase.Scanning
                ? $"Scanning: {item.Completed:N0} file(s) discovered"
                : item.Total == 0
                    ? item.Phase.ToString()
                : $"{item.Phase}: {item.Completed:N0}/{item.Total:N0}";
        });

        Error = null;
        IsImporting = true;
        PackageFolderSearchMode searchMode = IncludeSubfolders
            ? PackageFolderSearchMode.IncludeSubfolders
            : PackageFolderSearchMode.CurrentFolderOnly;
        string scope = IncludeSubfolders ? "including subfolders" : "current folder only";
        Status = $"Scanning {Path.GetFileName(Path.TrimEndingDirectorySeparator(folder))} ({scope})...";
        try
        {
            PackageFolderImportResult result = await _packageService.CreateReferencesFromFolderAsync(
                folder,
                searchMode,
                progress,
                token);
            token.ThrowIfCancellationRequested();
            if (version != _editVersion)
                return;

            PackageDocument candidate = _workingDocument!.CloneForEditing();
            int sourcesAdded = 0;
            int assetReferencesAdded = 0;
            int duplicateSources = 0;
            int duplicateAssetReferences = 0;
            foreach (PackageFolderImportReference imported in result.GetMappedReferences())
            {
                PackageMergeResult merge = MergeReferenceDetailed(
                    candidate,
                    imported.Reference,
                    imported.RelativeDirectory);
                sourcesAdded += merge.SourceAdded ? 1 : 0;
                duplicateSources += merge.SourceAlreadyPresent ? 1 : 0;
                assetReferencesAdded += merge.AssetReferencesAdded;
                duplicateAssetReferences += merge.DuplicateAssetReferences;
            }

            if (sourcesAdded > 0 || assetReferencesAdded > 0)
            {
                _workingDocument = candidate;
                RefreshItems();
                MarkDirty();
            }

            Status = $"Folder import complete ({scope}): {result.ScannedFileCount:N0} file(s) scanned; "
                + $"{result.SupportedFileCount:N0} supported, {result.ImportedFileCount:N0} imported; "
                + $"{sourcesAdded:N0} source(s) and {assetReferencesAdded:N0} asset reference(s) added; "
                + $"{duplicateSources:N0} existing source(s) and {duplicateAssetReferences:N0} duplicate asset reference(s) skipped; "
                + $"{result.UnsupportedFileCount:N0} unsupported, {result.FailedFileCount:N0} file failure(s), "
                + $"{result.InaccessibleDirectoryCount:N0} directory failure(s), {result.SkippedLinkCount:N0} link(s) skipped, "
                + $"{result.SkippedSubdirectoryCount:N0} subdirector(ies) outside the selected scope."
                + (sourcesAdded > 0 || assetReferencesAdded > 0
                    ? " References were added to the editor but are not in the package on disk until Save completes."
                    : string.Empty);
            if (result.Failures.Count > 0)
            {
                PackageFolderImportFailure first = result.Failures[0];
                Error = $"{result.Failures.Count:N0} import path(s) failed. First failure: "
                    + $"{Path.GetFileName(first.Path)} — {first.Message}";
            }
            else if (assetReferencesAdded == 0)
            {
                Error = DescribeZeroAssetResult(result, sourcesAdded, duplicateSources, duplicateAssetReferences);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Status = "Folder import cancelled; no folder results were applied.";
        }
        catch (Exception exception) when (exception is PackageException
                                           or IOException
                                           or UnauthorizedAccessException
                                           or SecurityException
                                           or ArgumentException
                                           or NotSupportedException)
        {
            Error = exception.Message;
            Status = "Folder import failed; no folder results were applied.";
        }
        finally
        {
            _importCancellation?.Dispose();
            _importCancellation = null;
            IsImporting = false;
        }
    }

    public void RemoveSelected()
    {
        if (_workingDocument is null || SelectedItem is null || IsOperationRunning)
            return;

        PackageEditorItemViewModel selected = SelectedItem;
        if (selected.Asset is null)
            selected.Index.Bundles.Remove(selected.Source);
        else
            selected.Source.AssetInfos.Remove(selected.Asset);

        SelectedItem = null;
        RefreshItems();
        MarkDirty();
        Status = $"Removed {selected.Name}. Save to persist this change.";
    }

    public async Task SaveAsync() => await SaveCoreAsync().ConfigureAwait(true);

    private async Task<bool> SaveCoreAsync()
    {
        if (_workingDocument is null || IsOperationRunning)
            return false;

        Error = null;
        string? validation = ValidatePackageName(PackageName);
        if (validation is not null)
        {
            Error = validation;
            Status = "Correct the package information before saving.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(DestinationDirectory))
        {
            Error = "Choose a package destination directory before saving.";
            Status = "A destination is required.";
            return false;
        }

        string parent;
        string target;
        try
        {
            parent = Path.GetFullPath(DestinationDirectory);
            target = Path.GetFullPath(Path.Combine(parent, PackageName.Trim()));
            string prefix = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The package name escapes the selected destination.");
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Error = $"The package destination is invalid: {exception.Message}";
            Status = "Correct the package destination before saving.";
            return false;
        }

        bool overwrite = Directory.Exists(target);
        if (overwrite)
        {
            bool confirmed = await _confirmations.ConfirmAsync(
                "Replace existing package?",
                $"A package directory already exists at:\n{target}\n\nReplace it using the staged, rollback-safe writer?",
                "Replace Package",
                "Cancel",
                _lifetime.Token);
            if (!confirmed)
            {
                Status = "Save cancelled; no files were changed.";
                return false;
            }
        }

        PackageDocument snapshot = _workingDocument.CloneForEditing();
        int editVersion = _editVersion;
        _saveCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationToken token = _saveCancellation.Token;
        var progress = new Progress<PackageProgress>(item =>
        {
            ProgressText = item.Total == 0
                ? item.Phase.ToString()
                : $"{item.Phase}: {item.Completed:N0}/{item.Total:N0}";
        });

        IsSaving = true;
        _editorSessionController?.SetActivePackage(target);
        Status = $"Saving {PackageName.Trim()}...";
        bool savedCurrentVersion = false;
        try
        {
            await _writeCoordinator.ExecutePackageWriteAsync(
                target,
                async coordinatedToken =>
                {
                    int? cleanVersion = null;
                    await _packageService.SaveAsync(snapshot, target, overwrite, progress, coordinatedToken);
                    PackageDocument reopened = await _packageService.LoadAsync(
                        target,
                        cancellationToken: CancellationToken.None);

                    if (editVersion == _editVersion)
                    {
                        _workingDocument = reopened.CloneForEditing();
                        SetEditorValues(Path.GetFileName(target), parent, isNew: false, isDirty: false);
                        cleanVersion = _editVersion;
                        Status = $"Saved and verified {Path.GetFileName(target)}.";
                    }
                    else
                    {
                        Status = "The saved snapshot was verified, but newer unsaved edits remain.";
                    }

                    if (_saved is not null)
                        await _saved(target, reopened);

                    savedCurrentVersion = cleanVersion == _editVersion && !IsDirty;
                },
                token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Status = "Save cancelled; unsaved changes are retained.";
        }
        catch (Exception exception) when (exception is PackageException
                                           or IOException
                                           or UnauthorizedAccessException
                                           or ArgumentException
                                           or NotSupportedException)
        {
            Error = exception.Message;
            Status = "Save failed; unsaved changes are retained.";
        }
        finally
        {
            _saveCancellation?.Dispose();
            _saveCancellation = null;
            IsSaving = false;
        }

        return savedCurrentVersion;
    }

    public void CancelSave() => CancelCurrentOperation();

    public void CancelCurrentOperation()
    {
        _saveCancellation?.Cancel();
        _importCancellation?.Cancel();
        _memoCancellation?.Cancel();
    }

    public async Task ApplyMemoInheritanceAsync(
        string sourcePackageName,
        PackageDocument source,
        PackageMemoInheritanceMode mode)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_workingDocument is null || IsOperationRunning)
            return;

        int version = _editVersion;
        PackageMemoIdentity? selection = CreateMemoIdentity(SelectedItem);
        PackageDocument sourceSnapshot = source.CloneForEditing();
        PackageDocument targetSnapshot = _workingDocument.CloneForEditing();
        _memoCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationToken token = _memoCancellation.Token;
        Error = null;
        IsApplyingMemos = true;
        Status = $"Applying {mode} memo inheritance from {sourcePackageName}...";
        try
        {
            PackageMemoInheritanceResult result = await Task.Run(
                () => _memoService.Inherit(sourceSnapshot, targetSnapshot, mode, token),
                token);
            token.ThrowIfCancellationRequested();
            if (version != _editVersion)
                return;

            if (result.ChangedCount > 0)
            {
                _workingDocument = result.Document;
                RefreshItems(selection);
                MarkDirty();
            }

            Status = $"Memo inheritance complete: {result.ChangedCount:N0} changed, "
                + $"{result.UnchangedCount:N0} already equal, {result.SkippedByModeCount:N0} skipped by {mode}, "
                + $"{result.MissingMatchCount:N0} unmatched, {result.AmbiguousMatchCount:N0} ambiguous."
                + (result.ChangedCount > 0 ? " Save to persist these memo changes." : string.Empty);
            if (result.Issues.Count > 0)
                Error = $"{result.Issues.Count:N0} inheritance conflict(s) were left unchanged. First: {result.Issues[0].Diagnostic}";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Status = "Memo inheritance cancelled; no inheritance results were applied.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            Error = exception.Message;
            Status = "Memo inheritance failed; no inheritance results were applied.";
        }
        finally
        {
            _memoCancellation?.Dispose();
            _memoCancellation = null;
            IsApplyingMemos = false;
        }
    }

    public async Task DiscardAsync()
    {
        await TryAbandonAsync("Discard package changes?").ConfigureAwait(true);
    }

    public async Task CloseEditorAsync()
    {
        if (!IsEditing || IsOperationRunning)
            return;

        if (!IsDirty)
        {
            CloseEditor();
            return;
        }

        UnsavedChangesChoice choice = await _confirmations.ConfirmSaveDiscardCancelAsync(
            "Close Package Editor?",
            "This package has unsaved changes. Save them before closing, discard them, or keep editing?",
            _lifetime.Token).ConfigureAwait(true);

        switch (choice)
        {
            case UnsavedChangesChoice.Save:
                if (await SaveCoreAsync().ConfigureAwait(true))
                    CloseEditor();
                break;
            case UnsavedChangesChoice.Discard:
                CloseEditor();
                break;
            case UnsavedChangesChoice.Cancel:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(choice), choice, null);
        }
    }

    public Task<bool> ConfirmCloseAsync() => ConfirmAbandonAsync("Close UABS with unsaved package changes?");

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _lifetime.Cancel();
        _saveCancellation?.Cancel();
        _importCancellation?.Cancel();
        _memoCancellation?.Cancel();
        _saveCancellation?.Dispose();
        _importCancellation?.Dispose();
        _memoCancellation?.Dispose();
        _editorSessionController?.SetActivePackage(null);
        _lifetime.Dispose();
    }

    private async Task<bool> ConfirmAbandonAsync(string title)
    {
        if (!IsDirty)
            return true;
        return await _confirmations.ConfirmAsync(
            title,
            "This package has unsaved changes. Discard them?",
            "Discard Changes",
            "Keep Editing",
            _lifetime.Token);
    }

    private bool MergeReference(PackageBundleReference incoming)
    {
        if (_workingDocument is null)
            return false;

        bool changed = MergeReference(_workingDocument, incoming);
        if (changed)
        {
            RefreshItems();
            MarkDirty();
        }
        return changed;
    }

    private static bool MergeReference(PackageDocument document, PackageBundleReference incoming)
        => MergeReferenceDetailed(document, incoming, string.Empty).Changed;

    private static PackageMergeResult MergeReferenceDetailed(
        PackageDocument document,
        PackageBundleReference incoming,
        string relativeDirectory)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        PackageBundleReference? existing = document.Bundles.FirstOrDefault(bundle =>
            PackagePathComparer.Equals(bundle.Path, incoming.Path));
        if (existing is null)
        {
            document.AddReference(relativeDirectory, incoming);
            return new PackageMergeResult(
                SourceAdded: true,
                SourceAlreadyPresent: false,
                AssetReferencesAdded: incoming.AssetInfos.Count,
                DuplicateAssetReferences: 0);
        }

        int added = 0;
        int duplicates = 0;
        foreach (PackageAssetReference asset in incoming.AssetInfos)
        {
            if (existing.AssetInfos.Any(current => current.PathId == asset.PathId))
            {
                duplicates++;
                continue;
            }

            existing.AssetInfos.Add(asset);
            added++;
        }

        return new PackageMergeResult(false, true, added, duplicates);
    }

    private static string DescribeZeroAssetResult(
        PackageFolderImportResult result,
        int sourcesAdded,
        int duplicateSources,
        int duplicateAssetReferences)
    {
        if (result.ScannedFileCount == 0)
            return result.InaccessibleDirectoryCount > 0
                ? "No files were scanned because one or more directories could not be accessed."
                : result.SkippedSubdirectoryCount > 0
                    ? $"No files were found in the current folder. {result.SkippedSubdirectoryCount:N0} subdirector(ies) were not scanned; enable Include subfolders and import again."
                : "No files were found in the selected import scope.";
        if (result.SupportedFileCount == 0)
            return "No supported Unity bundle or serialized asset files were found by header inspection.";
        if (result.ImportedFileCount == 0)
            return "Supported Unity files were detected, but asset enumeration failed for every supported file.";
        if (sourcesAdded == 0 && (duplicateSources > 0 || duplicateAssetReferences > 0))
            return "All discovered source and asset references were already present in the editable package.";
        return "Supported Unity files were imported, but zero Texture2D or Sprite asset references were found.";
    }

    private readonly record struct PackageMergeResult(
        bool SourceAdded,
        bool SourceAlreadyPresent,
        int AssetReferencesAdded,
        int DuplicateAssetReferences)
    {
        public bool Changed => SourceAdded || AssetReferencesAdded > 0;
    }

    private void SetEditorValues(string packageName, string destination, bool isNew, bool isDirty)
    {
        _packageName = packageName;
        _destinationDirectory = destination;
        OnPropertyChanged(nameof(PackageName));
        OnPropertyChanged(nameof(DestinationDirectory));
        IsNew = isNew;
        IsEditing = true;
        IsDirty = isDirty;
        Error = null;
        ProgressText = string.Empty;
        SelectedItem = null;
        _editVersion++;
        RefreshItems();
    }

    private void CloseEditor()
    {
        _editorSessionController?.SetActivePackage(null);
        _workingDocument = null;
        _packageName = string.Empty;
        _destinationDirectory = string.Empty;
        OnPropertyChanged(nameof(PackageName));
        OnPropertyChanged(nameof(DestinationDirectory));
        Items = [];
        SelectedItem = null;
        IsNew = false;
        IsDirty = false;
        IsEditing = false;
        Error = null;
        ProgressText = string.Empty;
        Status = "No package is being edited.";
        _editVersion++;
    }

    private void RefreshItems(PackageMemoIdentity? selection = null)
    {
        Items = _workingDocument?.Indexes.SelectMany(index => index.Bundles.SelectMany(source =>
                new[] { new PackageEditorItemViewModel(index, source, null) }
                    .Concat(source.AssetInfos.Select(asset => new PackageEditorItemViewModel(index, source, asset)))))
            .ToArray() ?? [];
        if (selection is not null)
        {
            PackageEditorItemViewModel[] matches = Items.Where(item => item.Asset is not null
                && string.Equals(item.IndexDirectory, selection.IndexDirectory, StringComparison.OrdinalIgnoreCase)
                && PackagePathComparer.Equals(item.SourcePath, selection.SourcePath)
                && item.Asset.PathId == selection.PathId
                && string.Equals(item.Asset.Name, selection.AssetName, StringComparison.Ordinal))
                .Take(2)
                .ToArray();
            SelectedItem = matches.Length == 1 ? matches[0] : null;
        }
    }

    private void LoadSelectedMemo()
    {
        _loadingMemo = true;
        try
        {
            MemoText = SelectedItem?.Asset?.Memo ?? string.Empty;
        }
        finally
        {
            _loadingMemo = false;
        }
    }

    private void ClearSelectedMemo()
    {
        if (!IsMemoEditable || IsOperationRunning)
            return;
        SetSelectedMemo(string.Empty);
        _loadingMemo = true;
        try
        {
            if (!SetField(ref _memoText, string.Empty, nameof(MemoText)))
                OnPropertyChanged(nameof(MemoText));
        }
        finally
        {
            _loadingMemo = false;
        }
    }

    private void SetSelectedMemo(string value)
    {
        if (_workingDocument is null || SelectedItem?.Asset is null)
            return;

        PackageMemoIdentity identity = CreateMemoIdentity(SelectedItem)!;
        try
        {
            if (!_memoService.SetLocalMemo(_workingDocument, identity, value))
                return;
            SelectedItem.NotifyMemoChanged();
            OnPropertyChanged(nameof(MemoStorageDescription));
            MarkDirty();
            Status = "Memo changed in the editor. Save the package to persist it.";
            Error = null;
        }
        catch (InvalidOperationException exception)
        {
            Error = exception.Message;
            LoadSelectedMemo();
        }
    }

    private PackageMemoIdentity? CreateMemoIdentity(PackageEditorItemViewModel? item) => item?.Asset is null
        ? null
        : new PackageMemoIdentity(
            _workingDocument?.PackagePath,
            item.IndexDirectory,
            item.SourcePath,
            item.Asset.Name,
            item.Asset.PathId);

    private void MarkDirty()
    {
        if (!IsEditing)
            return;
        IsDirty = true;
        _editVersion++;
    }

    private void NotifyCommands()
    {
        ChooseDestinationCommand.NotifyCanExecuteChanged();
        AddSourceCommand.NotifyCanExecuteChanged();
        AddFolderCommand.NotifyCanExecuteChanged();
        RemoveSelectedCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        CancelSaveCommand.NotifyCanExecuteChanged();
        DiscardCommand.NotifyCanExecuteChanged();
        CloseEditorCommand.NotifyCanExecuteChanged();
        ClearMemoCommand.NotifyCanExecuteChanged();
    }

    private static string? ValidatePackageName(string value)
    {
        string name = value.Trim();
        if (name.Length == 0)
            return "Enter a package name.";
        if (name is "." or "..")
            return "The package name cannot be '.' or '..'.";
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || name.Contains(Path.DirectorySeparatorChar)
            || name.Contains(Path.AltDirectorySeparatorChar))
            return "The package name contains characters that are not valid in a directory name.";
        return null;
    }

}
