using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

internal static partial class Program
{
    private static AutomationElement NameSortButton()
    {
        var button = ById(Window!, "SearchNameSortButton");
        SearchAccessibility.RequireNameSortTarget(button.ControlType == ControlType.Button && button.Name == "Sort Name",
            !button.IsOffscreen, button.IsEnabled, Window!.Properties.ProcessId.Value == App!.Id
                && button.Properties.ProcessId.Value == App.Id, button.Patterns.Invoke.IsSupported);
        return button;
    }

    private static NameSortState ReadNameSortButton() =>
        SearchAccessibility.ReadNameSortState(NameSortButton().Properties.HelpText.Value);

    private static void VerifyAccessibleNameSorting(ExpectedRow[] expected)
    {
        // Validate the independent oracle before interacting. Never derive expected order from UIA.
        SearchAccessibility.NameOrderOracle(expected, "Ascending");
        PrepareSelectionWindowLayout("CP21-08");
        var before = CaptureVisualRowOrder("before-sort", expected, "CP21-08");
        InteractionEvidence("name-sort", ById(Window!, "SearchNameSortButton"));
        var directions = new HashSet<string>(StringComparer.Ordinal);
        for (int action = 0; action < 2; action++)
        {
            NameSortState? start = null;
            Wait("Name sort ready", () => { start = ReadNameSortButton(); return !start.Busy; });
            string direction = start!.Next;
            Step("Name sort activation", new { action, input = action == 0 ? "UIA Invoke" : "focused Enter", start, expected = SearchAccessibility.NameOrderOracle(expected, direction) });
            if (action == 0) Invoke(NameSortButton());
            else
            {
                var button = NameSortButton();
                Require(button.Properties.IsKeyboardFocusable.Value, "Sort Name Button must support keyboard focus.");
                Window!.SetForeground();
                button.FrameworkAutomationElement.SetFocus();
                Wait("Sort Name keyboard focus", () => button.Properties.HasKeyboardFocus.Value);
                bool owned = button.Properties.ProcessId.Value == App!.Id
                    && Window.Properties.NativeWindowHandle.Value != IntPtr.Zero
                    && GetForegroundWindow() == Window.Properties.NativeWindowHandle.Value
                    && button.Properties.HasKeyboardFocus.Value;
                Step("Name sort keyboard ownership", new { owned, processId = button.Properties.ProcessId.Value,
                    foreground = GetForegroundWindow().ToInt64(), window = Window.Properties.NativeWindowHandle.Value.ToInt64() });
                SearchAccessibility.GuardedSelectionKeys(() => { }, () => button.Properties.ProcessId.Value == App!.Id
                    && button.Properties.HasKeyboardFocus.Value && Window.Properties.NativeWindowHandle.Value != IntPtr.Zero
                    && GetForegroundWindow() == Window.Properties.NativeWindowHandle.Value, () =>
                {
                    try { Keyboard.Type(VirtualKeyShort.RETURN); }
                    finally { Keyboard.Release(VirtualKeyShort.RETURN); }
                }, "Sort Name Button");
            }
            NameSortState? completed = null;
            Wait("Name sort completed " + direction, () =>
            {
                Check(!Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("SearchErrorText"))
                    .Any(e => !e.IsOffscreen && !string.IsNullOrWhiteSpace(e.Name)), "Name sort reported an application error.");
                completed = ReadNameSortButton();
                return !completed.Busy && completed.Current == direction
                    && Find(Grid(), ControlType.HeaderItem, "Name").Properties.HelpText.Value == "Sort all matching V2 results; " + direction;
            });
            Check(!Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("SearchErrorText"))
                .Any(e => !e.IsOffscreen && !string.IsNullOrWhiteSpace(e.Name)), "Name sort reported an application error.");
            var actual = CaptureVisualRowOrder("after-" + direction, expected, "CP21-08");
            var report = new { action, start, completed, before, expected = SearchAccessibility.NameOrderOracle(expected, direction), actual };
            File.WriteAllText(Path.Combine(Output, "CP21-08-order-" + direction + ".json"), JsonSerializer.Serialize(report, Json));
            SearchAccessibility.VerifyNameOrder(actual, expected, direction);
            directions.Add(direction);
            Evidence("CP21-08-" + direction);
            before = actual;
        }
        Check(directions.SetEquals(new[] { "Ascending", "Descending" }), "Both exact Name directions must be exercised.");
        Rows(expected);
    }
}
