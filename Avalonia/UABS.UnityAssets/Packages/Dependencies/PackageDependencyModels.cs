namespace UABS.UnityAssets.Packages.Dependencies;

public enum PackageDependencyStatus
{
    Resolved,
    Unresolved,
    Ambiguous
}

public sealed class PackageDependencySource
{
    public PackageDependencySource(string packageName, PackageDocument package)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        PackageName = packageName;
        Package = package ?? throw new ArgumentNullException(nameof(package));
    }

    public string PackageName { get; }

    public PackageDocument Package { get; }

    public static PackageDependencySource FromDocument(PackageDocument package)
    {
        ArgumentNullException.ThrowIfNull(package);
        string? identityPath = package.PackagePath ?? package.SourceRoot;
        if (string.IsNullOrWhiteSpace(identityPath))
            throw new ArgumentException("An unsaved package requires an explicit package name.", nameof(package));

        string name = Path.GetFileName(identityPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return new PackageDependencySource(name, package);
    }
}

public sealed record PackageDependencyOccurrence(
    string PackageName,
    string? PackagePath,
    string IndexDirectory,
    string? SourceName,
    string? SourcePath,
    string? CabCode,
    bool HasCompleteProvenance);

public sealed class PackageDependencyCandidate
{
    internal PackageDependencyCandidate(
        string? sourcePath,
        IEnumerable<string> cabCodes,
        bool hasCompleteProvenance,
        IEnumerable<PackageDependencyOccurrence> occurrences)
    {
        SourcePath = sourcePath;
        CabCodes = Array.AsReadOnly(cabCodes.ToArray());
        HasCompleteProvenance = hasCompleteProvenance;
        Occurrences = Array.AsReadOnly(occurrences.ToArray());
    }

    public string? SourcePath { get; }

    public IReadOnlyList<string> CabCodes { get; }

    public bool HasCompleteProvenance { get; }

    public IReadOnlyList<PackageDependencyOccurrence> Occurrences { get; }
}

public sealed class PackageDependencyResult
{
    internal PackageDependencyResult(
        PackageDependencyOccurrence requester,
        string? requestedCabCode,
        PackageDependencyStatus status,
        IEnumerable<PackageDependencyCandidate> candidates,
        string diagnostic)
    {
        Requester = requester;
        RequestedCabCode = requestedCabCode;
        Status = status;
        Candidates = Array.AsReadOnly(candidates.ToArray());
        Diagnostic = diagnostic;
    }

    public PackageDependencyOccurrence Requester { get; }

    public string? RequestedCabCode { get; }

    public PackageDependencyStatus Status { get; }

    public IReadOnlyList<PackageDependencyCandidate> Candidates { get; }

    public string Diagnostic { get; }
}
