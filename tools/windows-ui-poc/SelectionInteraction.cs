using System.Runtime.InteropServices;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

internal static partial class Program
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private static void InteractionEvidence(string label, AutomationElement element)
    {
        object Read(AutomationElement e) => new
        {
            id = EvidenceValue.Read(() => e.AutomationId),
            name = EvidenceValue.Read(() => e.Name),
            type = EvidenceValue.Read(() => e.ControlType.ToString()),
            className = EvidenceValue.Read(() => e.ClassName),
            processId = EvidenceValue.Read(() => e.Properties.ProcessId.Value),
            keyboardFocusable = EvidenceValue.Read(() => e.Properties.IsKeyboardFocusable.Value),
            hasKeyboardFocus = EvidenceValue.Read(() => e.Properties.HasKeyboardFocus.Value),
            helpText = EvidenceValue.Read(() => e.Properties.HelpText.Value),
            accessKey = EvidenceValue.Read(() => e.Properties.AccessKey.Value),
            acceleratorKey = EvidenceValue.Read(() => e.Properties.AcceleratorKey.Value),
            invoke = EvidenceValue.Read(() => e.Patterns.Invoke.IsSupported),
            selectionItem = EvidenceValue.Read(() => e.Patterns.SelectionItem.IsSupported),
            selection = EvidenceValue.Read(() => e.Patterns.Selection.IsSupported),
            legacy = EvidenceValue.Read(() => e.Patterns.LegacyIAccessible.IsSupported),
            grid = EvidenceValue.Read(() => e.Patterns.Grid.IsSupported)
        };
        var snapshot = new { target = Read(element), descendants = element.FindAllDescendants().Take(20).Select(Read).ToArray() };
        Step(label + " capabilities", snapshot);
        File.WriteAllText(Path.Combine(Output, label + "-capabilities.json"), JsonSerializer.Serialize(snapshot, Json));
    }

    private static void SelectBoundaryWithKeyboard(bool last)
    {
        var grid = Grid();
        InteractionEvidence("grid-keyboard", grid);
        SearchAccessibility.GuardedSelectionKeys(() =>
        {
            Require(Window!.Properties.ProcessId.Value == App!.Id && grid.Properties.ProcessId.Value == App.Id,
                "Keyboard selection target must belong to the launched UABS process.");
            Require(grid.IsEnabled && grid.Properties.IsKeyboardFocusable.Value, "DataGrid must be enabled and keyboard-focusable.");
            Window.SetForeground();
            // Use the actual UIA SetFocus provider, not focus by window coordinates.
            grid.FrameworkAutomationElement.SetFocus();
            Wait("UIA DataGrid keyboard focus", () => grid.Properties.HasKeyboardFocus.Value);
        }, () =>
        {
            int processId = grid.Properties.ProcessId.Value;
            bool focused = grid.Properties.HasKeyboardFocus.Value;
            IntPtr handle = Window!.Properties.NativeWindowHandle.Value, foreground = GetForegroundWindow();
            Step("keyboard target verification", new
            {
                processId,
                expectedProcessId = App!.Id,
                focused,
                windowHandle = handle.ToInt64(),
                foregroundHandle = foreground.ToInt64()
            });
            return processId == App.Id && focused && handle != IntPtr.Zero && foreground == handle;
        },
        () =>
        {
            try { Keyboard.Press(VirtualKeyShort.CONTROL); Keyboard.Type(last ? VirtualKeyShort.END : VirtualKeyShort.HOME); }
            finally { Keyboard.Release(VirtualKeyShort.CONTROL); }
        });
        Step("owned grid keyboard selection", last ? "Ctrl+End" : "Ctrl+Home");
    }

    private static string? ReadSelectedInspector()
    {
        var matches = Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("V2SearchDetails"));
        Require(matches.Length <= 1, "Ambiguous selected-source Inspector.");
        if (matches.Length == 0 || matches[0].IsOffscreen) return null;
        var details = matches[0];
        if (details.Properties.Name.TryGetValue(out var name) && !string.IsNullOrWhiteSpace(name)) return name;
        if (details.Patterns.Text.IsSupported) return details.Patterns.Text.Pattern.DocumentRange.GetText(-1);
        throw new Blocked("Selected-source Inspector lacks readable Name/Text; keyboard selection cannot be verified.");
    }

    private static void VerifyKeyboardSelectionRetention(Scenario config, string work)
    {
        Require(config.ExpectedSortRows is { Length: > 1 }, "Independent multirow fixture oracle is needed to prove selection moves.");
        PrepareSelectionWindowLayout();
        SetValue(ById(Controls(), "IncludeSearchTextBox"), "asset"); ExecuteSearch();
        Wait("selection multirow query", () => Grid().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.DataItem)).Length >= config.ExpectedSortRows!.Length);
        Rows(config.ExpectedSortRows!);
        var enumeration = ReadAccessibleRows(Grid());
        var visualOrder = CaptureVisualRowOrder("before-keys", config.ExpectedSortRows!);
        string copy = Path.Combine(work, Path.GetFileName(config.Package));
        SelectBoundaryWithKeyboard(false);
        string first = WaitForInspectorIdentity("first", visualOrder[0], copy, enumeration, visualOrder);
        Check(CaptureVisualRowOrder("after-home", config.ExpectedSortRows!).SequenceEqual(visualOrder), "Visual result order changed during Ctrl+Home selection.");
        Step("first selected Inspector", first);
        Evidence("CP21-09-first-selection");
        SelectBoundaryWithKeyboard(true);
        string last = WaitForInspectorIdentity("last", visualOrder[^1], copy, enumeration, visualOrder);
        Check(CaptureVisualRowOrder("after-end", config.ExpectedSortRows!).SequenceEqual(visualOrder), "Visual result order changed during Ctrl+End selection.");
        Check(first != last, "Keyboard navigation did not change the selected source identity.");
        Step("last selected Inspector", last);
        Evidence("CP21-09-last-selection");
        Field("Names"); ExecuteSearch(); Error("Select Assets");
        string? retained = ReadSelectedInspector();
        Step("Inspector after rejection", retained ?? "unavailable");
        Require(retained is not null, "Inspector is unavailable after compatibility rejection.");
        Check(retained == last, "Compatibility rejection changed the selected immutable source Inspector identity.");
        Rows(config.ExpectedSortRows!);
        Check(CaptureVisualRowOrder("after-rejection", config.ExpectedSortRows!).SequenceEqual(visualOrder), "Compatibility rejection changed visual row order.");
        Check(Hash(copy).Equals(config.PackageSha256, StringComparison.OrdinalIgnoreCase), "Disposable package hash changed during selection test.");
    }

    private static string WaitForInspectorIdentity(string boundary, ExpectedRow expected, string copy,
        ExpectedRow[] enumeration, ExpectedRow[] visualOrder)
    {
        string? observed = null;
        try
        {
            Wait(boundary + "-row selected Inspector identity", () =>
            {
                observed = ReadSelectedInspector();
                return SearchAccessibility.SelectedInspectorMatches(observed, expected, copy);
            });
            return observed!;
        }
        finally
        {
            // The previous runner only recorded Inspector text AFTER matching,
            // so a timeout discarded the very evidence needed to diagnose it.
            try
            {
                var inspectors = Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("V2SearchDetails"))
                    .Select(e => new
                    {
                        name = EvidenceValue.Read(() => e.Name),
                        offscreen = EvidenceValue.Read(() => e.IsOffscreen),
                        text = EvidenceValue.Read(() => e.Patterns.Text.Pattern.DocumentRange.GetText(-1)),
                        comparisonFromRawName = EvidenceValue.Read(() => SearchAccessibility.ExplainInspector(e.Name, expected, copy))
                    }).ToArray();
                var headers = Grid().FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.HeaderItem))
                    .Select(e => new { name = EvidenceValue.Read(() => e.Name), helpText = EvidenceValue.Read(() => e.Properties.HelpText.Value) }).ToArray();
                var report = new
                {
                    boundary,
                    expected,
                    expectedPackagePath = copy,
                    uiaDescendantRowOrder = enumeration,
                    visualRowOrder = visualOrder,
                    comparison = SearchAccessibility.ExplainInspector(observed, expected, copy),
                    inspectors,
                    headers,
                    gridHasKeyboardFocus = EvidenceValue.Read(() => Grid().Properties.HasKeyboardFocus.Value)
                };
                Step(boundary + " Inspector comparison", report);
                File.WriteAllText(Path.Combine(Output, "CP21-09-" + boundary + "-identity.json"), JsonSerializer.Serialize(report, Json));
            }
            catch (Exception error) { Step("Inspector comparison diagnostic error", error.ToString()); }
        }
    }

    private static ExpectedRow[] CaptureVisualRowOrder(string label, ExpectedRow[] expected)
    {
        Rows(expected); // Keep independent complete-set/count validation unchanged.
        var observations = new List<PositionedRow>();
        UiBounds? gridBounds = null, viewport = null;
        UiBounds[]? headers = null;
        ExpectedRow[]? ordered = null;
        string? failure = null;
        bool rowSetMismatch = false;
        try
        {
            var grid = Grid();
            UiBounds Bounds(AutomationElement element)
            {
                var rect = element.BoundingRectangle;
                return new(rect.Left, rect.Top, rect.Width, rect.Height);
            }
            gridBounds = Bounds(grid);
            headers = grid.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.HeaderItem)).Select(Bounds).ToArray();
            Require(gridBounds.Valid && headers.Length == 2 && headers.All(header => header.Valid), "Usable grid and Name/Package header bounds are required.");
            double top = Math.Max(gridBounds.Top, headers.Max(header => header.Bottom));
            viewport = new(gridBounds.Left, top, gridBounds.Width, gridBounds.Bottom - top);
            foreach (var row in grid.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.DataItem)))
            {
                var cells = row.FindAllDescendants(Automation.ConditionFactory.ByClassName("DataGridCell"));
                Require(cells.Length == 2, "Positional row must expose exactly two Name/Package cells.");
                var identity = cells.Select(cell =>
                {
                    var text = cell.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.Text));
                    Require(text.Length == 1, "Positional cell must expose exactly one readable Text.");
                    return text[0].Name;
                }).ToArray();
                observations.Add(new(new(identity[0], identity[1]), Bounds(row), row.IsOffscreen));
            }
            rowSetMismatch = !observations.Select(row => row.Identity).OrderBy(row => row.Name).ThenBy(row => row.Package)
                .SequenceEqual(expected.OrderBy(row => row.Name).ThenBy(row => row.Package));
            Check(!rowSetMismatch, "Positional capture differs from the independent complete expected row set.");
            ordered = SearchAccessibility.VisualRowOrder(observations.ToArray(), viewport);
            return ordered;
        }
        catch (Exception error)
        {
            failure = error.ToString();
            if (error is NotSupportedException or AccessibilityUnavailable or Blocked) throw;
            // A readable but wrong row multiset is an assertion failure, not a capability gap.
            if (rowSetMismatch) throw;
            throw new Blocked("Visual boundary order could not be established: " + error.Message);
        }
        finally
        {
            var report = new
            {
                label,
                expected,
                gridBounds,
                headers,
                viewport,
                uiaDescendantRows = observations,
                visualRowOrder = ordered,
                failure
            };
            Step("visual boundary evidence " + label, report);
            try { File.WriteAllText(Path.Combine(Output, "CP21-09-layout-" + label + ".json"), JsonSerializer.Serialize(report, Json)); }
            catch (Exception error) { Step("layout diagnostic write error", error.ToString()); }
        }
    }
}
