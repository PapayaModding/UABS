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

namespace UABS.ViewModels;

public enum PackageEditorItemKind
{
    Source,
    Asset
}

public sealed class PackageEditorItemViewModel
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
    public string Name => Asset?.Name ?? Source.Name;
    public string SourceName => Source.Name;
    public string SourcePath => Source.Path;
    public string CabCode => Source.CabCode;
    public long? PathId => Asset?.PathId;
    public string? Memo => Asset?.Memo;
    public string IndexDirectory => Index.RelativeDirectory;
}

public sealed class PackageEditorViewModel : ViewModelBase, IDisposable
{
    private readonly IFilePickerService _filePicker;
    private readonly IUserConfirmationService _confirmations;
    private readonly IPackageService _packageService;
    private readonly Func<string, PackageDocument, Task>? _saved;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _saveCancellation;
    private CancellationTokenSource? _importCancellation;
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
    private int _editVersion;
    private bool _disposed;

    public PackageEditorViewModel(
        IFilePickerService filePicker,
        IUserConfirmationService confirmations,
        IPackageService packageService,
        Func<string, PackageDocument, Task>? saved = null)
    {
        _filePicker = filePicker;
        _confirmations = confirmations;
        _packageService = packageService;
        _saved = saved;
        NewCommand = new AsyncCommand(() => StartNewAsync());
        ChooseDestinationCommand = new AsyncCommand(ChooseDestinationAsync, () => IsEditing && !IsOperationRunning);
        AddSourceCommand = new AsyncCommand(AddSourceAsync, () => IsEditing && !IsOperationRunning);
        AddFolderCommand = new AsyncCommand(AddFolderAsync, () => IsEditing && !IsOperationRunning);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => IsEditing && !IsOperationRunning && SelectedItem is not null);
        SaveCommand = new AsyncCommand(SaveAsync, () => IsEditing && !IsOperationRunning);
        CancelSaveCommand = new RelayCommand(CancelCurrentOperation, () => IsOperationRunning);
        DiscardCommand = new AsyncCommand(DiscardAsync, () => IsEditing && !IsOperationRunning);
    }

    public AsyncCommand NewCommand { get; }
    public AsyncCommand ChooseDestinationCommand { get; }
    public AsyncCommand AddSourceCommand { get; }
    public AsyncCommand AddFolderCommand { get; }
    public RelayCommand RemoveSelectedCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public RelayCommand CancelSaveCommand { get; }
    public AsyncCommand DiscardCommand { get; }

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
                RemoveSelectedCommand.NotifyCanExecuteChanged();
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

    public bool IsOperationRunning => IsSaving || IsImporting;

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

    public async Task SaveAsync()
    {
        if (_workingDocument is null || IsOperationRunning)
            return;

        Error = null;
        string? validation = ValidatePackageName(PackageName);
        if (validation is not null)
        {
            Error = validation;
            Status = "Correct the package information before saving.";
            return;
        }
        if (string.IsNullOrWhiteSpace(DestinationDirectory))
        {
            Error = "Choose a package destination directory before saving.";
            Status = "A destination is required.";
            return;
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
            return;
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
                return;
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
        Status = $"Saving {PackageName.Trim()}...";
        try
        {
            await _packageService.SaveAsync(snapshot, target, overwrite, progress, token);
            PackageDocument reopened = await _packageService.LoadAsync(target, cancellationToken: CancellationToken.None);

            if (editVersion == _editVersion)
            {
                _workingDocument = reopened.CloneForEditing();
                SetEditorValues(Path.GetFileName(target), parent, isNew: false, isDirty: false);
                Status = $"Saved and verified {Path.GetFileName(target)}.";
            }
            else
            {
                Status = "The saved snapshot was verified, but newer unsaved edits remain.";
            }

            if (_saved is not null)
                await _saved(target, reopened);
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
    }

    public void CancelSave() => CancelCurrentOperation();

    public void CancelCurrentOperation()
    {
        _saveCancellation?.Cancel();
        _importCancellation?.Cancel();
    }

    public async Task DiscardAsync()
    {
        await TryAbandonAsync("Discard package changes?").ConfigureAwait(true);
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
        _saveCancellation?.Dispose();
        _importCancellation?.Dispose();
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

    private void RefreshItems()
    {
        Items = _workingDocument?.Indexes.SelectMany(index => index.Bundles.SelectMany(source =>
                new[] { new PackageEditorItemViewModel(index, source, null) }
                    .Concat(source.AssetInfos.Select(asset => new PackageEditorItemViewModel(index, source, asset)))))
            .ToArray() ?? [];
    }

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
