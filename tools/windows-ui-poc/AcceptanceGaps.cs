using System.Text.Json;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

internal static partial class Program
{
    private static Dictionary<string, string> GapRoles = [];
    private static readonly string[] GapFlags = ["Exact name", "Search memos", "Memo only"];
    private static void GapCase(string id, Action action) => Case(id, () =>
    {
        try { action(); }
        catch (COMException error) { throw new Blocked($"UIA provider/action unavailable (0x{error.HResult:X8}); actions were not replayed: {error}"); }
    });

    private static CriteriaEdit ObserveEdit(AutomationElement edit)
    {
        Require(edit.Properties.RuntimeId.TryGetValue(out var id) && id is { Length: > 0 }, "Criteria Edit lacks UIA RuntimeId.");
        Require(edit.Patterns.Value.IsSupported, "Criteria Edit lacks Value pattern.");
        var pattern = edit.Patterns.Value.Pattern;
        bool hasId = edit.Properties.AutomationId.TryGetValue(out var automationId);
        bool hasName = edit.Properties.Name.TryGetValue(out var name);
        // An unrelated Edit may omit these properties; required role identities still must match both.
        return new(string.Join(".", id!), hasId ? automationId : null, hasName ? name : null,
            edit.IsEnabled && !pattern.IsReadOnly.Value, pattern.Value.Value);
    }
    private static AutomationElement[] GapEdits() => Controls().FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Edit));
    private static AutomationElement BoundEdit(string role)
    {
        var edits = GapEdits();
        CriteriaContract.Read(GapRoles, edits.Select(ObserveEdit).ToArray()); // verify all semantic identities before any write
        var matches = edits.Where(edit => ObserveEdit(edit).RuntimeId == GapRoles[role]).ToArray();
        Require(matches.Length == 1, "Bound " + role + " Edit was replaced or ambiguous; no guessed remapping.");
        Require(ObserveEdit(matches[0]).Writable, "Bound " + role + " Edit is disabled or read-only; no Value action sent.");
        return matches[0];
    }
    private static Dictionary<string, string> GapValues() => UiaRead.Fresh("semantic criterion readback", () =>
        CriteriaContract.Read(GapRoles, GapEdits().Select(ObserveEdit).ToArray()));
    private static void SetGapValues(Dictionary<string, string> values)
    {
        foreach (var (role, value) in values) SetValue(BoundEdit(role), value);
        CriteriaContract.Equal(values, GapValues()); // actual changed/read-back values, not control existence
    }
    private static Dictionary<string, string> SupportedGapValues(Scenario config) => CriteriaContract.Fields.Keys
        .ToDictionary(role => role, role => role == "include" ? config.Keyword : "");
    private static void GapReport(string label, object report)
    {
        Step(label, report);
        File.WriteAllText(Path.Combine(Output, label + ".json"), JsonSerializer.Serialize(report, Json));
    }
    private static AutomationElement VerifiedGapScope(string copy, string hash)
    {
        var scopes = Scopes(); Require(scopes.Length == 1, "Gap slice requires one exact disposable V2 scope, no persisted substitutes.");
        Require(scopes[0].Name.StartsWith("[V2] ", StringComparison.Ordinal)
            && scopes[0].Properties.HelpText.TryGetValue(out var path)
            && PackageAdmission.IsVerifiedCopy(path, copy, hash, Hash), "Scope path/SHA does not identify the approved disposable copy.");
        Require(scopes[0].Patterns.Toggle.IsSupported, "Verified package scope lacks Toggle.");
        return scopes[0];
    }
    private static void PreparePreAdmission(Scenario config)
    {
        Select(Find(Window!, ControlType.TabItem, "Search"));
        Require(Scopes().Length == 0, "Pre-admission requires no loaded V1/V2 scopes; existing packages are not silently unloaded or substituted.");
        var observed = GapEdits().Select(ObserveEdit).ToArray();
        GapReport("CP21-G3-empty-control-identities", observed);
        GapRoles = CriteriaContract.Bind(observed);
        // Independently anchor the include role to the existing production AutomationId.
        Require(ObserveEdit(ById(Controls(), "IncludeSearchTextBox")).RuntimeId == GapRoles["include"], "Include role accessibility/AutomationId disagree.");
        SetGapValues(CriteriaContract.Sentinels(config.Keyword));
        GapReport("CP21-G3-before-admission", new { roles = GapRoles, observed = GapValues(), scopes = Scopes().Length });
        // Do not touch kind/field selectors: their untouched-default state is the subject of this test.
    }
    private static void VerifyPreAdmission(Scenario config, string copy, string hash)
    {
        VerifiedGapScope(copy, hash);
        var actual = GapValues(); var expected = CriteriaContract.Sentinels(config.Keyword);
        GapReport("CP21-G3-after-defaulting", new { expected, actual, roles = GapRoles, copy, sha256 = Hash(copy) });
        CriteriaContract.Equal(expected, actual);
        CheckToggle(Controls(), "Sources", ToggleState.On); CheckToggle(Controls(), "Packages", ToggleState.Off); CheckToggle(Controls(), "Assets", ToggleState.Off);
        VerifyFieldSelection(Find(Controls(), ControlType.ComboBox, null), "Assets");
        Evidence("CP21-G3-retained-sentinels");
        // Never execute the numeric/type/exclusion sentinel query; only the independently known Smoke query.
        SetGapValues(SupportedGapValues(config));
    }
    private static void AssertGapState(string label, Dictionary<string, string> expected, string field, bool flags, bool selected, string copy, string hash)
    {
        var actual = GapValues();
        var observedFlags = GapFlags.ToDictionary(name => name, name => {
            var control = Find(Controls(), ControlType.CheckBox, name);
            Require(control.Patterns.Toggle.IsSupported, name + " lacks Toggle.");
            return control.Patterns.Toggle.Pattern.ToggleState.Value.ToString();
        });
        var scope = VerifiedGapScope(copy, hash);
        var selection = scope.Patterns.Toggle.Pattern.ToggleState.Value;
        GapReport(label, new { expected, actual, expectedField = field, observedFlags, expectedFlags = flags, scopeSelected = selection.ToString(), expectedSelected = selected,
            copy, packageSha256 = Hash(copy), roles = GapRoles });
        CriteriaContract.Equal(expected, actual);
        Check(observedFlags.Values.All(value => value == (flags ? "On" : "Off")), "Non-default flags changed across transition.");
        Check(selection == (selected ? ToggleState.On : ToggleState.Off), "Exact disposable package selection changed unexpectedly.");
        VerifyFieldSelection(Find(Controls(), ControlType.ComboBox, null), field);
        CheckToggle(Controls(), "Packages", field == "Names" ? ToggleState.On : ToggleState.Off);
        CheckToggle(Controls(), "Sources", ToggleState.On); CheckToggle(Controls(), "Assets", ToggleState.Off);
        Evidence(label);
    }
    private static string RunAcceptanceGaps(Scenario config, string work, string copy, string hash)
    {
        GapCase("CP21-G3-independent-query", () => { VerifiedGapScope(copy, hash); Rows(config.ExpectedRows); });
        GapCase("CP21-G2-nondefault-retention", () =>
        {
            var baseline = CaptureRetainedBaseline(config);
            PrepareSelectionWindowLayout("CP21-G2");
            ResetSupported(config);
            var expected = CriteriaContract.Sentinels(config.Keyword + "_retention");
            void RetainedState(string label, bool selected, bool requireStale)
            {
                AssertGapState(label, expected, "Names", true, selected, copy, hash);
                VerifyRetainedResults(label, baseline, requireStale);
            }
            SetGapValues(expected); Field("Names"); Kind("Packages", true);
            foreach (string flag in GapFlags) Kind(flag, true);
            RetainedState("CP21-G2-changed-readback", true, false);
            Toggle(VerifiedGapScope(copy, hash), false);
            RetainedState("CP21-G2-scope-off", false, true);
            Toggle(VerifiedGapScope(copy, hash), true);
            RetainedState("CP21-G2-scope-on", true, true);
            Select(Find(Window!, ControlType.TabItem, "Assets")); Select(Find(Window!, ControlType.TabItem, "Packages"));
            Select(Find(Window!, ControlType.TabItem, "Search"));
            RetainedState("CP21-G2-workspace-return", true, true);
            OpenCatalog(work);
            Select(Find(Window!, ControlType.TabItem, "Search"));
            RetainedState("CP21-G2-catalog-open", true, true);
            Select(Find(Window!, ControlType.TabItem, "Packages"));
            var refresh = Find(Window!, ControlType.Button, "Refresh"); Require(refresh.IsEnabled, "Catalog Refresh is unavailable.");
            Invoke(refresh); Wait("gap catalog Refresh complete", () => refresh.IsEnabled);
            Select(Find(Window!, ControlType.TabItem, "Search"));
            RetainedState("CP21-G2-catalog-refresh", true, true);
            SetGapValues(SupportedGapValues(config)); ResetSupported(config); Search(config);
        });
        GapCase("CP21-G4-metadata-exclusion", () =>
        {
            // Also reset explicitly after a failed prior subcase, without replacing lost identities.
            SetGapValues(SupportedGapValues(config)); ResetSupported(config); Search(config);
            Require(config.ExpectedRows.Length == 1, "Metadata rejection selection requires the established one-row Unicode Smoke oracle.");
            PrepareSelectionWindowLayout("CP21-G4"); SelectBoundaryWithKeyboard(false);
            string? inspector = null;
            Wait("G4 exact selected Inspector", () => SearchAccessibility.SelectedInspectorMatches(inspector = ReadSelectedInspector(), config.ExpectedRows[0], copy));
            Field("CabCodes"); SetValue(BoundEdit("exclude"), "__cp21_metadata_exclusion__");
            var before = GapValues(); Rows(config.ExpectedRows);
            ExecuteSearch(); string corrective = Error("exclusions require Assets");
            var error = ById(Window!, "SearchErrorText");
            Require(error.Properties.HelpText.TryGetValue(out var original) && !string.IsNullOrWhiteSpace(original), "Original metadata-exclusion diagnostic is not available through HelpText.");
            GapReport("CP21-G4-rejection-state", new { corrective, original, before, after = GapValues(), inspectorBefore = inspector, inspectorAfter = ReadSelectedInspector(), copy, sha256 = Hash(copy) });
            Check(original!.Contains("InvalidRequest", StringComparison.Ordinal) && original.Contains("source/mixed automatic scope", StringComparison.Ordinal), "Original diagnostic is not the metadata-exclusion backend rejection.");
            AssertGapState("CP21-G4-rejected-controls", before, "CabCodes", false, true, copy, hash);
            Rows(config.ExpectedRows); Check(ReadSelectedInspector() == inspector, "Metadata rejection changed the exact confirmed Inspector identity/state.");
            SetGapValues(SupportedGapValues(config)); Field("Assets"); Search(config);
            var cleared = Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("SearchErrorText"));
            Require(cleared.Length <= 1, "Ambiguous error state after recovery.");
            if (cleared.Length == 1)
            {
                Check(cleared[0].IsOffscreen || string.IsNullOrWhiteSpace(cleared[0].Name), "Corrective error did not clear on a supported query.");
                Require(cleared[0].Properties.HelpText.TryGetValue(out var clearedHelp), "Cannot verify obsolete diagnostic clearing.");
                Check(string.IsNullOrWhiteSpace(clearedHelp), "Obsolete backend tooltip diagnostic was retained after success.");
            }
            GapReport("CP21-G4-recovery", new { exposedErrorControls = cleared.Length, packageSha256 = Hash(copy) });
            VerifiedGapScope(copy, hash); Rows(config.ExpectedRows);
        });
        return AcceptanceStatus();
    }
}
