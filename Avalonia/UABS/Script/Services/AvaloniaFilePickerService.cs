using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace UABS.Services;

// [UNITY-DEPENDENCY:REPLACEMENT-REQUIRED]
// Finding: UE-006
// Legacy source: Assets/Script/Wrapper/FileBrowser/StandaloneFileBrowserWrapper.cs, OpenFilePanel/OpenFolderPanel
// Unity API/plugin: Unity StandaloneFileBrowser (SFB) plugin
// Purpose: Present native file and folder selection dialogs to Unity UI workflows.
// Migration status: REPLACED
// Replacement: Avalonia StorageProvider behind IFilePickerService; file/folder cancellation is covered by Milestone 2 behavior tests.
public sealed class AvaloniaFilePickerService(TopLevel owner) : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickUnityFilesAsync(CancellationToken cancellationToken = default)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Add Unity source files", AllowMultiple = true, FileTypeFilter = [FilePickerFileTypes.All] });
        cancellationToken.ThrowIfCancellationRequested();
        if (files.Any(f => f.TryGetLocalPath() is null)) throw new System.IO.IOException("Only local Unity source files are supported.");
        return files.Select(f => f.TryGetLocalPath()!).ToArray();
    }
    public async Task<string?> PickV2PackageAsync(CancellationToken cancellationToken = default)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Open read-only UABS V2 package", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("UABS V2 SQLite") { Patterns = ["*.uabs.sqlite"] }] });
        cancellationToken.ThrowIfCancellationRequested(); return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }
    public async Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IStorageFile> files = await owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Add Unity assets from any file",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.All]
            });

        cancellationToken.ThrowIfCancellationRequested();
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
        => await PickFolderAsync("Choose folder", cancellationToken);

    public async Task<string?> PickUnityFolderAsync(CancellationToken cancellationToken = default)
        => await PickFolderAsync("Add Unity assets from folder", cancellationToken);

    public async Task<string?> PickPackageCatalogAsync(CancellationToken cancellationToken = default)
        => await PickFolderAsync("Open package catalog root or package directory", cancellationToken);

    public async Task<string?> PickPackageDestinationAsync(CancellationToken cancellationToken = default)
        => await PickFolderAsync("Choose package destination", cancellationToken);

    private async Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken)
    {
        IReadOnlyList<IStorageFolder> folders = await owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

        cancellationToken.ThrowIfCancellationRequested();
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }
}
