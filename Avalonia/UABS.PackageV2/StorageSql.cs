using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace UABS.PackageV2;

internal sealed class StorageSql(SqliteConnection connection) : IDisposable
{
    private readonly Dictionary<string, SqliteCommand> _commands = [];
    internal object? Scalar(string sql, params object?[] values) => Command(sql, values).ExecuteScalar();
    internal long Number(string sql, params object?[] values) => Convert.ToInt64(Scalar(sql, values), CultureInfo.InvariantCulture);
    internal int Exec(string sql, params object?[] values) => Command(sql, values).ExecuteNonQuery();
    internal SqliteCommand Command(string sql, params object?[] values)
    {
        if (!_commands.TryGetValue(sql, out SqliteCommand? command))
        {
            command = connection.CreateCommand(); command.CommandText = sql;
            for (int i = 0; i < values.Length; i++) command.Parameters.AddWithValue($"@p{i}", DBNull.Value);
            _commands.Add(sql, command);
        }
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] is string text) _ = new UTF8Encoding(false, true).GetByteCount(text);
            command.Parameters[i].Value = values[i] ?? DBNull.Value;
        }
        return command;
    }
    public void Dispose() { foreach (SqliteCommand command in _commands.Values) command.Dispose(); }
}

internal static partial class StorageSchema
{
    internal static string TextProfile => $"uabs-dotnet-ordinal-v1/{Environment.Version}/{RuntimeInformation.RuntimeIdentifier}";
    internal static string Ddl { get; } = ReadDdl();
    private static string ReadDdl()
    {
        using Stream stream = typeof(StorageSchema).Assembly.GetManifestResourceStream("UABS.PackageV2.Schema.sql")!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
    internal static SqliteConnection Open(string path, bool readOnly)
    {
        SqliteConnection c = new(new SqliteConnectionStringBuilder
        { DataSource = path, Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false, ForeignKeys = true, DefaultTimeout = 5 }.ToString());
        try
        {
            c.Open();
            c.CreateFunction<string?, int>("uabs_has_text_v1", value => string.IsNullOrWhiteSpace(value) ? 0 : 1, isDeterministic: true);
            return c;
        }
        catch { c.Dispose(); throw; }
    }
    internal static byte[] Bytes(Guid value) => value.ToByteArray(bigEndian: true);
    internal static Guid Uuid(object value) => new((byte[])value, bigEndian: true);
    internal static string? AsciiKey(string? name) => name is not null && name.All(c => c is >= ' ' and <= '~')
        ? string.Concat(name.Select(c => c is >= 'a' and <= 'z' ? (char)(c - 32) : c)) : null;

    internal static string Locator(string relative, string policy, bool allowEmpty = false)
    {
        if (allowEmpty && relative.Length == 0) return "";
        bool windows = policy == "windows-lexical-v1";
        if (!windows && policy != "posix-lexical-v1") throw new InvalidDataException("Unknown path policy.");
        string path = windows ? relative.Replace('\\', '/') : relative;
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.Contains('\0') || (windows && path.Contains(':')) ||
            path.Split('/').Any(part => part is "" or "." or "..")) throw new InvalidDataException("Unsafe or noncanonical relative path.");
        return windows ? path.ToUpperInvariant() : path;
    }
    internal static void ValidateRoot(SourceRoot root)
    {
        if (root.PathPolicy == "windows-lexical-v1")
        {
            if (!Regex.IsMatch(root.OriginalBasePath, @"^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+)"))
                throw new InvalidDataException("Windows source root must be absolute.");
        }
        else if (root.PathPolicy != "posix-lexical-v1" || !root.OriginalBasePath.StartsWith('/'))
            throw new InvalidDataException("POSIX source root must be absolute with an explicit policy.");
        if (root.OriginalBasePath.Contains('\0')) throw new InvalidDataException("Invalid source root.");
    }

    // Compare schema structure and SQL tokens, not whitespace hashes. Tokens retain literal spelling/case;
    // PRAGMA signatures additionally cover FK actions, index columns/order/uniqueness and STRICT attributes.
    internal static string Signature(SqliteConnection connection)
    {
        using StorageSql sql = new(connection);
        using SqliteDataReader rows = sql.Command("SELECT type,name,tbl_name,sql FROM sqlite_schema WHERE name NOT GLOB 'sqlite_*' ORDER BY type,name").ExecuteReader();
        List<string> objects = [];
        while (rows.Read())
            objects.Add(string.Join("|", rows.GetString(0), rows.GetString(1), rows.GetString(2), Tokens(rows.GetString(3))));
        rows.Close();
        foreach (string table in new[] { "package", "source_root", "source", "logical_file", "asset_type", "asset", "dependency", "indexing_issue" })
        foreach (string pragma in new[] { $"table_xinfo('{table}')", $"foreign_key_list('{table}')", $"index_list('{table}')" })
        {
            using SqliteDataReader detail = sql.Command($"PRAGMA {pragma}").ExecuteReader();
            while (detail.Read()) objects.Add(pragma + ":" + string.Join("|", Enumerable.Range(0, detail.FieldCount).Select(i => detail.GetValue(i).ToString())));
        }
        return string.Join("\n", objects);
    }
    private static string Tokens(string sql) => string.Join(" ", SqlTokens().Matches(sql).Select(m =>
        m.Value.StartsWith('\'') ? m.Value : m.Value.ToLowerInvariant()));
    [GeneratedRegex(@"'(?:''|[^'])*'|[A-Za-z_][A-Za-z_0-9]*|[0-9]+|[^\s]", RegexOptions.CultureInvariant)]
    private static partial Regex SqlTokens();
    private static readonly Lazy<string> CanonicalSignature = new(() =>
    {
        using SqliteConnection c = new("Data Source=:memory:"); c.Open();
        using StorageSql sql = new(c); sql.Exec(Ddl);
        return Signature(c);
    });

    internal static PackageInfo Validate(SqliteConnection c, bool full, CancellationToken token)
    {
        using var phase = UABS.UnityAssets.Packages.Memos.MemoTrace.Start("full-validation");
        PackageInfo info = ReadManifest(c, token);
        using StorageSql sql = new(c);
        string check = full ? "PRAGMA integrity_check" : "PRAGMA quick_check";
        using (UABS.UnityAssets.Packages.Memos.MemoTrace.Start(full ? "integrity-check" : "quick-check"))
        using (SqliteDataReader integrity = sql.Command(check).ExecuteReader())
        { if (!integrity.Read() || integrity.GetString(0) != "ok" || integrity.Read()) throw new InvalidDataException("SQLite integrity check failed."); }
        using (UABS.UnityAssets.Packages.Memos.MemoTrace.Start("foreign-key-check"))
        using (SqliteDataReader foreign = sql.Command("PRAGMA foreign_key_check").ExecuteReader())
        { if (foreign.Read()) throw new InvalidDataException("Foreign-key integrity failed."); }
        return ValidateContents(c, sql, info, token);
    }

    // Bounded schema/manifest check. It is not a replacement for full admission;
    // only a writer's proven unchanged, same-file post-commit snapshot may reuse it.
    internal static PackageInfo ReadManifest(SqliteConnection c, CancellationToken token)
    {
        using var phase = UABS.UnityAssets.Packages.Memos.MemoTrace.Start("schema-manifest");
        token.ThrowIfCancellationRequested(); using StorageSql sql = new(c);
        if (sql.Number("PRAGMA application_id") != 0x55414232 || sql.Number("PRAGMA user_version") != 20000) throw new UnsupportedSchemaException();
        if (Signature(c) != CanonicalSignature.Value) throw new InvalidDataException("Schema objects/constraints differ from canonical V2.0.");
        if (sql.Number("SELECT COUNT(*) FROM package") != 1) throw new InvalidDataException("Exactly one package manifest required.");
        using (SqliteDataReader p = sql.Command("SELECT package_uuid,generation_uuid,display_name,catalog_revision,index_status,derived_text_profile,schema_major,schema_minor,text_contract,created_utc,indexer_version FROM package").ExecuteReader())
        {
            p.Read();
            if (p.GetInt32(6) != 2 || p.GetInt32(7) != 0 || p.GetString(8) != "dotnet-ordinal-v1") throw new UnsupportedSchemaException();
            Guid id = Uuid(p.GetValue(0)), generation = Uuid(p.GetValue(1));
            if (id == Guid.Empty || generation == Guid.Empty) throw new InvalidDataException("Empty package/generation UUID.");
            return new(id, generation, p.GetString(2), p.GetInt64(3), p.GetString(4), p.GetString(5),
                p.GetString(5) == TextProfile, 0, 0, 0, p.GetString(9), p.GetString(10), p.GetString(8), p.GetInt32(6), p.GetInt32(7));
        }
    }

    private static PackageInfo ValidateContents(SqliteConnection c, StorageSql sql, PackageInfo info, CancellationToken token)
    {
        using (UABS.UnityAssets.Packages.Memos.MemoTrace.Start("global-semantics"))
        {
            foreach (string invalid in new[]
            {
                "SELECT 1 FROM source WHERE source_uuid=zeroblob(16) OR source_revision_uuid=zeroblob(16) LIMIT 1",
                "SELECT 1 FROM asset GROUP BY file_id,path_id HAVING path_id IS NOT NULL AND COUNT(*)>1 AND SUM(identity_status<>'ambiguous')>0 LIMIT 1",
                "SELECT 1 FROM asset GROUP BY file_id HAVING MIN(table_ordinal)<>0 OR MAX(table_ordinal)<>COUNT(*)-1 LIMIT 1",
                "SELECT 1 FROM asset a JOIN asset_type t ON a.type_id=t.type_id WHERE a.class_id IS NOT NULL AND t.class_id IS NOT NULL AND a.class_id<>t.class_id LIMIT 1",
                "SELECT 1 FROM dependency d JOIN logical_file f ON f.file_id=d.file_id JOIN source s ON s.source_id=f.source_id WHERE s.dependency_status='complete' GROUP BY d.file_id,d.relation_kind HAVING MIN(d.declaration_ordinal)<>0 OR MAX(d.declaration_ordinal)<>COUNT(*)-1 LIMIT 1",
                "SELECT 1 FROM logical_file f WHERE f.index_status='complete' AND (f.declared_asset_count IS NULL OR f.declared_asset_count<>(SELECT COUNT(*) FROM asset a WHERE a.file_id=f.file_id)) LIMIT 1",
                "SELECT 1 FROM source s WHERE s.index_status='complete' AND (s.kind='unknown' OR s.content_sha256 IS NULL OR s.byte_size IS NULL OR NOT EXISTS(SELECT 1 FROM logical_file f WHERE f.source_id=s.source_id) OR EXISTS(SELECT 1 FROM logical_file f WHERE f.source_id=s.source_id AND f.index_status<>'complete')) LIMIT 1",
                "SELECT 1 FROM package p WHERE p.index_status='complete' AND EXISTS(SELECT 1 FROM source s WHERE s.index_status<>'complete') LIMIT 1"
            }) if (sql.Scalar(invalid) is not null) throw new InvalidDataException("Semantic identity/count/completeness invariant failed.");
            using (SqliteDataReader roots = sql.Command("SELECT label,original_base_path,path_policy FROM source_root").ExecuteReader())
                while (roots.Read()) { token.ThrowIfCancellationRequested(); ValidateRoot(new(roots.GetString(0), roots.GetString(1), roots.GetString(2))); }
            using (SqliteDataReader sources = sql.Command("SELECT s.relative_path,s.locator_key,s.index_directory,r.path_policy FROM source s JOIN source_root r ON r.root_id=s.root_id").ExecuteReader())
                while (sources.Read())
                {
                    token.ThrowIfCancellationRequested();
                    if (Locator(sources.GetString(0), sources.GetString(3)) != sources.GetString(1)) throw new InvalidDataException("Locator key does not match declared path policy.");
                    _ = Locator(sources.GetString(2), sources.GetString(3), true);
                }
        }
        if (info.DerivedFieldsCompatible)
        {
            using var derivedPhase = UABS.UnityAssets.Packages.Memos.MemoTrace.Start("derived-text-validation");
            using SqliteDataReader derived = sql.Command("SELECT name,name_ascii_key,memo,memo_nonblank FROM asset").ExecuteReader();
            while (derived.Read())
            {
                token.ThrowIfCancellationRequested();
                string? name = derived.IsDBNull(0) ? null : derived.GetString(0);
                string? key = derived.IsDBNull(1) ? null : derived.GetString(1);
                string? memo = derived.IsDBNull(2) ? null : derived.GetString(2);
                if (AsciiKey(name) != key || (string.IsNullOrWhiteSpace(memo) ? 0 : 1) != derived.GetInt32(3))
                    throw new InvalidDataException("Derived metadata inconsistent with accepted profile.");
            }
        }
        return info with { Sources = sql.Number("SELECT COUNT(*) FROM source"), Assets = sql.Number("SELECT COUNT(*) FROM asset"), Issues = sql.Number("SELECT COUNT(*) FROM indexing_issue") };
    }
    internal sealed class UnsupportedSchemaException : IOException;
}
