using System;
using System.Collections.Generic;
using System.Globalization;

namespace UABS.Models;

public sealed record V2AssetIdentity(Guid PackageId, Guid GenerationId, Guid SourceId, Guid SourceRevision,
    int DirectoryOrdinal, long? PathId, int TableOrdinal);
public sealed record V2AssetRow(long Cursor, V2AssetIdentity Identity, string? Name, int? ClassId,
    string? TypeName, string? Memo, long MemoRevision, string IdentityStatus, long? Size)
{
    public string NameLabel => Value(Name);
    public string TypeLabel => TypeName ?? (ClassId is int id ? $"Class {id} (unresolved)" : "Class ID unavailable");
    public string IconType => TypeName ?? "Asset";
    public string ValueStates => $"Name state: {(Name is null ? "NULL" : Name.Length == 0 ? "empty" : "value")} · Memo state: {(Memo is null ? "NULL" : Memo.Length == 0 ? "empty" : "value")}";
    public string Details => $"Read-only V2 metadata. Content preview is not available.\nPackage: {Identity.PackageId}\nGeneration: {Identity.GenerationId}\nSource: {Identity.SourceId}\nSource revision: {Identity.SourceRevision}\nOriginal directory ordinal: {Identity.DirectoryOrdinal}\nTable ordinal: {Identity.TableOrdinal}\nPath ID: {Identity.PathId?.ToString(CultureInfo.InvariantCulture) ?? "<NULL>"}\nClass ID: {ClassId?.ToString(CultureInfo.InvariantCulture) ?? "<NULL>"}\nIdentity: {IdentityStatus}\nName: {Value(Name)}\nType: {TypeLabel}\nMemo revision: {MemoRevision}\nMemo: {Value(Memo)}";
    public static string Value(string? value) => value is null ? "<NULL>" : value.Length == 0 ? "\"\" (empty)" : value;
}
public sealed record V2MetadataItem(long Cursor, string Details);
public sealed record V2BrowsePage<T>(IReadOnlyList<T> Rows, long After, bool HasMore);
public enum V2MetadataKind { SourceRoots, Sources, LogicalFiles, Dependencies, Issues }
