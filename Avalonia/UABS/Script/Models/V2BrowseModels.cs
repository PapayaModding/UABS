using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;

namespace UABS.Models;

public sealed record V2AssetIdentity(Guid PackageId, Guid GenerationId, Guid SourceId, Guid SourceRevision,
    int DirectoryOrdinal, long? PathId, int TableOrdinal);
public sealed record V2AssetRow(long Cursor, V2AssetIdentity Identity, string? Name, int? ClassId,
    string? TypeName, string? Memo, long MemoRevision, string IdentityStatus, long? Size) : INotifyPropertyChanged
{
    public string? Memo { get; private set; } = Memo;
    public long MemoRevision { get; private set; } = MemoRevision;
    public event PropertyChangedEventHandler? PropertyChanged;
    public void ApplyVerifiedMemo(string? memo, long revision)
    {
        if (revision < MemoRevision || (revision == MemoRevision && memo == Memo)) return;
        if (revision == MemoRevision) return; // Conflicting same-revision delta is never authoritative.
        Memo = memo; MemoRevision = revision;
        foreach (string property in new[] { nameof(Memo), nameof(MemoRevision), nameof(ValueStates), nameof(Details) })
            PropertyChanged?.Invoke(this, new(property));
    }
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
