using System.Runtime.InteropServices;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

internal static partial class Program
{
    private static void OpenCatalogWithFreshReads(string directory)
    {
        const string title = "Open package catalog root or package directory";
        var failures = new List<object>();
        Task? invocation = null;
        IntPtr ownerHandle = IntPtr.Zero;
        string phase = "owner discovery";
        string? failure = null;
        string? failedPhase = null;
        T Read<T>(string name, Func<T> query)
        {
            phase = name;
            try
            {
                return UiaRead.Fresh(name, query, (attempt, error) =>
                {
                    var record = new
                    {
                        phase = name,
                        attempt,
                        hresult = $"0x{error.HResult:X8}",
                        utc = DateTime.UtcNow,
                        error = error.ToString()
                    };
                    failures.Add(record); Step("catalog provider query failure", record);
                });
            }
            catch (AccessibilityUnavailable error) when (App?.HasExited == true)
            { throw new InvalidOperationException("UABS exited during catalog query " + name, error); }
        }
        AutomationElement Owner()
        {
            var owner = Automation!.FromHandle(ownerHandle);
            Require(owner.Properties.ProcessId.Value == App!.Id && owner.ControlType == ControlType.Window
                && owner.Properties.NativeWindowHandle.Value == ownerHandle, "Fresh catalog owner identity differs from the launched UABS window.");
            Window = owner; // New UIA wrapper for the same native window, not a replacement application.
            return owner;
        }
        AutomationElement? Picker()
        {
            var filter = Automation!.ConditionFactory.ByProcessId(App!.Id)
                .And(Automation.ConditionFactory.ByControlType(ControlType.Window))
                .And(Automation.ConditionFactory.ByName(title));
            // Avoid walking the disabled owner tree when a top-level owned picker is already found.
            var matches = Automation.GetDesktop().FindAllChildren(filter);
            if (matches.Length == 0) matches = Owner().FindAllDescendants(filter);
            Require(matches.Length <= 1, "Ambiguous exact owned catalog picker.");
            var picker = matches.SingleOrDefault();
            if (picker != null) Require(picker.Properties.ProcessId.Value == App.Id && picker.Name == title,
                "Catalog picker process/title identity differs.");
            return picker;
        }
        AutomationElement RequiredPicker() => Picker() ?? throw new Blocked("Exact owned catalog picker disappeared before submission.");
        try
        {
            App!.Refresh(); Check(!App.HasExited, "UABS exited before catalog opening."); ownerHandle = App.MainWindowHandle;
            Require(ownerHandle != IntPtr.Zero, "Launched UABS main-window HWND is unavailable.");
            var tab = Read("Packages tab discovery", () => Find(Owner(), ControlType.TabItem, "Packages"));
            phase = "select Packages"; Select(tab); // Actions are deliberately never retried.
            var button = Read("catalog button discovery", () => Find(Owner(), ControlType.Button, "Open Package Catalog..."));
            Require(button.IsEnabled && button.Patterns.Invoke.IsSupported, "Catalog button must be enabled with Invoke support.");
            phase = "invoke catalog"; invocation = Task.Run(() => button.Patterns.Invoke.Pattern.Invoke());
            Wait("owned catalog folder picker", () =>
            {
                if (invocation.IsFaulted) { phase = "catalog Invoke task fault"; invocation.GetAwaiter().GetResult(); }
                return Read("exact picker discovery", Picker) != null;
            });
            Read("picker evidence", () => { Evidence("catalog-dialog", RequiredPicker()); return true; });
            var edit = PackageAdmission.WaitForUnique(() => Read("folder Edit discovery", () => RequiredPicker()
                .FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Edit)
                    .And(Automation.ConditionFactory.ByAutomationId("1152")))), TimeSpan.FromSeconds(10),
                description: "catalog folder Edit 1152");
            phase = "set folder path"; SetValue(edit, Path.GetFullPath(directory));
            string readback = Read("folder value readback", () =>
            {
                var current = RequiredPicker().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Edit)
                    .And(Automation.ConditionFactory.ByAutomationId("1152")));
                Require(current.Length == 1 && current[0].Patterns.Value.IsSupported, "Folder Edit is ambiguous or lacks Value.");
                return current[0].Patterns.Value.Pattern.Value.Value;
            });
            Check(readback == Path.GetFullPath(directory), "Folder path readback differs.");
            var open = Read("folder Open discovery", () => ById(RequiredPicker(), "1"));
            phase = "submit folder"; Invoke(open);
            Wait("fresh catalog load completion", () => Read("catalog completion tree", () =>
            {
                if (Picker() != null) return false;
                var owner = Owner();
                if (!Find(owner, ControlType.Button, "Open Package Catalog...").IsEnabled) return false;
                var text = owner.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Text)).Select(e => e.Name).ToArray();
                Check(!text.Contains("No package directories were found."), "Application did not load the disposable catalog.");
                var statuses = text.Where(value => value.StartsWith("Found ", StringComparison.Ordinal)).ToArray();
                if (statuses.Length == 0) return false;
                Check(statuses.Length == 1 && statuses[0].Contains("0 error(s)", StringComparison.Ordinal), "Catalog reported load errors: " + string.Join("; ", statuses));
                return true;
            }));
            phase = "catalog Invoke return";
            Wait("catalog Invoke returned", () => invocation.IsCompleted); invocation.GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            failure = error.ToString();
            failedPhase = phase;
            try
            {
                var picker = Read("cleanup picker discovery", Picker);
                if (picker != null)
                {
                    try { Evidence("catalog-failure", picker); } catch (Exception diagnostic) { Step("catalog failure evidence unavailable", diagnostic.ToString()); }
                    var cancel = Read("cleanup Cancel discovery", () => ById(RequiredPicker(), "2"));
                    Invoke(cancel); // At most once; cleanup failure cannot turn the primary failure into PASS.
                }
            }
            catch (Exception cleanup) { Step("catalog cleanup unavailable", cleanup.ToString()); }
            if (error is COMException provider)
                throw new Blocked($"Catalog UIA provider/action inaccessible in {failedPhase}: 0x{provider.HResult:X8}. Action was not replayed. {provider}");
            throw;
        }
        finally
        {
            var report = new
            {
                expectedTitle = title,
                expectedProcessId = App!.Id,
                ownerHandle = ownerHandle.ToInt64(),
                directory,
                phase,
                failedPhase,
                invocation = invocation?.Status.ToString(),
                invocationError = invocation?.Exception?.ToString(),
                applicationExited = EvidenceValue.Read(() => App!.HasExited),
                failures,
                failure
            };
            Step("catalog provider report", report);
            try { File.WriteAllText(Path.Combine(Output, $"catalog-provider-{Steps.Count}.json"), JsonSerializer.Serialize(report, Json)); }
            catch (Exception diagnostic) { Step("catalog provider report unavailable", diagnostic.ToString()); }
        }
    }
}
