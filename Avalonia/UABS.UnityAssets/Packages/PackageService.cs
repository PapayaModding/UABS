using System.Security;
using System.Text.Json;

namespace UABS.UnityAssets.Packages;

public sealed class PackageService : IPackageService
{
    public const string IndexFileName = "index.json";
    public const string ValidationFileName = "Validation.txt";

    private readonly IAssetSessionFactory _sessionFactory;
    private readonly LegacyPackageMetadataReader _metadataReader;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    public PackageService(string? classDataPath = null, IAssetSessionFactory? sessionFactory = null)
    {
        string resolvedClassDataPath = classDataPath
            ?? Path.Combine(AppContext.BaseDirectory, "Resources", "classdata.tpk");
        _sessionFactory = sessionFactory ?? new AssetsToolsAssetSessionFactory(resolvedClassDataPath);
        _metadataReader = new LegacyPackageMetadataReader(resolvedClassDataPath);
    }

    public Task<PackageDocument> LoadAsync(
        string packagePath,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Load(packagePath, progress, cancellationToken), cancellationToken);
    }

    public async Task<PackageDocument> CreateAsync(
        string sourceRoot,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
        string fullSourceRoot = Path.GetFullPath(sourceRoot);
        if (!Directory.Exists(fullSourceRoot))
            throw new DirectoryNotFoundException($"The package source directory does not exist: {fullSourceRoot}");

        (string[] directories, List<(string Directory, string File)> files) = await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] foundDirectories = Directory.EnumerateDirectories(fullSourceRoot, "*", SearchOption.AllDirectories)
                .Prepend(fullSourceRoot)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            List<(string Directory, string File)> foundFiles = foundDirectories
                .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                    .Where(IsSupportedSource)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(file => (directory, file)))
                .ToList();
            return (foundDirectories, foundFiles);
        }, cancellationToken).ConfigureAwait(false);

        progress?.Report(new PackageProgress(PackageProgressPhase.Scanning, 0, files.Count, fullSourceRoot));
        Dictionary<string, List<PackageBundleReference>> referencesByDirectory = directories.ToDictionary(
            path => path,
            _ => new List<PackageBundleReference>(),
            StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string directory, string file) = files[index];
            progress?.Report(new PackageProgress(PackageProgressPhase.Reading, index, files.Count, file));

            try
            {
                referencesByDirectory[directory].Add(
                    await CreateReferenceAsync(file, cancellationToken: cancellationToken).ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new PackageBuildException(file, exception);
            }

            progress?.Report(new PackageProgress(PackageProgressPhase.Reading, index + 1, files.Count, file));
        }

        List<PackageIndex> packageIndexes = directories.Select(directory =>
            new PackageIndex(Path.GetRelativePath(fullSourceRoot, directory) is "." ? string.Empty : Path.GetRelativePath(fullSourceRoot, directory),
                referencesByDirectory[directory])).ToList();
        progress?.Report(new PackageProgress(PackageProgressPhase.Complete, files.Count, files.Count, fullSourceRoot));
        return new PackageDocument(packageIndexes, sourceRoot: fullSourceRoot);
    }

    public async Task<PackageBundleReference> CreateReferenceAsync(
        string sourcePath,
        IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        string fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
            throw new FileNotFoundException("The Unity source file does not exist.", fullSourcePath);
        if (UnityFileDetector.Detect(fullSourcePath) == UnityFileKind.Unknown)
            throw new PackageException(
                $"The selected file is not a supported Unity bundle or serialized asset file based on its header: '{fullSourcePath}'.");

        try
        {
            using IAssetSession session = await _sessionFactory.OpenAsync(fullSourcePath, cancellationToken).ConfigureAwait(false);
            PackageFileMetadata metadata = await Task.Run(
                () => _metadataReader.Read(fullSourcePath, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            IReadOnlyCollection<PackageAssetSelection>? requested = selectedAssets;
            if (requested is not null && requested.Count == 0)
                throw new PackageException("At least one asset must be selected when adding selected assets.");

            List<AssetEntry> supported = session.Assets
                .Where(asset => asset.ClassId is 213 or 28)
                .ToList();
            List<AssetEntry> included = requested is null
                ? supported
                : supported.Where(asset => requested.Any(selection =>
                    selection.PathId == asset.PathId
                    && string.Equals(selection.Name, asset.Name, StringComparison.Ordinal)
                    && string.Equals(selection.LogicalFile, asset.LogicalFile, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            if (requested is not null && included.Count != requested.Count)
                throw new PackageException("One or more selected assets are unsupported or no longer present in the source file.");

            return new PackageBundleReference
            {
                Name = Path.GetFileName(fullSourcePath),
                Path = fullSourcePath,
                CabCode = metadata.CabCode,
                AssetInfos = included
                    .OrderBy(asset => asset.ClassId == 213 ? 0 : 1)
                    .ThenBy(asset => asset.PathId)
                    .Select(asset => new PackageAssetReference { Name = asset.Name, PathId = asset.PathId })
                    .ToList(),
                Dependencies = metadata.Dependencies.ToList()
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PackageException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new PackageBuildException(fullSourcePath, exception);
        }
    }

    public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(
        string folderPath,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => CreateReferencesFromFolderAsync(
            folderPath,
            PackageFolderSearchMode.CurrentFolderOnly,
            progress,
            cancellationToken);

    public async Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(
        string folderPath,
        PackageFolderSearchMode searchMode,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        string fullFolderPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(fullFolderPath))
            throw new DirectoryNotFoundException($"The asset source directory does not exist: {fullFolderPath}");

        PackageFolderScanResult scan = await Task.Run(
            () => new PackageFolderScanner(new PhysicalPackageFolderFileSystem())
                .Scan(fullFolderPath, searchMode, progress, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        List<PackageBundleReference> references = [];
        List<PackageFolderImportReference> mappedReferences = [];
        List<PackageFolderImportFailure> failures = [.. scan.Failures];
        int unsupported = 0;
        int supported = 0;

        for (int index = 0; index < scan.Files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string file = scan.Files[index];
            progress?.Report(new PackageProgress(PackageProgressPhase.Reading, index, scan.Files.Count, file));

            try
            {
                UnityFileKind kind = await Task.Run(
                    () => UnityFileDetector.Detect(file),
                    cancellationToken).ConfigureAwait(false);
                if (kind == UnityFileKind.Unknown)
                {
                    unsupported++;
                }
                else
                {
                    supported++;
                    PackageBundleReference reference = await CreateReferenceAsync(
                        file,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    references.Add(reference);
                    string? sourceDirectory = Path.GetDirectoryName(file);
                    string relativeDirectory = sourceDirectory is null
                        ? string.Empty
                        : Path.GetRelativePath(fullFolderPath, sourceDirectory);
                    mappedReferences.Add(new PackageFolderImportReference(
                        relativeDirectory is "." ? string.Empty : relativeDirectory,
                        reference));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is PackageException
                                               or AssetLoadException
                                               or IOException
                                               or UnauthorizedAccessException
                                               or SecurityException)
            {
                failures.Add(new PackageFolderImportFailure(file, exception.Message));
            }

            progress?.Report(new PackageProgress(PackageProgressPhase.Reading, index + 1, scan.Files.Count, file));
        }

        progress?.Report(new PackageProgress(PackageProgressPhase.Complete, scan.ScannedFileCount, scan.ScannedFileCount, fullFolderPath));
        return new PackageFolderImportResult(
            scan.ScannedFileCount,
            supported,
            references.AsReadOnly(),
            unsupported,
            failures.AsReadOnly(),
            scan.SkippedLinkCount,
            searchMode,
            scan.SkippedSubdirectoryCount,
            mappedReferences.AsReadOnly());
    }

    public Task SaveAsync(
        PackageDocument package,
        string targetPath,
        bool overwrite = false,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        return Task.Run(() => Save(package, targetPath, overwrite, progress, cancellationToken), cancellationToken);
    }

    // [UNITY-DEPENDENCY:REPLACEMENT-REQUIRED]
    // Finding: UE-007
    // Legacy source: Assets/Script/Writer/UserPackage/WriteUserPackage.cs::CreateAndSaveNewPackage
    // Unity API/plugin: UnityMainThreadDispatcher MonoBehaviour.Update queue and UnityEngine.Debug
    // Purpose: Run package construction off the UI thread and signal completion on Unity's main thread.
    // Migration status: REPLACED
    // Replacement: Awaitable Task workflow with CancellationToken, IProgress<PackageProgress>, staged persistence, and no presentation dependency.
    public async Task<PackageDocument> BuildAndSaveAsync(
        string sourceRoot,
        string targetPath,
        bool overwrite = false,
        IProgress<PackageProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        PackageDocument package = await CreateAsync(sourceRoot, progress, cancellationToken).ConfigureAwait(false);
        await SaveAsync(package, targetPath, overwrite, progress, cancellationToken).ConfigureAwait(false);
        return package;
    }

    public Task<IReadOnlyList<PackageSummary>> ListAsync(
        string packageParentPath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<PackageSummary>>(() =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(packageParentPath);
            string fullParent = Path.GetFullPath(packageParentPath);
            if (!Directory.Exists(fullParent))
                return [];

            List<PackageSummary> packages = [];
            foreach (string directory in EnumeratePackageDirectories(fullParent, requireValidationMarker: true))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(Path.Combine(directory, ValidationFileName)))
                    continue;

                PackageDocument package = Load(directory, null, cancellationToken);
                packages.Add(new PackageSummary(
                    Path.GetFileName(directory),
                    directory,
                    package.Indexes.Count,
                    package.Bundles.Count()));
            }

            return packages;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(
        string packageParentPath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<PackageCatalogEntry>>(() =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(packageParentPath);
            string fullParent = Path.GetFullPath(packageParentPath);
            if (!Directory.Exists(fullParent))
                return [];

            List<PackageCatalogEntry> packages = [];
            foreach (string directory in EnumeratePackageDirectories(fullParent, requireValidationMarker: false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string name = Path.GetFileName(directory);
                try
                {
                    PackageDocument package = Load(directory, null, cancellationToken);
                    packages.Add(new PackageCatalogEntry(name, directory, package, null));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (exception is PackageException
                                                   or IOException
                                                   or UnauthorizedAccessException)
                {
                    packages.Add(new PackageCatalogEntry(name, directory, null, exception.Message));
                }
            }

            return packages.AsReadOnly();
        }, cancellationToken);
    }

    private static IEnumerable<string> EnumeratePackageDirectories(
        string selectedDirectory,
        bool requireValidationMarker)
    {
        bool selectedIsPackage = File.Exists(Path.Combine(selectedDirectory, ValidationFileName))
            || File.Exists(Path.Combine(selectedDirectory, IndexFileName));

        IEnumerable<string> candidates = selectedIsPackage
            ? [selectedDirectory]
            : Directory.EnumerateDirectories(selectedDirectory, "*", SearchOption.TopDirectoryOnly);

        return candidates
            .Where(path => !requireValidationMarker || File.Exists(Path.Combine(path, ValidationFileName)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private PackageDocument Load(
        string packagePath,
        IProgress<PackageProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        string fullPackagePath = Path.GetFullPath(packagePath);
        if (!Directory.Exists(fullPackagePath))
            throw new DirectoryNotFoundException($"The package directory does not exist: {fullPackagePath}");

        string[] indexFiles = Directory.EnumerateFiles(fullPackagePath, IndexFileName, SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (indexFiles.Length == 0)
            throw new PackageFormatException("The package does not contain any index.json files.", fullPackagePath);

        List<PackageValidationIssue> issues = [];
        if (!File.Exists(Path.Combine(fullPackagePath, ValidationFileName)))
        {
            issues.Add(new PackageValidationIssue(
                PackageIssueSeverity.Error,
                "missing-validation-marker",
                "The legacy Validation.txt package marker is missing.",
                fullPackagePath));
        }

        List<PackageIndex> indexes = [];
        for (int index = 0; index < indexFiles.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string indexFile = indexFiles[index];
            progress?.Report(new PackageProgress(PackageProgressPhase.Reading, index, indexFiles.Length, indexFile));
            List<PackageBundleReference>? bundles;
            try
            {
                using FileStream stream = new(indexFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                bundles = JsonSerializer.Deserialize<List<PackageBundleReference>>(stream, _jsonOptions);
            }
            catch (JsonException exception)
            {
                throw new PackageFormatException($"Package index JSON is malformed: {exception.Message}", indexFile, exception);
            }

            if (bundles is null)
                throw new PackageFormatException("Package index must contain a JSON array.", indexFile);

            ValidateBundles(bundles, indexFile, issues);
            string relativeDirectory = Path.GetRelativePath(fullPackagePath, Path.GetDirectoryName(indexFile)!);
            indexes.Add(new PackageIndex(relativeDirectory is "." ? string.Empty : relativeDirectory, bundles));
            progress?.Report(new PackageProgress(PackageProgressPhase.Reading, index + 1, indexFiles.Length, indexFile));
        }

        progress?.Report(new PackageProgress(PackageProgressPhase.Complete, indexFiles.Length, indexFiles.Length, fullPackagePath));
        return new PackageDocument(indexes, fullPackagePath, validationIssues: issues);
    }

    private void Save(
        PackageDocument package,
        string targetPath,
        bool overwrite,
        IProgress<PackageProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        string fullTarget = Path.GetFullPath(targetPath);
        string? parent = Path.GetDirectoryName(fullTarget);
        if (string.IsNullOrWhiteSpace(parent))
            throw new ArgumentException("The package target must have a parent directory.", nameof(targetPath));

        ValidateDocumentForSave(package);
        if (Directory.Exists(fullTarget) && !overwrite)
            throw new PackageAlreadyExistsException(fullTarget);
        if (File.Exists(fullTarget))
            throw new PackageException($"The package target is an existing file: '{fullTarget}'.");

        Directory.CreateDirectory(parent);
        string targetName = Path.GetFileName(fullTarget);
        string staging = Path.Combine(parent, $".{targetName}.{Guid.NewGuid():N}.tmp");
        string? backup = null;

        try
        {
            Directory.CreateDirectory(staging);
            for (int index = 0; index < package.Indexes.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PackageIndex packageIndex = package.Indexes[index];
                string indexDirectory = ResolveIndexDirectory(staging, packageIndex.RelativeDirectory);
                Directory.CreateDirectory(indexDirectory);
                string indexPath = Path.Combine(indexDirectory, IndexFileName);
                progress?.Report(new PackageProgress(PackageProgressPhase.Writing, index, package.Indexes.Count, indexPath));

                using FileStream stream = new(indexPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                JsonSerializer.Serialize(stream, packageIndex.Bundles, _jsonOptions);
                stream.Flush(true);
                progress?.Report(new PackageProgress(PackageProgressPhase.Writing, index + 1, package.Indexes.Count, indexPath));
            }

            cancellationToken.ThrowIfCancellationRequested();
            using (FileStream marker = new(Path.Combine(staging, ValidationFileName), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                marker.Flush(true);

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new PackageProgress(PackageProgressPhase.Committing, package.Indexes.Count, package.Indexes.Count, fullTarget));

            if (Directory.Exists(fullTarget))
            {
                backup = Path.Combine(parent, $".{targetName}.{Guid.NewGuid():N}.bak");
                Directory.Move(fullTarget, backup);
            }

            try
            {
                Directory.Move(staging, fullTarget);
            }
            catch
            {
                if (backup is not null && Directory.Exists(backup) && !Directory.Exists(fullTarget))
                    Directory.Move(backup, fullTarget);
                throw;
            }

            package.PackagePath = fullTarget;
            if (backup is not null && Directory.Exists(backup))
                Directory.Delete(backup, true);
            progress?.Report(new PackageProgress(PackageProgressPhase.Complete, package.Indexes.Count, package.Indexes.Count, fullTarget));
        }
        finally
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, true);
        }
    }

    private static bool IsSupportedSource(string path)
    {
        try
        {
            return UnityFileDetector.Detect(path) != UnityFileKind.Unknown;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void ValidateBundles(
        IEnumerable<PackageBundleReference> bundles,
        string indexFile,
        List<PackageValidationIssue> issues)
    {
        int index = 0;
        foreach (PackageBundleReference? bundle in bundles)
        {
            string location = $"{indexFile} entry {index}";
            if (bundle is null)
                throw new PackageFormatException($"{location} is null.", indexFile);
            if (string.IsNullOrWhiteSpace(bundle.Name))
                throw new PackageFormatException($"{location} has no Name.", indexFile);
            if (string.IsNullOrWhiteSpace(bundle.Path))
                throw new PackageFormatException($"{location} has no Path.", indexFile);
            if (bundle.AssetInfos is null)
                throw new PackageFormatException($"{location} has null AssetInfos.", indexFile);
            if (bundle.Dependencies is null)
                throw new PackageFormatException($"{location} has null Dependencies.", indexFile);
            if (bundle.AssetInfos.Any(asset => asset is null || string.IsNullOrWhiteSpace(asset.Name)))
                throw new PackageFormatException($"{location} contains an invalid asset reference.", indexFile);
            if (!File.Exists(bundle.Path))
            {
                issues.Add(new PackageValidationIssue(
                    PackageIssueSeverity.Warning,
                    "missing-source-file",
                    $"The referenced source file is missing: {bundle.Path}",
                    location));
            }

            index++;
        }
    }

    private static void ValidateDocumentForSave(PackageDocument package)
    {
        if (package.Indexes.Count == 0)
            throw new PackageFormatException("A package must contain at least one index.");

        HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
        foreach (PackageIndex index in package.Indexes)
        {
            string normalized = PackageIndex.NormalizeRelativeDirectory(index.RelativeDirectory);
            if (!directories.Add(normalized))
                throw new PackageFormatException($"Duplicate package index directory: '{normalized}'.");

            List<PackageValidationIssue> ignored = [];
            ValidateBundles(index.Bundles, normalized.Length == 0 ? IndexFileName : Path.Combine(normalized, IndexFileName), ignored);
        }
    }

    private static string ResolveIndexDirectory(string stagingRoot, string relativeDirectory)
    {
        string normalized = PackageIndex.NormalizeRelativeDirectory(relativeDirectory);
        string result = normalized.Length == 0 ? stagingRoot : Path.GetFullPath(Path.Combine(stagingRoot, normalized));
        string rootPrefix = Path.GetFullPath(stagingRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!string.Equals(result, Path.GetFullPath(stagingRoot), StringComparison.OrdinalIgnoreCase)
            && !result.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new PackageFormatException($"Package index path escapes the target directory: '{relativeDirectory}'.");
        }

        return result;
    }
}
