namespace UABS.UnityAssets;

public sealed record AssetSessionMetadata(
    string SourcePath,
    string DisplayName,
    UnityFileKind Kind,
    string? UnityVersion,
    int LogicalFileCount,
    string? SourceSha256 = null);
