using System.Text.RegularExpressions;

internal sealed record PackageSortSource(string Name, string OriginalPath, string Sha256, Guid SourceId, Guid SourceRevision, string[] Witnesses);
internal sealed record PackageSortFixture(string Package, string Path, string Sha256, Guid PackageId, Guid GenerationId, PackageSortSource[] Sources);

internal static class PackageSortContract
{
    // FlaUI Type already releases the key. A second Space key-up activates an
    // Avalonia Release-mode Button again; pair Press with exactly one Release.
    internal static void PressAndRelease(Action press, Action release)
    {
        try { press(); }
        finally { release(); }
    }
    internal static void RequireAction(bool correctButton, bool enabled, bool visible, bool owned, bool invoke)
    {
        if (!correctButton || !enabled || !visible || !owned || !invoke)
            throw new AccessibilityUnavailable("Unique visible enabled owned Sort Package Button with Invoke is required.");
    }
    internal static readonly string[] Names = ["uabs-smoke-0000.bundle", "uabs-smoke-0001.bundle", "uabs-smoke-0002.bundle"];
    internal static ExpectedRow[] Oracle(string direction) => direction switch {
        "Ascending" => [new(Names[1], "Alpha"), new(Names[2], "Alpha"), new(Names[0], "Zeta")],
        "Descending" => [new(Names[0], "Zeta"), new(Names[1], "Alpha"), new(Names[2], "Alpha")],
        _ => throw new AccessibilityUnavailable("Unknown Package direction: " + direction)
    };
    internal static void Validate(PackageSortFixture[] fixtures)
    {
        if (fixtures is null || fixtures.Any(f => f is null || f.Sources is null || string.IsNullOrWhiteSpace(f.Package)
            || string.IsNullOrWhiteSpace(f.Path) || string.IsNullOrWhiteSpace(f.Sha256)
            || f.Sources.Any(s => s is null || s.Witnesses is null || string.IsNullOrWhiteSpace(s.Name)
                || string.IsNullOrWhiteSpace(s.OriginalPath) || string.IsNullOrWhiteSpace(s.Sha256))))
            throw new AccessibilityUnavailable("Incomplete Package fixture metadata.");
        if (fixtures.Length != 2 || !fixtures.Select(f => f.Package).Order(StringComparer.Ordinal).SequenceEqual(new[] { "Alpha", "Zeta" })
            || fixtures.Select(f => System.IO.Path.GetFullPath(f.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2)
            throw new AccessibilityUnavailable("Exactly two distinct Alpha/Zeta databases are required.");
        bool Sha(string hash) => Regex.IsMatch(hash, @"\A[0-9a-fA-F]{64}\z");
        var ids = fixtures.SelectMany(f => new[] { f.PackageId, f.GenerationId }).ToArray();
        var allSources = fixtures.SelectMany(f => f.Sources).ToArray();
        var sourceIds = allSources.SelectMany(s => new[] { s.SourceId, s.SourceRevision }).ToArray();
        if (ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != 4 || sourceIds.Any(id => id == Guid.Empty)
            || sourceIds.Distinct().Count() != 6 || ids.Concat(sourceIds).Distinct().Count() != 10 || allSources.Length != 3)
            throw new AccessibilityUnavailable("Package/generation/source/revision identities must be nonempty and distinct.");
        foreach (var f in fixtures)
        {
            string[] expected = f.Package == "Alpha" ? [Names[1], Names[2]] : [Names[0]];
            if (!System.IO.Path.IsPathFullyQualified(f.Path) || System.IO.Path.GetFullPath(f.Path) != f.Path
                || System.IO.Path.GetFileName(f.Path) != f.Package + ".uabs.sqlite" || !Sha(f.Sha256)
                || !f.Sources.Select(s => s.Name).Order(StringComparer.Ordinal).SequenceEqual(expected))
                throw new AccessibilityUnavailable("Unexpected fixture allocation, canonical database path or hash.");
            foreach (var s in f.Sources)
                if (!System.IO.Path.IsPathFullyQualified(s.OriginalPath) || System.IO.Path.GetFullPath(s.OriginalPath) != s.OriginalPath
                    || System.IO.Path.GetFileName(s.OriginalPath) != s.Name || !Sha(s.Sha256)
                    || s.Witnesses.Length == 0 || s.Witnesses.Any(w => string.IsNullOrEmpty(w) || !w.Contains("asset", StringComparison.OrdinalIgnoreCase)))
                    throw new AccessibilityUnavailable("Missing raw asset witness or original source path/hash evidence.");
            if (f.Package == "Alpha")
            {
                var sources = f.Sources.OrderBy(s => s.Name, StringComparer.Ordinal).ToArray();
                if (System.IO.Path.GetDirectoryName(sources[0].OriginalPath) != System.IO.Path.GetDirectoryName(sources[1].OriginalPath)
                    || StringComparer.OrdinalIgnoreCase.Compare(sources[0].OriginalPath, sources[1].OriginalPath) >= 0)
                    throw new AccessibilityUnavailable("Independent ascending SourcePath tie precondition failed.");
            }
        }
    }
    internal static void VerifyOrder(ExpectedRow[] actual, string direction)
    {
        if (!actual.SequenceEqual(Oracle(direction)))
            throw new InvalidOperationException("Package visible order differs from the independent primary/tie oracle.");
    }
    internal static NameSortState ReadState(string? help)
    {
        if (help is null || !help.Contains("Current Package order:", StringComparison.Ordinal))
            throw new AccessibilityUnavailable("Package sort field/direction evidence unavailable.");
        return SearchAccessibility.ReadNameSortState(help.Replace("Current Package order:", "Current Name order:", StringComparison.Ordinal)
            .Replace("Next Package order:", "Next Name order:", StringComparison.Ordinal));
    }
    internal static bool Completed(NameSortState before, NameSortState after, bool observedBusy, string direction, string packageHelp, string nameHelp)
    {
        if (after.Busy) return false;
        if (after.Current.StartsWith("not active (", StringComparison.Ordinal)
            && (observedBusy || after.Current != before.Current)
            && packageHelp == "Sort all matching V2 results; not the active order"
            && (nameHelp == "Sort all matching V2 results; Ascending" || nameHelp == "Sort all matching V2 results; Descending"))
            throw new InvalidOperationException("Package action completed with a different primary sort field active.");
        return after.Current == direction && packageHelp == "Sort all matching V2 results; " + direction
            && nameHelp == "Sort all matching V2 results; not the active order";
    }
}
