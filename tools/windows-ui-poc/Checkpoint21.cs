using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

internal sealed record AcceptanceResult(string Requirement, string Status, string Detail, string Evidence);

internal static partial class Program
{
    private static readonly List<AcceptanceResult> AcceptanceResults = [];
    private static AutomationElement Controls()
    {
        var controls = ById(Window!, "SearchControlsScroller");
        Require(controls.ControlType == ControlType.Pane, "SearchControlsScroller must expose Pane.");
        return controls;
    }
    private static AutomationElement Grid() => ById(Window!, "ResultsGrid");
    private static void Case(string id, Action action)
    {
        string status = "PASS", detail = "All exercised UI assertions passed.";
        try { action(); }
        catch (Exception error)
        { status = error is Blocked or NotSupportedException or AccessibilityUnavailable ? "BLOCKED" : "FAIL"; detail = error.ToString(); }
        try { Evidence(id); }
        catch (Exception error) { if (status == "PASS") status = "BLOCKED"; detail += " Evidence: " + error.Message; }
        AcceptanceResults.Add(new(id, status, detail, id + "-uia.json / " + id + ".png / results.json steps"));
        Step(id, new { status, detail });
        File.WriteAllText(Path.Combine(Output, "acceptance.json"), JsonSerializer.Serialize(AcceptanceResults, Json));
    }
    private static string AcceptanceStatus() => SearchAccessibility.AggregateAcceptance(AcceptanceResults.Select(r => r.Status));
    private static void Toggle(AutomationElement element, bool enabled)
    {
        Require(element.Patterns.Toggle.IsSupported, element.Name + " requires Toggle.");
        var expected = enabled ? ToggleState.On : ToggleState.Off;
        if (element.Patterns.Toggle.Pattern.ToggleState.Value != expected) element.Patterns.Toggle.Pattern.Toggle();
        Check(element.Patterns.Toggle.Pattern.ToggleState.Value == expected, "Toggle readback differs: " + element.Name);
        Step("toggle", new { name = element.Name, enabled });
    }
    private static void Kind(string name, bool enabled) => Toggle(Find(Controls(), ControlType.CheckBox, name), enabled);
    private static void Field(string name)
    {
        var field = Find(Controls(), ControlType.ComboBox, null);
        Require(field.Patterns.ExpandCollapse.IsSupported, "Field ComboBox requires ExpandCollapse to change choices.");
        field.Patterns.ExpandCollapse.Pattern.Expand();
        var choice = Find(field, ControlType.ListItem, name);
        Select(choice);
        field.Patterns.ExpandCollapse.Pattern.Collapse();
        VerifyFieldSelection(field, name);
    }
    private static void ResetSupported(Scenario config)
    {
        Select(Find(Window!, ControlType.TabItem, "Search"));
        Field("Assets"); Kind("Packages", false); Kind("Assets", false); Kind("Sources", true);
        foreach (string name in new[] { "Exact name", "Search memos", "Memo only" }) Kind(name, false);
        SetValue(ById(Controls(), "IncludeSearchTextBox"), config.Keyword);
        foreach (var scope in Scopes()) Toggle(scope, scope.Name.StartsWith("[V2] ", StringComparison.Ordinal));
    }
    private static AutomationElement[] Scopes() => Controls().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.CheckBox))
        .Where(e => e.Name.StartsWith("[V2] ", StringComparison.Ordinal) || e.Name.StartsWith("[V1] ", StringComparison.Ordinal)).ToArray();
    private static void Rows(ExpectedRow[] expected)
    {
        var observed = ReadAccessibleRows(Grid());
        Step("checkpoint rows", observed);
        Check(observed.OrderBy(r => r.Name).ThenBy(r => r.Package).SequenceEqual(expected.OrderBy(r => r.Name).ThenBy(r => r.Package)),
            "Exact bundle Name/Package row multiset differs.");
    }
    private static void Search(Scenario config)
    {
        ExecuteSearch();
        Wait("checkpoint query result", () => Grid().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.DataItem)).Length > 0);
        Check(!Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("SearchErrorText"))
            .Any(e => !e.IsOffscreen && !string.IsNullOrWhiteSpace(e.Name)), "Supported Search retained an error.");
        Rows(config.ExpectedRows);
    }
    private static void ExecuteSearch()
    {
        var button = ById(Controls(), "SearchButton");
        Require(button.IsEnabled, "Search command is disabled or still running.");
        Invoke(button);
        // AsyncCommand disables its bound button until the requested query ends.
        Wait("Search command completion", () => button.IsEnabled);
    }
    private static void ExecuteRefresh()
    {
        var button = ById(Window!, "RefreshSearchButton");
        Require(button.IsEnabled, "Refresh command is disabled or still running.");
        Invoke(button); Wait("Refresh command completion", () => button.IsEnabled);
    }
    private static string Error(string contains)
    {
        string text = "";
        Wait("corrective Search error", () =>
        {
            var errors = Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("SearchErrorText"));
            if (errors.Length != 1 || errors[0].IsOffscreen) return false;
            text = errors[0].Name;
            return !string.IsNullOrWhiteSpace(text);
        });
        Check(text.Contains(contains, StringComparison.Ordinal), "Wrong corrective error: " + text);
        Step("corrective error", text);
        return text;
    }
    private static string Criteria()
    {
        var controls = Controls();
        var edits = controls.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Edit));
        Require(edits.Length >= 2 && edits.All(e => e.Patterns.Value.IsSupported), "All criterion Edits need readable Value patterns.");
        return JsonSerializer.Serialize(new
        {
            text = edits.Select(e => e.Patterns.Value.Pattern.Value.Value).ToArray(),
            flags = new[] { "Exact name", "Search memos", "Memo only" }.Select(name =>
            {
                var e = Find(controls, ControlType.CheckBox, name);
                Require(e.Patterns.Toggle.IsSupported, name + " requires Toggle.");
                return e.Patterns.Toggle.Pattern.ToggleState.Value.ToString();
            }).ToArray()
        });
    }
    private static string RunCheckpoint21(Scenario config, string work)
    {
        Case("CP21-01-v2-defaults-unicode", () => Rows(config.ExpectedRows)); // Baseline ran without selector edits.
        foreach (string field in new[] { "Names", "Paths", "All" })
            Case("CP21-02-" + field, () =>
            {
                ResetSupported(config); Search(config); Field(field);
                ExecuteSearch(); Error("Select Assets");
                VerifyFieldSelection(Find(Controls(), ControlType.ComboBox, null), field);
                CheckToggle(Controls(), "Sources", ToggleState.On); Rows(config.ExpectedRows);
            });
        Case("CP21-03-mixed-kinds", () =>
        {
            ResetSupported(config); Search(config); Kind("Packages", true);
            ExecuteSearch(); Error("Sources only");
            CheckToggle(Controls(), "Packages", ToggleState.On); CheckToggle(Controls(), "Sources", ToggleState.On);
            Rows(config.ExpectedRows);
        });
        Case("CP21-04-original-diagnostic", () =>
        {
            ResetSupported(config); Field("Names"); ExecuteSearch(); Error("Select Assets");
            var error = ById(Window!, "SearchErrorText");
            object? help = EvidenceValue.Read(() => error.Properties.HelpText.Value);
            Step("original diagnostic accessibility", help ?? "unavailable");
            Require(help is string text && text.Contains("InvalidRequest", StringComparison.Ordinal),
                "Original diagnostic tooltip is not exposed through UIA HelpText. Actual hover verification remains required; no coordinate fallback.");
        });
        Case("CP21-05-scope-workspace-retention", () =>
        {
            ResetSupported(config); Field("Names"); Kind("Packages", true);
            foreach (string name in new[] { "Exact name", "Search memos", "Memo only" }) Kind(name, true);
            string before = Criteria();
            var scopes = Scopes(); Require(scopes.Length == 1, "Expected one exact disposable scope.");
            Toggle(scopes[0], false); Toggle(scopes[0], true);
            Select(Find(Window!, ControlType.TabItem, "Assets")); Select(Find(Window!, ControlType.TabItem, "Packages"));
            Select(Find(Window!, ControlType.TabItem, "Search"));
            VerifyFieldSelection(Find(Controls(), ControlType.ComboBox, null), "Names");
            CheckToggle(Controls(), "Packages", ToggleState.On); CheckToggle(Controls(), "Sources", ToggleState.On);
            CheckToggle(Controls(), "Assets", ToggleState.Off); Check(Criteria() == before, "Criteria changed across scope/workspace transitions.");
        });
        Case("CP21-06-catalog-retention", () =>
        {
            ResetSupported(config); Field("Names"); Kind("Packages", true); string before = Criteria();
            PrepareLegacy(config, work); OpenCatalog(work);
            var refresh = Find(Window!, ControlType.Button, "Refresh");
            Require(refresh.IsEnabled, "Catalog Refresh is disabled."); Invoke(refresh);
            Wait("catalog Refresh completion", () => refresh.IsEnabled);
            Select(Find(Window!, ControlType.TabItem, "Search"));
            VerifyFieldSelection(Find(Controls(), ControlType.ComboBox, null), "Names");
            CheckToggle(Controls(), "Packages", ToggleState.On); CheckToggle(Controls(), "Sources", ToggleState.On);
            CheckToggle(Controls(), "Assets", ToggleState.Off);
            Check(Criteria() == before, "Criteria changed across catalog reload.");
        });
        Case("CP21-07-explicit-refresh", () =>
        {
            ResetSupported(config); Search(config);
            SetValue(ById(Controls(), "IncludeSearchTextBox"), "__checkpoint21_no_match__");
            Rows(config.ExpectedRows); // Text change alone must not rerun Search.
            ExecuteRefresh();
            Wait("explicit Refresh empty result", () => Grid().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.DataItem)).Length == 0);
            var counts = Window!.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Text)).Select(e => e.Name);
            Check(counts.Contains("0–0 displayed / 0 exact indexed matches"), "Refresh did not publish exact empty results.");
            SetValue(ById(Controls(), "IncludeSearchTextBox"), config.Keyword); ExecuteRefresh();
            Wait("Refresh restored query", () => Grid().FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.DataItem)).Length > 0);
            Rows(config.ExpectedRows);
        });
        Case("CP21-08-sorting", () =>
        {
            ResetSupported(config); Require(config.ExpectedSortRows is { Length: > 1 }, "Independent multirow sort oracle is required.");
            SetValue(ById(Controls(), "IncludeSearchTextBox"), "asset"); ExecuteSearch();
            Wait("multirow query", () => Grid().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.DataItem)).Length >= config.ExpectedSortRows!.Length);
            Rows(config.ExpectedSortRows!);
            var header = Find(Grid(), ControlType.HeaderItem, "Name");
            InteractionEvidence("name-sort", header);
            SearchAccessibility.RequireSortInvoke(header.Patterns.Invoke.IsSupported);
            var before = ReadAccessibleRows(Grid()); Invoke(header);
            Wait("first Name sort changes order", () => !ReadAccessibleRows(Grid()).SequenceEqual(before));
            var first = ReadAccessibleRows(Grid()); Invoke(header);
            Wait("second Name sort reverses order", () => ReadAccessibleRows(Grid()).SequenceEqual(first.Reverse()));
            var second = ReadAccessibleRows(Grid());
            Check(first.Length > 1 && first.SequenceEqual(second.Reverse()), "Name sort did not reverse the complete global order.");
            Rows(config.ExpectedSortRows!);
        });
        Case("CP21-09-selection", () =>
        {
            ResetSupported(config); Search(config);
            var row = Find(Grid(), ControlType.DataItem, null);
            InteractionEvidence("result-selection", row);
            if (row.Patterns.SelectionItem.IsSupported)
            {
                Select(row); Check(row.Patterns.SelectionItem.Pattern.IsSelected.Value, "Result selection did not persist.");
                Field("Names"); ExecuteSearch(); Error("Select Assets");
                Check(row.Patterns.SelectionItem.Pattern.IsSelected.Value, "Compatibility rejection lost confirmed selection.");
            }
            else VerifyKeyboardSelectionRetention(config, work);
        });
        Case("CP21-10-mixed-formats", () =>
        {
            ResetSupported(config); PrepareLegacy(config, work); OpenCatalog(work);
            Select(Find(Window!, ControlType.TabItem, "Search"));
            Require(Scopes().Length == 2, "Expected exact V1/V2 disposable scopes.");
            foreach (var scope in Scopes()) Toggle(scope, true);
            ExecuteSearch(); Error("Mixed-format");
            Check(Scopes().All(e => e.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On), "Mixed-format rejection changed package scope.");
        });
        return AcceptanceStatus();
    }

    private static string PrepareLegacy(Scenario config, string work)
    {
        string source = Path.Combine(config.Repository, "Avalonia/UABS.UnityAssets.Tests/Fixtures/LegacyPackage");
        string target = Path.Combine(work, "LegacyPackage");
        foreach (string relative in new[] { "Validation.txt", "index.json", "nested/index.json" })
        {
            string input = Path.Combine(source, relative), output = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            string hash = Hash(input);
            if (!File.Exists(output)) File.Copy(input, output);
            Check(Hash(output) == hash, "Legacy fixture copy SHA differs: " + relative);
            Step("legacy fixture copy", new { relative, sha256 = hash });
        }
        return target;
    }
    private static void OpenCatalog(string directory)
    {
        Select(Find(Window!, ControlType.TabItem, "Packages"));
        var button = Find(Window!, ControlType.Button, "Open Package Catalog...");
        Require(button.Patterns.Invoke.IsSupported, "Catalog button requires Invoke.");
        var task = Task.Run(() => button.Patterns.Invoke.Pattern.Invoke());
        const string title = "Open package catalog root or package directory";
        AutomationElement? dialog = null;
        try
        {
            Wait("owned catalog folder picker", () =>
            {
                if (task.IsFaulted) task.GetAwaiter().GetResult();
                var windows = ProcessWindows().Where(e => e.Name == title).ToArray();
                Require(windows.Length <= 1, "Ambiguous owned folder picker."); dialog = windows.SingleOrDefault(); return dialog != null;
            });
            Evidence("catalog-dialog", dialog);
            var edit = PackageAdmission.WaitForUnique(() => dialog!.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Edit)
                .And(Automation.ConditionFactory.ByAutomationId("1152"))), TimeSpan.FromSeconds(10), description: "catalog folder Edit 1152");
            SetValue(edit, Path.GetFullPath(directory));
            Check(edit.Patterns.Value.Pattern.Value.Value == Path.GetFullPath(directory), "Folder path readback differs.");
            Invoke(ById(dialog!, "1"));
            Wait("catalog load completion", () => Window!.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Text))
                .Any(e => e.Name.StartsWith("Found ", StringComparison.Ordinal) && e.Name.Contains("0 error(s)", StringComparison.Ordinal)));
            Wait("catalog command completed", () => button.IsEnabled);
            Wait("catalog Invoke returned", () => task.IsCompleted); task.GetAwaiter().GetResult();
        }
        catch
        {
            if (dialog != null)
            {
                try { Evidence("catalog-failure", dialog); Invoke(ById(dialog, "2")); }
                catch (Exception error) { Step("catalog diagnostic/cancel unavailable", error.Message); }
            }
            throw;
        }
    }
    private static string RunFreshV1(Scenario config, string work)
    {
        Case("CP21-11-fresh-v1", () =>
        {
            string legacy = PrepareLegacy(config, work); OpenCatalog(legacy);
            Select(Find(Window!, ControlType.TabItem, "Search"));
            var scopes = Scopes(); Require(scopes.Length == 1 && scopes[0].Name.StartsWith("[V1] ", StringComparison.Ordinal), "Fresh workflow must have V1 only.");
            foreach (string kind in new[] { "Packages", "Sources", "Assets" }) CheckToggle(Controls(), kind, ToggleState.On);
            VerifyFieldSelection(Find(Controls(), ControlType.ComboBox, null), "Names");
            SetValue(ById(Controls(), "IncludeSearchTextBox"), "portrait"); ExecuteSearch();
            Wait("V1 exact result", () => Grid().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.DataItem)).Length == 1);
            var headers = Grid().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.HeaderItem)).Select(e => e.Name).ToArray();
            Require(headers.Length == 6 && headers.Skip(1).SequenceEqual(new[] { "Name", "Package", "Source", "Path ID", "Memo" }), "V1 six-column identities unavailable.");
            var row = Find(Grid(), ControlType.DataItem, null);
            var cells = row.FindAllDescendants(Automation.ConditionFactory.ByClassName("DataGridCell"));
            Require(cells.Length == 6, "V1 exact cell structure unavailable.");
            var values = cells.Skip(1).Select(cell => Find(cell, ControlType.Text, null).Name).ToArray();
            Check(values.SequenceEqual(new[] { "portrait", "LegacyPackage", "character.ab", "1001", "legacy memo" }), "V1 expected asset identity differs.");
            Check(Window!.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.Text)).Any(e => e.Name == "1 loaded / 1 exact"), "V1 complete result count unverified.");
            Step("V1 exact row", values);
        });
        return AcceptanceStatus();
    }
}
