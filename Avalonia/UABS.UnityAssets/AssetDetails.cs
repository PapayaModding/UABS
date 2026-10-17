namespace UABS.UnityAssets;

public sealed record AssetDetails(
    string Name,
    string Type,
    int ClassId,
    long PathId,
    uint ByteSize,
    string LogicalFile,
    string SourcePath,
    UnityFileKind FileKind,
    string? UnityVersion,
    int LogicalFileCount);
