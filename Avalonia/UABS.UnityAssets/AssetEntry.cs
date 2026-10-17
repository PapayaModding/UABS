namespace UABS.UnityAssets;

public sealed record AssetEntry(
    string Name,
    string Type,
    int ClassId,
    long PathId,
    uint ByteSize,
    string LogicalFile,
    string SourcePath);
