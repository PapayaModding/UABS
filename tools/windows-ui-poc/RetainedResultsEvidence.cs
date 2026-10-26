using System.Globalization;
using FlaUI.Core.Definitions;

internal static partial class Program
{
    private static string[] LastExecutedEvidence() => Controls().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Text))
        .Select(element => element.Name).Where(name => name.StartsWith("Include: ", StringComparison.Ordinal)
            && name.Contains("; Path ID: ", StringComparison.Ordinal) && name.Contains("; packages: ", StringComparison.Ordinal)).ToArray();

    private static RetainedResultsBaseline CaptureRetainedBaseline(Scenario config)
    {
        var rows = ReadAccessibleRows(Grid()); // Existing strict fresh count/completeness proof, unchanged.
        RetainedResultsContract.EqualRows(rows, config.ExpectedRows);
        var executed = LastExecutedEvidence();
        Require(executed.Length == 1, "Exactly one last-executed Search summary is required before G2.");
        Check(executed[0] == $"Include: {config.Keyword}; Path ID: (none); packages: {1.ToString("N0", CultureInfo.CurrentCulture)}.",
            "Fresh baseline last-executed criteria differ from the independent Smoke query.");
        var baseline = new RetainedResultsBaseline(rows, executed[0]);
        GapReport("CP21-G2-fresh-baseline", baseline);
        return baseline;
    }

    private static void VerifyRetainedResults(string label, RetainedResultsBaseline baseline, bool requireStale)
    {
        UiaRead.Fresh("G2 retained-result evidence " + label, () =>
        {
            var grid = Grid();
            var headers = grid.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.HeaderItem)).Select(element => element.Name).ToArray();
            var cells = grid.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.DataItem)).Select(row =>
                row.FindAllDescendants(Automation.ConditionFactory.ByClassName("DataGridCell")).Select(cell =>
                {
                    var text = cell.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.Text));
                    Require(text.Length == 1, "Retained DataGridCell must expose exactly one Text identity.");
                    return text[0].Name;
                }).ToArray()).ToArray();
            var texts = Window!.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.Text)).Select(element => element.Name).ToArray();
            var state = new RetainedResultsState(
                texts.Where(name => name.Contains(" displayed / ", StringComparison.Ordinal) || name.Contains(" displayed · stale count", StringComparison.Ordinal)).ToArray(),
                texts.Where(name => name == RetainedResultsContract.StaleIndicator).ToArray(),
                texts.Where(name => name == RetainedResultsContract.ChangedIndicator).ToArray(), LastExecutedEvidence(),
                Window.FindAllDescendants(Automation.ConditionFactory.ByAutomationId("SearchErrorText"))
                    .Where(element => !string.IsNullOrWhiteSpace(element.Name)).Select(element => element.Name).ToArray(),
                Controls().FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.Button)).Count(element => element.Name == "Cancel"));
            // Write the actual evidence before assertions, including wrong counts/identities/status.
            GapReport(label + "-retained-results", new { baseline, requireStale, headers, cells, observed = state });
            RetainedResultsContract.Verify(headers, cells, state, baseline, requireStale);
            return true;
        });
    }
}
