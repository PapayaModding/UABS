using System.Text.Json;
using System.Text.Json.Serialization;

namespace UABS.UnityAssets.Packages;

public sealed class PackageAssetReference
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("PathId")]
    public long PathId { get; set; }

    [JsonPropertyName("Memo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Memo { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    public PackageAssetReference Clone() => new()
    {
        Name = Name,
        PathId = PathId,
        Memo = Memo,
        ExtensionData = ExtensionData is null ? null : new Dictionary<string, JsonElement>(ExtensionData)
    };
}

public sealed class PackageBundleReference
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("CabCode")]
    public string CabCode { get; set; } = string.Empty;

    [JsonPropertyName("AssetInfos")]
    public List<PackageAssetReference> AssetInfos { get; set; } = [];

    [JsonPropertyName("Dependencies")]
    public List<string> Dependencies { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    public PackageBundleReference Clone() => new()
    {
        Name = Name,
        Path = Path,
        CabCode = CabCode,
        AssetInfos = AssetInfos.Select(asset => asset.Clone()).ToList(),
        Dependencies = Dependencies.ToList(),
        ExtensionData = ExtensionData is null ? null : new Dictionary<string, JsonElement>(ExtensionData)
    };
}

public sealed record PackageAssetSelection(string Name, long PathId, string LogicalFile);

public enum PackageFolderSearchMode
{
    CurrentFolderOnly,
    IncludeSubfolders
}

public enum PackageFolderImportFailureKind
{
    File,
    Directory
}

public sealed record PackageFolderImportFailure(
    string Path,
    string Message,
    PackageFolderImportFailureKind Kind = PackageFolderImportFailureKind.File);

public sealed record PackageFolderImportReference(
    string RelativeDirectory,
    PackageBundleReference Reference);

public sealed record PackageFolderImportResult(
    int ScannedFileCount,
    int SupportedFileCount,
    IReadOnlyList<PackageBundleReference> References,
    int UnsupportedFileCount,
    IReadOnlyList<PackageFolderImportFailure> Failures,
    int SkippedLinkCount,
    PackageFolderSearchMode SearchMode,
    int SkippedSubdirectoryCount = 0,
    IReadOnlyList<PackageFolderImportReference>? MappedReferences = null)
{
    public int ImportedFileCount => References.Count;
    public int ImportedAssetReferenceCount => References.Sum(reference => reference.AssetInfos.Count);
    public int FailedFileCount => Failures.Count(failure => failure.Kind == PackageFolderImportFailureKind.File);
    public int InaccessibleDirectoryCount => Failures.Count(failure => failure.Kind == PackageFolderImportFailureKind.Directory);
    public bool IncludedSubfolders => SearchMode == PackageFolderSearchMode.IncludeSubfolders;

    public IReadOnlyList<PackageFolderImportReference> GetMappedReferences() =>
        MappedReferences ?? References.Select(reference => new PackageFolderImportReference(string.Empty, reference)).ToArray();
}

public sealed class PackageIndex
{
    public PackageIndex(string relativeDirectory, IEnumerable<PackageBundleReference>? bundles = null)
    {
        RelativeDirectory = NormalizeRelativeDirectory(relativeDirectory);
        Bundles = bundles?.ToList() ?? [];
    }

    public string RelativeDirectory { get; }

    public List<PackageBundleReference> Bundles { get; }

    internal static string NormalizeRelativeDirectory(string relativeDirectory)
    {
        ArgumentNullException.ThrowIfNull(relativeDirectory);
        string normalized = relativeDirectory.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);

        if (normalized.Length == 0)
            return string.Empty;

        if (Path.IsPathRooted(normalized) || normalized.Split(Path.DirectorySeparatorChar).Any(part => part is ".." or "."))
            throw new ArgumentException("Package index directories must be relative and cannot traverse outside the package.", nameof(relativeDirectory));

        return normalized;
    }
}

public sealed class PackageDocument
{
    public PackageDocument(
        IEnumerable<PackageIndex> indexes,
        string? packagePath = null,
        string? sourceRoot = null,
        IEnumerable<PackageValidationIssue>? validationIssues = null)
    {
        Indexes = indexes?.ToList() ?? throw new ArgumentNullException(nameof(indexes));
        PackagePath = packagePath;
        SourceRoot = sourceRoot;
        ValidationIssues = validationIssues?.ToList() ?? [];
    }

    public string? PackagePath { get; internal set; }

    public string? SourceRoot { get; }

    public List<PackageIndex> Indexes { get; }

    public List<PackageValidationIssue> ValidationIssues { get; }

    public IEnumerable<PackageBundleReference> Bundles => Indexes.SelectMany(index => index.Bundles);

    public PackageDocument CloneForEditing() => new(
        Indexes.Select(index => new PackageIndex(index.RelativeDirectory, index.Bundles.Select(bundle => bundle.Clone()))),
        PackagePath,
        SourceRoot,
        ValidationIssues);

    public void AddReference(string relativeDirectory, PackageBundleReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (Bundles.Any(existing => PackagePathComparer.Equals(existing.Path, reference.Path)))
            throw new InvalidOperationException($"The source path is already present in the package: {reference.Path}");

        string normalized = PackageIndex.NormalizeRelativeDirectory(relativeDirectory);
        PackageIndex? index = Indexes.FirstOrDefault(item =>
            string.Equals(item.RelativeDirectory, normalized, StringComparison.OrdinalIgnoreCase));

        if (index is null)
        {
            index = new PackageIndex(normalized);
            Indexes.Add(index);
        }

        index.Bundles.Add(reference);
    }

    public bool RemoveReference(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        foreach (PackageIndex index in Indexes)
        {
            int removed = index.Bundles.RemoveAll(reference => PackagePathComparer.Equals(reference.Path, sourcePath));
            if (removed > 0)
                return true;
        }

        return false;
    }

}

public enum PackageIssueSeverity
{
    Warning,
    Error
}

public sealed record PackageValidationIssue(
    PackageIssueSeverity Severity,
    string Code,
    string Message,
    string? Location = null);

public sealed record PackageSummary(string Name, string Path, int IndexCount, int BundleCount);

public sealed record PackageCatalogEntry(
    string Name,
    string Path,
    PackageDocument? Package,
    string? ErrorMessage)
{
    public bool IsLoaded => Package is not null;

    public IReadOnlyList<PackageValidationIssue> ValidationIssues =>
        Package is null ? Array.Empty<PackageValidationIssue>() : Package.ValidationIssues.AsReadOnly();
}

public enum PackageProgressPhase
{
    Scanning,
    Reading,
    Writing,
    Committing,
    Complete
}

public sealed record PackageProgress(
    PackageProgressPhase Phase,
    int Completed,
    int Total,
    string? CurrentPath = null);
