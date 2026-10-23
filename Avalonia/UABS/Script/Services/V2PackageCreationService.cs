using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using UABS.PackageV2;
using UABS.UnityAssets;
using UABS.UnityAssets.Metadata;

namespace UABS.Services;

public sealed record V2CreationRequest(string Name, string Directory, IReadOnlyList<string> Sources);
public sealed record V2CreationResult(PackageBuildResult? Build, bool Verified, string Message,
    TimeSpan Elapsed, IReadOnlyList<V2ExtractionMetrics> Extraction, PackageInfo? Info = null)
{
    public bool Published => Build?.PublishedPath is not null;
}

/// <summary>Initial creation only. No legacy documents, payload decoding, replacement or registration ownership.</summary>
public sealed class V2PackageCreationService(IV2MetadataExtractor? extractor = null,
    Action<BuildBoundary, string>? boundary = null)
{
    public static string Destination(V2CreationRequest request)
    {
        string name = request.Name.Trim();
        if (name.Length == 0 || name != Path.GetFileName(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || name.EndsWith('.') || name.EndsWith(' ') || name.EndsWith(".uabs.sqlite", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Enter a package name without a path or .uabs.sqlite extension.");
        return Path.Combine(Path.GetFullPath(request.Directory), name + ".uabs.sqlite");
    }

    public Task<V2CreationResult> CreateAsync(V2CreationRequest request,
        IProgress<PackageBuildProgress>? progress = null,
        Func<PartialBuildSummary, CancellationToken, Task<bool>>? acknowledge = null,
        CancellationToken token = default)
    {
        var snapshot = request with { Sources = request.Sources.ToArray() };
        return Task.Run<V2CreationResult>(async () =>
        {
            var watch = Stopwatch.StartNew();
            var metrics = new List<V2ExtractionMetrics>();
            PackageBuildResult? built = null;
            try
            {
                token.ThrowIfCancellationRequested();
                string destination = Destination(snapshot);
                ValidateDestination(destination);
                RejectSidecars(destination);
                if (File.Exists(destination)) throw new IOException("Destination exists. Choose a different name; existing packages are never overwritten.");
                if (snapshot.Sources.Count < 1) throw new ArgumentException("Select at least one source file.");
                progress?.Report(new("Preparing sources", 0, snapshot.Sources.Count, 0));
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var physical = new HashSet<string>(StringComparer.Ordinal);
                var roots = new List<SourceRoot>();
                var rootIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var sources = new List<BuildSource>();
                var prepared = new Dictionary<Guid, PreparedSource>();
                foreach (string input in snapshot.Sources)
                {
                    token.ThrowIfCancellationRequested();
                    string path = Path.GetFullPath(input);
                    if (!paths.Add(path)) throw new ArgumentException("Duplicate source selection: " + path);
                    using var pin = OpenSource(path);
                    string identity = PhysicalIdentity(pin);
                    if (!physical.Add(identity))
                        throw new ArgumentException("Duplicate physical source selection (including aliases): " + path);
                    if (UnityFileDetector.Detect(pin) == UnityFileKind.Unknown)
                        throw new ArgumentException("Unsupported Unity source header: " + path + ". Select a bundle or serialized asset file.");
                    string parent = Path.GetDirectoryName(path)!;
                    if (!rootIndices.TryGetValue(parent, out int root))
                    { root = roots.Count; rootIndices.Add(parent, root); roots.Add(new("Source root " + (root + 1), parent, "windows-lexical-v1")); }
                    var extraction = new V2ExtractionRequest(input, Guid.NewGuid(), Guid.NewGuid());
                    prepared.Add(extraction.SourceId, new(path, identity));
                    sources.Add(new(root, Path.GetFileName(path), "", extraction));
                }
                Guid package = Guid.NewGuid(), generation = Guid.NewGuid();
                var tracked = new TrackingExtractor(extractor ?? new AssetsToolsV2MetadataExtractor(), progress, metrics, sources.Count, prepared);
                built = await new PackageBuilder(tracked, (stage, staging) =>
                {
                    boundary?.Invoke(stage, staging);
                    if (stage == BuildBoundary.BeforePublication)
                    {
                        RejectSidecars(destination);
                        int number = 0;
                        byte[] buffer = new byte[65536];
                        foreach (var source in sources)
                        {
                            token.ThrowIfCancellationRequested();
                            var evidence = prepared[source.Request.SourceId];
                            progress?.Report(new("Rechecking source bytes before publication", ++number, sources.Count, 0));
                            using var pin = OpenSource(evidence.Path);
                            if (PhysicalIdentity(pin) != evidence.Physical || !string.Equals(Hash(pin, token, buffer), evidence.Sha256, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidDataException("Source changed since extraction: " + evidence.Path + ". No package published.");
                        }
                    }
                }).BuildAsync(new(destination, package, generation,
                    snapshot.Name.Trim(), roots, sources), token, progress, acknowledge).ConfigureAwait(false);
                if (!built.Succeeded) return new(built, false, built.Message, watch.Elapsed, metrics.ToArray());
                // Publication is irreversible here. Finish verification even if cancellation arrived at commit.
                progress?.Report(new("Verifying published package", sources.Count, sources.Count, 0));
                var admission = await PackageReader.OpenAsync(built.PublishedPath!, CancellationToken.None).ConfigureAwait(false);
                using var reader = admission.Reader;
                if (reader is null || reader.Info.PackageId != package || reader.Info.GenerationId != generation
                    || reader.Info.Sources != sources.Count)
                    return new(built, false, "File published but verification failed: " + admission.Message, watch.Elapsed, metrics.ToArray());
                return new(built, true, $"Created {reader.Info.IndexStatus} V2 package: {reader.Info.Assets:N0} assets, {reader.Info.Issues:N0} issues. "
                    + "Review Issues in Open Package for full indexing diagnostics.", watch.Elapsed, metrics.ToArray(), reader.Info);
            }
            catch (OperationCanceledException) when (built?.PublishedPath is null)
            { return new(built, false, "Cancelled before publication; existing output preserved.", watch.Elapsed, metrics.ToArray()); }
            catch (Exception e)
            { return new(built, false, (built?.PublishedPath is null ? "Not created: " : "File published; verification unavailable: ") + e.Message, watch.Elapsed, metrics.ToArray()); }
        });
    }

    private static void ValidateDestination(string destination)
    {
        string parent = Path.GetDirectoryName(destination)!;
        if (!OperatingSystem.IsWindows() || destination.StartsWith("\\\\", StringComparison.Ordinal) || !System.IO.Directory.Exists(parent))
            throw new IOException("Creation requires an existing local Windows NTFS destination directory.");
        var drive = new DriveInfo(Path.GetPathRoot(destination)!);
        if (drive.DriveType != DriveType.Fixed || drive.DriveFormat != "NTFS")
            throw new IOException("Safe publication is supported only on fixed local NTFS storage in this gate.");
        RejectLinks(parent);
    }
    private static void RejectSidecars(string destination)
    {
        if (new[] { "-journal", "-wal", "-shm" }.Any(s => File.Exists(destination + s)))
            throw new IOException("Destination has existing SQLite recovery/sidecar files. Choose a different name; no sidecar will be deleted.");
    }
    private static void RejectLinks(string path)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked/reparse paths are not supported for package creation: " + current);
    }
    private sealed record PreparedSource(string Path, string Physical)
    {
        public string? Sha256 { get; set; }
    }
    private static FileStream OpenSource(string path)
    {
        RejectLinks(path);
        return new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
    }
    internal static (bool Supported, string Physical) InspectSelectionSource(string path)
    {
        using var pin = OpenSource(path);
        return (UnityFileDetector.Detect(pin) != UnityFileKind.Unknown, PhysicalIdentity(pin));
    }
    private static string PhysicalIdentity(FileStream pin)
    {
        if (!GetFileInformationByHandle(pin.SafeFileHandle, out FileInformation info))
            throw new IOException("Cannot establish physical source identity: " + pin.Name);
        return $"{info.Volume}:{info.IndexHigh}:{info.IndexLow}";
    }
    private static string Hash(FileStream source, CancellationToken token, byte[] buffer)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        int count;
        while ((count = source.Read(buffer, 0, buffer.Length)) > 0) { token.ThrowIfCancellationRequested(); hash.AppendData(buffer, 0, count); }
        token.ThrowIfCancellationRequested(); return Convert.ToHexString(hash.GetHashAndReset());
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInformation info);

    private sealed class TrackingExtractor(IV2MetadataExtractor inner, IProgress<PackageBuildProgress>? progress,
        List<V2ExtractionMetrics> metrics, int count, Dictionary<Guid, PreparedSource> prepared) : IV2MetadataExtractor
    {
        public async Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
            Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive,
            V2ExtractionOptions? options = null, IProgress<V2ExtractionProgress>? ignored = null, CancellationToken cancellationToken = default)
        {
            int source = metrics.Count + 1;
            var evidence = prepared[request.SourceId];
            using var pin = OpenSource(evidence.Path);
            if (PhysicalIdentity(pin) != evidence.Physical) throw new InvalidDataException("Source replaced after selection: " + evidence.Path);
            var report = new InlineProgress<V2ExtractionProgress>(p => progress?.Report(new(
                p.Phase == "fingerprint" ? "Verifying source bytes" : "Extracting metadata / writing SQLite records", source, count, p.Assets)));
            var result = await inner.ExtractAsync(request, receive, options, report, cancellationToken).ConfigureAwait(false);
            metrics.Add(result.Metrics);
            if (!result.CommitEligible) throw new InvalidDataException($"Source {request.Path}: {result.Status}. No package published; check source readability and byte stability.");
            evidence.Sha256 = result.Source.Sha256;
            return result;
        }
    }
}

internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
