using System.Security;

namespace UABS.UnityAssets.Packages;

internal interface IPackageFolderFileSystem
{
    IEnumerable<string> EnumerateFiles(string directory);
    IEnumerable<string> EnumerateDirectories(string directory);
    FileAttributes GetAttributes(string path);
}

internal sealed class PhysicalPackageFolderFileSystem : IPackageFolderFileSystem
{
    public IEnumerable<string> EnumerateFiles(string directory) => Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly);
    public IEnumerable<string> EnumerateDirectories(string directory) => Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly);
    public FileAttributes GetAttributes(string path) => File.GetAttributes(path);
}

internal sealed record PackageFolderScanResult(
    IReadOnlyList<string> Files,
    int ScannedFileCount,
    IReadOnlyList<PackageFolderImportFailure> Failures,
    int SkippedLinkCount,
    int SkippedSubdirectoryCount);

internal sealed class PackageFolderScanner(IPackageFolderFileSystem fileSystem)
{
    public PackageFolderScanResult Scan(
        string root,
        PackageFolderSearchMode searchMode,
        IProgress<PackageProgress>? progress,
        CancellationToken cancellationToken)
    {
        Queue<string> pending = new();
        pending.Enqueue(root);
        List<string> files = [];
        List<PackageFolderImportFailure> failures = [];
        int scannedFiles = 0;
        int skippedLinks = 0;
        int skippedSubdirectories = 0;

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string directory = pending.Dequeue();

            try
            {
                foreach (string file in fileSystem.EnumerateFiles(directory)
                             .Select(path => CheckCancellation(path, cancellationToken))
                             .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(path => path, StringComparer.Ordinal))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    scannedFiles++;
                    progress?.Report(new PackageProgress(PackageProgressPhase.Scanning, scannedFiles, 0, file));
                    try
                    {
                        if ((fileSystem.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                        {
                            skippedLinks++;
                            continue;
                        }

                        files.Add(file);
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
                    {
                        failures.Add(new PackageFolderImportFailure(file, exception.Message));
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                failures.Add(new PackageFolderImportFailure(
                    directory,
                    exception.Message,
                    PackageFolderImportFailureKind.Directory));
            }

            IEnumerable<string> childDirectories;
            try
            {
                childDirectories = fileSystem.EnumerateDirectories(directory)
                    .Select(path => CheckCancellation(path, cancellationToken))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(path => path, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                failures.Add(new PackageFolderImportFailure(
                    directory,
                    exception.Message,
                    PackageFolderImportFailureKind.Directory));
                continue;
            }

            foreach (string child in childDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if ((fileSystem.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                    {
                        skippedLinks++;
                        continue;
                    }

                    if (searchMode == PackageFolderSearchMode.IncludeSubfolders)
                        pending.Enqueue(child);
                    else
                        skippedSubdirectories++;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
                {
                    failures.Add(new PackageFolderImportFailure(
                        child,
                        exception.Message,
                        PackageFolderImportFailureKind.Directory));
                }
            }
        }

        List<PackageFolderImportFailure> distinctFailures = failures
            .GroupBy(
                failure => $"{failure.Kind}\0{failure.Path}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        return new PackageFolderScanResult(
            files.AsReadOnly(),
            scannedFiles,
            distinctFailures.AsReadOnly(),
            skippedLinks,
            skippedSubdirectories);
    }

    private static string CheckCancellation(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return path;
    }
}
