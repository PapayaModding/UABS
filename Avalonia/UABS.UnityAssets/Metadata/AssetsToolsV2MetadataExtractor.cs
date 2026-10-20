using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

[assembly: InternalsVisibleTo("UABS.UnityAssets.Tests")]

namespace UABS.UnityAssets.Metadata;

/// <summary>Isolated Gate 1 adapter; deliberately not wired into V1 sessions or package creation.</summary>
public sealed class AssetsToolsV2MetadataExtractor : IV2MetadataExtractor
{
    private readonly string? _classDataPath;
    private readonly Func<string, DateTime> _timestamp;
    public AssetsToolsV2MetadataExtractor(string? classDataPath = null) : this(classDataPath, File.GetLastWriteTimeUtc) { }
    internal AssetsToolsV2MetadataExtractor(string? classDataPath, Func<string, DateTime> timestamp)
    { _classDataPath = classDataPath; _timestamp = timestamp; }
    public Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
        Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive,
        V2ExtractionOptions? options = null, IProgress<V2ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(receive);
        if (request.SourceId == Guid.Empty || request.RevisionId == Guid.Empty)
            throw new ArgumentException("Caller-assigned source and revision UUIDs are required.");
        options ??= new();
        if (options.BatchSize is < 1 or > 4096 || options.MaxNameBytes is < 0 or > 1048576 ||
            options.MaxDependencyBytes is < 4 or > 16777216 || options.MaxDependencyItems is < 1 or > 65536)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (request.ExpectedSha256 is not null && (request.ExpectedSha256.Length != 64 ||
            request.ExpectedSha256.Any(c => !Uri.IsHexDigit(c))))
            throw new ArgumentException("Expected SHA-256 must contain 64 hexadecimal digits.");
        return Task.Run(() => Extract(request, receive, options, progress, cancellationToken), cancellationToken);
    }

    private async Task<V2ExtractionResult> Extract(V2ExtractionRequest request,
        Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive,
        V2ExtractionOptions options, IProgress<V2ExtractionProgress>? progress, CancellationToken token)
    {
        Stopwatch total = Stopwatch.StartNew();
        long allocations = GC.GetTotalAllocatedBytes();
        Meter meter = new();
        string path = Path.GetFullPath(request.Path);
        V2SourceEvidence evidence = new(request.SourceId, request.RevisionId, request.Path, path, UnityFileKind.Unknown, null, null, null, false);
        V2ExtractionStatus status = V2ExtractionStatus.Failed;
        List<V2MetadataRecord> batch = new(options.BatchSize);
        int files = 0, maximumBatch = 0;
        long assets = 0;
        async ValueTask Flush()
        {
            token.ThrowIfCancellationRequested();
            if (batch.Count == 0) return;
            V2MetadataRecord[] owned = batch.ToArray();
            batch.Clear();
            maximumBatch = Math.Max(maximumBatch, owned.Length);
            try { await receive(Array.AsReadOnly(owned), token).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { throw new ReceiverException(e); }
        }
        async ValueTask Emit(V2MetadataRecord record)
        {
            batch.Add(record);
            if (record is V2Asset) assets++;
            if (record is V2LogicalFile file)
            {
                files++;
                if (file.Status != V2ExtractionStatus.Complete) status = V2ExtractionStatus.Partial;
            }
            if (batch.Count >= options.BatchSize) await Flush().ConfigureAwait(false);
        }
        try
        {
            token.ThrowIfCancellationRequested();
            // Denies cooperative writes/deletes on Windows; no source reopening/decompression for enrichment.
            using FileStream source = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
            long length = source.Length;
            DateTime stamp = _timestamp(path);
            progress?.Report(new("fingerprint", -1, 0));
            Stopwatch phase = Stopwatch.StartNew();
            string before = Hash(source, token);
            meter.Hash += phase.Elapsed;
            evidence = evidence with { ByteSize = length, LastWriteUtc = stamp, Sha256 = before };
            if (request.ExpectedSha256 is not null && !before.Equals(request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                status = V2ExtractionStatus.RevisionMismatch;
                await Emit(new V2Diagnostic(-1, null, "fingerprint", "revision-mismatch", "Source bytes differ from the caller's expected revision."));
            }
            else
            {
                UnityFileKind kind = UnityFileDetector.Detect(source);
                evidence = evidence with { Kind = kind };
                status = kind == UnityFileKind.Unknown ? V2ExtractionStatus.Unsupported : V2ExtractionStatus.Complete;
                using ManagerScope managerScope = new();
                AssetsManager manager = managerScope.Manager;
                string database = _classDataPath ?? Path.Combine(AppContext.BaseDirectory, "Resources", "classdata.tpk");
                phase.Restart();
                if ((options.ReadNames || options.ReadDependencies) && File.Exists(database))
                {
                    try { manager.LoadClassPackage(database); }
                    catch (Exception e) when (e is not OperationCanceledException)
                    { await Emit(Diagnostic(-1, null, "name", "class-database-unavailable", e)); }
                }
                meter.Names += phase.Elapsed;
                if (kind == UnityFileKind.Unknown)
                    await Emit(new V2Diagnostic(-1, null, "discovery", "unsupported-source", "Header is not a supported Unity bundle or serialized file."));
                else if (kind == UnityFileKind.SerializedFile)
                {
                    foreach (V2MetadataRecord record in ReadFile(source, 0, length, 0, Path.GetFileName(path), manager, options, meter, token))
                        await Emit(record);
                }
                else
                {
                    AssetBundleFile bundle = new();
                    FileStream? unpacked = null;
                    try
                    {
                        phase.Restart();
                        bundle.Read(new AssetsFileReader(new V2ReadWindow(source, 0, length, token)));
                        if (bundle.DataIsCompressed)
                        {
                            // Disk-backed unpacking avoids the V1 full-bundle MemoryStream; invocation-owned and auto-deleted.
                            unpacked = new(Path.Combine(Path.GetTempPath(), $"uabs-v2-{Guid.NewGuid():N}.tmp"),
                                FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.DeleteOnClose);
                            bundle.Unpack(new AssetsFileWriter(unpacked));
                            bundle.Close();
                            unpacked.Position = 0;
                            bundle = new();
                            bundle.Read(new AssetsFileReader(new V2ReadWindow(unpacked, 0, unpacked.Length, token)));
                        }
                        meter.Structure += phase.Elapsed;
                        for (int ordinal = 0; ordinal < bundle.BlockAndDirInfo.DirectoryInfos.Count; ordinal++)
                        {
                            token.ThrowIfCancellationRequested();
                            AssetBundleDirectoryInfo entry = bundle.BlockAndDirInfo.DirectoryInfos[ordinal];
                            bool candidate;
                            try { candidate = (entry.Flags & 4) != 0 || bundle.IsAssetsFile(ordinal); }
                            catch (Exception e) when (e is not OperationCanceledException)
                            {
                                status = V2ExtractionStatus.Partial;
                                await Emit(Diagnostic(ordinal, null, "file", "candidate-unreadable", e));
                                continue;
                            }
                            if (!candidate) continue;
                            long offset = 0, size = 0;
                            V2Diagnostic? rangeFailure = null;
                            try { bundle.GetFileRange(ordinal, out offset, out size); }
                            catch (Exception e) when (e is not OperationCanceledException)
                            { rangeFailure = Diagnostic(ordinal, null, "file", "logical-range-unavailable", e); }
                            if (rangeFailure is not null)
                            {
                                await Emit(rangeFailure);
                                await Emit(new V2LogicalFile(ordinal, entry.Name, Cab(entry.Name), null, null, null, null, 0,
                                    V2ExtractionStatus.Failed, V2DependencyStatus.Unavailable));
                                continue;
                            }
                            foreach (V2MetadataRecord record in ReadFile(bundle.DataReader.BaseStream, offset, size,
                                ordinal, entry.Name, manager, options, meter, token)) await Emit(record);
                            progress?.Report(new("table", ordinal, assets));
                        }
                        if (files == 0)
                        {
                            status = V2ExtractionStatus.Partial;
                            await Emit(new V2Diagnostic(-1, null, "table", "no-readable-table", "Bundle contains no discoverable serialized table; this is not a verified zero-asset source."));
                        }
                    }
                    finally { bundle.Close(); unpacked?.Dispose(); }
                }
                // Deliver all provisional records before verification, including consumer backpressure/mutation.
                await Flush();
                progress?.Report(new("verify", -1, assets));
                phase.Restart();
                string after = Hash(source, token);
                meter.Hash += phase.Elapsed;
                bool stable = source.Length == length && stamp == _timestamp(path) && before == after;
                evidence = evidence with { Verified = stable };
                if (!stable)
                {
                    status = V2ExtractionStatus.RevisionMismatch;
                    await Emit(new V2Diagnostic(-1, null, "fingerprint", "source-changed", "Source changed during extraction; discard all provisional records for this source."));
                }
            }
        }
        catch (Exception e) when (e is not OperationCanceledException and not ReceiverException)
        {
            status = V2ExtractionStatus.Failed;
            evidence = evidence with { Verified = false };
            await Emit(Diagnostic(-1, null, "file", "source-unreadable", e));
        }
        await Flush();
        using Process process = Process.GetCurrentProcess();
        return new(evidence, status, files, assets, new(meter.Hash, meter.Structure, meter.Names, meter.Dependencies,
            total.Elapsed, GC.GetTotalAllocatedBytes() - allocations, process.PeakWorkingSet64, maximumBatch));
    }

    private static IEnumerable<V2MetadataRecord> ReadFile(Stream source, long offset, long size, int ordinal,
        string? name, AssetsManager manager, V2ExtractionOptions options, Meter meter, CancellationToken token)
    {
        AssetsFile file = new();
        Exception? failure = null;
        Stopwatch phase = Stopwatch.StartNew();
        try
        {
            if (offset < 0 || size < 0 || offset > source.Length || size > source.Length - offset)
                throw new InvalidDataException("Invalid logical-file byte range.");
            file.Read(new AssetsFileReader(new V2ReadWindow(source, offset, size, token)));
            if (file.Header.Version is < 5 or > 22 || file.Header.FileSize > size ||
                file.Header.FileSize < file.Header.GetSize() || file.Header.DataOffset > size)
                throw new InvalidDataException("Unsupported or inconsistent serialized-file header; zero assets cannot be confirmed.");
            if (file.Metadata?.AssetInfos is null) throw new InvalidDataException("Asset table unavailable.");
        }
        catch (Exception e) when (e is not OperationCanceledException) { failure = e; }
        meter.Structure += phase.Elapsed;
        try
        {
            if (failure is not null)
            {
                yield return Diagnostic(ordinal, null, "table", "table-unreadable", failure);
                yield return new V2LogicalFile(ordinal, name, Cab(name), null, null, null, null, 0,
                    V2ExtractionStatus.Failed, V2DependencyStatus.Unavailable);
                yield break;
            }
            AssetsFileMetadata metadata = file.Metadata;
            phase.Restart();
            Dictionary<int, V2MetadataEnrichment.Field?> shapes = [];
            V2Diagnostic? classFailure = null;
            try
            {
                manager.UnloadClassDatabase();
                if ((options.ReadNames || options.ReadDependencies) && manager.ClassPackage is not null && !string.IsNullOrWhiteSpace(metadata.UnityVersion))
                    manager.LoadClassDatabaseFromPackage(metadata.UnityVersion);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            { classFailure = Diagnostic(ordinal, null, "name", "class-version-unavailable", e); }
            if (classFailure is not null) yield return classFailure;
            for (int i = 0; i < metadata.TypeTreeTypes.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                TypeTreeType type = metadata.TypeTreeTypes[i];
                V2MetadataEnrichment.Field? shape = null;
                V2Diagnostic? shapeFailure = null;
                try
                {
                    if (options.ReadNames || options.ReadDependencies) shape = V2MetadataEnrichment.Shape(type);
                    if (shape is null && manager.ClassDatabase is { } db && db.FindAssetClassByID(type.TypeId) is { } dbType)
                        shape = V2MetadataEnrichment.Shape(dbType.GetPreferredNode(false), db);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                { shapeFailure = Diagnostic(ordinal, null, "name", "type-enrichment-unavailable", e); }
                shapes[i] = shape;
                meter.Names += phase.Elapsed;
                yield return new V2AssetType(ordinal, i, type.TypeId, type.ScriptTypeIndex, shape?.Type);
                if (shapeFailure is not null) yield return shapeFailure;
                phase.Restart();
            }
            phase.Restart();
            Dictionary<long, int> counts = [];
            foreach (AssetFileInfo info in file.AssetInfos)
            {
                token.ThrowIfCancellationRequested();
                counts[info.PathId] = counts.GetValueOrDefault(info.PathId) + 1;
            }
            meter.Structure += phase.Elapsed;
            bool partial = false;
            V2DependencyStatus deps = options.ReadDependencies ? V2DependencyStatus.Complete : V2DependencyStatus.Unavailable;
            int dependencyOrdinal = 0;
            for (int i = 0; i < file.AssetInfos.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                phase.Restart();
                AssetFileInfo info = file.AssetInfos[i];
                int? typeIndex = file.Header.Version >= 16 && info.TypeIdOrIndex >= 0 && info.TypeIdOrIndex < metadata.TypeTreeTypes.Count
                    ? info.TypeIdOrIndex : null;
                int? classId = null;
                int? scriptIndex = null;
                try { classId = info.TypeId; }
                catch (Exception e) when (e is not OperationCanceledException) { /* Kept as an incomplete row below. */ }
                try { scriptIndex = info.GetScriptIndex(file); }
                catch (Exception e) when (e is not OperationCanceledException) { }
                if (typeIndex is null && classId is not null)
                {
                    try
                    {
                        int found = metadata.FindTypeTreeTypeIndexByID(classId.Value, (ushort)(scriptIndex ?? ushort.MaxValue));
                        if (found >= 0) typeIndex = found;
                    }
                    catch (Exception e) when (e is not OperationCanceledException) { /* Optional type evidence only. */ }
                }
                V2MetadataEnrichment.Field? shape = typeIndex is int index ? shapes.GetValueOrDefault(index) : null;
                long absolute = -1;
                try { absolute = info.GetAbsoluteByteOffset(file); } catch (OverflowException) { }
                bool validRange = absolute >= 0 && absolute <= size && info.ByteSize <= size - absolute;
                V2IdentityStatus identity = counts[info.PathId] > 1 ? V2IdentityStatus.Ambiguous
                    : validRange ? V2IdentityStatus.Addressable : V2IdentityStatus.Incomplete;
                meter.Structure += phase.Elapsed;
                phase.Restart();
                (string? rawName, V2NameStatus nameStatus) = validRange
                    ? V2MetadataEnrichment.Name(file, info, shape, options, token)
                    : (null, V2NameStatus.Failed);
                meter.Names += phase.Elapsed;
                yield return new V2Asset(ordinal, i, info.PathId, classId, typeIndex, scriptIndex,
                    absolute < 0 ? null : absolute, info.ByteSize, identity, rawName, nameStatus, shape?.Type);
                if (!validRange)
                {
                    partial = true;
                    yield return new V2Diagnostic(ordinal, i, "table", "invalid-byte-range", "Asset byte range exceeds logical file bounds.");
                }
                if (classId is null) yield return new V2Diagnostic(ordinal, i, "table", "class-unavailable", "Class ID could not be resolved; table row retained.");
                if (identity == V2IdentityStatus.Ambiguous)
                    yield return new V2Diagnostic(ordinal, i, "table", "duplicate-path-id", "Path ID is duplicated in this logical file; individual memo targeting is unavailable.");
                if (nameStatus is V2NameStatus.Failed or V2NameStatus.Budgeted)
                    yield return new V2Diagnostic(ordinal, i, "name", nameStatus == V2NameStatus.Budgeted ? "name-budget" : "name-failed", "Optional raw name unavailable; asset row retained.");
                if (!options.ReadDependencies || classId != 142) continue;
                phase.Restart();
                IReadOnlyList<string>? declarations = null;
                V2Diagnostic? depFailure = null;
                try
                {
                    if (validRange) declarations = V2MetadataEnrichment.BundleDependencies(file, info, shape, options, token);
                    if (declarations is null) deps = V2DependencyStatus.Partial;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                { depFailure = Diagnostic(ordinal, i, "dependency", "bundle-dependencies-unavailable", e); deps = V2DependencyStatus.Partial; }
                meter.Dependencies += phase.Elapsed;
                if (depFailure is not null) yield return depFailure;
                if (declarations is null && depFailure is null)
                    yield return new V2Diagnostic(ordinal, i, "dependency", "bundle-dependencies-unavailable", "Safe dependency layout unavailable; not a verified empty declaration list.");
                if (declarations is not null)
                    foreach (string raw in declarations)
                        yield return new V2Dependency(ordinal, "assetbundle_dependency", dependencyOrdinal++, info.PathId,
                            raw, null, null, Cab(raw), null, null);
            }
            if (options.ReadDependencies)
            {
                for (int i = 0; i < metadata.Externals.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    phase.Restart();
                    AssetsFileExternal ext = metadata.Externals[i];
                    V2Dependency record = new(ordinal, "external_file", i, null, ext.PathName,
                        ext.OriginalPathName, ext.VirtualAssetPathName, Cab(ext.PathName), V2MetadataEnrichment.GuidHex(ext.Guid), (int)ext.Type);
                    meter.Dependencies += phase.Elapsed;
                    yield return record;
                }
            }
            yield return new V2LogicalFile(ordinal, name, Cab(name), metadata.UnityVersion, file.Header.Version,
                metadata.TypeTreeEnabled, file.AssetInfos.Count, file.AssetInfos.Count,
                partial ? V2ExtractionStatus.Partial : V2ExtractionStatus.Complete, deps);
        }
        finally { if (file.Reader is not null) file.Close(); }
    }

    private static string Hash(Stream stream, CancellationToken token)
    {
        stream.Position = 0;
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[65536];
        int read;
        while ((read = stream.Read(buffer)) > 0) { token.ThrowIfCancellationRequested(); hash.AppendData(buffer, 0, read); }
        stream.Position = 0;
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private static string? Cab(string? raw) => raw?.StartsWith("CAB-", StringComparison.OrdinalIgnoreCase) == true ? raw : null;
    private static V2Diagnostic Diagnostic(int ordinal, int? row, string phase, string code, Exception e) =>
        new(ordinal, row, phase, code, $"{e.GetType().Name}: {e.Message}"[..Math.Min(256, e.GetType().Name.Length + 2 + e.Message.Length)]);
    private sealed class ReceiverException(Exception inner) : Exception("Metadata receiver failed; discard staged source.", inner);
    private sealed class ManagerScope : IDisposable
    {
        public AssetsManager Manager { get; } = new();
        public void Dispose() { Manager.UnloadAll(true); Manager.UnloadClassDatabase(); Manager.UnloadClassPackage(); }
    }
    private sealed class Meter { public TimeSpan Hash, Structure, Names, Dependencies; }
}
