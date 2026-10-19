using System;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.ViewModels;

public sealed class ReadOnlyMemoInspectorViewModel : ViewModelBase
{
    private PackageMemoResolutionStatus? _status;
    private PackageMemoStorageState? _storageState;
    private string _stateText = "Select an asset to inspect its package memo.";
    private string _memoText = string.Empty;
    private string _packageContext = string.Empty;
    private string _diagnostic = string.Empty;
    private bool _hasPackageContext;
    private bool _hasMemoValue;

    public PackageMemoResolutionStatus? Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public PackageMemoStorageState? StorageState
    {
        get => _storageState;
        private set => SetField(ref _storageState, value);
    }

    public string StateText
    {
        get => _stateText;
        private set => SetField(ref _stateText, value);
    }

    public string MemoText
    {
        get => _memoText;
        private set => SetField(ref _memoText, value);
    }

    public string PackageContext
    {
        get => _packageContext;
        private set => SetField(ref _packageContext, value);
    }

    public string Diagnostic
    {
        get => _diagnostic;
        private set => SetField(ref _diagnostic, value);
    }

    public bool HasPackageContext
    {
        get => _hasPackageContext;
        private set => SetField(ref _hasPackageContext, value);
    }

    public bool HasMemoValue
    {
        get => _hasMemoValue;
        private set => SetField(ref _hasMemoValue, value);
    }

    public void ShowNoSelection(string message)
    {
        Status = null;
        StorageState = null;
        StateText = message;
        MemoText = string.Empty;
        PackageContext = string.Empty;
        Diagnostic = string.Empty;
        HasPackageContext = false;
        HasMemoValue = false;
    }

    public void Show(PackageMemoResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        Status = resolution.Status;
        StorageState = resolution.StorageState;
        PackageContext = resolution.PackageName is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(resolution.PackagePath)
                ? resolution.PackageName
                : $"{resolution.PackageName} — {resolution.PackagePath}";
        HasPackageContext = PackageContext.Length > 0;
        Diagnostic = resolution.Diagnostic;
        HasMemoValue = resolution.IsAvailable
                           && resolution.StorageState == PackageMemoStorageState.Value;
        MemoText = HasMemoValue ? resolution.Value ?? string.Empty : string.Empty;
        StateText = resolution.Status switch
        {
            PackageMemoResolutionStatus.Available when resolution.StorageState == PackageMemoStorageState.Missing =>
                "No memo is stored for this asset.",
            PackageMemoResolutionStatus.Available when resolution.StorageState == PackageMemoStorageState.Empty =>
                "This asset stores an explicit empty memo.",
            PackageMemoResolutionStatus.Available => "Stored package memo",
            PackageMemoResolutionStatus.NoMatch => "No matching package asset was found.",
            PackageMemoResolutionStatus.Ambiguous => "Memo identity is ambiguous.",
            PackageMemoResolutionStatus.Unsupported => "Memo lookup is unavailable for this selection.",
            _ => "Memo identity is missing or stale."
        };
    }
}
