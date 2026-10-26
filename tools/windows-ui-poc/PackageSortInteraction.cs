using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

internal static partial class Program
{
    private static bool PackageCopiesPrepared;
    private static PackageSortFixture[] PackageFixtures(Scenario config)
    {
        Require(config.PackageSortFixtures != null, "Independent Package-sort fixture records required.");
        var fixtures = config.PackageSortFixtures!; PackageSortContract.Validate(fixtures);
        Require(config.Keyword == "asset" && config.Package == fixtures.Single(f => f.Package == "Alpha").Path
            && config.PackageSha256.Equals(fixtures.Single(f => f.Package == "Alpha").Sha256, StringComparison.OrdinalIgnoreCase), "Primary fixture/keyword mismatch.");
        PackageSortContract.VerifyOrder(config.ExpectedRows, "Ascending");
        return fixtures;
    }
    private static string PackageCopy(string work, PackageSortFixture fixture) => Path.Combine(work, Path.GetFileName(fixture.Path));
    private static void VerifyPackageFiles(Scenario config, string work)
    {
        foreach (var fixture in PackageFixtures(config))
        {
            Check(Hash(fixture.Path).Equals(fixture.Sha256, StringComparison.OrdinalIgnoreCase), "Original " + fixture.Package + " database changed.");
            Check(Hash(PackageCopy(work, fixture)).Equals(fixture.Sha256, StringComparison.OrdinalIgnoreCase), "Disposable " + fixture.Package + " database changed.");
            foreach (var source in fixture.Sources)
                Check(Hash(source.OriginalPath).Equals(source.Sha256, StringComparison.OrdinalIgnoreCase), "Published source changed: " + source.Name);
        }
    }
    private static void PreparePackageCopies(Scenario config, string work)
    {
        var fixtures = PackageFixtures(config);
        string bundleRoot = Path.GetFullPath(Path.Combine(config.Repository, "test-fixtures/unity/collections/smoke/bundles"));
        string manifestPath = Path.Combine(Path.GetDirectoryName(bundleRoot)!, "fixture-manifest.json");
        Require((File.GetAttributes(bundleRoot) & FileAttributes.ReparsePoint) == 0
            && (File.GetAttributes(Path.GetDirectoryName(bundleRoot)!) & FileAttributes.ReparsePoint) == 0
            && (File.GetAttributes(manifestPath) & FileAttributes.ReparsePoint) == 0, "Linked published collection/manifest rejected.");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var inputs = manifest.RootElement.GetProperty("bundles").EnumerateArray().ToDictionary(e => e.GetProperty("name").GetString()!, e => e.GetProperty("sha256").GetString()!);
        Require(inputs.Count == 3, "Published Smoke allowlist differs.");
        foreach (var fixture in fixtures)
        {
            Require(File.Exists(fixture.Path) && (File.GetAttributes(fixture.Path) & FileAttributes.ReparsePoint) == 0
                && !File.Exists(fixture.Path + "-wal") && !File.Exists(fixture.Path + "-shm"), "Closed unlinked fixture database required.");
            Require(Hash(fixture.Path).Equals(fixture.Sha256, StringComparison.OrdinalIgnoreCase), "Independent fixture hash mismatch.");
            using (var stream = File.OpenRead(fixture.Path))
            {
                var signature = new byte[16];
                Require(stream.Read(signature) == 16 && System.Text.Encoding.ASCII.GetString(signature) == "SQLite format 3\0", "Fixture is not SQLite.");
            }
            foreach (var source in fixture.Sources)
                Require(source.OriginalPath == Path.Combine(bundleRoot, source.Name) && inputs.TryGetValue(source.Name, out var hash)
                    && hash.Equals(source.Sha256, StringComparison.OrdinalIgnoreCase)
                    && (File.GetAttributes(source.OriginalPath) & FileAttributes.ReparsePoint) == 0
                    && Hash(source.OriginalPath).Equals(hash, StringComparison.OrdinalIgnoreCase), "Fixture source path/hash is not the published allowlist input.");
            string copy = PackageCopy(work, fixture);
            if (!File.Exists(copy)) File.Copy(fixture.Path, copy); // Alpha was copied by the unchanged common preflight.
            Require(Hash(copy).Equals(fixture.Sha256, StringComparison.OrdinalIgnoreCase), "Disposable fixture copy hash differs.");
        }
        PackageCopiesPrepared = true;
        GapReport("CP21-G1-fixtures", fixtures);
    }
    private static void VerifyPackageScopes(Scenario config, string work)
    {
        var fixtures = PackageFixtures(config); var scopes = Scopes();
        Require(scopes.Length == 2, "Exactly the two approved V2 package scopes are required.");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var scope in scopes)
        {
            Require(scope.Name.StartsWith("[V2] ", StringComparison.Ordinal), "Only V2 scopes are allowed.");
            Require(scope.Properties.HelpText.TryGetValue(out var pathValue) && pathValue != null, "Scope needs exact package-path evidence.");
            var matches = fixtures.Where(f => PackageAdmission.IsVerifiedCopy(pathValue, PackageCopy(work, f), f.Sha256, Hash)).ToArray();
            Require(matches.Length == 1 && paths.Add(Path.GetFullPath(pathValue!)), "Unrelated/duplicate package scope; names are not identity.");
            Check(scope.Name.StartsWith("[V2] " + matches[0].Package + " ·", StringComparison.Ordinal), "Scope label differs from independently verified package metadata.");
            Require(scope.Patterns.Toggle.IsSupported, "Package scope requires Toggle.");
            Check(scope.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On, "Package sort lost selected scope.");
        }
        VerifyPackageFiles(config, work);
    }
    private static AutomationElement PackageSortButton()
    {
        var button = ById(Window!, "SearchPackageSortButton");
        PackageSortContract.RequireAction(button.ControlType == ControlType.Button && button.Name == "Sort Package", button.IsEnabled, !button.IsOffscreen,
            button.Properties.ProcessId.Value == App!.Id && Window!.Properties.ProcessId.Value == App.Id, button.Patterns.Invoke.IsSupported);
        return button;
    }
    private static NameSortState PackageState()
    {
        Require(PackageSortButton().Properties.HelpText.TryGetValue(out var help), "Package current/next/busy HelpText is unavailable.");
        return PackageSortContract.ReadState(help);
    }
    private static string SortHeaderHelp(string name)
    {
        Require(Find(Grid(), ControlType.HeaderItem, name).Properties.HelpText.TryGetValue(out var help)
            && help != null, name + " header active-field evidence is unavailable.");
        return help!;
    }
    private static bool OwnedFocus(AutomationElement element) => Window!.Properties.NativeWindowHandle.Value != IntPtr.Zero
        && GetForegroundWindow() == Window.Properties.NativeWindowHandle.Value && element.Properties.ProcessId.Value == App!.Id
        && element.Properties.HasKeyboardFocus.Value;
    private static void PackageKeys(int action)
    {
        var target = action == 1 ? ById(Window!, "SearchNameSortButton") : PackageSortButton();
        Require(target.IsEnabled && target.Properties.IsKeyboardFocusable.Value, "Sort keyboard target must be focusable and enabled.");
        Window!.SetForeground(); target.FrameworkAutomationElement.SetFocus();
        Wait("owned sort keyboard focus", () => target.Properties.HasKeyboardFocus.Value);
        if (action == 1)
        {
            SearchAccessibility.GuardedSelectionKeys(() => { }, () => OwnedFocus(target), () =>
            { try { Keyboard.Type(VirtualKeyShort.TAB); } finally { Keyboard.Release(VirtualKeyShort.TAB); } }, "Sort Name before Tab");
            Wait("Tab reaches Sort Package", () => PackageSortButton().Properties.HasKeyboardFocus.Value);
        }
        var button = PackageSortButton(); var key = action == 1 ? VirtualKeyShort.RETURN : VirtualKeyShort.SPACE;
        SearchAccessibility.GuardedSelectionKeys(() => { }, () => OwnedFocus(button), () =>
        { try { Keyboard.Type(key); } finally { Keyboard.Release(key); } }, "Sort Package");
        Step("Package keyboard input", new { input = action == 1 ? "Tab then Enter" : "Space", ownedProcess = App!.Id, windowHandle = Window.Properties.NativeWindowHandle.Value.ToInt64() });
    }
    private static bool PackageInspectorMatches(string? text, ExpectedRow row, PackageSortFixture fixture, string work)
    {
        if (!SearchAccessibility.SelectedInspectorMatches(text, row, PackageCopy(work, fixture))) return false;
        var comparison = SearchAccessibility.ExplainInspector(text, row, PackageCopy(work, fixture));
        var source = fixture.Sources.Single(s => s.Name == row.Name); var fields = comparison.ActualFields;
        bool Id(string field, Guid expected) => Guid.TryParse(fields.GetValueOrDefault(field), out var actual) && actual == expected;
        return Id("Package UUID", fixture.PackageId) && Id("Generation", fixture.GenerationId)
            && Id("Source UUID", source.SourceId) && Id("Source revision", source.SourceRevision)
            && fields.TryGetValue("Original indexed source", out var path)
            && Path.GetFullPath(path).Equals(source.OriginalPath, StringComparison.OrdinalIgnoreCase);
    }
    private static string WaitPackageInspector(string label, ExpectedRow row, PackageSortFixture fixture, string work)
    {
        string? actual = null;
        try
        {
            Wait("canonical Package Inspector " + label, () => PackageInspectorMatches(actual = ReadSelectedInspector(), row, fixture, work));
            return actual!;
        }
        catch (Blocked)
        {
            var report = SearchAccessibility.ExplainInspector(actual, row, PackageCopy(work, fixture));
            string[] required = ["Name", "Package", "Package path", "Original indexed source", "Package UUID", "Generation", "Source UUID", "Source revision"];
            if (report.DuplicateFields.Length == 0 && required.All(field => report.ActualFields.ContainsKey(field))
                && required.Skip(4).All(field => Guid.TryParse(report.ActualFields[field], out var id) && id != Guid.Empty))
                throw new InvalidOperationException("Readable settled Inspector differs from independent canonical package/source identity.");
            throw;
        }
        finally
        {
            GapReport("CP21-G1-inspector-" + label, new { row, expectedCopy = PackageCopy(work, fixture), fixture.PackageId, fixture.GenerationId,
                expectedSource = fixture.Sources.Single(s => s.Name == row.Name), actual,
                comparison = SearchAccessibility.ExplainInspector(actual, row, PackageCopy(work, fixture)) });
        }
    }
    private static string RunPackageSortAcceptance(Scenario config, string work)
    {
        GapCase("CP21-G1-package-order", () =>
        {
            Select(Find(Window!, ControlType.TabItem, "Search"));
            Require(Scopes().Length == 0, "Package scenario requires a fresh empty process, no persisted packages.");
            OpenCatalog(work); Select(Find(Window!, ControlType.TabItem, "Search"));
            var fixtures = PackageFixtures(config);
            Require(Scopes().Length == 2, "Native catalog admission did not expose exactly two scopes.");
            // Verify paths/hashes before setting even their selection state.
            foreach (var scope in Scopes())
            {
                Require(scope.Properties.HelpText.TryGetValue(out var path) && fixtures.Count(f => PackageAdmission.IsVerifiedCopy(path, PackageCopy(work, f), f.Sha256, Hash)) == 1,
                    "Catalog contains an unrelated scope.");
                Toggle(scope, true);
            }
            VerifyPackageScopes(config, work); PrepareSelectionWindowLayout("CP21-G1");
            GapRoles = CriteriaContract.Bind(GapEdits().Select(ObserveEdit).ToArray());
            SetGapValues(SupportedGapValues(config)); ResetSupported(config); Search(config);
            var expected = PackageSortContract.Oracle("Ascending");
            var visual = CaptureVisualRowOrder("before", expected, "CP21-G1");
            SelectBoundaryWithKeyboard(false); var selected = visual[0]; var selectedFixture = fixtures.Single(f => f.Package == selected.Package);
            WaitPackageInspector("selected", selected, selectedFixture, work);
            // A live unexecuted edit must not become the admitted criteria of a sort.
            SetValue(BoundEdit("include"), "asset_unexecuted_保持"); var liveCriteria = GapValues();
            string confirmed = $"Include: asset; Path ID: (none); packages: {2:N0}.";
            var directions = new HashSet<string>(StringComparer.Ordinal);
            InteractionEvidence("package-sort", PackageSortButton());
            for (int action = 0; action < 3; action++)
            {
                NameSortState? before = null;
                Wait("Package ready", () => { before = PackageState(); return !before.Busy; });
                string direction = before!.Next;
                Step("Package action requested", new { action, direction, before, expected = PackageSortContract.Oracle(direction) });
                if (action == 0) Invoke(PackageSortButton()); else PackageKeys(action);
                NameSortState? after = null; bool observedBusy = false;
                try
                {
                    Wait("Package active/completed " + direction, () =>
                    {
                        after = PackageState();
                        observedBusy |= after.Busy;
                        return PackageSortContract.Completed(before!, after, observedBusy, direction,
                            SortHeaderHelp("Package"), SortHeaderHelp("Name"));
                    });
                }
                finally { GapReport("CP21-G1-completion-" + action, new { action, direction, before, after, observedBusy }); }
                var actual = CaptureVisualRowOrder("after-" + action, expected, "CP21-G1");
                string inspector = WaitPackageInspector("after-" + action, selected, selectedFixture, work);
                GapReport("CP21-G1-action-" + action, new { input = action == 0 ? "Invoke" : action == 1 ? "Tab/Enter" : "Space", before, after,
                    expected = PackageSortContract.Oracle(direction), actual, selected, inspector, liveCriteria, observedCriteria = GapValues(), executed = LastExecutedEvidence() });
                PackageSortContract.VerifyOrder(actual, direction);
                Check(PackageInspectorMatches(inspector, selected, selectedFixture, work), "Sort retargeted canonical selected source/package identity.");
                CriteriaContract.Equal(liveCriteria, GapValues());
                Check(LastExecutedEvidence().SequenceEqual(new[] { confirmed }), "Sort admitted unexecuted criteria instead of the confirmed query.");
                VerifyFieldSelection(Find(Controls(), ControlType.ComboBox, null), "Assets");
                CheckToggle(Controls(), "Sources", ToggleState.On); CheckToggle(Controls(), "Packages", ToggleState.Off); CheckToggle(Controls(), "Assets", ToggleState.Off);
                foreach (var flag in GapFlags) CheckToggle(Controls(), flag, ToggleState.Off);
                Check(!Window!.FindAllDescendants(Automation!.ConditionFactory.ByAutomationId("SearchErrorText")).Any(e => !string.IsNullOrWhiteSpace(e.Name)), "Package sort reported an error.");
                VerifyPackageScopes(config, work); Rows(expected); Evidence("CP21-G1-" + action + "-" + direction); directions.Add(direction);
            }
            Check(directions.SetEquals(new[] { "Ascending", "Descending" }), "Both Package directions must be exercised.");
        });
        return AcceptanceStatus();
    }
}
