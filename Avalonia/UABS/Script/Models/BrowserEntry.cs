using System;

namespace UABS.Models;

public enum BrowserEntryKind
{
    Directory,
    File
}

public sealed record BrowserEntry(
    string Name,
    string FullPath,
    BrowserEntryKind Kind,
    long Size,
    DateTime LastModified)
{
    public bool IsDirectory => Kind == BrowserEntryKind.Directory;
}
