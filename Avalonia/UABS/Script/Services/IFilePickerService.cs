using System.Threading;
using System.Threading.Tasks;

namespace UABS.Services;

public interface IFilePickerService
{
    async Task<System.Collections.Generic.IReadOnlyList<string>> PickUnityFilesAsync(CancellationToken cancellationToken = default)
    {
        var path = await PickUnityFileAsync(cancellationToken);
        return path is null ? System.Array.Empty<string>() : new[] { path };
    }
    Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default);
    Task<string?> PickV2PackageAsync(CancellationToken cancellationToken = default) => PickUnityFileAsync(cancellationToken);
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
    Task<string?> PickUnityFolderAsync(CancellationToken cancellationToken = default) =>
        PickFolderAsync(cancellationToken);
    Task<string?> PickPackageCatalogAsync(CancellationToken cancellationToken = default) =>
        PickFolderAsync(cancellationToken);
    Task<string?> PickPackageDestinationAsync(CancellationToken cancellationToken = default) =>
        PickFolderAsync(cancellationToken);
}
