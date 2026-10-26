using System.Text.RegularExpressions;

internal sealed record IndeterminateFixture(string Package, string Path, string Sha256, Guid PackageId, Guid GenerationId,
    string SourcePath, string SourceSha256, Guid SourceId, Guid SourceRevision, bool Partial);
internal static class IndeterminateContract
{
    internal const string Keyword = "g5_known_asset";
    internal const string Exclusion = "g5_absent_exclusion";
    internal const string Corrective = "Exclusions cannot be verified against incomplete asset/name evidence. Select fully indexed packages or clear exclusions.";
    internal const string Backend = "InvalidRequest: Bundle exclusion is indeterminate: an eligible bundle has incomplete asset coverage or unavailable requested name evidence. No sources were skipped or queried; an unknown-coverage policy is required for this selected scope.";
    internal static ExpectedRow Row(IndeterminateFixture f) => new(System.IO.Path.GetFileName(f.SourcePath), f.Package);
    internal static void Validate(IndeterminateFixture[]? fixtures)
    {
        if (fixtures is not { Length: 2 } || fixtures.Any(f => f is null) || fixtures.Count(f => f.Partial) != 1)
            throw new AccessibilityUnavailable("Exactly one complete and one eligible partial fixture required.");
        var ids = fixtures.SelectMany(f => new[] { f.PackageId, f.GenerationId, f.SourceId, f.SourceRevision }).ToArray();
        if (ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != 8) throw new AccessibilityUnavailable("Independent canonical identities required.");
        foreach (var f in fixtures)
        {
            string prefix = f.Partial ? "partial" : "complete";
            bool Canonical(string? path) => !string.IsNullOrWhiteSpace(path) && System.IO.Path.IsPathFullyQualified(path) && System.IO.Path.GetFullPath(path) == path;
            bool Sha(string? hash) => hash is not null && Regex.IsMatch(hash, @"\A[0-9a-fA-F]{64}\z");
            if (f.Package != "G5 " + prefix + " synthetic metadata" || !Canonical(f.Path) || !Canonical(f.SourcePath)
                || System.IO.Path.GetFileName(f.Path) != "G5-" + prefix + ".uabs.sqlite"
                || System.IO.Path.GetFileName(f.SourcePath) != "g5-" + prefix + ".metadata.json" || !Sha(f.Sha256) || !Sha(f.SourceSha256))
                throw new AccessibilityUnavailable("Unverified synthetic provenance/path/hash contract.");
        }
    }
    internal static void Rejection(string? corrective, string? backend, string[] summaryBefore, string[] summaryAfter,
        string[] executedBefore, string[] executedAfter, string[] statuses)
    {
        if (string.IsNullOrWhiteSpace(corrective) || string.IsNullOrWhiteSpace(backend) || summaryBefore.Length != 1 || summaryAfter.Length != 1
            || executedBefore.Length != 1 || executedAfter.Length != 1 || statuses.Length != 1)
            throw new AccessibilityUnavailable("Complete unambiguous error/count/confirmed-query/no-publication evidence is required.");
        if (corrective != Corrective || backend != Backend || !summaryBefore.SequenceEqual(summaryAfter)
            || !executedBefore.SequenceEqual(executedAfter) || statuses[0] != "No new rows published.")
            throw new InvalidOperationException("Wrong G5 rejection/diagnostic or misleading new count/query/publication state.");
    }
}
