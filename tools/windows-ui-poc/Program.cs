using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

internal sealed record Scenario(string Repository, string ExpectedCommit, string Package,
    string PackageSha256, string Keyword, ExpectedRow[] ExpectedRows,
    string? Acceptance = null, ExpectedRow[]? ExpectedSortRows = null, PackageSortFixture[]? PackageSortFixtures = null,
    IndeterminateFixture[]? IndeterminateFixtures = null);
internal sealed class Blocked(string message) : Exception(message);

internal static partial class Program
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static readonly List<object> Steps = [];
    private static string Output = "";
    private static Process? App;
    private static AutomationElement? Window;
    private static UIA3Automation? Automation;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 2) { Console.Error.WriteLine("Usage: WindowsUiPoc <scenario.json> <new-output-directory>"); return 2; }
        Output = Path.GetFullPath(args[1]);
        if (Directory.Exists(Output)) { Console.Error.WriteLine("Output must be a new directory."); return 2; }
        Directory.CreateDirectory(Output);
        string status = "BLOCKED", detail = "", stage = "preflight";
        string? sourceHash = null;
        Scenario? config = null;
        try
        {
            Require(OperatingSystem.IsWindows() && Environment.UserInteractive, "Requires interactive Windows desktop.");
            config = JsonSerializer.Deserialize<Scenario>(File.ReadAllText(args[0]), Json) ?? throw new Blocked("Invalid scenario JSON.");
            Require(config.Acceptance is null or "checkpoint21-v2" or "checkpoint21-v1" or "checkpoint21-gaps" or "checkpoint21-package-sort" or "checkpoint21-indeterminate", "Unknown acceptance scenario.");
            Require(Git(config.Repository, "branch", "--show-current") == "v2-recovery", "Repository must be on v2-recovery.");
            string remote = Git(config.Repository, "remote", "get-url", "origin").TrimEnd('/');
            Require(remote is "https://github.com/PapayaModding/UABS.git" or "https://github.com/PapayaModding/UABS" or "git@github.com:PapayaModding/UABS.git", "Unexpected origin repository.");
            Require(config.ExpectedCommit.Length == 40 && Git(config.Repository, "rev-parse", "HEAD").Equals(config.ExpectedCommit, StringComparison.OrdinalIgnoreCase), "Checkout SHA does not match expectedCommit.");
            Require(Git(config.Repository, "diff", "--name-only").Length == 0 && Git(config.Repository, "diff", "--cached", "--name-only").Length == 0, "Tracked repository changes must be reviewed before running.");
            Require(!string.IsNullOrWhiteSpace(config.Keyword) && config.ExpectedRows.Length > 0, "Keyword and independently known expectedRows are required.");
            string exe = Path.Combine(config.Repository, "Avalonia/UABS/bin/Debug/net9.0/UABS.exe");
            Require(File.Exists(exe), "Build the existing UABS Debug executable first.");
            Require(File.Exists(config.Package) && config.Package.EndsWith(".uabs.sqlite", StringComparison.OrdinalIgnoreCase), "Supply an indexed disposable .uabs.sqlite package.");
            Require(!File.Exists(config.Package + "-wal") && !File.Exists(config.Package + "-shm"), "Close/checkpoint the disposable package before running; sidecars are not copied.");
            sourceHash = Hash(config.Package);
            Require(sourceHash.Equals(config.PackageSha256, StringComparison.OrdinalIgnoreCase), "Package SHA256 mismatch.");
            using (var stream = File.OpenRead(config.Package))
            {
                byte[] signature = new byte[16];
                Require(stream.Read(signature) == 16 && System.Text.Encoding.ASCII.GetString(signature) == "SQLite format 3\0", "Package is not SQLite.");
            }
            string work = Path.Combine(Output, "work"); Directory.CreateDirectory(work);
            string copy = Path.Combine(work, Path.GetFileName(config.Package)); File.Copy(config.Package, copy);
            if (config.Acceptance == "checkpoint21-package-sort") PreparePackageCopies(config, work);
            if (config.Acceptance == "checkpoint21-indeterminate") PrepareCoverageCopies(config, work);
            Step("preflight", new { commit = config.ExpectedCommit, executableSha256 = Hash(exe), packageSha256 = sourceHash });
            Automation = new UIA3Automation();
            stage = "launch";
            App = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = work, UseShellExecute = false }) ?? throw new Blocked("Launch failed.");
            var attached = FlaUI.Core.Application.Attach(App.Id);
            Wait("main window", () => (Window = attached.GetMainWindow(Automation, TimeSpan.FromSeconds(1))) != null);
            Evidence("initial");
            if (config.Acceptance == "checkpoint21-indeterminate")
            {
                stage = "Checkpoint 2.1 G5 indeterminate coverage";
                status = RunIndeterminateAcceptance(config, work);
                detail = "G5 synthetic contract fixture; see acceptance.json. Windows evidence required; checkpoint remains open.";
                goto ScenarioFinished;
            }
            if (config.Acceptance == "checkpoint21-package-sort")
            {
                stage = "Checkpoint 2.1 Package sorting";
                status = RunPackageSortAcceptance(config, work);
                detail = "G1 Package sorting; see acceptance.json. Checkpoint remains open.";
                goto ScenarioFinished;
            }
            if (config.Acceptance == "checkpoint21-gaps")
            {
                stage = "Checkpoint 2.1 pre-admission input";
                GapCase("CP21-G3-pre-admission-input", () => PreparePreAdmission(config));
                if (AcceptanceStatus() != "PASS") { status = AcceptanceStatus(); detail = "Pre-admission prerequisites were not satisfied; see acceptance.json."; goto ScenarioFinished; }
            }
            if (config.Acceptance == "checkpoint21-v1")
            {
                stage = "Checkpoint 2.1 V1";
                status = RunFreshV1(config, work);
                detail = "Checkpoint 2.1 V1 checks; see acceptance.json. Checkpoint remains open.";
                goto ScenarioFinished;
            }
            stage = "open package";
            Select(Find(Window!, ControlType.TabItem, "Packages"));
            Evidence("packages-before-open");
            DialogSnapshot("before-invoke", null);
            var openButton = Find(Window!, ControlType.Button, "Open V2...");
            Require(openButton.IsEnabled && openButton.Patterns.Invoke.IsSupported, "Open V2 must be enabled with Invoke support.");
            // Some UIA providers return from Invoke only after the modal interaction.
            // Observe the dialog while Invoke is pending, not only after it returns.
            var openTask = Task.Run(() => openButton.Patterns.Invoke.Pattern.Invoke());
            AutomationElement? dialog = null;
            var discovery = Stopwatch.StartNew();
            int snapshots = 0;
            bool admitted = false;
            try
            {
                Wait("native file dialog or verified copy admission", () =>
                {
                    if (openTask.IsFaulted) openTask.GetAwaiter().GetResult();
                    if (snapshots < 4 && discovery.Elapsed.TotalSeconds >= snapshots * 5)
                    { DialogSnapshot("poll-" + snapshots++, openTask); }
                    var dialogs = ProcessWindows().Where(e => PackageAdmission.IsPicker(
                        e.Properties.ProcessId.Value, e.Name, App.Id)).ToArray();
                    Require(dialogs.Length <= 1, "Multiple native dialogs owned by UABS; refusing ambiguous interaction.");
                    dialog = dialogs.SingleOrDefault();
                    if (dialog != null) return true;
                    admitted = openTask.IsCompletedSuccessfully && HasVerifiedCopyScope(copy, sourceHash);
                    return admitted;
                });
            }
            finally { DialogSnapshot("discovery-end", openTask); }
            if (!admitted)
            {
                Evidence("open-dialog", dialog);
                // The ComboBox and its Edit share 1148. Query the Edit explicitly,
                // and allow the dialog's UIA children time to be published.
                try
                {
                    var fileName = PackageAdmission.WaitForUnique(() => dialog!.FindAllDescendants(
                        Automation.ConditionFactory.ByAutomationId("1148")
                            .And(Automation.ConditionFactory.ByControlType(ControlType.Edit))), TimeSpan.FromSeconds(25));
                    Require(fileName.Patterns.Value.IsSupported, "Filename Edit 1148 lacks Value pattern.");
                    Require(!fileName.Patterns.Value.Pattern.IsReadOnly.Value, "Filename Edit 1148 is read-only.");
                    string absoluteCopy = Path.GetFullPath(copy);
                    SetValue(fileName, absoluteCopy);
                    Wait("filename value confirmation", () => string.Equals(
                        fileName.Patterns.Value.Pattern.Value.Value, absoluteCopy, StringComparison.OrdinalIgnoreCase));
                    Step("confirmed filename", fileName.Patterns.Value.Pattern.Value.Value);
                    var open = PackageAdmission.WaitForUnique(() => dialog!.FindAllDescendants(
                        Automation.ConditionFactory.ByAutomationId("1")
                            .And(Automation.ConditionFactory.ByControlType(ControlType.Button))), TimeSpan.FromSeconds(25),
                        description: "Open Button AutomationId 1");
                    Require(open.IsEnabled, "Native Open button is disabled.");
                    Invoke(open);
                }
                catch (Exception error)
                {
                    DialogSnapshot("filename-failure", openTask);
                    try { Evidence("filename-failure", dialog); }
                    catch (Exception diagnosticError) { Step("filename diagnostic error", diagnosticError.ToString()); }
                    throw new Blocked("Native filename/Open controls could not be safely operated: " + error.Message);
                }
                Wait("file dialog closed", () => !ProcessWindows().Any(e => PackageAdmission.IsPicker(
                    e.Properties.ProcessId.Value, e.Name, App.Id)));
            }
            Wait("Open V2 invocation completed", () => openTask.IsCompleted);
            openTask.GetAwaiter().GetResult();
            Step("package admission path", admitted ? "verified exact copy path exposed by UIA" : "native picker submitted exact disposable copy path");
            stage = "V2 defaults";
            Select(Find(Window!, ControlType.TabItem, "Search"));
            var controls = ById(Window!, "SearchControlsScroller");
            Require(controls.ControlType == ControlType.Pane, "SearchControlsScroller must be the expected Pane.");
            Wait("V2 package admission", () => controls.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.CheckBox))
                .Any(e => e.Name.StartsWith("[V2] ", StringComparison.Ordinal)));
            var scopes = controls.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.CheckBox))
                .Where(e => e.Name.StartsWith("[V2] ", StringComparison.Ordinal) || e.Name.StartsWith("[V1] ", StringComparison.Ordinal)).ToArray();
            Require(scopes.Length == 1, "Expected exactly one package scope; refusing unrelated loaded packages.");
            Require(scopes[0].Patterns.Toggle.IsSupported, "Package scope lacks Toggle pattern.");
            Check(scopes[0].Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On, "Disposable package scope must be selected.");
            Check(Hash(copy).Equals(sourceHash, StringComparison.OrdinalIgnoreCase), "Disposable package copy changed during admission.");
            CheckToggle(controls, "Sources", ToggleState.On);
            CheckToggle(controls, "Packages", ToggleState.Off);
            CheckToggle(controls, "Assets", ToggleState.Off);
            var fields = Find(controls, ControlType.ComboBox, null);
            VerifyFieldSelection(fields);
            Step("defaults", new { sources = true, packages = false, assets = false, fields = "Assets" });
            if (config.Acceptance == "checkpoint21-gaps")
            {
                GapCase("CP21-G3-defaulting-retention", () => VerifyPreAdmission(config, copy, sourceHash));
                if (AcceptanceStatus() != "PASS") { status = AcceptanceStatus(); detail = "Pre-admission retention was not verified; sentinel query was not executed."; goto ScenarioFinished; }
            }
            stage = "search";
            var include = ById(controls, "IncludeSearchTextBox");
            SetValue(include, config.Keyword);
            Check(include.Patterns.Value.Pattern.Value.Value == config.Keyword, "Search keyword readback differs (including Unicode).");
            Invoke(ById(controls, "SearchButton"));
            var grid = ById(Window!, "ResultsGrid");
            Require(grid.ControlType == ControlType.DataGrid, "ResultsGrid must expose DataGrid control type.");
            Wait("search result", () =>
            {
                var errors = Window!.FindAllDescendants(Automation.ConditionFactory.ByAutomationId("SearchErrorText"));
                if (errors.Any(e => !e.IsOffscreen && !string.IsNullOrWhiteSpace(e.Name))) throw new InvalidOperationException("Search error: " + errors[0].Name);
                return grid.Patterns.Grid.IsSupported ? grid.Patterns.Grid.Pattern.RowCount.Value > 0
                    : grid.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.DataItem)).Length > 0;
            });
            ExpectedRow[] rows;
            if (grid.Patterns.Grid.IsSupported)
            {
                Require(grid.Patterns.Table.IsSupported, "ResultsGrid lacks Table pattern; cannot verify headers.");
                var headers = grid.Patterns.Table.Pattern.ColumnHeaders.Value.Select(e => e.Name).ToArray();
                Check(headers.SequenceEqual(new[] { "Name", "Package" }), "Expected exactly Name/Package headers; observed " + string.Join(", ", headers));
                var pattern = grid.Patterns.Grid.Pattern;
                Check(pattern.ColumnCount.Value == 2, "Expected two bundle-result columns.");
                Require(pattern.RowCount.Value <= 100, "POC supports at most 100 loaded rows; choose a narrower keyword.");
                rows = Enumerable.Range(0, pattern.RowCount.Value).Select(i => new ExpectedRow(
                    pattern.GetItem(i, 0).Name, pattern.GetItem(i, 1).Name)).ToArray();
            }
            else
            {
                try { rows = ReadAccessibleRows(grid); }
                catch (Exception error) { throw new Blocked("ResultsGrid structural accessibility unavailable: " + error.Message); }
            }
            Step("observed rows", rows);
            Check(rows.Length == config.ExpectedRows.Length && rows.OrderBy(r => r.Name).ThenBy(r => r.Package)
                .SequenceEqual(config.ExpectedRows.OrderBy(r => r.Name).ThenBy(r => r.Package)), "Bundle Name/Package results differ from expectedRows.");
            Evidence("final");
            status = "PASS"; detail = "One real Windows V2 Search default scenario exercised; full Checkpoint 2.1 acceptance remains pending.";
            if (config.Acceptance == "checkpoint21-v2")
            {
                stage = "Checkpoint 2.1 V2";
                status = RunCheckpoint21(config, work);
                detail = "Checkpoint 2.1 V2 checks; see acceptance.json. Checkpoint remains open.";
            }
            if (config.Acceptance == "checkpoint21-gaps")
            {
                stage = "Checkpoint 2.1 gap slice";
                status = RunAcceptanceGaps(config, work, copy, sourceHash);
                detail = "G2/G3/G4 test-only slice; see acceptance.json. Other acceptance gaps remain open.";
            }
        ScenarioFinished:;
        }
        catch (Exception exception)
        {
            status = exception is Blocked or NotSupportedException ? "BLOCKED" : "FAIL";
            detail = $"{stage}: {exception}";
            if (config?.Acceptance == "checkpoint21-gaps" && Window != null)
            {
                // Admission/default/query failures outside subcases still need a case result;
                // an input-only PASS cannot look like successful G3 acceptance.
                GapCase("CP21-gap-stage-failure", () => System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw());
                status = AcceptanceStatus();
            }
            try { if (Window != null) Evidence("failure"); } catch (Exception evidenceError) { Step("evidence error", evidenceError.ToString()); }
        }
        finally
        {
            if (App != null)
            {
                try { if (!App.HasExited) { App.CloseMainWindow(); if (!App.WaitForExit(3000)) App.Kill(entireProcessTree: true); } }
                catch (Exception error) { Step("cleanup error", error.ToString()); }
            }
            try
            {
                if (config != null && sourceHash != null && Hash(config.Package) != sourceHash)
                { status = "FAIL"; detail += " Original package hash changed during execution."; }
                if (config?.Acceptance == "checkpoint21-package-sort" && PackageCopiesPrepared) VerifyPackageFiles(config, Path.Combine(Output, "work"));
                if (config?.Acceptance == "checkpoint21-indeterminate" && IndeterminateCopiesPrepared) VerifyCoverageFiles(config, Path.Combine(Output, "work"));
            }
            catch (Exception error) { status = "FAIL"; detail += " Cannot verify original package: " + error.Message; }
            Automation?.Dispose();
            File.WriteAllText(Path.Combine(Output, "results.json"), JsonSerializer.Serialize(new { status, detail, steps = Steps, completedUtc = DateTime.UtcNow }, Json));
            Console.WriteLine($"{status}: {detail}\nEvidence: {Output}");
        }
        return status == "PASS" ? 0 : status == "FAIL" ? 1 : 2;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Blocked(message); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Step(string action, object observed) => Steps.Add(new { action, observed, utc = DateTime.UtcNow });
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static string Git(string repository, params string[] arguments)
    {
        var info = new ProcessStartInfo("git") { WorkingDirectory = repository, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new Blocked("Cannot run Git.");
        string value = process.StandardOutput.ReadToEnd();
        Require(process.WaitForExit(10000) && process.ExitCode == 0, "Git preflight failed.");
        return value.Trim();
    }
    private static void Wait(string description, Func<bool> predicate)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(25))
        {
            if (App?.HasExited == true) throw new InvalidOperationException("UABS exited during " + description);
            if (predicate()) { Step(description, "completed"); return; }
            Thread.Sleep(200);
        }
        throw new Blocked("Timed out waiting for " + description);
    }
    private static AutomationElement Find(AutomationElement root, ControlType type, string? name)
    {
        var candidates = root.FindAllDescendants(Automation!.ConditionFactory.ByControlType(type))
            .Where(e => name == null || e.Name == name).ToArray();
        Require(candidates.Length == 1, $"Expected one {type} '{name}', found {candidates.Length}.");
        return candidates[0];
    }
    private static AutomationElement ById(AutomationElement root, string id)
    {
        var elements = root.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId(id));
        Require(elements.Length == 1, $"Required AutomationId '{id}': found {elements.Length}.");
        return elements[0];
    }
    private static void Invoke(AutomationElement element) { Require(element.Patterns.Invoke.IsSupported, element.Name + " lacks Invoke pattern."); element.Patterns.Invoke.Pattern.Invoke(); Step("invoke", element.Name); }
    private static void Select(AutomationElement element) { Require(element.Patterns.SelectionItem.IsSupported, element.Name + " lacks SelectionItem pattern."); element.Patterns.SelectionItem.Pattern.Select(); Step("select", element.Name); }
    private static void SetValue(AutomationElement element, string value) { Require(element.Patterns.Value.IsSupported, element.Name + " lacks Value pattern."); element.Patterns.Value.Pattern.SetValue(value); Step("set value", value); }
    private static void CheckToggle(AutomationElement root, string name, ToggleState expected)
    {
        var element = Find(root, ControlType.CheckBox, name);
        Require(element.Patterns.Toggle.IsSupported, name + " lacks Toggle pattern.");
        Check(element.Patterns.Toggle.Pattern.ToggleState.Value == expected, name + " has incorrect untouched state.");
    }
    private static AutomationElement[] ProcessWindows() => Automation!.GetDesktop()
        .FindAllChildren(Automation.ConditionFactory.ByProcessId(App!.Id))
        .Concat(Window!.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.Window)))
        .Where(e => e.Properties.ProcessId.Value == App.Id).Distinct().ToArray();

    private static ExpectedRow[] ReadAccessibleRows(AutomationElement grid)
    {
        var headers = grid.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.HeaderItem))
            .Select(e => e.Name).ToArray();
        var rows = grid.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.DataItem));
        var cells = rows.Select(row => row.FindAllDescendants(Automation.ConditionFactory.ByClassName("DataGridCell"))
            .Select(cell =>
            {
                var text = cell.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.Text));
                Require(text.Length == 1, "DataGridCell must expose exactly one Text descendant.");
                return text[0].Name;
            }).ToArray()).ToArray();
        var summaries = Window!.FindAllDescendants(Automation.ConditionFactory.ByControlType(ControlType.Text))
            .Select(e => e.Name).Where(name => name.Contains(" displayed / ", StringComparison.Ordinal)).ToArray();
        Step("structural result evidence", new { headers, cells, countSummaries = summaries });
        Require(summaries.Length == 1, "Expected one accessible result count summary.");
        return SearchAccessibility.CompleteRows(headers, cells, summaries[0]);
    }

    private static void VerifyFieldSelection(AutomationElement fields, string expected = "Assets")
    {
        // Evidence reads never mutate or expand the ComboBox. An unavailable read
        // cannot count as Assets; only a usable, unambiguous representation can.
        object? valueSupported = EvidenceValue.Read(() => fields.Patterns.Value.IsSupported);
        object? value = EvidenceValue.Read(() => fields.Patterns.Value.Pattern.Value.Value);
        object? selectionSupported = EvidenceValue.Read(() => fields.Patterns.Selection.IsSupported);
        object? selection = EvidenceValue.Read(() => fields.Patterns.Selection.Pattern.Selection.Value
            .Select(item => EvidenceValue.Read(() => item.Name)).ToArray());
        object? expansion = EvidenceValue.Read(() => fields.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value.ToString());
        object? text = EvidenceValue.Read(() => fields.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Text))
            .Where(item => !item.IsOffscreen).Select(item => item.Name).ToArray());
        var verdict = SearchAccessibility.VerifyField(valueSupported is true ? value as string : null,
            selectionSupported is true && selection is object?[] selected ? selected.Select(item => item as string).ToArray() : null,
            expansion is "Collapsed" ? text as string[] : null, expected);
        var evidence = new { valueSupported, value, selectionSupported, selection, expansion, visibleText = text, verdict };
        Step("field selector UIA evidence", evidence);
        File.WriteAllText(Path.Combine(Output, "field-selection.json"), JsonSerializer.Serialize(evidence, Json));
        Require(verdict.Status != "BLOCKED", verdict.Reason + " See field-selection.json.");
        Check(verdict.Status == "PASS", verdict.Reason + " See field-selection.json.");
    }

    private static bool HasVerifiedCopyScope(string copy, string expectedHash)
    {
        var scopes = Window!.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.CheckBox))
            .Where(e => e.Name.StartsWith("[V2] ", StringComparison.Ordinal)).ToArray();
        return scopes.Length == 1 && scopes[0].Properties.HelpText.TryGetValue(out var path)
            && PackageAdmission.IsVerifiedCopy(path, copy, expectedHash, Hash);
    }

    private static void DialogSnapshot(string label, Task? invocation)
    {
        try
        {
            var snapshot = EvidenceValue.CreateDialogSnapshot(App!.Id, PackageAdmission.PickerTitle, invocation,
                () => ProcessWindows().Take(30).Select(e => new
                {
                    id = EvidenceValue.Read(() => e.AutomationId),
                    type = EvidenceValue.Read(() => e.ControlType.ToString()),
                    name = EvidenceValue.Read(() => e.Name),
                    pid = EvidenceValue.Read(() => e.Properties.ProcessId.Value),
                    handle = EvidenceValue.ReadHandle(() => e.Properties.NativeWindowHandle.Value),
                    className = EvidenceValue.Read(() => e.ClassName),
                    enabled = EvidenceValue.Read(() => e.IsEnabled),
                    offscreen = EvidenceValue.Read(() => e.IsOffscreen),
                    elements = e.FindAllDescendants().Take(120).Select(child => new
                    {
                        id = EvidenceValue.Read(() => child.AutomationId),
                        name = EvidenceValue.Read(() => child.Name),
                        type = EvidenceValue.Read(() => child.ControlType.ToString())
                    }).ToArray()
                }).ToArray());
            File.WriteAllText(Path.Combine(Output, "dialog-" + label + ".json"),
                JsonSerializer.Serialize(snapshot, Json));
        }
        catch (Exception error) { Step("dialog diagnostic error", error.ToString()); }
    }
    private static void Evidence(string label, AutomationElement? root = null)
    {
        root ??= Window!;
        Capture.Element(root).ToFile(Path.Combine(Output, label + ".png"));
        var tree = root.FindAllDescendants().Take(2000).Select(e => new
        {
            id = EvidenceValue.Read(() => e.AutomationId),
            name = EvidenceValue.Read(() => e.Name),
            type = EvidenceValue.Read(() => e.ControlType.ToString()),
            IsOffscreen = EvidenceValue.Read(() => e.IsOffscreen),
            IsEnabled = EvidenceValue.Read(() => e.IsEnabled),
            invoke = EvidenceValue.Read(() => e.Patterns.Invoke.IsSupported),
            value = EvidenceValue.Read(() => e.Patterns.Value.IsSupported),
            toggle = EvidenceValue.Read(() => e.Patterns.Toggle.IsSupported),
            selection = EvidenceValue.Read(() => e.Patterns.Selection.IsSupported),
            grid = EvidenceValue.Read(() => e.Patterns.Grid.IsSupported)
        }).ToArray();
        File.WriteAllText(Path.Combine(Output, label + "-uia.json"), JsonSerializer.Serialize(tree, Json));
    }
}
