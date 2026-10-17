namespace UABS.UnityAssets.Packages.Search;

public sealed class PackageSearchService : IPackageSearchService
{
    public Task<IReadOnlyList<PackageSearchResult>> SearchAsync(
        IReadOnlyList<PackageSearchSource> sources,
        PackageSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Search(sources, request, cancellationToken), cancellationToken);
    }

    public IReadOnlyList<PackageSearchResult> Search(
        IReadOnlyList<PackageSearchSource> sources,
        PackageSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string[] includeTerms = NormalizeTerms(request.IncludeTerms);
        string[] excludeTerms = NormalizeTerms(request.ExcludeTerms);
        if ((includeTerms.Length == 0 && request.PathId is null)
            || (request.Fields == PackageSearchFields.None && request.PathId is null)
            || request.ResultKinds == PackageSearchResultKinds.None)
        {
            return Array.Empty<PackageSearchResult>();
        }

        List<PackageSearchResult> results = [];
        foreach (PackageSearchSource source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source is null)
                continue;

            PackageDocument package = source.Package;
            if (request.PathId is null
                && request.ResultKinds.HasFlag(PackageSearchResultKinds.Package))
            {
                PackageSearchMatchCategory category = MatchPackage(
                    source,
                    request.Fields,
                    includeTerms,
                    excludeTerms,
                    request.ExactText);
                if (category != PackageSearchMatchCategory.None)
                {
                    results.Add(new PackageSearchResult(
                        PackageSearchResultKind.Package,
                        category,
                        source.PackageName,
                        package.PackagePath));
                }
            }

            foreach (PackageIndex index in package.Indexes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (PackageBundleReference bundle in index.Bundles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (bundle is null)
                        continue;

                    if (request.PathId is null && request.ResultKinds.HasFlag(PackageSearchResultKinds.SourceFile))
                    {
                        PackageSearchMatchCategory categories = MatchSource(
                            index,
                            bundle,
                            request.Fields,
                            includeTerms,
                            excludeTerms,
                            request.ExactText);
                        if (categories != PackageSearchMatchCategory.None)
                            results.Add(CreateResult(source, index, bundle, null, PackageSearchResultKind.SourceFile, categories));
                    }

                    if (!request.ResultKinds.HasFlag(PackageSearchResultKinds.Asset))
                        continue;

                    foreach (PackageAssetReference asset in bundle.AssetInfos ?? [])
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (asset is null || (request.PathId is not null && asset.PathId != request.PathId.Value))
                            continue;
                        if (request.PathId is not null && AssetIsExcluded(asset, request.Fields, excludeTerms, request.ExactText))
                            continue;

                        PackageSearchMatchCategory categories = MatchAsset(
                            asset,
                            request.Fields,
                            includeTerms,
                            excludeTerms,
                            request.ExactText);
                        if (request.PathId is not null)
                            categories |= PackageSearchMatchCategory.PathId;

                        bool textRequired = includeTerms.Length > 0;
                        bool textMatched = (categories & (PackageSearchMatchCategory.AssetName | PackageSearchMatchCategory.Memo)) != 0;
                        if ((!textRequired || textMatched) && categories != PackageSearchMatchCategory.None)
                            results.Add(CreateResult(source, index, bundle, asset, PackageSearchResultKind.Asset, categories));
                    }
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        List<PackageSearchResult> sorted = Sort(results, request);
        cancellationToken.ThrowIfCancellationRequested();
        return sorted.AsReadOnly();
    }

    private static PackageSearchMatchCategory MatchSource(
        PackageIndex index,
        PackageBundleReference bundle,
        PackageSearchFields fields,
        string[] includeTerms,
        string[] excludeTerms,
        bool exact)
    {
        List<(string? Value, PackageSearchMatchCategory Category)> values = [];
        if (fields.HasFlag(PackageSearchFields.IndexDirectory))
            values.Add((index.RelativeDirectory, PackageSearchMatchCategory.IndexDirectory));
        if (fields.HasFlag(PackageSearchFields.SourceName))
            values.Add((bundle.Name, PackageSearchMatchCategory.SourceName));
        if (fields.HasFlag(PackageSearchFields.SourcePath))
            values.Add((bundle.Path, PackageSearchMatchCategory.SourcePath));
        if (fields.HasFlag(PackageSearchFields.CabCode))
            values.Add((bundle.CabCode, PackageSearchMatchCategory.CabCode));
        if (fields.HasFlag(PackageSearchFields.Dependency) && bundle.Dependencies is not null)
            values.AddRange(bundle.Dependencies.Select(value => ((string?)value, PackageSearchMatchCategory.Dependency)));

        return MatchFields(values, includeTerms, excludeTerms, exact);
    }

    private static PackageSearchMatchCategory MatchAsset(
        PackageAssetReference asset,
        PackageSearchFields fields,
        string[] includeTerms,
        string[] excludeTerms,
        bool exact)
    {
        List<(string? Value, PackageSearchMatchCategory Category)> values = [];
        if (fields.HasFlag(PackageSearchFields.AssetName))
            values.Add((asset.Name, PackageSearchMatchCategory.AssetName));
        if (fields.HasFlag(PackageSearchFields.Memo))
            values.Add((asset.Memo, PackageSearchMatchCategory.Memo));
        return MatchFields(values, includeTerms, excludeTerms, exact);
    }

    private static bool AssetIsExcluded(
        PackageAssetReference asset,
        PackageSearchFields fields,
        string[] excludeTerms,
        bool exact)
    {
        if (excludeTerms.Length == 0)
            return false;

        return (fields.HasFlag(PackageSearchFields.AssetName)
                && excludeTerms.Any(term => TextMatches(asset.Name, term, exact)))
            || (fields.HasFlag(PackageSearchFields.Memo)
                && excludeTerms.Any(term => TextMatches(asset.Memo, term, exact)));
    }

    private static PackageSearchMatchCategory MatchPackage(
        PackageSearchSource source,
        PackageSearchFields fields,
        string[] includeTerms,
        string[] excludeTerms,
        bool exact)
    {
        List<(string? Value, PackageSearchMatchCategory Category)> values = [];
        if (fields.HasFlag(PackageSearchFields.PackageName))
            values.Add((source.PackageName, PackageSearchMatchCategory.PackageName));
        if (fields.HasFlag(PackageSearchFields.PackagePath))
            values.Add((source.Package.PackagePath, PackageSearchMatchCategory.PackagePath));
        return MatchFields(values, includeTerms, excludeTerms, exact);
    }

    private static PackageSearchMatchCategory MatchFields(
        IEnumerable<(string? Value, PackageSearchMatchCategory Category)> fields,
        string[] includeTerms,
        string[] excludeTerms,
        bool exact)
    {
        (string? Value, PackageSearchMatchCategory Category)[] snapshot = fields.ToArray();
        if (excludeTerms.Any(term => snapshot.Any(field => TextMatches(field.Value, term, exact))))
            return PackageSearchMatchCategory.None;

        PackageSearchMatchCategory categories = PackageSearchMatchCategory.None;
        foreach ((string? value, PackageSearchMatchCategory category) in snapshot)
        {
            if (includeTerms.Any(term => TextMatches(value, term, exact)))
                categories |= category;
        }

        return categories;
    }

    private static bool TextMatches(string? value, string term, bool exact)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        return exact
            ? string.Equals(value, term, StringComparison.OrdinalIgnoreCase)
            : value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] NormalizeTerms(IReadOnlyList<string>? terms)
    {
        if (terms is null || terms.Count == 0)
            return [];

        return terms
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Select(term => term.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PackageSearchResult CreateResult(
        PackageSearchSource source,
        PackageIndex index,
        PackageBundleReference bundle,
        PackageAssetReference? asset,
        PackageSearchResultKind kind,
        PackageSearchMatchCategory categories)
    {
        return new PackageSearchResult(
            kind,
            categories,
            source.PackageName,
            source.Package.PackagePath,
            index.RelativeDirectory,
            bundle.Name,
            bundle.Path,
            bundle.CabCode,
            bundle.Dependencies,
            asset?.Name,
            asset?.PathId,
            asset?.Memo);
    }

    private static List<PackageSearchResult> Sort(
        List<PackageSearchResult> results,
        PackageSearchRequest request)
    {
        Comparison<PackageSearchResult> primary = request.SortBy switch
        {
            PackageSearchSortField.IndexDirectory => (left, right) => CompareText(left.IndexDirectory, right.IndexDirectory),
            PackageSearchSortField.SourceName => (left, right) => CompareText(left.SourceName, right.SourceName),
            PackageSearchSortField.SourcePath => (left, right) => CompareText(left.SourcePath, right.SourcePath),
            PackageSearchSortField.AssetName => (left, right) => CompareText(left.AssetName, right.AssetName),
            PackageSearchSortField.PathId => (left, right) => Nullable.Compare(left.PathId, right.PathId),
            PackageSearchSortField.MatchCategory => (left, right) => left.MatchCategories.CompareTo(right.MatchCategories),
            _ => (left, right) => CompareText(left.PackageName, right.PackageName)
        };

        results.Sort((left, right) =>
        {
            int comparison = primary(left, right);
            if (comparison != 0)
                return request.SortDirection == PackageSearchSortDirection.Descending ? -comparison : comparison;

            comparison = CompareText(left.PackageName, right.PackageName);
            if (comparison != 0) return comparison;
            comparison = CompareText(left.PackagePath, right.PackagePath);
            if (comparison != 0) return comparison;
            comparison = CompareText(left.IndexDirectory, right.IndexDirectory);
            if (comparison != 0) return comparison;
            comparison = CompareText(left.SourcePath, right.SourcePath);
            if (comparison != 0) return comparison;
            comparison = CompareText(left.AssetName, right.AssetName);
            if (comparison != 0) return comparison;
            comparison = Nullable.Compare(left.PathId, right.PathId);
            if (comparison != 0) return comparison;
            return left.Kind.CompareTo(right.Kind);
        });
        return results;
    }

    private static int CompareText(string? left, string? right)
    {
        int comparison = StringComparer.OrdinalIgnoreCase.Compare(left, right);
        return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left, right);
    }
}
