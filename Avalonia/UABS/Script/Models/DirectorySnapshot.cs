using System.Collections.Generic;

namespace UABS.Models;

public sealed record DirectorySnapshot(string Path, IReadOnlyList<BrowserEntry> Entries);
