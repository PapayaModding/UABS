using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

internal static partial class Program
{
    private static void PrepareSelectionWindowLayout()
    {
        LayoutEvidence("before-maximize");
        try
        {
            Require(Window!.Properties.ProcessId.Value == App!.Id, "Layout target must be the launched UABS window.");
            bool supported = Window.Patterns.Window.IsSupported;
            bool maximized = supported && Window.Patterns.Window.Pattern.WindowVisualState.Value == WindowVisualState.Maximized;
            bool canMaximize = supported && Window.Patterns.Window.Pattern.CanMaximize.Value;
            SearchAccessibility.PrepareMaximizedWindow(supported, maximized, canMaximize,
                () => Window.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Maximized));
            Wait("maximized test window", () => Window.Patterns.Window.Pattern.WindowVisualState.Value == WindowVisualState.Maximized);
            Step("CP21-09 test window", "UIA WindowVisualState.Maximized; existing panel widths/splitters unchanged.");
        }
        catch (Exception error) when (error is not Blocked && error is not AccessibilityUnavailable)
        { throw new Blocked("Could not establish maximized test window: " + error); }
        finally { LayoutEvidence("after-maximize"); }
    }

    private static void LayoutEvidence(string label)
    {
        try
        {
            object Bounds(AutomationElement element)
            {
                var rect = element.BoundingRectangle;
                return new UiBounds(rect.Left, rect.Top, rect.Width, rect.Height);
            }
            var targets = new[] { "ResultsGrid", "SearchControlsScroller", "SearchControlsSplitter",
                "SearchInspectorPanel", "SearchInspectorSplitter", "SearchInspectorCollapseButton", "V2SearchDetails" };
            var elements = targets.Select(id => new
            {
                id,
                matches = EvidenceValue.Read(() => Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId(id)).Select(element => new
                {
                    bounds = EvidenceValue.Read(() => Bounds(element)),
                    offscreen = EvidenceValue.Read(() => element.IsOffscreen),
                    className = EvidenceValue.Read(() => element.ClassName)
                }).ToArray())
            }).ToArray();
            var viewports = EvidenceValue.Read(() => Grid().FindAllDescendants().Where(e => e.ControlType is ControlType.Pane or ControlType.ScrollBar).Take(20)
                .Select(element => new
                {
                    id = EvidenceValue.Read(() => element.AutomationId),
                    className = EvidenceValue.Read(() => element.ClassName),
                    bounds = EvidenceValue.Read(() => Bounds(element)),
                    scrollPattern = EvidenceValue.Read(() => element.Patterns.Scroll.IsSupported),
                    verticallyScrollable = EvidenceValue.Read(() => element.Patterns.Scroll.Pattern.VerticallyScrollable.Value),
                    verticalScrollPercent = EvidenceValue.Read(() => element.Patterns.Scroll.Pattern.VerticalScrollPercent.Value),
                    verticalViewSize = EvidenceValue.Read(() => element.Patterns.Scroll.Pattern.VerticalViewSize.Value)
                }).ToArray());
            var headers = EvidenceValue.Read(() => Grid().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.HeaderItem))
                .Select(element => new { name = EvidenceValue.Read(() => element.Name), bounds = EvidenceValue.Read(() => Bounds(element)) }).ToArray());
            object WorkArea()
            {
                var monitor = System.Windows.Forms.Screen.FromHandle(Window!.Properties.NativeWindowHandle.Value);
                return new UiBounds(monitor.WorkingArea.Left, monitor.WorkingArea.Top, monitor.WorkingArea.Width, monitor.WorkingArea.Height);
            }
            var report = new
            {
                label,
                windowBounds = EvidenceValue.Read(() => Bounds(Window!)),
                windowState = EvidenceValue.Read(() => Window!.Patterns.Window.Pattern.WindowVisualState.Value.ToString()),
                workArea = EvidenceValue.Read(WorkArea),
                elements,
                viewports, headers
            };
            Step("window layout " + label, report);
            File.WriteAllText(Path.Combine(Output, "CP21-09-window-" + label + ".json"), JsonSerializer.Serialize(report, Json));
        }
        catch (Exception error) { Step("window layout diagnostic error", error.ToString()); }
    }
}
