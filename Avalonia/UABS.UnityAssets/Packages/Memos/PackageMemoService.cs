namespace UABS.UnityAssets.Packages.Memos;

public enum PackageMemoStorageState
{
    Missing,
    Empty,
    Value
}

public enum PackageMemoLookupStatus
{
    Found,
    NotFound,
    Ambiguous
}

public enum PackageMemoInheritanceMode
{
    Safe,
    Overwrite,
    Force
}

public sealed record PackageMemoIdentity(
    string? PackagePath,
    string IndexDirectory,
    string SourcePath,
    string AssetName,
    long PathId,
    V2MemoIdentity? V2 = null);

public sealed record PackageMemoReadResult(
    PackageMemoLookupStatus Status,
    PackageMemoIdentity Identity,
    string? Value,
    PackageMemoStorageState StorageState,
    string Diagnostic);

public sealed record PackageMemoInheritanceIssue(
    string IndexDirectory,
    string? SourceName,
    string? AssetName,
    string Diagnostic);

public sealed record PackageMemoInheritanceResult(
    PackageDocument Document,
    int ChangedCount,
    int UnchangedCount,
    int SkippedByModeCount,
    int MissingMatchCount,
    int AmbiguousMatchCount,
    IReadOnlyList<PackageMemoInheritanceIssue> Issues);

public interface IPackageMemoService
{
    PackageMemoReadResult Read(PackageDocument package, PackageMemoIdentity identity);

    bool SetLocalMemo(PackageDocument package, PackageMemoIdentity identity, string? value);

    PackageMemoInheritanceResult Inherit(
        PackageDocument source,
        PackageDocument target,
        PackageMemoInheritanceMode mode,
        CancellationToken cancellationToken = default);
}

// [UNITY-DEPENDENCY:REPLACEMENT-REQUIRED]
// Finding: UE-008
// Legacy source: Assets/Script/UI/BundleView/MemoField.cs — Debounce / DebounceRoutine / HandleDebouncedInput
// Unity API/plugin: Coroutines, frame lifecycle, and TextMeshPro input events
// Purpose: Delay memo writes while the user types.
// Migration status: PARTIALLY_REPLACED
// Replacement: Avalonia MVVM edits neutral package metadata in memory; explicit package Save performs the only disk write.
public sealed class PackageMemoService : IPackageMemoService
{
    public PackageMemoReadResult Read(PackageDocument package, PackageMemoIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(identity);

        (PackageMemoLookupStatus status, PackageAssetReference? asset) = FindAsset(package, identity);
        if (status != PackageMemoLookupStatus.Found)
        {
            string diagnostic = status == PackageMemoLookupStatus.Ambiguous
                ? "More than one asset matches the complete memo identity. No value was selected."
                : "The asset memo identity is no longer present in the package.";
            return new PackageMemoReadResult(status, identity, null, PackageMemoStorageState.Missing, diagnostic);
        }

        return new PackageMemoReadResult(
            PackageMemoLookupStatus.Found,
            identity,
            asset!.Memo,
            GetStorageState(asset.Memo),
            "The memo is stored directly on this asset record.");
    }

    public bool SetLocalMemo(PackageDocument package, PackageMemoIdentity identity, string? value)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(identity);

        (PackageMemoLookupStatus status, PackageAssetReference? asset) = FindAsset(package, identity);
        if (status == PackageMemoLookupStatus.Ambiguous)
            throw new InvalidOperationException("The memo identity is ambiguous; no asset was changed.");
        if (status == PackageMemoLookupStatus.NotFound)
            throw new InvalidOperationException("The memo target is no longer present in the package.");
        if (string.Equals(asset!.Memo, value, StringComparison.Ordinal))
            return false;

        asset.Memo = value;
        return true;
    }

    public PackageMemoInheritanceResult Inherit(
        PackageDocument source,
        PackageDocument target,
        PackageMemoInheritanceMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        cancellationToken.ThrowIfCancellationRequested();
        PackageDocument candidate = target.CloneForEditing();
        List<PackageMemoInheritanceIssue> issues = [];
        int changed = 0;
        int unchanged = 0;
        int skippedByMode = 0;
        int missing = 0;
        int ambiguous = 0;

        foreach (PackageIndex targetIndex in candidate.Indexes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PackageIndex[] matchingSourceIndexes = source.Indexes
                .Where(index => string.Equals(
                    index.RelativeDirectory,
                    targetIndex.RelativeDirectory,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matchingSourceIndexes.Length == 0)
            {
                missing += targetIndex.Bundles.Sum(bundle => bundle.AssetInfos.Count);
                continue;
            }

            if (matchingSourceIndexes.Length > 1)
            {
                int affected = targetIndex.Bundles.Sum(bundle => bundle.AssetInfos.Count);
                ambiguous += affected;
                issues.Add(new PackageMemoInheritanceIssue(
                    targetIndex.RelativeDirectory,
                    null,
                    null,
                    "Multiple source indexes use this relative directory; inheritance was skipped."));
                continue;
            }

            Dictionary<string, List<PackageBundleReference>> sourceBundles = matchingSourceIndexes[0].Bundles
                .GroupBy(bundle => GetLegacyBundleIdentity(bundle.Name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            Dictionary<string, List<PackageBundleReference>> targetBundles = targetIndex.Bundles
                .GroupBy(bundle => GetLegacyBundleIdentity(bundle.Name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

            foreach ((string bundleIdentity, List<PackageBundleReference> targetMatches) in targetBundles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!sourceBundles.TryGetValue(bundleIdentity, out List<PackageBundleReference>? sourceMatches))
                {
                    missing += targetMatches.Sum(bundle => bundle.AssetInfos.Count);
                    continue;
                }

                if (sourceMatches.Count != 1 || targetMatches.Count != 1)
                {
                    int affected = targetMatches.Sum(bundle => bundle.AssetInfos.Count);
                    ambiguous += affected;
                    issues.Add(new PackageMemoInheritanceIssue(
                        targetIndex.RelativeDirectory,
                        targetMatches[0].Name,
                        null,
                        "The legacy bundle-name identity matches multiple source records; inheritance was skipped."));
                    continue;
                }

                PackageBundleReference sourceBundle = sourceMatches[0];
                PackageBundleReference targetBundle = targetMatches[0];
                Dictionary<string, List<PackageAssetReference>> sourceAssets = sourceBundle.AssetInfos
                    .GroupBy(asset => asset.Name, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
                Dictionary<string, List<PackageAssetReference>> targetAssets = targetBundle.AssetInfos
                    .GroupBy(asset => asset.Name, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

                foreach ((string assetName, List<PackageAssetReference> sourceAssetMatches) in sourceAssets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!targetAssets.TryGetValue(assetName, out List<PackageAssetReference>? targetAssetMatches))
                    {
                        missing++;
                        continue;
                    }

                    if (sourceAssetMatches.Count != 1 || targetAssetMatches.Count != 1)
                    {
                        ambiguous += targetAssetMatches.Count;
                        issues.Add(new PackageMemoInheritanceIssue(
                            targetIndex.RelativeDirectory,
                            targetBundle.Name,
                            assetName,
                            "The asset name is not unique within its matched source record; inheritance was skipped."));
                        continue;
                    }

                    string? sourceMemo = sourceAssetMatches[0].Memo;
                    PackageAssetReference targetAsset = targetAssetMatches[0];
                    if (!ShouldCopy(mode, sourceMemo, targetAsset.Memo))
                    {
                        skippedByMode++;
                        continue;
                    }

                    if (string.Equals(sourceMemo, targetAsset.Memo, StringComparison.Ordinal))
                    {
                        unchanged++;
                        continue;
                    }

                    targetAsset.Memo = sourceMemo;
                    changed++;
                }
            }
        }

        return new PackageMemoInheritanceResult(
            candidate,
            changed,
            unchanged,
            skippedByMode,
            missing,
            ambiguous,
            issues.AsReadOnly());
    }

    private static (PackageMemoLookupStatus Status, PackageAssetReference? Asset) FindAsset(
        PackageDocument package,
        PackageMemoIdentity identity)
    {
        if (!string.IsNullOrWhiteSpace(identity.PackagePath)
            && !string.IsNullOrWhiteSpace(package.PackagePath)
            && !PackagePathComparer.Equals(identity.PackagePath, package.PackagePath))
        {
            return (PackageMemoLookupStatus.NotFound, null);
        }

        PackageAssetReference[] matches = package.Indexes
            .Where(index => string.Equals(index.RelativeDirectory, identity.IndexDirectory, StringComparison.OrdinalIgnoreCase))
            .SelectMany(index => index.Bundles)
            .Where(source => PackagePathComparer.Equals(source.Path, identity.SourcePath))
            .SelectMany(source => source.AssetInfos)
            .Where(asset => asset.PathId == identity.PathId
                            && string.Equals(asset.Name, identity.AssetName, StringComparison.Ordinal))
            .Take(2)
            .ToArray();

        return matches.Length switch
        {
            0 => (PackageMemoLookupStatus.NotFound, null),
            1 => (PackageMemoLookupStatus.Found, matches[0]),
            _ => (PackageMemoLookupStatus.Ambiguous, null)
        };
    }

    private static PackageMemoStorageState GetStorageState(string? memo) => memo switch
    {
        null => PackageMemoStorageState.Missing,
        "" => PackageMemoStorageState.Empty,
        _ => PackageMemoStorageState.Value
    };

    private static bool ShouldCopy(PackageMemoInheritanceMode mode, string? sourceMemo, string? targetMemo) => mode switch
    {
        PackageMemoInheritanceMode.Safe => !string.IsNullOrWhiteSpace(sourceMemo)
                                           && string.IsNullOrWhiteSpace(targetMemo),
        PackageMemoInheritanceMode.Overwrite => !string.IsNullOrWhiteSpace(sourceMemo),
        PackageMemoInheritanceMode.Force => true,
        _ => false
    };

    internal static string GetLegacyBundleIdentity(string name)
    {
        string stem = Path.GetFileNameWithoutExtension(name ?? string.Empty);
        int lastUnderscore = stem.LastIndexOf('_');
        return lastUnderscore > 0 ? stem[..lastUnderscore] : stem;
    }
}
