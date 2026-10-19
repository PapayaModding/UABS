namespace UABS.UnityAssets.Packages.Search;

[Flags]
public enum PackageSearchFields
{
    None = 0,
    PackageName = 1 << 0,
    PackagePath = 1 << 1,
    IndexDirectory = 1 << 2,
    SourceName = 1 << 3,
    SourcePath = 1 << 4,
    CabCode = 1 << 5,
    AssetName = 1 << 6,
    Memo = 1 << 7,
    Dependency = 1 << 8,
    All = PackageName | PackagePath | IndexDirectory | SourceName | SourcePath | CabCode | AssetName | Memo | Dependency
}

[Flags]
public enum PackageSearchResultKinds
{
    None = 0,
    Package = 1 << 0,
    SourceFile = 1 << 1,
    Asset = 1 << 2,
    All = Package | SourceFile | Asset
}

[Flags]
public enum PackageSearchMatchCategory
{
    None = 0,
    PackageName = 1 << 0,
    PackagePath = 1 << 1,
    IndexDirectory = 1 << 2,
    SourceName = 1 << 3,
    SourcePath = 1 << 4,
    CabCode = 1 << 5,
    AssetName = 1 << 6,
    Memo = 1 << 7,
    Dependency = 1 << 8,
    PathId = 1 << 9
}

public enum PackageSearchResultKind
{
    Package,
    SourceFile,
    Asset
}

public enum PackageSearchSortField
{
    PackageName,
    IndexDirectory,
    SourceName,
    SourcePath,
    AssetName,
    PathId,
    MatchCategory
}

public enum PackageSearchSortDirection
{
    Ascending,
    Descending
}

public sealed class PackageSearchRequest
{
    public PackageSearchRequest(string? query = null, string? excludedQuery = null)
    {
        IncludeTerms = SplitTerms(query);
        ExcludeTerms = SplitTerms(excludedQuery);
    }

    public IReadOnlyList<string> IncludeTerms { get; init; }

    public IReadOnlyList<string> ExcludeTerms { get; init; }

    public PackageSearchFields Fields { get; init; } = PackageSearchFields.All;

    public PackageSearchResultKinds ResultKinds { get; init; } = PackageSearchResultKinds.All;

    public bool ExactText { get; init; }

    public bool RequireNonEmptyMemo { get; init; }

    public long? PathId { get; init; }

    public PackageSearchSortField SortBy { get; init; } = PackageSearchSortField.PackageName;

    public PackageSearchSortDirection SortDirection { get; init; } = PackageSearchSortDirection.Ascending;

    private static IReadOnlyList<string> SplitTerms(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value.Split([',', '，'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length > 0)
            .ToArray();
    }
}

public sealed class PackageSearchSource
{
    public PackageSearchSource(string packageName, PackageDocument package)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        PackageName = packageName;
        Package = package ?? throw new ArgumentNullException(nameof(package));
    }

    public string PackageName { get; }

    public PackageDocument Package { get; }

    public static PackageSearchSource FromDocument(PackageDocument package)
    {
        ArgumentNullException.ThrowIfNull(package);
        string? identityPath = package.PackagePath ?? package.SourceRoot;
        if (string.IsNullOrWhiteSpace(identityPath))
            throw new ArgumentException("An unsaved package requires an explicit package name.", nameof(package));

        string name = Path.GetFileName(identityPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return new PackageSearchSource(name, package);
    }
}

public sealed class PackageSearchResult
{
    internal PackageSearchResult(
        PackageSearchResultKind kind,
        PackageSearchMatchCategory matchCategories,
        string packageName,
        string? packagePath,
        string? indexDirectory = null,
        string? sourceName = null,
        string? sourcePath = null,
        string? cabCode = null,
        IEnumerable<string>? dependencies = null,
        string? assetName = null,
        long? pathId = null,
        string? memo = null)
    {
        Kind = kind;
        MatchCategories = matchCategories;
        PackageName = packageName;
        PackagePath = packagePath;
        IndexDirectory = indexDirectory;
        SourceName = sourceName;
        SourcePath = sourcePath;
        CabCode = cabCode;
        Dependencies = Array.AsReadOnly(dependencies?.ToArray() ?? []);
        AssetName = assetName;
        PathId = pathId;
        Memo = memo;
    }

    public PackageSearchResultKind Kind { get; }

    public PackageSearchMatchCategory MatchCategories { get; }

    public string PackageName { get; }

    public string? PackagePath { get; }

    public string? IndexDirectory { get; }

    public string? SourceName { get; }

    public string? SourcePath { get; }

    public string? CabCode { get; }

    public IReadOnlyList<string> Dependencies { get; }

    public string? AssetName { get; }

    public long? PathId { get; }

    public string? Memo { get; }
}
