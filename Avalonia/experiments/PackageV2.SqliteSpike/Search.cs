using Microsoft.Data.Sqlite;

namespace PackageV2.SqliteSpike;

public sealed record Query(string[]? Include = null, string[]? Exclude = null, bool Exact = false,
    int? Class = null, long? Path = null, string? Memo = null, bool MemoOnly = false);

public static class Search
{
    public static bool Matches(Row r, Query q) =>
        (q.Include is null || q.Include.Any(t => q.Exact
            ? string.Equals(r.Name, t, StringComparison.OrdinalIgnoreCase) : Contains(r.Name, t))) &&
        (q.Exclude is null || !q.Exclude.Any(t => q.Exact
            ? string.Equals(r.Name, t, StringComparison.OrdinalIgnoreCase) : Contains(r.Name, t))) &&
        (q.Class is null || r.ClassId == q.Class) && (q.Path is null || r.Key.PathId == q.Path) &&
        (q.Memo is null || Contains(r.Memo, q.Memo)) && (!q.MemoOnly || !string.IsNullOrWhiteSpace(r.Memo));

    private static bool Contains(string? value, string term) => !string.IsNullOrEmpty(value)
        && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    public static List<Row> Run(string path, Query q, bool useFts = false, int? take = null, long after = -1,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var c = Store.Open(path);
        bool ascii = Store.Scalar<long>(c, "SELECT ascii_only FROM package") == 1;
        using var registration = token.Register(() => SQLitePCL.raw.sqlite3_interrupt(c.Handle));
        try { return Read(c, "main", q, ascii, useFts, take, after, token); }
        catch (SqliteException) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
    }

    internal static List<Row> Read(SqliteConnection c, string schema, Query q, bool ascii, bool fts,
        int? take, long after, CancellationToken token)
    {
        // schema is generated internally, never user input.
        using var cmd = c.CreateCommand();
        int parameter = 0;
        string Bind(object value) { string p = "$p" + parameter++; cmd.Parameters.AddWithValue(p, value); return p; }
        string Text(string column, string value, bool exact)
        {
            string p = Bind(value);
            if (exact) return $"{column} COLLATE ORDINAL_NOCASE={p}";
            return ascii && value.All(char.IsAscii)
                ? $"instr(lower({column}),lower({p}))>0" : $"contains_oi({column},{p})";
        }
        List<string> conditions = ["a.id>" + Bind(after)];
        if (q.Include is { Length: > 0 })
        {
            conditions.Add("(" + string.Join(" OR ", q.Include.Select(t => Text("a.name", t, q.Exact))) + ")");
            if (fts && ascii && !q.Exact && q.Include.All(t => t.Length >= 3 && t.All(char.IsAsciiLetterOrDigit)))
                conditions.Add($"a.id IN(SELECT rowid FROM {schema}.ft WHERE " +
                    string.Join(" OR ", q.Include.Select(t => "name LIKE " + Bind("%" + t + "%"))) + ")");
        }
        if (q.Exclude is { Length: > 0 })
            conditions.Add("NOT(" + string.Join(" OR ", q.Exclude.Select(t => Text("coalesce(a.name,'')", t, q.Exact))) + ")");
        if (q.Class is not null) conditions.Add("a.classid=" + Bind(q.Class.Value));
        if (q.Path is not null) conditions.Add("a.pathid=" + Bind(q.Path.Value));
        if (q.Memo is not null)
        {
            conditions.Add(Text("a.memo", q.Memo, false));
            if(fts && ascii && q.Memo.Length>=3 && q.Memo.All(char.IsAsciiLetterOrDigit))
                conditions.Add($"a.id IN(SELECT rowid FROM {schema}.ft WHERE memo LIKE {Bind("%"+q.Memo+"%")})");
        }
        if (q.MemoOnly) conditions.Add("a.memo_nonblank=1");
        cmd.CommandText = $"""
            SELECT a.id,p.id,p.generation,s.id,f.id,a.pathid,a.classid,a.type,a.name,a.memo,a.revision,s.path,f.name
            FROM {schema}.asset a JOIN {schema}.serialized f ON f.id=a.serialized
            JOIN {schema}.source s ON s.id=f.source CROSS JOIN {schema}.package p
            WHERE {string.Join(" AND ", conditions)} ORDER BY a.id
            """ + (take is null ? "" : " LIMIT " + Bind(take.Value));
        List<Row> rows = [];
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            rows.Add(new Row(reader.GetInt64(0), new(reader.GetString(1), reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5)),
                reader.GetInt32(6), reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9), reader.GetInt64(10), reader.GetString(11), reader.GetString(12)));
        }
        return rows;
    }

    public static async Task<List<Row>> Federate(string[] paths, Query q, int concurrency, bool fts,
        int? perPackageTake = null, CancellationToken token = default)
    {
        using var gate = new SemaphoreSlim(concurrency);
        var batches = await Task.WhenAll(paths.Select(async path =>
        {
            await gate.WaitAsync(token).ConfigureAwait(false);
            try { return await Task.Run(() => Run(path, q, fts, perPackageTake, token: token), token).ConfigureAwait(false); }
            finally { gate.Release(); }
        }));
        token.ThrowIfCancellationRequested();
        return batches.SelectMany(r => r).OrderBy(r => r.Id).ThenBy(r => r.Key.Package, StringComparer.Ordinal).ToList();
    }

    public static List<Row> Attached(string[] paths, Query q)
    {
        using var c = new SqliteConnection("Data Source=:memory:;Pooling=False"); c.Open();
        c.CreateCollation("ORDINAL_NOCASE", (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x, y));
        c.CreateFunction<string?, string?, bool>("contains_oi", (x, y) => !string.IsNullOrEmpty(x)
            && y is not null && x.Contains(y, StringComparison.OrdinalIgnoreCase), true);
        List<Row> result = [];
        for (int i = 0; i < paths.Length; i++)
        {
            using var cmd = c.CreateCommand(); cmd.CommandText = $"ATTACH DATABASE $file AS p{i}";
            cmd.Parameters.AddWithValue("$file", paths[i]); cmd.ExecuteNonQuery();
            bool ascii=Store.Scalar<long>(c,$"SELECT ascii_only FROM p{i}.package")==1;
            result.AddRange(Read(c, "p" + i, q, ascii, true, null, -1, default));
        }
        return result.OrderBy(r => r.Id).ThenBy(r => r.Key.Package, StringComparer.Ordinal).ToList();
    }
}
