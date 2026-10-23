using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UABS.Models;
using UABS.UnityAssets;

namespace UABS.Services;

public sealed class DirectoryBrowserService : IDirectoryBrowserService
{
    private readonly IUnityFileRecognitionService _recognitionService;
    private readonly int _maxConcurrentInspections;
    private readonly Action<string, Exception> _reportInspectionFailure;

    public DirectoryBrowserService(
        IUnityFileRecognitionService? recognitionService = null,
        int maxConcurrentInspections = 4,
        Action<string, Exception>? reportInspectionFailure = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentInspections, 1);
        _recognitionService = recognitionService ?? new UnityFileRecognitionService();
        _maxConcurrentInspections = maxConcurrentInspections;
        _reportInspectionFailure = reportInspectionFailure ?? Printer.PrintException;
    }

    public Task<DirectorySnapshot> ReadAsync(string path, CancellationToken cancellationToken = default) =>
        Task.Run(() => Read(path, cancellationToken), cancellationToken);

    private DirectorySnapshot Read(string path, CancellationToken cancellationToken)
    {
        string canonicalPath = Path.GetFullPath(path);
        var directory = new DirectoryInfo(canonicalPath);
        if (!directory.Exists)
            throw new DirectoryNotFoundException($"The folder does not exist: {canonicalPath}");

        List<BrowserEntry> entries = [];
        foreach (DirectoryInfo child in directory.EnumerateDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add(new BrowserEntry(
                child.Name,
                child.FullName,
                BrowserEntryKind.Directory,
                0,
                child.LastWriteTime));
        }

        FileInfo[] files = directory.EnumerateFiles().ToArray();
        var visibleFiles = new BrowserEntry?[files.Length];
        Parallel.For(
            0,
            files.Length,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = _maxConcurrentInspections
            },
            index => InspectFile(files[index], visibleFiles, index, cancellationToken));

        entries.AddRange(visibleFiles.OfType<BrowserEntry>());
        BrowserEntry[] sorted = entries
            .OrderBy(entry => entry.Kind)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DirectorySnapshot(directory.FullName, sorted);
    }

    private void InspectFile(
        FileInfo file,
        BrowserEntry?[] visibleFiles,
        int index,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // A package candidate is admitted by the V2 reader on activation, never treated as Unity data.
        if (!file.Name.EndsWith(".uabs.sqlite", StringComparison.OrdinalIgnoreCase))
        {
        UnityFileRecognition recognition = _recognitionService.Recognize(file.FullName, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (recognition.Status == UnityFileRecognitionStatus.Unreadable)
        {
            if (recognition.Error is not null)
            {
                _reportInspectionFailure(
                    $"Unity file header inspection failed for '{file.FullName}'.",
                    recognition.Error);
            }

            return;
        }

        if (!recognition.IsSupported)
            return;
        }

        try
        {
            file.Refresh();
            visibleFiles[index] = new BrowserEntry(
                file.Name,
                file.FullName,
                BrowserEntryKind.File,
                file.Length,
                file.LastWriteTime);
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or SecurityException)
        {
            _reportInspectionFailure(
                $"Recognized Unity file metadata could not be read for '{file.FullName}'.",
                exception);
        }
    }
}
