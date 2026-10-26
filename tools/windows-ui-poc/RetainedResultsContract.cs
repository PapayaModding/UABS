using System.Globalization;
using System.Text.RegularExpressions;

internal sealed record RetainedResultsState(string[] Summaries, string[] StaleIndicators, string[] ChangedIndicators,
    string[] LastExecutedCriteria, string[] Errors, int ActiveSearches);
internal sealed record RetainedResultsBaseline(ExpectedRow[] Rows, string LastExecutedCriteria);

internal static class RetainedResultsContract
{
    internal const string StaleIndicator = "Stale — refresh required";
    internal const string ChangedIndicator = "Criteria changed — Search to update results.";

    internal static void EqualRows(ExpectedRow[] actual, ExpectedRow[] expected)
    {
        ExpectedRow[] Order(ExpectedRow[] rows) => rows.OrderBy(row => row.Name, StringComparer.Ordinal)
            .ThenBy(row => row.Package, StringComparer.Ordinal).ToArray();
        if (!Order(actual).SequenceEqual(Order(expected)))
            throw new InvalidOperationException("Retained Name/Package row multiset differs from the independently confirmed baseline.");
    }

    internal static ExpectedRow[] Verify(string[] headers, string[][] cells, RetainedResultsState state,
        RetainedResultsBaseline baseline, bool requireStale)
    {
        if (baseline.Rows.Length is < 1 or > 100 || string.IsNullOrEmpty(baseline.LastExecutedCriteria))
            throw new AccessibilityUnavailable("A previously verified complete fresh baseline is required.");
        if (!headers.SequenceEqual(new[] { "Name", "Package" })
            || cells.Any(cell => cell.Length != 2 || cell.Any(string.IsNullOrEmpty)))
            throw new AccessibilityUnavailable("Unambiguous retained Name/Package headers and two-cell DataItems are required.");
        if (state.Summaries.Length != 1)
            throw new AccessibilityUnavailable("Expected exactly one retained-result count summary; missing/duplicate evidence is not accepted.");
        string summary = state.Summaries[0];
        var stale = Regex.Match(summary, @"\A(?<count>.+?) displayed · stale count\z");
        ExpectedRow[] actual;
        if (stale.Success)
        {
            if (!int.TryParse(stale.Groups["count"].Value, NumberStyles.Integer | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture, out int count))
                throw new AccessibilityUnavailable("Stale displayed count cannot be read unambiguously.");
            if (count != baseline.Rows.Length || summary != $"{count:N0} displayed · stale count")
                throw new InvalidOperationException("Stale displayed count does not equal the exact retained baseline count/presentation.");
            if (cells.Length != count)
                throw new AccessibilityUnavailable("Realized retained DataItems do not cover the displayed count; virtualization/incomplete UIA evidence is not accepted.");
            actual = cells.Select(cell => new ExpectedRow(cell[0], cell[1])).ToArray();
            EqualRows(actual, baseline.Rows); // A proven wrong complete set stays FAIL even if a status marker is also missing.
            if (state.StaleIndicators.Length != 1 || state.StaleIndicators[0] != StaleIndicator)
                throw new AccessibilityUnavailable("Missing/ambiguous exact stale-state indicator.");
        }
        else
        {
            // Some criteria edits preserve a still-current snapshot. Never relabel it as stale,
            // and never feed a manufactured fresh count into the existing strict reader.
            actual = SearchAccessibility.CompleteRows(headers, cells, summary);
            EqualRows(actual, baseline.Rows);
            if (state.StaleIndicators.Length != 0)
                throw new AccessibilityUnavailable("Conflicting fresh-count/stale-indicator evidence.");
            if (requireStale)
                throw new InvalidOperationException("Unexpected fresh count after scope/session invalidation; no Search was requested.");
        }
        if (state.LastExecutedCriteria.Length != 1 || state.ChangedIndicators.Length != 1
            || state.ChangedIndicators[0] != ChangedIndicator)
            throw new AccessibilityUnavailable("Missing/ambiguous last-executed or changed-criteria evidence.");
        if (state.LastExecutedCriteria[0] != baseline.LastExecutedCriteria || state.Errors.Length != 0 || state.ActiveSearches != 0)
            throw new InvalidOperationException("Unexpected Search execution/error activity or changed last-executed criteria during retention-only transitions.");
        return actual;
    }
}
