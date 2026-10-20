using Microsoft.Data.Sqlite;

namespace PackageV2.SqliteSpike;

public sealed record Identity(string Package, string Generation, long Source, long Serialized, long PathId);
public sealed record Row(long Id, Identity Key, int ClassId, string? Type, string? Name, string? Memo,
    long Revision, string SourcePath, string LogicalFile);
public sealed record Input(long Id, long Source, long Serialized, long PathId, int ClassId,
    string? Type, string? Name, string? Memo, string? SourcePath = null, string? LogicalName = null, string? Cab = null);
public sealed record BuildResult(double InsertMs, double IndexMs, double FtsMs);
public sealed record SaveResult(string Status, bool Committed);

public static class Store
{
    public static SqliteConnection Open(string path)
    {
        var c = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 1 }.ToString());
        c.Open();
        c.CreateCollation("ORDINAL_NOCASE", (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x, y));
        c.CreateFunction<string?, string?, bool>("contains_oi", (x, y) =>
            !string.IsNullOrEmpty(x) && y is not null && x.Contains(y, StringComparison.OrdinalIgnoreCase), true);
        Exec(c, "PRAGMA foreign_keys=ON; PRAGMA synchronous=FULL;");
        return c;
    }

    public static void Exec(SqliteConnection c, string sql, SqliteTransaction? tx = null)
    {
        using var command = c.CreateCommand(); command.Transaction = tx; command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public static T Scalar<T>(SqliteConnection c, string sql)
    {
        using var command = c.CreateCommand(); command.CommandText = sql;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    public static BuildResult Build(string path, string package, IEnumerable<Input> values, bool fts = true)
    {
        // The caller supplies a fresh file inside an invocation-owned temporary directory.
        using (var create = new SqliteConnection($"Data Source={path};Pooling=False")) create.Open();
        using var c = Open(path);
        Exec(c, """
            PRAGMA application_id=1430340179; PRAGMA user_version=2;
            CREATE TABLE package(id TEXT PRIMARY KEY,generation TEXT NOT NULL,version INTEGER NOT NULL CHECK(version=2),ascii_only INTEGER NOT NULL);
            CREATE TABLE source(id INTEGER PRIMARY KEY,path TEXT NOT NULL);
            CREATE TABLE serialized(id INTEGER PRIMARY KEY,source INTEGER NOT NULL REFERENCES source(id),name TEXT NOT NULL,cab TEXT);
            CREATE TABLE asset(id INTEGER PRIMARY KEY,serialized INTEGER NOT NULL REFERENCES serialized(id),pathid INTEGER NOT NULL,classid INTEGER NOT NULL,
                type TEXT,name TEXT,memo TEXT,revision INTEGER NOT NULL DEFAULT 0,memo_nonblank INTEGER NOT NULL,
                UNIQUE(serialized,pathid));
            """);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        using (var tx = c.BeginTransaction())
        {
            using var packageCommand = c.CreateCommand(); packageCommand.Transaction = tx;
            packageCommand.CommandText = "INSERT INTO package VALUES($p,'generation-1',2,1)";
            packageCommand.Parameters.AddWithValue("$p", package); packageCommand.ExecuteNonQuery();
            using var source = c.CreateCommand(); source.Transaction = tx;
            source.CommandText = "INSERT OR IGNORE INTO source VALUES($s,$path);INSERT OR IGNORE INTO serialized VALUES($f,$s,$logical,$cab);";
            foreach (string key in new[] { "$s", "$path", "$f", "$logical", "$cab" }) source.Parameters.Add(new SqliteParameter(key, null));
            using var asset = c.CreateCommand(); asset.Transaction = tx;
            asset.CommandText = "INSERT INTO asset(id,serialized,pathid,classid,type,name,memo,memo_nonblank) VALUES($i,$f,$p,$c,$t,$n,$m,$b)";
            foreach (string key in new[] { "$i", "$f", "$p", "$c", "$t", "$n", "$m", "$b" }) asset.Parameters.Add(new SqliteParameter(key, null));
            bool ascii = true;
            foreach (var r in values)
            {
                source.Parameters[0].Value = r.Source; source.Parameters[1].Value = r.SourcePath ?? $"X:/fixture/source-{r.Source}.bundle";
                source.Parameters[2].Value = r.Serialized; source.Parameters[3].Value = r.LogicalName ?? $"logical-{r.Serialized}";
                source.Parameters[4].Value = (object?)r.Cab ?? DBNull.Value; source.ExecuteNonQuery();
                object[] parameters = [r.Id, r.Serialized, r.PathId, r.ClassId, (object?)r.Type ?? DBNull.Value,
                    (object?)r.Name ?? DBNull.Value, (object?)r.Memo ?? DBNull.Value, !string.IsNullOrWhiteSpace(r.Memo)];
                for (int i = 0; i < parameters.Length; i++) asset.Parameters[i].Value = parameters[i];
                asset.ExecuteNonQuery();
                ascii &= (r.Name?.All(char.IsAscii) ?? true) && (r.Memo?.All(char.IsAscii) ?? true);
            }
            if (!ascii) Exec(c, "UPDATE package SET ascii_only=0", tx);
            tx.Commit();
        }
        double insert = watch.Elapsed.TotalMilliseconds; watch.Restart();
        Exec(c, "CREATE INDEX by_name ON asset(name COLLATE ORDINAL_NOCASE);CREATE INDEX by_class ON asset(classid);CREATE INDEX by_path ON asset(pathid);");
        double indexes = watch.Elapsed.TotalMilliseconds; watch.Restart();
        if (fts) Exec(c, """
            CREATE VIRTUAL TABLE ft USING fts5(name,memo,content='asset',content_rowid='id',tokenize='trigram',detail=none);
            INSERT INTO ft(ft) VALUES('rebuild');
            CREATE TRIGGER asset_update AFTER UPDATE ON asset BEGIN
              INSERT INTO ft(ft,rowid,name,memo) VALUES('delete',old.id,old.name,old.memo);
              INSERT INTO ft(rowid,name,memo) VALUES(new.id,new.name,new.memo); END;
            """);
        return new(insert, indexes, fts ? watch.Elapsed.TotalMilliseconds : 0);
    }

    public static SaveResult Save(string path, Identity key, string? expectedMemo, long revision, string? desired,
        bool rollback = false, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using (var c = Open(path))
        using (var tx = c.BeginTransaction(deferred: false))
        {
            using var cmd = c.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = """
                UPDATE asset SET memo=$memo,revision=revision+1,memo_nonblank=$nonblank
                WHERE serialized=$file AND pathid=$path AND revision=$revision AND memo IS $expected
                AND EXISTS(SELECT 1 FROM serialized s WHERE s.id=asset.serialized AND s.source=$source)
                AND EXISTS(SELECT 1 FROM package WHERE id=$package AND generation=$generation)
                """;
            foreach (var (k, v) in new (string, object?)[] { ("$memo", desired), ("$nonblank", !string.IsNullOrWhiteSpace(desired)),
                ("$file", key.Serialized), ("$path", key.PathId), ("$revision", revision), ("$expected", expectedMemo),
                ("$source", key.Source), ("$package", key.Package), ("$generation", key.Generation) })
                cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
            if (cmd.ExecuteNonQuery() != 1) return new("ConflictOrMissing", false);
            if (desired is not null && !desired.All(char.IsAscii)) Exec(c, "UPDATE package SET ascii_only=0", tx);
            token.ThrowIfCancellationRequested();
            if (rollback) { tx.Rollback(); return new("RolledBack", false); }
            tx.Commit();
        }
        // Never report verified success based only on ExecuteNonQuery/Commit.
        var found = Search.Run(path, new Query(Path: key.PathId)).SingleOrDefault(r => r.Key == key);
        return found is not null && found.Memo == desired && found.Revision == revision + 1
            ? new("Saved", true) : new("VerificationFailed", true);
    }
}
