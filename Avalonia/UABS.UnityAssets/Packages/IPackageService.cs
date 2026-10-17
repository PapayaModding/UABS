namespace UABS.UnityAssets.Packages;

public interface IPackageService
{
    Task<PackageDocument> LoadAsync(
        string packagePath,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<PackageDocument> CreateAsync(
        string sourceRoot,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<PackageBundleReference> CreateReferenceAsync(
        string sourcePath,
        IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null,
        CancellationToken cancellationToken = default);

    Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(
        string folderPath,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(
        string folderPath,
        PackageFolderSearchMode searchMode,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        PackageDocument package,
        string targetPath,
        bool overwrite = false,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<PackageDocument> BuildAndSaveAsync(
        string sourceRoot,
        string targetPath,
        bool overwrite = false,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageSummary>> ListAsync(
        string packageParentPath,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(
        string packageParentPath,
        CancellationToken cancellationToken = default);
}
