using Microsoft.Data.Sqlite;
using UABS.UnityAssets.Packages.Search;

namespace UABS.PackageV2.Search;

internal static class SearchQuery
{
    internal enum Partition { All, Ascii, Uncovered }
    internal static IEnumerable<SqliteCommand> PageCommands(SqliteConnection c, SearchSession session, Registration registration,
        SearchResult? after, int limit, bool indexedAscii)
    {
        // Only exact source-sort constants let asset-name order follow the existing
        // B-tree. Other sorts, mixed source paths and incompatible profiles fall back.
        bool safe = indexedAscii && registration.Value.Package.DerivedFieldsCompatible
            && session.Request.ResultKinds == PackageSearchResultKinds.Asset
            && session.Request.SortBy == PackageSearchSortField.PackageName
            // Do not force the name index over selective identity/class/memo plans.
            && session.Request.PathId is null && session.Request.AssetIdentity is null
            && session.Request.ClassId is null && session.Request.TypeName is null
            && !session.Request.RequireNonEmptyMemo
            && registration.ConstantSortValues.ContainsKey("index_directory")
            && registration.ConstantSortValues.ContainsKey("source_path");
        if (!safe) { yield return Command(c, session, registration, after, limit, false); yield break; }
        // These disjoint partitions cover ALL rows, including NULL/control/Unicode
        // names. Each top K+1 is sufficient for their bounded global merge.
        yield return Command(c, session, registration, after, limit, false, Partition.Ascii);
        yield return Command(c, session, registration, after, limit, false, Partition.Uncovered);
    }
    internal static string Key(SearchRowKey key) => key.PackageId.ToString("N").ToUpperInvariant() + "|"
        + key.GenerationId.ToString("N").ToUpperInvariant() + "|" + key.SourceId?.ToString("N").ToUpperInvariant() + "|"
        + key.SourceRevision?.ToString("N").ToUpperInvariant() + "|"
        + key.DirectoryOrdinal?.ToString("D10", System.Globalization.CultureInfo.InvariantCulture) + "|"
        + key.TableOrdinal?.ToString("D10", System.Globalization.CultureInfo.InvariantCulture);

    internal static int Text(string? a, string? b)
    {
        int c = StringComparer.OrdinalIgnoreCase.Compare(a, b);
        return c != 0 ? c : StringComparer.Ordinal.Compare(a, b);
    }
    internal static object? Primary(SearchResult row, PackageSearchSortField field) => field switch {
        PackageSearchSortField.IndexDirectory => row.IndexDirectory, PackageSearchSortField.SourceName => row.SourceName,
        PackageSearchSortField.SourcePath => row.SourcePath, PackageSearchSortField.AssetName => row.AssetName,
        PackageSearchSortField.PathId => row.PathId, PackageSearchSortField.MatchCategory => (long)row.MatchCategories,
        _ => row.PackageName };
    private static int Value(object? a, object? b) => a is string || b is string ? Text((string?)a, (string?)b)
        : Nullable.Compare<long>(a is null ? null : Convert.ToInt64(a), b is null ? null : Convert.ToInt64(b));
    internal static int Compare(SearchResult a, SearchResult b, SearchRequest request)
    {
        int c = Value(Primary(a, request.SortBy), Primary(b, request.SortBy));
        if (c != 0) return request.SortDirection == PackageSearchSortDirection.Descending ? -Math.Sign(c) : c;
        c = Text(a.PackageName, b.PackageName); if (c != 0) return c;
        c = Text(a.PackagePath, b.PackagePath); if (c != 0) return c;
        c = Text(a.IndexDirectory, b.IndexDirectory); if (c != 0) return c;
        c = Text(a.SourcePath, b.SourcePath); if (c != 0) return c;
        c = Text(a.AssetName, b.AssetName); if (c != 0) return c;
        c = Nullable.Compare(a.PathId, b.PathId); if (c != 0) return c;
        c = a.Key.Kind.CompareTo(b.Key.Kind); if (c != 0) return c;
        return StringComparer.Ordinal.Compare(Key(a.Key), Key(b.Key));
    }

    internal static SqliteCommand Command(SqliteConnection connection, SearchSession session, Registration registration,
        SearchResult? after, int limit, bool count, Partition partition = Partition.All)
    {
        using var planning = SearchTrace.Start("cursor-planning");
        SearchRequest r = session.Request;
        SearchRegistration reg = registration.Value;
        SqliteCommand command = connection.CreateCommand();
        string Param(object? value)
        { string name = "@q" + command.Parameters.Count; command.Parameters.AddWithValue(name, value ?? DBNull.Value); return name; }
        string packageName = Param(reg.Package.Name), packagePath = Param(reg.Path);
        string prefix = Param(Key(new(reg.Package.PackageId, reg.Package.GenerationId, PackageSearchResultKind.Package, null, null, null, null)));
        // Prefix ends in the four empty source/revision/ordinal slots; construct actual keys explicitly below.
        string baseKey = Param(reg.Package.PackageId.ToString("N").ToUpperInvariant() + "|" + reg.Package.GenerationId.ToString("N").ToUpperInvariant() + "|");
        string Match(string expression, PackageSearchFields field) => session.Include.Length > 0 && r.Fields.HasFlag(field)
            ? $"uabs_match({expression},{(int)field})" : "0";
        string Exclude(string expression, PackageSearchFields field) => session.Exclude.Length > 0 && r.Fields.HasFlag(field) ? $"uabs_exclude({expression})" : "0";
        string SourceKey = $"{baseKey}||hex(s.source_uuid)||'|'||hex(s.source_revision_uuid)||'||'";
        string AssetKeySql = $"{baseKey}||hex(s.source_uuid)||'|'||hex(s.source_revision_uuid)||'|'||printf('%010d',f.directory_ordinal)||'|'||printf('%010d',a.table_ordinal)";
        List<string> branches = [];
        if (session.Bundle is not null)
            branches.Add(BundleSearchQuery.Branch(command, session, packageName, packagePath, SourceKey));
        if (session.Bundle is null && r.PathId is null && r.AssetIdentity is null && r.ResultKinds.HasFlag(PackageSearchResultKinds.Package))
            branches.Add($"SELECT 0 kind,({Match(packageName, PackageSearchFields.PackageName)}|{Match(packagePath, PackageSearchFields.PackagePath)}) categories,"
                + $"({Exclude(packageName, PackageSearchFields.PackageName)}|{Exclude(packagePath, PackageSearchFields.PackagePath)}) excluded,"
                + $"{packageName} package_name,{packagePath} package_path,NULL index_directory,NULL source_name,NULL source_path,NULL asset_name,NULL path_id,NULL class_id,NULL type_name,NULL source_uuid,NULL source_revision,NULL directory_ordinal,NULL table_ordinal,0 memo_revision,NULL identity_status,0 asset_id,{prefix} row_key,NULL ascii_key");
        if (session.Bundle is null && r.PathId is null && r.AssetIdentity is null && r.ResultKinds.HasFlag(PackageSearchResultKinds.SourceFile))
        {
            string[] expressions = ["s.index_directory", "s.original_name", "s.original_path"];
            PackageSearchFields[] fields = [PackageSearchFields.IndexDirectory, PackageSearchFields.SourceName, PackageSearchFields.SourcePath];
            List<string> matches = [], excluded = [];
            for (int i = 0; i < expressions.Length; i++) { matches.Add(Match(expressions[i], fields[i])); excluded.Add(Exclude(expressions[i], fields[i])); }
            if (r.Fields.HasFlag(PackageSearchFields.CabCode))
            {
                matches.Add($"CASE WHEN EXISTS(SELECT 1 FROM logical_file f WHERE f.source_id=s.source_id AND uabs_match(f.cab_name,32)<>0) THEN 32 ELSE 0 END");
                if (session.Exclude.Length > 0) excluded.Add("EXISTS(SELECT 1 FROM logical_file f WHERE f.source_id=s.source_id AND uabs_exclude(f.cab_name)<>0)");
            }
            if (r.Fields.HasFlag(PackageSearchFields.Dependency))
            {
                matches.Add("CASE WHEN EXISTS(SELECT 1 FROM dependency d JOIN logical_file f ON f.file_id=d.file_id WHERE f.source_id=s.source_id AND (uabs_match(d.raw_path,256)|uabs_match(d.raw_virtual_path,256)|uabs_match(d.raw_cab,256))<>0) THEN 256 ELSE 0 END");
                if (session.Exclude.Length > 0) excluded.Add("EXISTS(SELECT 1 FROM dependency d JOIN logical_file f ON f.file_id=d.file_id WHERE f.source_id=s.source_id AND (uabs_exclude(d.raw_path)|uabs_exclude(d.raw_virtual_path)|uabs_exclude(d.raw_cab))<>0)");
            }
            branches.Add($"SELECT 1,({string.Join('|', matches)}),({string.Join('|', excluded)}),{packageName},{packagePath},s.index_directory,s.original_name,s.original_path,NULL,NULL,NULL,NULL,s.source_uuid,s.source_revision_uuid,NULL,NULL,0,NULL,0,{SourceKey},NULL FROM source s");
        }
        if (session.Bundle is null && r.ResultKinds.HasFlag(PackageSearchResultKinds.Asset))
        {
            List<string> gates = ["1=1"];
            if (partition != Partition.All)
                gates.Add(partition == Partition.Ascii ? "a.name_ascii_key IS NOT NULL" : "a.name_ascii_key IS NULL");
            if (partition == Partition.Ascii && after is not null
                && after.PackageName == reg.Package.Name && after.PackagePath == reg.Path
                && after.IndexDirectory == registration.ConstantSortValues["index_directory"]
                && after.SourcePath == registration.ConstantSortValues["source_path"]
                && StorageSchema.AsciiKey(after.AssetName) is { } lower)
                gates.Add("a.name_ascii_key>=" + Param(lower)); // inclusive; original case/path/ties remain authoritative below
            if (r.PathId is long pathId) gates.Add("a.path_id=" + Param(pathId));
            if (r.ClassId is int classId) gates.Add("a.class_id=" + Param(classId));
            if (r.TypeName is not null) gates.Add("uabs_type(t.type_name)<>0");
            if (r.RequireNonEmptyMemo) gates.Add(reg.Package.DerivedFieldsCompatible ? "a.memo_nonblank=1" : "uabs_has_text_v1(a.memo)=1");
            if (r.AssetIdentity is AssetKey identity)
            {
                gates.Add(reg.Package.PackageId == identity.PackageId && reg.Package.GenerationId == identity.GenerationId ? "1=1" : "0=1");
                gates.Add("s.source_uuid=" + Param(StorageSchema.Bytes(identity.SourceId)));
                gates.Add("s.source_revision_uuid=" + Param(StorageSchema.Bytes(identity.SourceRevision)));
                gates.Add("f.directory_ordinal=" + Param(identity.DirectoryOrdinal));
                gates.Add("a.table_ordinal=" + Param(identity.TableOrdinal));
                gates.Add("a.path_id IS " + Param(identity.PathId));
            }
            // ASCII partition lookup UNION-equivalent OR NULL includes every uncovered Unicode/control row.
            if (reg.Package.DerivedFieldsCompatible && r.ExactText && r.Fields == PackageSearchFields.AssetName
                && session.Include.Length > 0 && session.Include.All(t => StorageSchema.AsciiKey(t) is not null))
                gates.Add("(a.name_ascii_key IN(" + string.Join(',', session.Include.Select(t => Param(StorageSchema.AsciiKey(t)))) + ") OR a.name_ascii_key IS NULL)");
            int pathCategory = r.PathId is null && r.AssetIdentity is null ? 0 : (int)PackageSearchMatchCategory.PathId;
            branches.Add($"SELECT 2,({Match("a.name", PackageSearchFields.AssetName)}|{Match("a.memo", PackageSearchFields.Memo)}|{pathCategory}),"
                + $"({Exclude("a.name", PackageSearchFields.AssetName)}|{Exclude("a.memo", PackageSearchFields.Memo)}),{packageName},{packagePath},s.index_directory,s.original_name,s.original_path,a.name,a.path_id,a.class_id,t.type_name,s.source_uuid,s.source_revision_uuid,f.directory_ordinal,a.table_ordinal,a.memo_revision,a.identity_status,a.asset_id,{AssetKeySql},a.name_ascii_key"
                + " FROM asset a" + (partition == Partition.Ascii ? " INDEXED BY ix_asset_ascii_name" : "")
                + " JOIN logical_file f ON f.file_id=a.file_id JOIN source s ON s.source_id=f.source_id LEFT JOIN asset_type t ON t.type_id=a.type_id WHERE " + string.Join(" AND ", gates));
        }
        if (branches.Count == 0) throw new InvalidOperationException("Empty result scope.");
        string sql = "WITH candidates(kind,categories,excluded,package_name,package_path,index_directory,source_name,source_path,asset_name,path_id,class_id,type_name,source_uuid,source_revision,directory_ordinal,table_ordinal,memo_revision,identity_status,asset_id,row_key,ascii_key) AS("
            + string.Join(" UNION ALL ", branches) + ") SELECT " + (count ? "COUNT(*)" : "*")
            + " FROM candidates WHERE excluded=0 AND categories<>0";
        // Without a numeric/identity match category, an asset's nonzero categories already prove
        // name/memo matching. Re-evaluating the bitmask repeats managed UDF/string conversions.
        if (session.Include.Length > 0 && (r.PathId is not null || r.AssetIdentity is not null))
            sql += " AND (kind<>2 OR (categories & 192)<>0)";
        if (!count)
        {
            string primary = r.SortBy switch {
                PackageSearchSortField.IndexDirectory => "index_directory", PackageSearchSortField.SourceName => "source_name",
                PackageSearchSortField.SourcePath => "source_path", PackageSearchSortField.AssetName => "asset_name",
                PackageSearchSortField.PathId => "path_id", PackageSearchSortField.MatchCategory => "categories", _ => "package_name" };
            bool numeric = r.SortBy is PackageSearchSortField.PathId or PackageSearchSortField.MatchCategory;
            List<(string Sql, object? Value, bool Desc)> keys = [(primary + (numeric ? "" : " COLLATE uabs_order"), after is null ? null : Primary(after, r.SortBy), r.SortDirection == PackageSearchSortDirection.Descending)];
            keys.AddRange(new[] { ("package_name COLLATE uabs_order", (object?)after?.PackageName, false),
                ("package_path COLLATE uabs_order", after?.PackagePath, false), ("index_directory COLLATE uabs_order", after?.IndexDirectory, false),
                ("source_path COLLATE uabs_order", after?.SourcePath, false), ("asset_name COLLATE uabs_order", after?.AssetName, false),
                ("path_id", after?.PathId, false), ("kind", after is null ? null : (object)(long)after.Key.Kind, false),
                ("row_key COLLATE BINARY", after is null ? null : Key(after.Key), false) });
            bool packageRows = r.PathId is null && r.AssetIdentity is null && r.ResultKinds.HasFlag(PackageSearchResultKinds.Package);
            bool TryConstant(string expression, out object? constant)
            {
                string column = expression.Split(' ')[0];
                if (column == "package_name") { constant = reg.Package.Name; return true; }
                if (column == "package_path") { constant = reg.Path; return true; }
                if (!packageRows && registration.ConstantSortValues.TryGetValue(column, out string? value))
                { constant = value; return true; }
                constant = null; return false;
            }
            if (after is not null)
            {
                List<string> disjunction = [], equal = [];
                foreach (var key in keys)
                {
                    if (TryConstant(key.Sql, out object? constant))
                    {
                        int comparison = Value(constant, key.Value);
                        if (comparison == 0) continue;
                        if (key.Desc ? comparison < 0 : comparison > 0)
                            disjunction.Add(equal.Count == 0 ? "1=1" : "(" + string.Join(" AND ", equal) + ")");
                        break; // this constant is unequal: no deeper lexicographic equality is possible
                    }
                    string parameter = Param(key.Value);
                    string later = key.Desc ? $"(({key.Sql} IS NULL AND {parameter} IS NOT NULL) OR {key.Sql} < {parameter})"
                        : $"(({parameter} IS NULL AND {key.Sql} IS NOT NULL) OR {key.Sql} > {parameter})";
                    disjunction.Add("(" + string.Join(" AND ", equal.Append(later)) + ")");
                    equal.Add($"({key.Sql} IS {parameter})");
                }
                sql += " AND (" + (disjunction.Count == 0 ? "0=1" : string.Join(" OR ", disjunction)) + ")";
            }
            // Package name/path are constants within this database. Keep them in global continuation
            // predicates, but do not invoke managed collations on constants during the local sort.
            var localKeys = keys.Where(k => !TryConstant(k.Sql, out _)).DistinctBy(k => k.Sql);
            // Printable ASCII: upper key BINARY then raw BINARY is exactly .NET
            // OrdinalIgnoreCase-then-Ordinal. No claim is made for uncovered text.
            sql += " ORDER BY " + (partition == Partition.Ascii
                ? "ascii_key COLLATE BINARY,asset_name COLLATE BINARY,path_id,kind,row_key COLLATE BINARY"
                : string.Join(',', localKeys.Select(k => k.Sql + (k.Desc ? " DESC" : " ASC")))) + " LIMIT " + Param(limit);
        }
        command.CommandText = sql;
        return command;
    }

    internal static SearchResult Row(SqliteDataReader reader, Registration registration)
    {
        string? TextAt(int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
        long? Number(int index) => reader.IsDBNull(index) ? null : reader.GetInt64(index);
        Guid? Uuid(int index) => reader.IsDBNull(index) ? null : StorageSchema.Uuid(reader.GetValue(index));
        PackageInfo p = registration.Value.Package;
        return new(new(p.PackageId, p.GenerationId, (PackageSearchResultKind)reader.GetInt32(0), Uuid(12), Uuid(13),
            Number(14) is long directory ? checked((int)directory) : null, Number(15) is long table ? checked((int)table) : null),
            (PackageSearchMatchCategory)reader.GetInt32(1), reader.GetString(3), reader.GetString(4), TextAt(5), TextAt(6), TextAt(7),
            TextAt(8), Number(9), Number(10) is long cls ? checked((int)cls) : null, TextAt(11), null, reader.GetInt64(16), TextAt(17));
    }
}
