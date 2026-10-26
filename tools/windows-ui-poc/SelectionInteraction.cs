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
        SetValue(ById(Controls(), "IncludeSearchTextBox"), "asset"); ExecuteSearch();
        Wait("selection multirow query", () => Grid().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.DataItem)).Length >= config.ExpectedSortRows!.Length);
        Rows(config.ExpectedSortRows!);
        var ordered = ReadAccessibleRows(Grid());
        string copy = Path.Combine(work, Path.GetFileName(config.Package));
        SelectBoundaryWithKeyboard(false);
        Wait("first-row selected Inspector identity", () => SearchAccessibility.SelectedInspectorMatches(ReadSelectedInspector(), ordered[0], copy));
        string first = ReadSelectedInspector()!;
        Step("first selected Inspector", first);
        Evidence("CP21-09-first-selection");
        SelectBoundaryWithKeyboard(true);
        Wait("last-row selected Inspector identity", () => SearchAccessibility.SelectedInspectorMatches(ReadSelectedInspector(), ordered[^1], copy));
        string last = ReadSelectedInspector()!;
        Check(first != last, "Keyboard navigation did not change the selected source identity.");
        Step("last selected Inspector", last);
        Evidence("CP21-09-last-selection");
        Field("Names"); ExecuteSearch(); Error("Select Assets");
        string? retained = ReadSelectedInspector();
        Step("Inspector after rejection", retained ?? "unavailable");
        Require(retained is not null, "Inspector is unavailable after compatibility rejection.");
        Check(retained == last, "Compatibility rejection changed the selected immutable source Inspector identity.");
        Rows(config.ExpectedSortRows!);
        Check(Hash(copy).Equals(config.PackageSha256, StringComparison.OrdinalIgnoreCase), "Disposable package hash changed during selection test.");
    }
}
