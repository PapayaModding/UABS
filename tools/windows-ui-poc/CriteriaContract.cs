internal sealed record CriteriaEdit(string RuntimeId, string[] Watermarks, bool Writable, string Value);

internal static class CriteriaContract
{
    internal static readonly IReadOnlyDictionary<string, string> Watermarks = new Dictionary<string, string> {
        ["include"] = "Include terms (comma = OR)", ["exclude"] = "Exclude terms",
        ["pathId"] = "Exact Path ID (optional)", ["classId"] = "V2 Class ID (asset-only, optional)",
        ["typeName"] = "V2 exact type name (asset-only, optional)"
    };

    internal static Dictionary<string, string> Bind(CriteriaEdit[] edits)
    {
        var result = new Dictionary<string, string>();
        foreach (var (role, watermark) in Watermarks)
        {
            var matches = edits.Where(edit => edit.Watermarks.Contains(watermark, StringComparer.Ordinal)).ToArray();
            if (matches.Length != 1 || matches[0].Watermarks.Count(text => text == watermark) != 1
                || string.IsNullOrWhiteSpace(matches[0].RuntimeId) || !matches[0].Writable
                || matches[0].Value != "")
                throw new AccessibilityUnavailable($"Cannot bind empty writable {role} Edit by unique PART_Watermark '{watermark}'; matches={matches.Length}.");
            result.Add(role, matches[0].RuntimeId);
        }
        if (result.Values.Distinct(StringComparer.Ordinal).Count() != result.Count)
            throw new AccessibilityUnavailable("Criteria roles have ambiguous/reused UIA RuntimeIds.");
        return result;
    }

    internal static Dictionary<string, string> Read(IReadOnlyDictionary<string, string> roles, CriteriaEdit[] edits)
    {
        var result = new Dictionary<string, string>();
        foreach (var (role, id) in roles)
        {
            var matches = edits.Where(edit => edit.RuntimeId == id).ToArray();
            if (matches.Length != 1 || !matches[0].Writable)
                throw new AccessibilityUnavailable($"Bound {role} Edit RuntimeId is missing, ambiguous or lacks writable Value. No positional/value-based remapping.");
            result.Add(role, matches[0].Value);
        }
        return result;
    }

    internal static void Equal(IReadOnlyDictionary<string, string> expected, IReadOnlyDictionary<string, string> actual)
    {
        if (expected.Count != actual.Count || expected.Any(pair => !actual.TryGetValue(pair.Key, out string? value) || value != pair.Value))
            throw new InvalidOperationException("Observed semantic criteria values differ from the exact non-default expected state.");
    }

    internal static Dictionary<string, string> Sentinels(string keyword) => new() {
        ["include"] = keyword, ["exclude"] = "__cp21_exclusion_保持__", ["pathId"] = "-42424241",
        ["classId"] = "114", ["typeName"] = "CP21_Retention_Type_テスト"
    };
}
