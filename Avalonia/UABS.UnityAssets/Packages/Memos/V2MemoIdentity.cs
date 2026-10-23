namespace UABS.UnityAssets.Packages.Memos;

/// <summary>Neutral V2 draft key. Epoch is registration ownership, not a display/path identity.</summary>
public sealed record V2MemoIdentity(Guid RegistrationEpoch, Guid PackageId, Guid GenerationId,
    Guid SourceId, Guid SourceRevision, int DirectoryOrdinal, long PathId);
public enum PackageMemoCommitState { NotCommitted, Committed, Uncertain }
public sealed record MemoPersistenceBasis(string? Memo, string? Revision);
public static class PackageMemoIdentityComparer
{
    public static bool Same(PackageMemoIdentity? left, PackageMemoIdentity? right) => left is not null && right is not null
        && (left.V2 is not null || right.V2 is not null ? left.V2 is not null && left.V2 == right.V2
            : PackagePathComparer.Equals(left.PackagePath, right.PackagePath)
            && string.Equals(left.IndexDirectory, right.IndexDirectory, StringComparison.Ordinal)
            && PackagePathComparer.Equals(left.SourcePath, right.SourcePath)
            && string.Equals(left.AssetName, right.AssetName, StringComparison.Ordinal) && left.PathId == right.PathId);
}
