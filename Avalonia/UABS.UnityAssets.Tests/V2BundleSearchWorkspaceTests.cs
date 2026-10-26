using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UABS.Page;
using UABS.PackageV2.Search;
using UABS.Services;
using UABS.UnityAssets;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed partial class V2BundleSearchWorkspaceTests
{
    private static void Configure(SearchWorkspaceViewModel search)
    {
        search.IncludePackageResults = false; search.IncludeAssetResults = false; search.IncludeSourceResults = true;
        search.FieldSelection = SearchWorkspaceFieldSelection.Assets; search.IncludeText = "hero";
    }
    private static SearchWorkspaceViewModel Create(PackageManagementViewModel packages, Func<V2SearchService>? engine = null) =>
        new(packages, new PackageSearchService(), v2EngineFactory: engine, useBundleProjection: true);
    private static void Edit(string path, string sql)
    {
        using var c = V2SearchTests.Connect(path);
        c.CreateFunction<string?, int>("uabs_has_text_v1", value => string.IsNullOrWhiteSpace(value) ? 0 : 1);
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery();
    }
    private sealed class Picker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken token = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken token = default) => Task.FromResult<string?>(null);
    }
    private sealed class NoSourceLoads : IAssetSessionFactory
    {
        public Task<IAssetSession> OpenAsync(string path, CancellationToken token = default) =>
            throw new InvalidOperationException("Search integration must not open Unity sources.");
    }

    [AvaloniaFact]
    public async Task ActualApplicationComposition_UsesBundles_ExactTwoColumnsAndSelectableSourceInspector()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(36, sourceCount: 2);
        using var main = new MainWindowViewModel(new Picker(), new NoSourceLoads(), initialPath: corpus.DirectoryPath);
        await main.Packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        Assert.False(main.Search.IncludePackageResults); Assert.False(main.Search.IncludeAssetResults);
        Assert.True(main.Search.IncludeSourceResults); Assert.Equal(SearchWorkspaceFieldSelection.Assets, main.Search.FieldSelection);
        main.Search.IncludeText = "hero"; await main.Search.SearchAsync();
        Assert.Null(main.Search.Error); Assert.Equal(2, main.Search.Results.Count); Assert.True(main.Search.IsBundleSearch);
        Assert.All(main.Search.Results, r => { Assert.Equal(PackageSearchResultKind.SourceFile, r.Kind);
            Assert.Null(r.PathId); Assert.Null(r.AssetName); Assert.Null(r.Memo); });
        main.Search.SelectedResult = main.Search.Results[0]; await main.Search.MemoTransitionTask;
        var selected = main.Search.SelectedResult;
        Assert.Contains(selected.V2Result!.Key.SourceId!.Value.ToString(), selected.V2Details);
        Assert.Contains(selected.SourcePath!, selected.V2Details); Assert.Contains("Index status: complete", selected.V2Details);
        Assert.DoesNotContain("Path ID:", selected.V2Details); Assert.False(main.Search.CanShowMemoEditor);
        var view = new SearchWorkspaceView { DataContext = main.Search };
        var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            var grid = view.FindControl<DataGrid>("ResultsGrid")!;
            Assert.Equal(new[] { "Name", "Package" }, grid.Columns.Where(c => c.IsVisible).Select(c => c.Header?.ToString()));
            Assert.Contains("assetList", grid.Classes);
            Assert.True(view.FindControl<SelectableTextBlock>("V2SearchDetails")!.IsEffectivelyVisible);
            Assert.False(view.FindControl<EditableMemoInspectorView>("SearchMemoInspector")!.IsEffectivelyVisible);
            Assert.Null(view.FindControl<Button>("GoToAssetsButton"));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("package-field")]
    [InlineData("asset-kind")]
    [InlineData("exclusion-scope")]
    [InlineData("mixed-kinds")]
    public async Task UnsupportedControls_ReturnBackendError_WithoutChangingControlsOrPreviousRows(string input)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(10);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var rows = search.Results; search.SelectedResult = rows[0]; var selected = search.SelectedResult;
        if (input == "package-field") search.FieldSelection = SearchWorkspaceFieldSelection.Names;
        if (input == "asset-kind") { search.IncludeAssetResults = true; search.IncludeSourceResults = false; }
        if (input == "mixed-kinds") search.IncludePackageResults = true;
        if (input == "exclusion-scope") { search.FieldSelection = SearchWorkspaceFieldSelection.CabCodes; search.ExcludeText = "bad"; }
        await search.SearchAsync();
        Assert.Contains("InvalidRequest", search.ErrorDetails); Assert.DoesNotContain("InvalidRequest", search.Error);
        Assert.Contains(input == "package-field" ? "Select Assets" : input == "exclusion-scope" ? "exclusions require Assets" : "Sources only", search.Error);
        Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult);
        Assert.True(search.CriteriaChanged); Assert.False(search.IsSearching);
        if (input == "package-field") Assert.Equal(SearchWorkspaceFieldSelection.Names, search.FieldSelection);
        if (input == "asset-kind") Assert.True(search.IncludeAssetResults);
        if (input == "mixed-kinds") Assert.True(search.IncludePackageResults);
        if (input == "exclusion-scope") Assert.Equal("bad", search.ExcludeText);
    }

    [AvaloniaFact]
    public async Task ControlsRouteCrossAssetMandatoryGates_AndBundleWideSelectedFieldExclusions()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(10, sourceCount: 3);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search);
        search.PathIdText = "-77"; search.ClassIdText = "1234567"; search.TypeNameText = "unknowntype"; search.MemoOnly = true;
        await search.SearchAsync(); Assert.Null(search.Error); Assert.Equal(3, search.Results.Count);
        search.PathIdText = long.MinValue.ToString(); await search.SearchAsync(); Assert.Null(search.Error); Assert.Empty(search.Results);
        search.PathIdText = "-77"; search.ExcludeText = "钩爪";
        await search.SearchAsync(); Assert.Null(search.Error); Assert.Empty(search.Results); // another child vetoes all sources
        search.SearchMemos = true; search.ExcludeText = "blocked";
        await search.SearchAsync(); Assert.Null(search.Error); Assert.Empty(search.Results); // memo field adds explicit veto scope
    }

    [AvaloniaFact]
    public async Task ScopeAndIdenticalNames_RetainDistinctSourceKeysAndRegistrationEvidence()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 2); await corpus.Add(2, "Other");
        foreach (var p in corpus.Packages) Edit(p.Path, "UPDATE source SET original_name='same.bundle'");
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        var first = await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        await packages.V2.OpenAsync(corpus.Packages[1].Path, select: false); Configure(search);
        await search.SearchAsync(); Assert.Null(search.Error); Assert.Equal(2, search.Results.Count);
        Assert.All(search.Results, r => Assert.Equal(first!.PackageId, r.V2Result!.Key.PackageId));
        search.SelectAllPackagesCommand.Execute(null); await search.SearchAsync(); Assert.Equal(3, search.Results.Count);
        Assert.All(search.Results, r => Assert.Equal("same.bundle", r.Name));
        Assert.Equal(3, search.Results.Select(r => r.V2Result!.Key).Distinct().Count());
        search.SelectedResult = search.Results[^1];
        Assert.Contains(search.SelectedResult.V2Revision!.RegistrationId.ToString(), search.SelectedResult.V2Details);
    }

    [AvaloniaTheory]
    [InlineData("Name", PackageSearchSortField.SourceName)]
    [InlineData("Package", PackageSearchSortField.PackageName)]
    public async Task ActualHeaders_ToggleGlobalBundleOrderAndPreserveSelection(string headerText, PackageSearchSortField field)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, "Z", sourceCount: 3); await corpus.Add(2, "A", sourceCount: 2);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        foreach (var p in corpus.Packages) await packages.V2.OpenAsync(p.Path, select: false);
        search.SelectAllPackagesCommand.Execute(null); Configure(search); await search.SearchAsync();
        search.SelectedResult = search.Results[0]; var selected = search.SelectedResult;
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            var grid = view.FindControl<DataGrid>("ResultsGrid")!;
            var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, headerText));
            bool descending = search.DisplayedSortBy == field;
            for (int click = 0; click < 2; click++)
            {
                var point = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), window)!.Value;
                window.MouseDown(point, MouseButton.Left, RawInputModifiers.None); window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs(); await view.HeaderSortTask; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
                Assert.Null(search.Error); Assert.Equal(field, search.DisplayedSortBy); Assert.Same(selected, search.SelectedResult);
                Assert.Equal(descending ? PackageSearchSortDirection.Descending : PackageSearchSortDirection.Ascending, search.DisplayedSortDirection);
                header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, headerText));
                Assert.Contains(descending ? ":sortdescending" : ":sortascending", header.Classes); descending = !descending;
            }
            Assert.Empty(grid.CollectionView.SortDescriptions);
            // The accessible route must agree with both Name and Package header state,
            // then use the same guarded global Name path (never a local collection sort).
            var button = view.FindControl<Button>("SearchNameSortButton")!;
            Assert.Contains(field == PackageSearchSortField.SourceName ? "Current Name order: Descending."
                : "Current Name order: not active (PackageName Ascending).", Avalonia.Automation.AutomationProperties.GetHelpText(button));
            Assert.Contains("Next Name order: Ascending.", Avalonia.Automation.AutomationProperties.GetHelpText(button));
            InvokeSort(button); await view.HeaderSortTask; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.Equal(PackageSearchSortField.SourceName, search.DisplayedSortBy);
            Assert.Equal(PackageSearchSortDirection.Ascending, search.DisplayedSortDirection);
            Assert.Same(selected, search.SelectedResult); Assert.Same(selected, grid.SelectedItem);
            Assert.Empty(grid.CollectionView.SortDescriptions);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task BundleCursors_SortBeyond600_ReplayBackwardAndRetainExactInspectorSelection()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 805);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        search.SelectedResult = search.Results[0]; var selected = search.SelectedResult;
        for (int i = 0; i < 7; i++) await search.NextPageAsync();
        Assert.Null(search.Error); Assert.Equal(600, search.Results.Count); Assert.True(search.IsSelectionOutsideWindow);
        Assert.Same(selected, search.SelectedResult); Assert.Equal(PackageSearchResultKind.SourceFile, selected!.Kind);
        await search.PreviousPageAsync(); Assert.Null(search.Error); Assert.Equal(600, search.Results.Count);
        await search.SortV2Async(PackageSearchSortField.SourceName); Assert.Null(search.Error);
        Assert.Same(selected, search.SelectedResult); Assert.Equal(100, search.Results.Count);
        var actual = search.Results.Select(r => r.Name).ToList();
        while (search.CanReadNextPage)
        {
            var before = search.Results.Select(r => r.V2Result!.Key).ToHashSet(); await search.NextPageAsync();
            Assert.Null(search.Error); actual.AddRange(search.Results.Where(r => !before.Contains(r.V2Result!.Key)).Select(r => r.Name));
            Assert.InRange(search.Results.Count, 1, 600);
        }
        var expected = corpus.Packages[0].Legacy.Package.Indexes.SelectMany(i => i.Bundles).Select(b => b.Name!)
            .OrderBy(n => n, Comparer<string>.Create(CompareText));
        Assert.Equal(expected, actual); Assert.Equal(805, actual.Count);
    }

    [AvaloniaFact]
    public async Task MemoCatalogChange_RequiresExplicitRefresh_WithoutFabricatingBundleMemo()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 3);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var rows = search.Results; search.SelectedResult = rows[0]; var selected = search.SelectedResult;
        Edit(corpus.Packages[0].Path, "UPDATE asset SET memo='new indexed memo'");
        await search.ActivateAsync(); Assert.True(search.IsStale); Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult);
        await search.NextPageAsync(); Assert.Same(rows, search.Results); Assert.All(rows, r => Assert.Null(r.Memo));
        await search.RefreshSearchCommand.ExecuteAsync(); Assert.Null(search.Error); Assert.False(search.IsStale);
        Assert.All(search.Results, r => Assert.Null(r.Memo));
    }

    [AvaloniaFact]
    public async Task V1PresentationAndMemoIdentity_RemainNative_InProductionBundleComposition()
    {
        using var corpus = new V2SearchTests.Corpus();
        var legacy = new PackageDocument([new PackageIndex("characters", [new PackageBundleReference {
            Name = "legacy.bundle", Path = "legacy.bundle", AssetInfos = [new() { Name = "Hero", PathId = 42 }] }])]);
        await new PackageService().SaveAsync(legacy, Path.Combine(corpus.DirectoryPath, "legacy"));
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.LoadCatalogAsync(corpus.DirectoryPath); search.SelectAllPackagesCommand.Execute(null);
        search.IncludeText = "Hero"; V2SearchIntegrationTests.AssetsOnly(search); await search.SearchAsync();
        Assert.Null(search.Error); Assert.False(search.IsBundleSearch); Assert.True(search.CanSortResultsLocally);
        Assert.Equal(42, Assert.Single(search.Results).PathId);
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); Assert.Equal(6, view.FindControl<DataGrid>("ResultsGrid")!.Columns.Count(c => c.IsVisible));
            Assert.False(view.FindControl<Button>("SearchNameSortButton")!.IsVisible);
            Assert.False(view.FindControl<Button>("SearchPackageSortButton")!.IsVisible); }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RapidBundleSort_RejectsLatePages_AndCancellationKeepsConfirmedRows(bool cancel)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 205);
        using var packages = V2SearchIntegrationTests.Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int pages = 0;
        using var search = Create(packages, () => new V2SearchService(observe: (boundary, _) => {
            if (boundary == SearchBoundary.BeforePublication && Interlocked.Increment(ref pages) == 2)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); }
        }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var rows = search.Results; var selected = search.SelectedResult = rows[0];
        var first = search.SortV2Async(PackageSearchSortField.SourceName);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(15)); Task latest = Task.CompletedTask;
        try
        {
            search.IncludeText = "unexecuted criteria";
            latest = search.SortV2Async(PackageSearchSortField.SourceName);
            if (cancel) search.CancelSearchCommand.Execute(null);
        }
        finally { release.Set(); }
        await Task.WhenAll(first, latest);
        Assert.Null(search.Error); Assert.Same(selected, search.SelectedResult); Assert.True(search.CriteriaChanged);
        Assert.Equal("unexecuted criteria", search.IncludeText);
        if (cancel)
        {
            Assert.Same(rows, search.Results); Assert.Equal(PackageSearchSortField.PackageName, search.DisplayedSortBy);
            Assert.Contains("cancelled", search.Status);
        }
        else
        {
            Assert.Equal(PackageSearchSortField.SourceName, search.DisplayedSortBy);
            Assert.Equal(PackageSearchSortDirection.Descending, search.DisplayedSortDirection);
            Assert.Equal(100, search.Results.Count);
            Assert.Equal(search.Results.Select(r => r.Name).OrderByDescending(n => n, Comparer<string>.Create(CompareText)),
                search.Results.Select(r => r.Name));
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitSelectors_BeforeOrAfterAdmission_SurviveScopeAndWorkspaceChanges(bool beforeAdmission)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2); await corpus.Add(2, "Other");
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        void Customize()
        {
            search.FieldSelection = SearchWorkspaceFieldSelection.Paths;
            search.IncludePackageResults = true; search.IncludeAssetResults = true;
            search.IncludeText = "hero"; search.ExcludeText = "kept"; search.ExactText = true; search.SearchMemos = true;
        }
        if (beforeAdmission) Customize();
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        if (!beforeAdmission) Customize();
        await packages.V2.OpenAsync(corpus.Packages[1].Path, select: false);
        search.ClearPackagesCommand.Execute(null); search.SelectAllPackagesCommand.Execute(null);
        search.Deactivate(); await search.ActivateAsync(); await search.SearchAsync();
        Assert.Equal(SearchWorkspaceFieldSelection.Paths, search.FieldSelection);
        Assert.True(search.IncludePackageResults); Assert.True(search.IncludeAssetResults); Assert.True(search.IncludeSourceResults);
        Assert.Equal("hero", search.IncludeText); Assert.Equal("kept", search.ExcludeText); Assert.True(search.ExactText); Assert.True(search.SearchMemos);
        Assert.Contains("Sources only", search.Error); Assert.Contains("InvalidRequest", search.ErrorDetails);
    }

    [AvaloniaFact]
    public async Task PristineSelectors_FollowUnambiguousFormat_V1DefaultsAndExplicitEditsArePreserved()
    {
        using var corpus = new V2SearchTests.Corpus();
        var legacy = new PackageDocument([new PackageIndex("characters", [new PackageBundleReference {
            Name = "legacy.bundle", Path = "legacy.bundle", AssetInfos = [new() { Name = "Hero", PathId = 42 }] }])]);
        await new PackageService().SaveAsync(legacy, Path.Combine(corpus.DirectoryPath, "legacy"));
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.LoadCatalogAsync(corpus.DirectoryPath);
        Assert.Equal(SearchWorkspaceFieldSelection.Names, search.FieldSelection);
        Assert.True(search.IncludePackageResults); Assert.True(search.IncludeSourceResults); Assert.True(search.IncludeAssetResults);
        search.IncludeText = "Hero"; await search.SearchAsync(); Assert.Null(search.Error); Assert.False(search.IsBundleSearch);
        await corpus.Add(2); await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false);
        search.ClearPackagesCommand.Execute(null);
        search.PackageScopes.Single(p => p.ScopeKey.StartsWith("V2:")).IsSelected = true;
        Assert.Equal(SearchWorkspaceFieldSelection.Assets, search.FieldSelection); Assert.False(search.IncludePackageResults); Assert.False(search.IncludeAssetResults);
        Assert.Equal("Hero", search.IncludeText); await search.SearchAsync(); Assert.Null(search.Error); Assert.True(search.IsBundleSearch);
        search.ClearPackagesCommand.Execute(null);
        search.PackageScopes.Single(p => p.ScopeKey.StartsWith("V1:")).IsSelected = true;
        Assert.Equal(SearchWorkspaceFieldSelection.Names, search.FieldSelection); Assert.True(search.IncludePackageResults); Assert.True(search.IncludeAssetResults);
        search.FieldSelection = SearchWorkspaceFieldSelection.Paths; // Explicit intent now takes priority in every scope.
        search.SelectAllPackagesCommand.Execute(null); await search.SearchAsync(); Assert.Contains("Mixed-format", search.Error);
        search.ClearPackagesCommand.Execute(null);
        search.PackageScopes.Single(p => p.ScopeKey.StartsWith("V2:")).IsSelected = true;
        Assert.Equal(SearchWorkspaceFieldSelection.Paths, search.FieldSelection); Assert.True(search.IncludePackageResults);
        Assert.Equal("Hero", search.IncludeText);
    }

    [AvaloniaFact]
    public async Task CompatibilityFeedback_RendersActionAndOriginalDiagnosticTooltip_ThenClearsOnSuccess()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); search.IncludeText = "hero";
        search.FieldSelection = SearchWorkspaceFieldSelection.Names; await search.SearchAsync();
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            var error = view.FindControl<TextBlock>("SearchErrorText")!;
            Assert.True(error.IsEffectivelyVisible); Assert.Equal(search.Error, error.Text);
            Assert.Equal(search.ErrorDetails, ToolTip.GetTip(error)); Assert.Contains("InvalidRequest", search.ErrorDetails);
            search.FieldSelection = SearchWorkspaceFieldSelection.Assets; await search.SearchAsync();
            Dispatcher.UIThread.RunJobs(); Assert.Null(search.Error); Assert.Null(search.ErrorDetails); Assert.Null(ToolTip.GetTip(error));
        }
        finally { window.Close(); }
    }

    private static int CompareText(string a, string b)
    { int result = StringComparer.OrdinalIgnoreCase.Compare(a, b); return result != 0 ? result : StringComparer.Ordinal.Compare(a, b); }
}
