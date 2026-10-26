using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

internal static partial class Program
{
    private static bool IndeterminateCopiesPrepared;
    private static IndeterminateFixture[] CoverageFixtures(Scenario config)
    {
        IndeterminateContract.Validate(config.IndeterminateFixtures);
        var fixtures = config.IndeterminateFixtures!; var complete = fixtures.Single(f => !f.Partial);
        Require(config.Keyword == IndeterminateContract.Keyword && config.Package == complete.Path && config.PackageSha256 == complete.Sha256,
            "G5 primary baseline does not match the verified complete fixture.");
        Require(config.ExpectedRows.SequenceEqual(new[] { IndeterminateContract.Row(complete) }), "G5 complete baseline oracle differs.");
        return fixtures;
    }
    private static string CoverageCopy(string work, IndeterminateFixture f) => Path.Combine(work, Path.GetFileName(f.Path));
    private static void VerifyCoverageFiles(Scenario config, string work)
    {
        foreach (var f in CoverageFixtures(config))
        {
            Check(Hash(f.Path) == f.Sha256 && Hash(CoverageCopy(work, f)) == f.Sha256, "G5 original/disposable database changed.");
            Check(Hash(f.SourcePath) == f.SourceSha256, "G5 synthetic source provenance changed.");
        }
    }
    private static void PrepareCoverageCopies(Scenario config, string work)
    {
        foreach (var f in CoverageFixtures(config))
        {
            Require(File.Exists(f.Path) && File.Exists(f.SourcePath)
                && (File.GetAttributes(f.Path) & FileAttributes.ReparsePoint) == 0 && (File.GetAttributes(f.SourcePath) & FileAttributes.ReparsePoint) == 0
                && !File.Exists(f.Path + "-wal") && !File.Exists(f.Path + "-shm"), "G5 fixture must be closed and unlinked.");
            Require(Hash(f.Path) == f.Sha256 && Hash(f.SourcePath) == f.SourceSha256, "Invalid prepared G5 database/source hashes.");
            string copy = CoverageCopy(work, f); if (!File.Exists(copy)) File.Copy(f.Path, copy);
            Require(Hash(copy) == f.Sha256, "G5 disposable copy hash differs.");
        }
        IndeterminateCopiesPrepared = true; GapReport("CP21-G5-fixtures", CoverageFixtures(config));
    }
    private static AutomationElement CoverageScope(Scenario config, string work, bool partial)
    {
        var fixtures = CoverageFixtures(config); var scopes = Scopes();
        Require(scopes.Length == 2, "G5 requires exactly two verified V2 scopes, no persisted substitutes.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); AutomationElement? wanted = null;
        foreach (var scope in scopes)
        {
            Require(scope.Properties.HelpText.TryGetValue(out var path), "G5 scope path unavailable.");
            var matches = fixtures.Where(f => PackageAdmission.IsVerifiedCopy(path, CoverageCopy(work, f), f.Sha256, Hash)).ToArray();
            Require(matches.Length == 1 && seen.Add(path!), "Unexpected/duplicate G5 scope identity.");
            var f = matches[0]; string status = f.Partial ? "partial" : "complete";
            Check(scope.Name == "[V2] " + f.Package + " · " + status, "Actual G5 scope label/coverage differs from raw prepared evidence.");
            Require(scope.Patterns.Toggle.IsSupported, "G5 scope lacks Toggle.");
            if (f.Partial == partial) wanted = scope;
        }
        VerifyCoverageFiles(config, work); return wanted ?? throw new Blocked("Missing verified G5 scope.");
    }
    private static void CoverageControls(Scenario config, string work, bool partialSelected, Dictionary<string, string> criteria)
    {
        CriteriaContract.Equal(criteria, GapValues());
        VerifyFieldSelection(Find(Controls(), ControlType.ComboBox, null), "Assets");
        CheckToggle(Controls(), "Sources", ToggleState.On); CheckToggle(Controls(), "Assets", ToggleState.Off); CheckToggle(Controls(), "Packages", ToggleState.Off);
        foreach (var flag in GapFlags) CheckToggle(Controls(), flag, ToggleState.Off);
        Check(CoverageScope(config, work, false).Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On, "Complete G5 scope was deselected.");
        Check(CoverageScope(config, work, true).Patterns.Toggle.Pattern.ToggleState.Value == (partialSelected ? ToggleState.On : ToggleState.Off), "G5 eligible partial scope selection changed.");
    }
    private static string[] CoverageSummaries() => Window!.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Text))
        .Select(e => e.Name).Where(n => n.Contains(" displayed / ", StringComparison.Ordinal) || n.Contains(" displayed · stale count", StringComparison.Ordinal)).ToArray();
    private static void CoverageErrorCleared()
    {
        var errors = Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("SearchErrorText"));
        Require(errors.Length <= 1, "Ambiguous G5 recovery error controls.");
        if (errors.Length == 1)
        {
            Require(errors[0].Properties.HelpText.TryGetValue(out var diagnostic), "Recovery diagnostic clearing is unavailable.");
            Check(string.IsNullOrWhiteSpace(errors[0].Name) && string.IsNullOrWhiteSpace(diagnostic), "Obsolete G5 corrective/backend diagnostic did not clear.");
        }
    }
    private static string RunIndeterminateAcceptance(Scenario config, string work)
    {
        var fixtures = CoverageFixtures(config); var complete = fixtures.Single(f => !f.Partial);
        var allRows = fixtures.Select(IndeterminateContract.Row).ToArray();
        var allConfig = config with { ExpectedRows = allRows };
        GapCase("CP21-G5-01-native-admission", () =>
        {
            Select(Find(Window!, ControlType.TabItem, "Search")); Require(Scopes().Length == 0, "G5 requires fresh empty process.");
            OpenCatalog(work); Select(Find(Window!, ControlType.TabItem, "Search"));
            // Both admitted scope identities/coverage must be proven before any toggle/query.
            CoverageScope(config, work, false); CoverageScope(config, work, true);
            GapRoles = CriteriaContract.Bind(GapEdits().Select(ObserveEdit).ToArray());
            SetGapValues(SupportedGapValues(config)); ResetSupported(config);
            Toggle(CoverageScope(config, work, true), false);
            Search(config); CoverageControls(config, work, false, SupportedGapValues(config));
            GapReport("CP21-G5-complete-baseline", new { config.ExpectedRows, criteria = GapValues(), summaries = CoverageSummaries(), executed = LastExecutedEvidence() });
        });
        if (AcceptanceStatus() != "PASS") return AcceptanceStatus(); // Never run against unproved admission.
        string? inspector = null; ExpectedRow? selected = null; PackageSortFixture? canonical = null;
        string[] baselineSummary = [], baselineExecuted = [];
        GapCase("CP21-G5-02-indexed-baseline", () =>
        {
            Toggle(CoverageScope(config, work, true), true); Search(allConfig);
            CoverageControls(config, work, true, SupportedGapValues(config)); PrepareSelectionWindowLayout("CP21-G5");
            var visual = CaptureVisualRowOrder("baseline", allRows, "CP21-G5"); selected = visual[0];
            var f = fixtures.Single(f => f.Package == selected.Package);
            canonical = new(f.Package, f.Path, f.Sha256, f.PackageId, f.GenerationId,
                [new(Path.GetFileName(f.SourcePath), f.SourcePath, f.SourceSha256, f.SourceId, f.SourceRevision, [IndeterminateContract.Keyword])]);
            SelectBoundaryWithKeyboard(false); inspector = WaitPackageInspector("baseline", selected, canonical, work, "CP21-G5");
            baselineSummary = CoverageSummaries(); baselineExecuted = LastExecutedEvidence();
            Require(baselineSummary.Length == 1 && baselineExecuted.Length == 1, "Unique G5 confirmed count/criteria evidence required.");
            Check(baselineExecuted[0] == $"Include: {config.Keyword}; Path ID: (none); packages: {2:N0}.", "G5 baseline confirmed wrong criteria/scope.");
            GapReport("CP21-G5-indexed-baseline", new { expected = allRows, selected, inspector, baselineSummary, baselineExecuted, criteria = GapValues() });
        });
        if (AcceptanceStatus() != "PASS") return AcceptanceStatus();
        GapCase("CP21-G5-03-indeterminate-rejection", () =>
        {
            var criteria = SupportedGapValues(config); criteria["exclude"] = IndeterminateContract.Exclusion;
            SetGapValues(criteria); // Verify the requested nonempty value, not an inferred/read-back expectation.
            ExecuteSearch(); string corrective = Error("Select fully indexed packages or clear exclusions.");
            var error = ById(Window!, "SearchErrorText");
            Require(error.Properties.HelpText.TryGetValue(out var original), "Original G5 backend diagnostic unavailable.");
            var summaries = CoverageSummaries(); var executed = LastExecutedEvidence();
            var statuses = Window!.FindAllDescendants(Automation!.ConditionFactory.ByControlType(ControlType.Text)).Where(e => !e.IsOffscreen).Select(e => e.Name).Where(n => n == "No new rows published.").ToArray();
            GapReport("CP21-G5-rejection", new { corrective, original, criteria, observedCriteria = GapValues(), baselineSummary, summaries,
                baselineExecuted, executed, statuses, inspectorBefore = inspector, inspectorAfter = ReadSelectedInspector() });
            IndeterminateContract.Rejection(corrective, original, baselineSummary, summaries, baselineExecuted, executed, statuses);
            CoverageControls(config, work, true, criteria); Rows(allRows); // Actual retained summary, never a manufactured fresh count.
            Check(ReadSelectedInspector() == inspector && PackageInspectorMatches(inspector, selected!, canonical!, work), "G5 rejection changed exact selected Inspector/canonical identity.");
        });
        if (AcceptanceStatus() != "PASS") return AcceptanceStatus();
        GapCase("CP21-G5-04-complete-scope-recovery", () =>
        {
            var criteria = GapValues(); Toggle(CoverageScope(config, work, true), false);
            Search(config); CoverageErrorCleared(); CoverageControls(config, work, false, criteria);
            GapReport("CP21-G5-complete-recovery", new { criteria = GapValues(), expected = config.ExpectedRows, summaries = CoverageSummaries(), executed = LastExecutedEvidence() });
        });
        if (AcceptanceStatus() != "PASS") return AcceptanceStatus();
        GapCase("CP21-G5-05-clear-exclusion-recovery", () =>
        {
            SetGapValues(SupportedGapValues(config)); Toggle(CoverageScope(config, work, true), true);
            Search(allConfig); CoverageErrorCleared(); CoverageControls(config, work, true, SupportedGapValues(config));
            GapReport("CP21-G5-clear-recovery", new { criteria = GapValues(), expected = allRows, summaries = CoverageSummaries(), executed = LastExecutedEvidence() });
        });
        return AcceptanceStatus();
    }
}
