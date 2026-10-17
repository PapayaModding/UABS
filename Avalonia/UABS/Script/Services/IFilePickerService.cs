using System.Threading;
using System.Threading.Tasks;

namespace UABS.Services;

public interface IFilePickerService
{
    Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default);
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
    Task<string?> PickUnityFolderAsync(CancellationToken cancellationToken = default) =>
        PickFolderAsync(cancellationToken);
    Task<string?> PickPackageCatalogAsync(CancellationToken cancellationToken = default) =>
        PickFolderAsync(cancellationToken);
    Task<string?> PickPackageDestinationAsync(CancellationToken cancellationToken = default) =>
        PickFolderAsync(cancellationToken);
}
