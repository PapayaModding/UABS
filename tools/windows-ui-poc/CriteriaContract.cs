internal sealed record CriteriaEdit(string RuntimeId, string? AutomationId, string? AccessibleName, bool Writable, string Value);
internal sealed record CriteriaField(string AutomationId, string AccessibleName);

internal static class CriteriaContract
{
    internal static readonly IReadOnlyDictionary<string, CriteriaField> Fields = new Dictionary<string, CriteriaField> {
        ["include"] = new("IncludeSearchTextBox", "Include terms"), ["exclude"] = new("ExcludeSearchTextBox", "Exclude terms"),
        ["pathId"] = new("PathIdSearchTextBox", "Exact Path ID"), ["classId"] = new("ClassIdSearchTextBox", "V2 Class ID"),
        ["typeName"] = new("TypeNameSearchTextBox", "V2 exact type name")
    };

    internal static Dictionary<string, string> Bind(CriteriaEdit[] edits)
    {
        var result = new Dictionary<string, string>();
        foreach (var (role, field) in Fields)
        {
            // Matching either property also detects conflicting or duplicate identities;
            // accepting just the correctly paired candidate would hide ambiguity.
            var matches = edits.Where(edit => edit.AutomationId == field.AutomationId || edit.AccessibleName == field.AccessibleName).ToArray();
            if (matches.Length != 1 || matches[0].AutomationId != field.AutomationId || matches[0].AccessibleName != field.AccessibleName
                || string.IsNullOrWhiteSpace(matches[0].RuntimeId) || !matches[0].Writable
                || matches[0].Value != "")
                throw new AccessibilityUnavailable($"Cannot bind empty writable {role} Edit by unique AutomationId '{field.AutomationId}' and Name '{field.AccessibleName}'; matches={matches.Length}.");
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
            var field = Fields[role];
            if (matches.Length != 1 || !matches[0].Writable || matches[0].AutomationId != field.AutomationId
                || matches[0].AccessibleName != field.AccessibleName)
                throw new AccessibilityUnavailable($"Bound {role} Edit RuntimeId/AutomationId/Name is missing, ambiguous, changed or lacks writable Value. No positional/value-based remapping.");
            if (edits.Count(edit => edit.AutomationId == field.AutomationId || edit.AccessibleName == field.AccessibleName) != 1)
                throw new AccessibilityUnavailable($"Duplicate/conflicting accessible identity for bound {role} Edit.");
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
