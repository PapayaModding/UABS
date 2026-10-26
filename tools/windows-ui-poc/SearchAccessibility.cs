using System.Globalization;
using System.Text.RegularExpressions;

internal sealed record ExpectedRow(string Name, string Package);
internal sealed class AccessibilityUnavailable(string message) : Exception(message);
internal sealed record FieldVerdict(string Status, string Reason);
internal sealed record InspectorComparison(ExpectedRow Expected, string ExpectedPackagePath, string? RawDetails,
    Dictionary<string, string> ActualFields, string[] DuplicateFields, Dictionary<string, bool> Checks,
    bool Matches, string? ReadError);
internal sealed record UiBounds(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
    public bool Valid => new[] { Left, Top, Width, Height, Right, Bottom }.All(double.IsFinite) && Width > 0 && Height > 0;
}
internal sealed record PositionedRow(ExpectedRow Identity, UiBounds Bounds, bool Offscreen);

internal static class SearchAccessibility
{
    internal static ExpectedRow[] VisualRowOrder(PositionedRow[] rows, UiBounds viewport)
    {
        if (!viewport.Valid || rows.Length < 2 || rows.Select(row => row.Identity).Distinct().Count() != rows.Length)
            throw new AccessibilityUnavailable("Visual boundary selection requires a valid viewport and distinct complete row identities.");
        if (rows.Any(row => row.Offscreen || !row.Bounds.Valid || row.Bounds.Top < viewport.Top
            || row.Bounds.Bottom > viewport.Bottom || row.Bounds.Left < viewport.Left || row.Bounds.Right > viewport.Right))
            throw new AccessibilityUnavailable("Row bounds are unavailable, offscreen, clipped, or outside the verified grid viewport.");
        var ordered = rows.OrderBy(row => row.Bounds.Top).ToArray();
        for (int index = 1; index < ordered.Length; index++)
            if (ordered[index].Bounds.Top < ordered[index - 1].Bounds.Bottom)
                throw new AccessibilityUnavailable("Row bounds overlap or have tied vertical positions; visual order is ambiguous.");
        return ordered.Select(row => row.Identity).ToArray();
    }

    // Diagnostic only. Keep SelectedInspectorMatches as the independent strict predicate.
    internal static InspectorComparison ExplainInspector(string? details, ExpectedRow expected, string copy)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        foreach (string line in (details ?? "").Replace("\r\n", "\n").Split('\n'))
        {
            int separator = line.IndexOf(": ", StringComparison.Ordinal);
            if (separator > 0 && !fields.TryAdd(line[..separator], line[(separator + 2)..])) duplicates.Add(line[..separator]);
        }
        var checks = new Dictionary<string, bool> {
            ["hasDetails"] = !string.IsNullOrWhiteSpace(details),
            ["uniqueFields"] = duplicates.Count == 0,
            ["name"] = fields.GetValueOrDefault("Name") == expected.Name,
            ["package"] = fields.GetValueOrDefault("Package") == expected.Package
        };
        bool matches = false;
        string? error = null;
        try
        {
            string path = fields.GetValueOrDefault("Package path") ?? "";
            checks["absolutePackagePath"] = Path.IsPathFullyQualified(path);
            checks["packagePathEqualsCopy"] = checks["absolutePackagePath"]
                && Path.GetFullPath(path).Equals(Path.GetFullPath(copy), StringComparison.OrdinalIgnoreCase);
            string source = fields.GetValueOrDefault("Original indexed source") ?? "";
            checks["absoluteSourcePath"] = Path.IsPathFullyQualified(source);
            checks["sourceFileName"] = Path.GetFileName(source) == expected.Name;
            foreach (string key in new[] { "Package UUID", "Generation", "Source UUID", "Source revision" })
                checks[key] = Guid.TryParse(fields.GetValueOrDefault(key), out var id) && id != Guid.Empty;
            matches = SelectedInspectorMatches(details, expected, copy);
        }
        catch (Exception failure) { error = failure.ToString(); }
        return new(expected, copy, details, fields, duplicates.ToArray(), checks, matches, error);
    }

    internal static void RequireSortInvoke(bool supported)
    {
        if (!supported) throw new AccessibilityUnavailable(
            "Name HeaderItem lacks Invoke. Avalonia 11.3.3 header has no keyboard sort handler and DataGrid has no sort key; supported coordinate-free sorting remains unavailable. See name-sort-capabilities.json.");
    }

    internal static void GuardedSelectionKeys(Action focus, Func<bool> hasOwnedFocus, Action sendKeys)
    {
        focus();
        if (!hasOwnedFocus()) throw new AccessibilityUnavailable("Owned foreground DataGrid keyboard focus could not be verified; no keys sent.");
        sendKeys();
    }

    internal static bool SelectedInspectorMatches(string? details, ExpectedRow expected, string copy)
    {
        if (string.IsNullOrWhiteSpace(details)) return false;
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in details.Replace("\r\n", "\n").Split('\n'))
        {
            int separator = line.IndexOf(": ", StringComparison.Ordinal);
            if (separator > 0 && !fields.TryAdd(line[..separator], line[(separator + 2)..])) return false;
        }
        if (!fields.TryGetValue("Name", out var name) || name != expected.Name
            || !fields.TryGetValue("Package", out var package) || package != expected.Package
            || !fields.TryGetValue("Package path", out var path) || !Path.IsPathFullyQualified(path)
            || !Path.GetFullPath(path).Equals(Path.GetFullPath(copy), StringComparison.OrdinalIgnoreCase)
            || !fields.TryGetValue("Original indexed source", out var source) || !Path.IsPathFullyQualified(source)
            || Path.GetFileName(source) != expected.Name) return false;
        return new[] { "Package UUID", "Generation", "Source UUID", "Source revision" }
            .All(key => fields.TryGetValue(key, out var value) && Guid.TryParse(value, out var id) && id != Guid.Empty);
    }

    internal static string AggregateAcceptance(IEnumerable<string> statuses)
    {
        var observed = statuses.ToArray();
        return observed.Contains("FAIL") ? "FAIL"
            : observed.Length == 0 || observed.Any(status => status != "PASS") ? "BLOCKED" : "PASS";
    }
    internal static FieldVerdict VerifyField(string? value, string?[]? selectedNames, string[]? collapsedTexts, string expected = "Assets")
    {
        string[] known = ["All", "Names", "Paths", "CabCodes", "Assets", "Dependencies"];
        if (selectedNames is { Length: > 1 })
            return new("BLOCKED", "Field selector reports multiple selected items.");
        if (!string.IsNullOrEmpty(value) && !known.Contains(value))
            return new("BLOCKED", "ComboBox Value is not a recognized field selection: " + value);
        var strong = new[] { value, selectedNames is { Length: 1 } ? selectedNames[0] : null }
            .Where(text => text is not null && known.Contains(text)).Distinct().ToArray();
        string? display = collapsedTexts is { Length: 1 } && known.Contains(collapsedTexts[0]) ? collapsedTexts[0] : null;
        if (strong.Length > 1 || (strong.Length == 1 && display is not null && display != strong[0]))
            return new("BLOCKED", "Conflicting ComboBox Value, selected-item Name, or collapsed text evidence.");
        string? observed = strong.SingleOrDefault() ?? display;
        if (observed is null)
            return new("BLOCKED", "No unique supported field value/selection or collapsed display text could be established.");
        return observed == expected ? new("PASS", "Observed " + expected + " through UI Automation.")
            : new("FAIL", "Field selection is " + observed + "; expected " + expected + ".");
    }

    // The visible header/cell child order is the column order in this fresh,
    // uncustomized grid. Never flatten text across rows or guess missing cells.
    internal static ExpectedRow[] CompleteRows(string[] headers, string[][] cells, string countSummary)
    {
        if (!headers.SequenceEqual(new[] { "Name", "Package" }))
            throw new AccessibilityUnavailable("Expected ordered Name/Package HeaderItems.");
        var count = Regex.Match(countSummary,
            @"^(?<first>.+?)–(?<last>.+?) displayed / (?<total>.+?) exact indexed matches$");
        bool Number(string key, out int value) => int.TryParse(count.Groups[key].Value,
            NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
        if (!count.Success || !Number("first", out int first) || !Number("last", out int last)
            || !Number("total", out int total) || first != 1 || last != total || total != cells.Length
            || total < 1 || total > 100)
            throw new AccessibilityUnavailable("Cannot prove complete accessible result set: " + countSummary
                + $"; realized DataItems={cells.Length}. Narrow the query if rows are virtualized.");
        if (cells.Any(row => row.Length != 2 || row.Any(string.IsNullOrEmpty)))
            throw new AccessibilityUnavailable("Each DataItem must have exactly two unambiguous Name/Package text cells.");
        return cells.Select(row => new ExpectedRow(row[0], row[1])).ToArray();
    }
}
