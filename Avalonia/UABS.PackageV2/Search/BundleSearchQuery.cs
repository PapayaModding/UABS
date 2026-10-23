using Microsoft.Data.Sqlite;
using UABS.UnityAssets.Packages.Search;

namespace UABS.PackageV2.Search;

internal static class BundleSearchQuery
{
    // One outer source row: all matching/witness/exclusion work precedes keyset ORDER/LIMIT.
    internal static string Branch(SqliteCommand command, SearchSession session,
        string packageName, string packagePath, string sourceKey)
    {
        BundleSearchRequest r = session.Bundle!;
        string Param(object value)
        {
            string name = "@q" + command.Parameters.Count;
            command.Parameters.AddWithValue(name, value); return name;
        }
        const string children = " FROM asset a JOIN logical_file f ON f.file_id=a.file_id "
            + "LEFT JOIN asset_type t ON t.type_id=a.type_id WHERE f.source_id=s.source_id AND ";
        string Any(string predicate) => "EXISTS(SELECT 1" + children + predicate + ")";
        string Match(string value, PackageSearchFields field) => r.Fields.HasFlag(field) && session.Include.Length > 0
            ? $"uabs_match({value},{(int)field})" : "0";
        string AssetMatch(PackageSearchFields field, string value) => r.Fields.HasFlag(field) && session.Include.Length > 0
            ? $"CASE WHEN {Any($"uabs_match({value},{(int)field})<>0")} THEN {(int)field} ELSE 0 END" : "0";
        string categories = $"({Match("s.original_name", PackageSearchFields.SourceName)}|"
            + AssetMatch(PackageSearchFields.AssetName, "a.name") + "|" + AssetMatch(PackageSearchFields.Memo, "a.memo")
            + (r.PathId is null ? ")" : $"|{(int)PackageSearchMatchCategory.PathId})");
        List<string> gates = ["s.kind='bundle'", "s.index_status IN('complete','partial')",
            "s.content_sha256 IS NOT NULL", "s.byte_size IS NOT NULL"];
        if (r.PathId is long path) gates.Add(Any("a.path_id=" + Param(path)));
        if (r.ClassId is int cls) gates.Add(Any("a.class_id=" + Param(cls)));
        if (r.TypeName is not null) gates.Add(Any("uabs_type(t.type_name)<>0"));
        if (r.RequireNonEmptyMemo) gates.Add(Any("uabs_has_text_v1(a.memo)=1"));
        // With Path ID plus keywords, retain text as an independent mandatory OR-group.
        if (r.PathId is not null && session.Include.Length > 0)
            gates.Add($"({Match("s.original_name", PackageSearchFields.SourceName)}|"
                + AssetMatch(PackageSearchFields.AssetName, "a.name") + "|" + AssetMatch(PackageSearchFields.Memo, "a.memo") + ")<>0");
        string excluded = "0";
        if (session.Exclude.Length > 0)
        {
            var fields = r.ExclusionFields!.Value;
            string name = fields.HasFlag(PackageSearchFields.AssetName) ? "uabs_exclude(a.name)" : "0";
            string memo = fields.HasFlag(PackageSearchFields.Memo) ? "uabs_exclude(a.memo)" : "0";
            // Deliberately independent of positive/gate witnesses: any child vetoes the source.
            excluded = Any($"({name}|{memo})<>0");
        }
        return $"SELECT 1 kind,{categories} categories,({excluded}) excluded,{packageName} package_name,{packagePath} package_path,"
            + "s.index_directory,s.original_name source_name,s.original_path source_path,NULL asset_name,NULL path_id,"
            + "NULL class_id,NULL type_name,s.source_uuid,s.source_revision_uuid,NULL directory_ordinal,NULL table_ordinal,"
            + $"0 memo_revision,s.index_status identity_status,0 asset_id,{sourceKey} row_key,NULL ascii_key FROM source s WHERE "
            + string.Join(" AND ", gates);
    }
}
