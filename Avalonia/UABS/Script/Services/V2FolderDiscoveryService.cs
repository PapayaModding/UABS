using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace UABS.Services;

/// <summary>Small filesystem seam for deterministic inaccessible-path/link tests; no V1 package operations.</summary>
public interface IV2FolderFileSystem
{
    IEnumerable<string> EnumerateEntries(string directory);
    FileAttributes GetAttributes(string path);
    (bool Supported, string Physical) InspectSource(string path);
}

public sealed record V2FolderProgress(long Scanned, long Supported);
public sealed record V2FolderDiscoveryResult(IReadOnlyList<string> Sources, long Scanned, long Unsupported,
    long SkippedLinks, long Duplicates, long Errors, string? FirstError, IReadOnlyList<string> Details)
{
    public bool Succeeded => Errors == 0;
    public string Summary => $"Folder scan: {Scanned:N0} files checked; {Sources.Count:N0} new sources, {Duplicates:N0} duplicates, "
        + $"{Unsupported:N0} unsupported, {SkippedLinks:N0} links/repeated directories skipped, {Errors:N0} inaccessible/failed paths.";
}

/// <summary>Selection only: one directory enumerator and at most one header handle; no payload extraction.</summary>
public sealed class V2FolderDiscoveryService(IV2FolderFileSystem? fileSystem = null)
{
    private readonly IV2FolderFileSystem _files = fileSystem ?? new PhysicalFiles();
    public Task<V2FolderDiscoveryResult> DiscoverAsync(string folder, bool recursive, IReadOnlyList<string> existing,
        IProgress<V2FolderProgress>? progress = null, CancellationToken token = default)
        => Task.Run(() => Discover(folder, recursive, existing, progress, token), token);

    private V2FolderDiscoveryResult Discover(string folder, bool recursive, IReadOnlyList<string> existing,
        IProgress<V2FolderProgress>? progress, CancellationToken token)
    {
        List<string> accepted = [], details = [];
        long scanned = 0, unsupported = 0, links = 0, duplicates = 0, errors = 0;
        string? firstError = null;
        void Detail(string text) { if (details.Count < 50) details.Add(text.Length > 1024 ? text[..1024] : text); }
        void Fail(string path, Exception error)
        {
            errors++; string message = path + ": " + error.Message;
            firstError ??= message; Detail("Failed: " + message);
        }
        try
        {
            token.ThrowIfCancellationRequested();
            string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
            string prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
            // Reject a linked selected root or any linked ancestor before enumerating it.
            for (string? ancestor = root; ancestor is not null; ancestor = Path.GetDirectoryName(ancestor))
                if ((_files.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Selected folder has a link/reparse ancestor; choose a physical directory.");
            if ((_files.GetAttributes(root) & FileAttributes.Directory) == 0) throw new IOException("Choose an existing directory.");
            HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase), physical = new(StringComparer.Ordinal);
            Dictionary<string, int> acceptedByPhysical = new(StringComparer.Ordinal);
            foreach (string input in existing)
            {
                token.ThrowIfCancellationRequested();
                string path = Path.GetFullPath(input); paths.Add(path);
                // An unavailable manually selected source stays selected for Create's existing diagnostics.
                try { var evidence = _files.InspectSource(path); if (evidence.Supported) physical.Add(evidence.Physical); }
                catch (Exception e) when (IsFileError(e)) { }
            }
            Stack<string> pending = new(); pending.Push(root);
            HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
            while (pending.Count > 0)
            {
                token.ThrowIfCancellationRequested(); string directory = pending.Pop();
                if (!visited.Add(directory)) { links++; continue; }
                try
                {
                    if ((_files.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) { links++; Detail("Skipped link: " + directory); continue; }
                    for (string? ancestor = Path.GetDirectoryName(directory); ancestor is not null; ancestor = Path.GetDirectoryName(ancestor))
                        if ((_files.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0) throw new IOException("Directory acquired a link/reparse ancestor; not traversed.");
                    // Streaming top-level enumeration; never materialize all unsupported files or open child enumerators concurrently.
                    foreach (string entry in _files.EnumerateEntries(directory))
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            string path = Path.GetFullPath(entry);
                            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                throw new IOException("Discovery returned a path outside the selected folder; not followed.");
                            var attributes = _files.GetAttributes(path);
                            if ((attributes & FileAttributes.ReparsePoint) != 0) { links++; Detail("Skipped link: " + path); continue; }
                            if ((attributes & FileAttributes.Directory) != 0) { if (recursive) pending.Push(path); continue; }
                            scanned++;
                            if (paths.Contains(path)) { duplicates++; continue; }
                            var source = _files.InspectSource(path);
                            if (!source.Supported) { unsupported++; Detail("Unsupported header: " + path); continue; }
                            paths.Add(path);
                            if (!physical.Add(source.Physical))
                            {
                                duplicates++;
                                if (acceptedByPhysical.TryGetValue(source.Physical, out int index) && ComparePaths(path, accepted[index]) < 0)
                                    accepted[index] = path; // Stable representative even if filesystem enumeration order differs.
                                continue;
                            }
                            acceptedByPhysical.Add(source.Physical, accepted.Count); accepted.Add(path);
                        }
                        catch (Exception e) when (IsFileError(e)) { Fail(entry, e); }
                        finally { progress?.Report(new(scanned, accepted.Count)); }
                    }
                }
                catch (Exception e) when (IsFileError(e)) { Fail(directory, e); }
            }
            token.ThrowIfCancellationRequested();
            accepted.Sort(ComparePaths);
            token.ThrowIfCancellationRequested();
        }
        catch (Exception e) when (IsFileError(e)) { Fail(folder, e); }
        return new(accepted.ToArray(), scanned, unsupported, links, duplicates, errors, firstError, details.ToArray());
    }
    private static bool IsFileError(Exception error) => error is IOException or UnauthorizedAccessException or SecurityException or ArgumentException or NotSupportedException;
    private static int ComparePaths(string a, string b)
    { int order = StringComparer.OrdinalIgnoreCase.Compare(a, b); return order != 0 ? order : StringComparer.Ordinal.Compare(a, b); }
    private sealed class PhysicalFiles : IV2FolderFileSystem
    {
        public IEnumerable<string> EnumerateEntries(string directory) => Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.TopDirectoryOnly);
        public FileAttributes GetAttributes(string path) => File.GetAttributes(path);
        public (bool Supported, string Physical) InspectSource(string path) => V2PackageCreationService.InspectSelectionSource(path);
    }
}
