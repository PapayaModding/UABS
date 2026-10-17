namespace UABS.UnityAssets.Packages.Dependencies;

public sealed class PackageDependencyResolver : IPackageDependencyResolver
{
    public Task<IReadOnlyList<PackageDependencyResult>> ResolveAsync(
        IReadOnlyList<PackageDependencySource> sources,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        return Task.Run(() => Resolve(sources, cancellationToken), cancellationToken);
    }

    public Task<IReadOnlyList<PackageDependencyResult>> ResolveForPackageAsync(
        IReadOnlyList<PackageDependencySource> catalog,
        PackageDependencySource requester,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(requester);
        return Task.Run(() => ResolveForPackage(catalog, requester, cancellationToken), cancellationToken);
    }

    public IReadOnlyList<PackageDependencyResult> Resolve(
        IReadOnlyList<PackageDependencySource> sources,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        return ResolveCore(sources, sources, cancellationToken);
    }

    public IReadOnlyList<PackageDependencyResult> ResolveForPackage(
        IReadOnlyList<PackageDependencySource> catalog,
        PackageDependencySource requester,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(requester);
        return ResolveCore(catalog, [requester], cancellationToken);
    }

    private static IReadOnlyList<PackageDependencyResult> ResolveCore(
        IReadOnlyList<PackageDependencySource> catalog,
        IReadOnlyList<PackageDependencySource> requesters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SourceSnapshot[] catalogSnapshot = Snapshot(catalog, cancellationToken);
        SourceSnapshot[] requesterSnapshot = ReferenceEquals(catalog, requesters)
            ? catalogSnapshot
            : Snapshot(requesters, cancellationToken);
        CandidateLookup lookup = BuildLookup(catalogSnapshot, cancellationToken);
        List<PackageDependencyResult> results = [];

        foreach (SourceSnapshot requester in requesterSnapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (string? requestedCabCode in requester.Dependencies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PackageDependencyOccurrence requesterOccurrence = requester.ToOccurrence();
                if (!TryNormalizeCabCode(requestedCabCode, out string normalized))
                {
                    results.Add(new PackageDependencyResult(
                        requesterOccurrence,
                        requestedCabCode,
                        PackageDependencyStatus.Unresolved,
                        [],
                        "The declared dependency CAB identifier is null, empty, or malformed."));
                    continue;
                }

                IReadOnlyList<PackageDependencyCandidate> candidates = lookup.Find(normalized);
                PackageDependencyStatus status = candidates.Count switch
                {
                    0 => PackageDependencyStatus.Unresolved,
                    1 => PackageDependencyStatus.Resolved,
                    _ => PackageDependencyStatus.Ambiguous
                };

                string diagnostic = status switch
                {
                    PackageDependencyStatus.Resolved => "Exactly one physical source matches the requested CAB identifier.",
                    PackageDependencyStatus.Ambiguous => $"{candidates.Count} distinct physical sources match the requested CAB identifier.",
                    _ => "No loaded package source matches the requested CAB identifier."
                };

                results.Add(new PackageDependencyResult(
                    requesterOccurrence,
                    requestedCabCode,
                    status,
                    candidates,
                    diagnostic));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return results.AsReadOnly();
    }

    private static SourceSnapshot[] Snapshot(
        IReadOnlyList<PackageDependencySource> sources,
        CancellationToken cancellationToken)
    {
        List<SourceSnapshot> snapshot = [];
        int ordinal = 0;
        foreach (PackageDependencySource source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source is null)
                continue;

            foreach (PackageIndex index in source.Package.Indexes ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (index is null)
                    continue;

                foreach (PackageBundleReference bundle in index.Bundles ?? [])
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (bundle is null)
                        continue;

                    snapshot.Add(new SourceSnapshot(
                        source.PackageName,
                        source.Package.PackagePath,
                        index.RelativeDirectory,
                        bundle.Name,
                        bundle.Path,
                        bundle.CabCode,
                        bundle.Dependencies?.Select(value => (string?)value).ToArray() ?? [],
                        ordinal++));
                }
            }
        }

        return snapshot
            .OrderBy(item => item.PackageName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.PackageName, StringComparer.Ordinal)
            .ThenBy(item => item.PackagePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.IndexDirectory, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SourceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Ordinal)
            .ToArray();
    }

    private static CandidateLookup BuildLookup(
        IReadOnlyList<SourceSnapshot> sources,
        CancellationToken cancellationToken)
    {
        Dictionary<string, CandidateBuilder> physicalCandidates = new(StringComparer.OrdinalIgnoreCase);
        foreach (SourceSnapshot source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryNormalizeCabCode(source.CabCode, out string normalizedCabCode))
                continue;

            string identity = GetPhysicalIdentity(source);
            if (!physicalCandidates.TryGetValue(identity, out CandidateBuilder? candidate))
            {
                candidate = new CandidateBuilder(
                    source.SourcePath,
                    !string.IsNullOrWhiteSpace(source.SourcePath));
                physicalCandidates.Add(identity, candidate);
            }

            candidate.Add(normalizedCabCode, source.ToOccurrence());
        }

        return new CandidateLookup(physicalCandidates.Values.Select(candidate => candidate.Build()));
    }

    private static string GetPhysicalIdentity(SourceSnapshot source)
    {
        if (!string.IsNullOrWhiteSpace(source.SourcePath))
        {
            return "PATH:" + PackagePathComparer.GetIdentity(source.SourcePath);
        }

        // Missing path provenance cannot safely establish that two records refer to one file.
        return $"INCOMPLETE:{source.Ordinal}";
    }

    private static bool TryNormalizeCabCode(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string candidate = value.Trim();
        if (candidate.Length <= 4
            || !candidate.StartsWith("CAB-", StringComparison.OrdinalIgnoreCase)
            || candidate.Skip(4).Any(char.IsWhiteSpace))
        {
            return false;
        }

        normalized = candidate.ToUpperInvariant();
        return true;
    }

    private sealed class CandidateLookup
    {
        private readonly Dictionary<string, List<PackageDependencyCandidate>> _exact =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PackageDependencyCandidate>> _oneCharacterLonger =
            new(StringComparer.OrdinalIgnoreCase);

        public CandidateLookup(IEnumerable<PackageDependencyCandidate> candidates)
        {
            foreach (PackageDependencyCandidate candidate in candidates)
            {
                foreach (string cabCode in candidate.CabCodes)
                {
                    Add(_exact, cabCode, candidate);
                    if (cabCode.Length > 5)
                        Add(_oneCharacterLonger, cabCode[..^1], candidate);
                }
            }
        }

        public IReadOnlyList<PackageDependencyCandidate> Find(string requestedCabCode)
        {
            HashSet<PackageDependencyCandidate> matches = [];
            AddMatches(_exact, requestedCabCode, matches);
            if (requestedCabCode.Length > 5)
                AddMatches(_exact, requestedCabCode[..^1], matches);
            AddMatches(_oneCharacterLonger, requestedCabCode, matches);

            return matches
                .OrderBy(candidate => candidate.SourcePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.CabCodes[0], StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.Occurrences[0].PackageName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void Add(
            Dictionary<string, List<PackageDependencyCandidate>> lookup,
            string key,
            PackageDependencyCandidate candidate)
        {
            if (!lookup.TryGetValue(key, out List<PackageDependencyCandidate>? candidates))
            {
                candidates = [];
                lookup.Add(key, candidates);
            }

            candidates.Add(candidate);
        }

        private static void AddMatches(
            Dictionary<string, List<PackageDependencyCandidate>> lookup,
            string key,
            HashSet<PackageDependencyCandidate> matches)
        {
            if (lookup.TryGetValue(key, out List<PackageDependencyCandidate>? candidates))
                matches.UnionWith(candidates);
        }
    }

    private sealed class CandidateBuilder(
        string? sourcePath,
        bool hasCompleteProvenance)
    {
        private readonly List<PackageDependencyOccurrence> _occurrences = [];
        private readonly HashSet<string> _cabCodes = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string cabCode, PackageDependencyOccurrence occurrence)
        {
            _cabCodes.Add(cabCode);
            _occurrences.Add(occurrence);
        }

        public PackageDependencyCandidate Build() => new(
            sourcePath,
            _cabCodes.OrderBy(value => value, StringComparer.OrdinalIgnoreCase),
            hasCompleteProvenance,
            _occurrences.OrderBy(item => item.PackageName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.PackagePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.IndexDirectory, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private sealed record SourceSnapshot(
        string PackageName,
        string? PackagePath,
        string IndexDirectory,
        string? SourceName,
        string? SourcePath,
        string? CabCode,
        IReadOnlyList<string?> Dependencies,
        int Ordinal)
    {
        public PackageDependencyOccurrence ToOccurrence() => new(
            PackageName,
            PackagePath,
            IndexDirectory,
            SourceName,
            SourcePath,
            CabCode,
            !string.IsNullOrWhiteSpace(SourcePath));
    }
}
