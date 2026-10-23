using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;

namespace UABS.PackageV2;

/// <summary>Read-only local metadata pages, not the Gate 3 Search/federation engine.</summary>
public sealed class PackageReader : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _access = new(1, 1);
    private bool _disposed;
    public PackageInfo Info { get; }
    private PackageReader(SqliteConnection connection, PackageInfo info) { _connection = connection; Info = info; }

    public static Task<AdmissionResult> OpenAsync(string path, CancellationToken token = default) => Task.Run(() =>
    {
        SqliteConnection? connection = null;
        try
        {
            token.ThrowIfCancellationRequested();
            using (FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Span<byte> signature = stackalloc byte[16]; stream.ReadExactly(signature);
                if (!signature.SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("Not a SQLite database.");
            }
            connection = StorageSchema.Open(Path.GetFullPath(path), true);
            using var interrupt = token.Register(() => SQLitePCL.raw.sqlite3_interrupt(connection.Handle));
            using StorageSql sql = new(connection); sql.Exec("BEGIN");
            PackageInfo info = StorageSchema.Validate(connection, false, token);
            sql.Exec("COMMIT");
            return new AdmissionResult(StorageStatus.Success, new PackageReader(connection, info),
                info.DerivedFieldsCompatible ? "Read-only V2 package admitted." : "Raw read-only fallback: derived profile unsupported; derived fields ignored.");
        }
        catch (Exception e)
        {
            connection?.Dispose();
            return new AdmissionResult(token.IsCancellationRequested ? StorageStatus.Cancelled : e is StorageSchema.UnsupportedSchemaException
                ? StorageStatus.UnsupportedVersion : StorageStatus.InvalidPackage, null, $"{e.GetType().Name}: {e.Message}"[..Math.Min(1024, e.GetType().Name.Length + 2 + e.Message.Length)]);
        }
    });

    public Task<IReadOnlyList<StoredAsset>> ReadAssetsAsync(long afterRowId = 0, int pageSize = 100, CancellationToken token = default) =>
        Read(token, sql =>
        {
            ValidatePage(afterRowId, pageSize);
            using SqliteDataReader reader = sql.Command("SELECT a.asset_id,s.source_uuid,s.source_revision_uuid,f.directory_ordinal,a.table_ordinal,a.path_id,a.class_id,a.type_id,t.type_name,a.byte_offset,a.byte_size,a.identity_status,a.name,a.name_status,a.memo,a.memo_revision FROM asset a JOIN logical_file f ON f.file_id=a.file_id JOIN source s ON s.source_id=f.source_id LEFT JOIN asset_type t ON t.type_id=a.type_id WHERE a.asset_id>@p0 ORDER BY a.asset_id LIMIT @p1", afterRowId, pageSize).ExecuteReader();
            List<StoredAsset> rows = [];
            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();
                long? Integer(int i) => reader.IsDBNull(i) ? null : reader.GetInt64(i);
                string? Text(int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
                rows.Add(new(reader.GetInt64(0), new(Info.PackageId, Info.GenerationId, StorageSchema.Uuid(reader.GetValue(1)),
                    StorageSchema.Uuid(reader.GetValue(2)), reader.GetInt32(3), Integer(5), reader.GetInt32(4)),
                    reader.IsDBNull(6) ? null : reader.GetInt32(6), Integer(7), Text(8), Integer(9), Integer(10),
                    reader.GetString(11), Text(12), reader.GetString(13), Text(14), reader.GetInt64(15)));
            }
            return (IReadOnlyList<StoredAsset>)rows.AsReadOnly();
        });

    public Task<IReadOnlyList<MetadataRow>> ReadMetadataAsync(MetadataTable table, long afterRowId = 0,
        int pageSize = 100, CancellationToken token = default) => Read(token, sql =>
    {
        ValidatePage(afterRowId, pageSize);
        (string name, string key, string joins, string provenance) = table switch
        {
            MetadataTable.SourceRoots => ("source_root", "root_id", "", ""),
            MetadataTable.Sources => ("source", "source_id", "", ",x.source_uuid AS provenance_source_uuid,x.source_revision_uuid AS provenance_source_revision"),
            MetadataTable.LogicalFiles => ("logical_file", "file_id", " JOIN source s ON s.source_id=x.source_id", ",s.source_uuid AS provenance_source_uuid,s.source_revision_uuid AS provenance_source_revision,x.directory_ordinal AS provenance_directory_ordinal"),
            MetadataTable.Types => ("asset_type", "type_id", " JOIN logical_file f ON f.file_id=x.file_id JOIN source s ON s.source_id=f.source_id", ",s.source_uuid AS provenance_source_uuid,s.source_revision_uuid AS provenance_source_revision,f.directory_ordinal AS provenance_directory_ordinal"),
            MetadataTable.Dependencies => ("dependency", "dependency_id", " JOIN logical_file f ON f.file_id=x.file_id JOIN source s ON s.source_id=f.source_id", ",s.source_uuid AS provenance_source_uuid,s.source_revision_uuid AS provenance_source_revision,f.directory_ordinal AS provenance_directory_ordinal"),
            MetadataTable.Issues => ("indexing_issue", "issue_id", " LEFT JOIN asset a ON a.asset_id=x.asset_id LEFT JOIN logical_file f ON f.file_id=COALESCE(x.file_id,a.file_id) LEFT JOIN source s ON s.source_id=COALESCE(x.source_id,f.source_id)", ",s.source_uuid AS provenance_source_uuid,s.source_revision_uuid AS provenance_source_revision,f.directory_ordinal AS provenance_directory_ordinal,a.table_ordinal AS provenance_table_ordinal"),
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };
        using SqliteDataReader reader = sql.Command($"SELECT x.*{provenance} FROM {name} x{joins} WHERE x.{key}>@p0 ORDER BY x.{key} LIMIT @p1", afterRowId, pageSize).ExecuteReader();
        List<MetadataRow> rows = [];
        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            Dictionary<string, object?> values = new(StringComparer.Ordinal);
            for (int i = 0; i < reader.FieldCount; i++) values.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
            values.Add("provenance_package_uuid", Info.PackageId); values.Add("provenance_generation_uuid", Info.GenerationId);
            rows.Add(new(Convert.ToInt64(values[key]), new ReadOnlyDictionary<string, object?>(values)));
        }
        return (IReadOnlyList<MetadataRow>)rows.AsReadOnly();
    });

    /// <summary>Bounded selected-occurrence evidence; no source file opened and no Search semantics changed.</summary>
    public Task<IReadOnlyDictionary<string, object?>> ReadAssetEvidenceAsync(AssetKey key, CancellationToken token = default) => Read(token, sql =>
    {
        if (key.PackageId != Info.PackageId || key.GenerationId != Info.GenerationId) throw new InvalidDataException("Asset belongs to another generation.");
        using var row = sql.Command("SELECT s.original_name,s.original_path,s.index_directory,s.index_status AS source_index_status,s.dependency_status,f.logical_name,f.cab_name,f.unity_version,f.directory_ordinal,f.index_status AS logical_index_status FROM asset a JOIN logical_file f ON f.file_id=a.file_id JOIN source s ON s.source_id=f.source_id WHERE s.source_uuid=@p0 AND s.source_revision_uuid=@p1 AND f.directory_ordinal=@p2 AND a.table_ordinal=@p3 AND a.path_id IS @p4",
            StorageSchema.Bytes(key.SourceId), StorageSchema.Bytes(key.SourceRevision), key.DirectoryOrdinal, key.TableOrdinal, key.PathId).ExecuteReader();
        if (!row.Read()) throw new InvalidDataException("Selected occurrence no longer exists.");
        Dictionary<string, object?> result = new(StringComparer.Ordinal);
        for (int i = 0; i < row.FieldCount; i++) result.Add(row.GetName(i), row.IsDBNull(i) ? null : row.GetValue(i));
        return (IReadOnlyDictionary<string, object?>)new ReadOnlyDictionary<string, object?>(result);
    });

    private Task<T> Read<T>(CancellationToken token, Func<StorageSql, T> read) => Task.Run(() =>
    {
        _access.Wait(token);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var interrupt = token.Register(() => SQLitePCL.raw.sqlite3_interrupt(_connection.Handle));
            using StorageSql sql = new(_connection);
            sql.Exec("BEGIN");
            try
            {
                using (SqliteDataReader p = sql.Command("SELECT package_uuid,generation_uuid,catalog_revision FROM package").ExecuteReader())
                {
                    if (!p.Read() || StorageSchema.Uuid(p.GetValue(0)) != Info.PackageId || StorageSchema.Uuid(p.GetValue(1)) != Info.GenerationId || p.GetInt64(2) != Info.CatalogRevision)
                        throw new InvalidDataException("Package revision changed; explicit readmission required.");
                }
                T result = read(sql); sql.Exec("COMMIT"); return result;
            }
            catch { sql.Exec("ROLLBACK"); throw; }
        }
        catch (SqliteException) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
        finally { _access.Release(); }
    }, token);

    private static void ValidatePage(long after, int size)
    { if (after < 0 || size is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(size), "Row cursor must be nonnegative; page size 1–500."); }
    public void Dispose()
    {
        _access.Wait();
        try { if (!_disposed) { _disposed = true; _connection.Dispose(); } }
        finally { _access.Release(); }
    }
}
