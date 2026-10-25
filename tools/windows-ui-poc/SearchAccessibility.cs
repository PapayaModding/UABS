using System.Globalization;
using System.Text.RegularExpressions;

internal sealed record ExpectedRow(string Name, string Package);
internal sealed class AccessibilityUnavailable(string message) : Exception(message);

internal static class SearchAccessibility
{
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
