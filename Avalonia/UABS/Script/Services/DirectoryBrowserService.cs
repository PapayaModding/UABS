using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UABS.Models;

namespace UABS.Services;

public sealed class DirectoryBrowserService : IDirectoryBrowserService
{
    public Task<DirectorySnapshot> ReadAsync(string path, CancellationToken cancellationToken = default) =>
        Task.Run(() => Read(path, cancellationToken), cancellationToken);

    private static DirectorySnapshot Read(string path, CancellationToken cancellationToken)
    {
        string canonicalPath = Path.GetFullPath(path);
        var directory = new DirectoryInfo(canonicalPath);
        if (!directory.Exists)
            throw new DirectoryNotFoundException($"The folder does not exist: {canonicalPath}");

        BrowserEntry[] entries = directory.EnumerateFileSystemInfos()
            .Select(info =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool isDirectory = (info.Attributes & FileAttributes.Directory) != 0;
                return new BrowserEntry(
                    info.Name,
                    info.FullName,
                    isDirectory ? BrowserEntryKind.Directory : BrowserEntryKind.File,
                    isDirectory ? 0 : ((FileInfo)info).Length,
                    info.LastWriteTime);
            })
            .OrderBy(entry => entry.Kind)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DirectorySnapshot(directory.FullName, entries);
    }
}
