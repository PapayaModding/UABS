using System.Diagnostics;
using Microsoft.Data.Sqlite;
using UABS.UnityAssets;
using UABS.UnityAssets.Metadata;

namespace UABS.PackageV2;

public sealed class PackageBuilder(IV2MetadataExtractor extractor, Action<BuildBoundary, string>? fault = null)
{
    public Task<PackageBuildResult> BuildAsync(PackageBuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        PackageBuildRequest snapshot = request with { Roots = request.Roots.ToArray(), Sources = request.Sources.ToArray() };
        return Task.Run(() => Build(snapshot, cancellationToken));
    }

    private async Task<PackageBuildResult> Build(PackageBuildRequest request, CancellationToken token)
    {
        Stopwatch total = Stopwatch.StartNew(), phase = new();
        long allocated = GC.GetTotalAllocatedBytes();
        TimeSpan schema = default, ingestion = default, validation = default, publication = default;
        string? staging = null;
        bool published = false;
        int maxBatch = 0;
        StorageStatus status = StorageStatus.InvalidInput;
        string message = "", destination = "";
        try
        {
            token.ThrowIfCancellationRequested();
            destination = Path.GetFullPath(request.Destination);
            ValidateRequest(request, destination);
            if (File.Exists(destination)) throw new DestinationException();
            staging = Path.Combine(Path.GetDirectoryName(destination)!, $".uabs-build-{Guid.NewGuid():N}.sqlite");
            using (FileStream reserve = new(staging, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None)) { }
            using (SqliteConnection c = StorageSchema.Open(staging, false))
            using (StorageSql sql = new(c))
            using (token.Register(() => SQLitePCL.raw.sqlite3_interrupt(c.Handle)))
            {
                phase.Restart(); fault?.Invoke(BuildBoundary.BeforeSchema, staging);
                if (!string.Equals((string?)sql.Scalar("PRAGMA journal_mode=DELETE"), "delete", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("DELETE journal mode unavailable.");
                sql.Exec("PRAGMA synchronous=FULL"); sql.Exec(StorageSchema.Ddl);
                if (sql.Number("PRAGMA synchronous") != 2) throw new InvalidDataException("FULL synchronization unavailable.");
                fault?.Invoke(BuildBoundary.AfterSchema, staging);
                schema = phase.Elapsed;
                sql.Exec("BEGIN IMMEDIATE");
                sql.Exec("INSERT INTO package(singleton,package_uuid,generation_uuid,schema_major,schema_minor,display_name,created_utc,indexer_version,text_contract,derived_text_profile,index_status) VALUES(1,@p0,@p1,2,0,@p2,@p3,'uabs-gate2-v1','dotnet-ordinal-v1',@p4,'complete')",
                    StorageSchema.Bytes(request.PackageId), StorageSchema.Bytes(request.GenerationId), request.Name, DateTime.UtcNow.ToString("O"), StorageSchema.TextProfile);
                for (int i = 0; i < request.Roots.Count; i++)
                {
                    SourceRoot root = request.Roots[i];
                    sql.Exec("INSERT INTO source_root(root_id,label,original_base_path,path_policy) VALUES(@p0,@p1,@p2,@p3)", i + 1, root.Label, root.OriginalBasePath, root.PathPolicy);
                }
                phase.Restart();
                bool partial = false;
                for (int i = 0; i < request.Sources.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    BuildSource source = request.Sources[i]; long sourceId = i + 1;
                    SourceRoot root = request.Roots[source.RootIndex];
                    sql.Exec("INSERT INTO source(source_id,source_uuid,source_revision_uuid,root_id,relative_path,locator_key,original_path,original_name,index_directory,kind,index_status,dependency_status) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,'unknown','failed','unavailable')",
                        sourceId, StorageSchema.Bytes(source.Request.SourceId), StorageSchema.Bytes(source.Request.RevisionId), source.RootIndex + 1,
                        source.RelativePath, StorageSchema.Locator(source.RelativePath, root.PathPolicy), source.Request.Path,
                        Path.GetFileName(source.Request.Path), source.IndexDirectory);
                    Ingestion sink = new(sql, sourceId);
                    V2ExtractionResult extracted = await extractor.ExtractAsync(source.Request, (batch, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        if (batch.Count > 4096) throw new InvalidDataException("Extractor batch exceeds storage ingestion budget.");
                        maxBatch = Math.Max(maxBatch, batch.Count);
                        fault?.Invoke(BuildBoundary.BeforeBatch, staging);
                        foreach (V2MetadataRecord record in batch) { ct.ThrowIfCancellationRequested(); sink.Add(record); }
                        return ValueTask.CompletedTask;
                    }, cancellationToken: token).ConfigureAwait(false);
                    if (extracted.Source.SourceId != source.Request.SourceId || extracted.Source.RevisionId != source.Request.RevisionId)
                        throw new InvalidDataException("Extractor returned a different immutable source identity.");
                    if (!extracted.CommitEligible)
                    {
                        // Retain diagnostics under the failed source, but discard ALL provisional indexed metadata.
                        sql.Exec("UPDATE indexing_issue SET source_id=@p0,file_id=NULL,asset_id=NULL WHERE file_id IN(SELECT file_id FROM logical_file WHERE source_id=@p0) OR asset_id IN(SELECT a.asset_id FROM asset a JOIN logical_file f ON f.file_id=a.file_id WHERE f.source_id=@p0)", sourceId);
                        sql.Exec("DELETE FROM asset WHERE file_id IN(SELECT file_id FROM logical_file WHERE source_id=@p0)", sourceId);
                        sql.Exec("DELETE FROM asset_type WHERE file_id IN(SELECT file_id FROM logical_file WHERE source_id=@p0)", sourceId);
                        sql.Exec("DELETE FROM logical_file WHERE source_id=@p0", sourceId);
                        sql.Exec("INSERT INTO indexing_issue(source_id,phase,code,severity,message) VALUES(@p0,'fingerprint','source-rejected','error',@p1)", sourceId,
                            $"Gate 1 verdict {extracted.Status}; provisional rows discarded.");
                        partial = true;
                    }
                    else
                    {
                        if (sql.Number("SELECT COUNT(*) FROM asset a JOIN logical_file f ON f.file_id=a.file_id WHERE f.source_id=@p0", sourceId) != extracted.CapturedAssets ||
                            sink.Summaries != extracted.LogicalFiles || sink.Summaries != sink.Files)
                            throw new InvalidDataException("Extractor final counts differ from captured records/summaries.");
                        if (extracted.Source.Sha256 is not { Length: 64 } || extracted.Source.ByteSize is null)
                            throw new InvalidDataException("Verified source lacks full revision evidence.");
                        partial |= extracted.Status != V2ExtractionStatus.Complete || sink.Partial;
                    }
                    string state = !extracted.CommitEligible ? extracted.Status == V2ExtractionStatus.Unsupported ? "unsupported" : "failed"
                        : extracted.Status == V2ExtractionStatus.Complete && !sink.Partial ? "complete" : "partial";
                    sql.Exec("UPDATE source SET kind=@p0,index_status=@p1,content_sha256=@p2,byte_size=@p3,original_mtime_utc=@p4,dependency_status=@p5 WHERE source_id=@p6",
                        extracted.Source.Kind switch { UnityFileKind.Bundle => "bundle", UnityFileKind.SerializedFile => "serialized", _ => "unknown" },
                        state, extracted.CommitEligible ? Convert.FromHexString(extracted.Source.Sha256!) : null,
                        extracted.Source.ByteSize, extracted.Source.LastWriteUtc?.ToString("O"), extracted.CommitEligible ? sink.DependencyStatus : "unavailable", sourceId);
                }
                sql.Exec("UPDATE package SET index_status=@p0", partial ? "partial" : "complete");
                ingestion = phase.Elapsed;
                if (partial && !request.AcknowledgePartial) throw new PartialException();
                token.ThrowIfCancellationRequested();
                phase.Restart(); fault?.Invoke(BuildBoundary.BeforeValidation, staging);
                _ = StorageSchema.Validate(c, true, token);
                fault?.Invoke(BuildBoundary.AfterValidation, staging);
                // Revalidate after injected boundary faults; hooks cannot turn corruption into a published package.
                _ = StorageSchema.Validate(c, false, token);
                sql.Exec("COMMIT"); validation = phase.Elapsed;
            }
            phase.Restart();
            using (FileStream flush = new(staging, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) flush.Flush(flushToDisk: true);
            fault?.Invoke(BuildBoundary.BeforePublication, staging);
            token.ThrowIfCancellationRequested();
            // Same-directory, no-overwrite publication. No replacement, live-copy or two-rename fallback.
            File.Move(staging, destination, overwrite: false);
            published = true; publication = phase.Elapsed; status = StorageStatus.Success; message = "Validated package published.";
        }
        catch (OperationCanceledException) { status = StorageStatus.Cancelled; message = "Cancelled before publication; destination preserved."; }
        catch (SqliteException) when (token.IsCancellationRequested) { status = StorageStatus.Cancelled; message = "SQLite operation cancelled before publication."; }
        catch (DestinationException) { status = StorageStatus.DestinationExists; message = "Destination already exists; no overwrite authorized."; }
        catch (PartialException) { status = StorageStatus.PartialAcknowledgementRequired; message = "Partial index requires explicit caller acknowledgement."; }
        catch (InvalidDataException e) { status = StorageStatus.ValidationFailure; message = e.Message; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SqliteException or ArgumentException)
        { status = File.Exists(destination) ? StorageStatus.DestinationExists : StorageStatus.IoFailure; message = $"{e.GetType().Name}: {e.Message}"; }
        catch (Exception e) { status = StorageStatus.ExtractionFailed; message = $"{e.GetType().Name}: {e.Message}"; }
        string? retained = null;
        if (!published && staging is not null && File.Exists(staging))
        {
            // Only remove our closed staging main file. Leave recovery artifacts intact if rollback/close was uncertain.
            try
            {
                if (new[] { "-journal", "-wal", "-shm" }.Any(s => File.Exists(staging + s))) retained = staging;
                else File.Delete(staging);
            }
            catch (IOException) { retained = staging; }
            catch (UnauthorizedAccessException) { retained = staging; }
        }
        using Process process = Process.GetCurrentProcess();
        return new(status, published ? destination : null, message[..Math.Min(message.Length, 1024)],
            new(schema, ingestion, validation, publication, total.Elapsed, published ? new FileInfo(destination).Length : 0,
                GC.GetTotalAllocatedBytes() - allocated, process.PeakWorkingSet64, maxBatch), retained);
    }

    private static void ValidateRequest(PackageBuildRequest request, string destination)
    {
        if (request.PackageId == Guid.Empty || request.GenerationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidDataException("Assigned package/generation UUIDs and package name required.");
        if (!destination.EndsWith(".uabs.sqlite", StringComparison.OrdinalIgnoreCase) || !Directory.Exists(Path.GetDirectoryName(destination)) || destination.StartsWith("\\\\"))
            throw new InvalidDataException("An existing local destination directory and .uabs.sqlite filename are required.");
        foreach (SourceRoot root in request.Roots) StorageSchema.ValidateRoot(root);
        HashSet<Guid> identities = [];
        HashSet<string> paths = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (BuildSource source in request.Sources)
        {
            if (source.RootIndex < 0 || source.RootIndex >= request.Roots.Count || source.Request.SourceId == Guid.Empty ||
                source.Request.RevisionId == Guid.Empty || !identities.Add(source.Request.SourceId) || !paths.Add(Path.GetFullPath(source.Request.Path)))
                throw new InvalidDataException("Invalid/duplicate source occurrence or root.");
            SourceRoot root = request.Roots[source.RootIndex];
            _ = StorageSchema.Locator(source.RelativePath, root.PathPolicy);
            _ = StorageSchema.Locator(source.IndexDirectory, root.PathPolicy, true);
            if ((OperatingSystem.IsWindows() && root.PathPolicy == "windows-lexical-v1") || (!OperatingSystem.IsWindows() && root.PathPolicy == "posix-lexical-v1"))
            {
                string resolved = Path.GetFullPath(Path.Combine(root.OriginalBasePath, source.RelativePath));
                if (!(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).Equals(resolved, Path.GetFullPath(source.Request.Path)))
                    throw new InvalidDataException("Source root/relative locator does not identify the requested source path.");
            }
        }
    }
    private sealed class DestinationException : IOException;
    private sealed class PartialException : IOException;

    private sealed class Ingestion(StorageSql sql, long sourceId)
    {
        private readonly Dictionary<int, long> _files = [];
        private readonly HashSet<int> _summaries = [];
        private readonly HashSet<(long File, int Local)> _declaredTypes = [];
        private readonly Dictionary<(long File, int Local), long> _types = [];
        private int _completeDependencies, _unavailableDependencies;
        public int Files => _files.Count;
        public int Summaries => _summaries.Count;
        public bool Partial { get; private set; }
        public string DependencyStatus { get; private set; } = "complete";
        private long FileId(int ordinal)
        {
            if (ordinal < 0) throw new InvalidDataException("Negative logical ordinal on indexed metadata.");
            if (!_files.TryGetValue(ordinal, out long id))
            {
                sql.Exec("INSERT INTO logical_file(source_id,directory_ordinal,index_status) VALUES(@p0,@p1,'failed')", sourceId, ordinal);
                id = sql.Number("SELECT last_insert_rowid()"); _files.Add(ordinal, id);
            }
            return id;
        }
        private long TypeId(long file, int local, int? classId, string? name, int? script)
        {
            if (_types.TryGetValue((file, local), out long existing)) return existing;
            sql.Exec("INSERT INTO asset_type(file_id,local_type_index,class_id,script_type_index,type_name,resolution_status) VALUES(@p0,@p1,@p2,@p3,@p4,@p5) ON CONFLICT(file_id,local_type_index) DO UPDATE SET class_id=COALESCE(excluded.class_id,class_id),script_type_index=COALESCE(excluded.script_type_index,script_type_index),type_name=COALESCE(excluded.type_name,type_name),resolution_status=CASE WHEN excluded.type_name IS NULL THEN resolution_status ELSE 'resolved' END",
                file, local, classId, script, name, name is null ? "unavailable" : "resolved");
            long id = sql.Number("SELECT type_id FROM asset_type WHERE file_id=@p0 AND local_type_index=@p1", file, local);
            _types.Add((file, local), id); return id;
        }
        public void Add(V2MetadataRecord record)
        {
            if (record is V2Diagnostic diagnostic)
            {
                long? file = diagnostic.DirectoryOrdinal < 0 ? null : FileId(diagnostic.DirectoryOrdinal);
                object? asset = diagnostic.TableOrdinal is int row && file is not null
                    ? sql.Scalar("SELECT asset_id FROM asset WHERE file_id=@p0 AND table_ordinal=@p1", file, row) : null;
                sql.Exec("INSERT INTO indexing_issue(source_id,file_id,asset_id,phase,code,severity,message) VALUES(@p0,@p1,@p2,@p3,@p4,'warning',@p5)",
                    file is null ? sourceId : null, asset is null ? file : null, asset, diagnostic.Phase, diagnostic.Code,
                    diagnostic.Message[..Math.Min(diagnostic.Message.Length, 1024)]);
                return;
            }
            long id = FileId(record.DirectoryOrdinal);
            switch (record)
            {
                case V2LogicalFile file:
                    if (!_summaries.Add(file.DirectoryOrdinal)) throw new InvalidDataException("Duplicate logical-file summary.");
                    long captured = sql.Number("SELECT COUNT(*) FROM asset WHERE file_id=@p0", id);
                    if (captured != file.CapturedAssetCount || (file.Status == V2ExtractionStatus.Complete && captured != file.DeclaredAssetCount))
                        throw new InvalidDataException("Declared/captured asset count mismatch.");
                    Partial |= file.Status != V2ExtractionStatus.Complete;
                    if (file.DependencyStatus == V2DependencyStatus.Complete) _completeDependencies++;
                    if (file.DependencyStatus == V2DependencyStatus.Unavailable) _unavailableDependencies++;
                    DependencyStatus = _completeDependencies == Summaries ? "complete" : _unavailableDependencies == Summaries ? "unavailable" : "partial";
                    if (file.DependencyStatus != V2DependencyStatus.Complete)
                        sql.Exec("INSERT INTO indexing_issue(file_id,phase,code,severity,message) VALUES(@p0,'dependency','dependency-enrichment-status','warning',@p1)", id,
                            $"Logical-file dependency metadata: {file.DependencyStatus}; independent of asset coverage.");
                    sql.Exec("UPDATE logical_file SET logical_name=@p0,cab_name=@p1,unity_version=@p2,serialized_version=@p3,has_type_tree=@p4,declared_asset_count=@p5,index_status=@p6 WHERE file_id=@p7",
                        file.Name, file.CabEvidence, file.UnityVersion, file.SerializedVersion, file.HasTypeTree, file.DeclaredAssetCount,
                        file.Status == V2ExtractionStatus.Complete ? "complete" : file.Status == V2ExtractionStatus.Partial ? "partial" : "failed", id);
                    break;
                case V2AssetType type:
                    if (!_declaredTypes.Add((id, type.LocalTypeIndex))) throw new InvalidDataException("Duplicate local type declaration.");
                    long declaredType = TypeId(id, type.LocalTypeIndex, type.ClassId, type.TypeName, type.ScriptTypeIndex);
                    sql.Exec("UPDATE asset_type SET class_id=@p0,script_type_index=@p1,type_name=@p2,resolution_status=@p3 WHERE type_id=@p4",
                        type.ClassId, type.ScriptTypeIndex, type.TypeName, type.TypeName is null ? "unavailable" : "resolved", declaredType);
                    break;
                case V2Asset asset:
                    long? typeId = asset.LocalTypeIndex is int local ? TypeId(id, local, asset.ClassId, asset.TypeName, asset.ScriptTypeIndex) : null;
                    if (typeId is null && (asset.TypeName is not null || asset.ScriptTypeIndex is not null))
                    {
                        sql.Exec("INSERT INTO asset_type(file_id,local_type_index,class_id,script_type_index,type_name,resolution_status) VALUES(@p0,NULL,@p1,@p2,@p3,@p4)",
                            id, asset.ClassId, asset.ScriptTypeIndex, asset.TypeName, asset.TypeName is null ? "unavailable" : "resolved");
                        typeId = sql.Number("SELECT last_insert_rowid()");
                    }
                    string identity = asset.IdentityStatus.ToString().ToLowerInvariant();
                    if (asset.PathId is long path && sql.Number("SELECT COUNT(*) FROM asset WHERE file_id=@p0 AND path_id=@p1", id, path) > 0)
                    {
                        sql.Exec("UPDATE asset SET identity_status='ambiguous' WHERE file_id=@p0 AND path_id=@p1", id, path);
                        identity = "ambiguous";
                    }
                    sql.Exec("INSERT INTO asset(file_id,table_ordinal,path_id,class_id,type_id,byte_offset,byte_size,identity_status,name,name_status,name_ascii_key) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)",
                        id, asset.TableOrdinal, asset.PathId, asset.ClassId, typeId, asset.ByteOffset, asset.ByteSize, identity,
                        asset.Name, asset.NameStatus.ToString().ToLowerInvariant(), StorageSchema.AsciiKey(asset.Name));
                    break;
                case V2Dependency dependency:
                    // External Path can be parser-normalized; never substitute it for serialized evidence.
                    if (dependency.Kind == "external_file" && dependency.OriginalPath is null && dependency.Path is not null)
                        throw new InvalidDataException("External dependency original serialized path evidence is unavailable; normalized path cannot replace it.");
                    string? rawPath = dependency.Kind == "external_file" ? dependency.OriginalPath : dependency.Path;
                    sql.Exec("INSERT INTO dependency(file_id,relation_kind,declaration_ordinal,declaring_asset_path_id,raw_path,raw_virtual_path,raw_cab,guid_bytes,external_type) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8)",
                        id, dependency.Kind, dependency.DeclarationOrdinal, dependency.DeclaringAssetPathId, rawPath, dependency.VirtualPath,
                        dependency.CabEvidence, dependency.GuidHex is null ? null : Convert.FromHexString(dependency.GuidHex), dependency.ExternalType);
                    break;
                default: throw new InvalidDataException("Unknown extraction record.");
            }
        }
    }
}
