using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using UABS.Page;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Dependencies;
using UABS.UnityAssets.Packages.Memos;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class SearchWorkspaceViewModelTests
{
    [AvaloniaFact]
    public void SearchPanels_CollapseIndependentlyAndKeyboardRestoreRememberedWidths()
    {
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService());
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());
        search.Layout.ControlsWidth = 390;
        search.Layout.InspectorWidth = 370;
        SearchWorkspaceView view = new() { DataContext = search };
        Window window = new() { Width = 1400, Height = 760, Content = view };
        window.Show();

        try
        {
            Grid regions = Find<Grid>(view, "SearchRegionsGrid");
            Button controlsCollapse = Find<Button>(view, "SearchControlsCollapseButton");
            Button inspectorCollapse = Find<Button>(view, "SearchInspectorCollapseButton");
            Button controlsRestore = Find<Button>(view, "SearchControlsRestoreHandle");
            Button inspectorRestore = Find<Button>(view, "SearchInspectorRestoreHandle");

            Assert.Equal("Collapse Search Controls", AutomationProperties.GetName(controlsCollapse));
            Assert.Equal("Collapse Search Inspector", AutomationProperties.GetName(inspectorCollapse));
            Assert.Equal("Restore Search Controls", AutomationProperties.GetName(controlsRestore));
            Assert.Equal("Restore Search Inspector", AutomationProperties.GetName(inspectorRestore));
            Assert.NotNull(ToolTip.GetTip(controlsRestore));
            Assert.NotNull(ToolTip.GetTip(inspectorRestore));
            AssertIconCentered(controlsCollapse);
            AssertIconCentered(inspectorCollapse);

            controlsCollapse.Command!.Execute(null);
            view.UpdateLayout();

            Assert.False(search.Layout.AreControlsVisible);
            Assert.True(search.Layout.IsInspectorVisible);
            Assert.Equal(0, regions.ColumnDefinitions[0].ActualWidth);
            Assert.Equal(0, regions.ColumnDefinitions[1].ActualWidth);
            Assert.True(controlsRestore.IsVisible);

            inspectorCollapse.Command!.Execute(null);
            view.UpdateLayout();

            Assert.False(search.Layout.IsInspectorVisible);
            Assert.Equal(0, regions.ColumnDefinitions[3].ActualWidth);
            Assert.Equal(0, regions.ColumnDefinitions[4].ActualWidth);
            Assert.True(inspectorRestore.IsVisible);
            AssertIconCentered(controlsRestore);
            AssertIconCentered(inspectorRestore);

            Assert.True(controlsRestore.Focus());
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            view.UpdateLayout();

            Assert.True(search.Layout.AreControlsVisible);
            Assert.False(search.Layout.IsInspectorVisible);
            Assert.Equal(390, regions.ColumnDefinitions[0].ActualWidth, precision: 1);

            Assert.True(inspectorRestore.Focus());
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            view.UpdateLayout();

            Assert.True(search.Layout.IsInspectorVisible);
            Assert.Equal(370, regions.ColumnDefinitions[4].ActualWidth, precision: 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SearchSplitters_RequireThirtySixPixelOvershootAndPreservePanelState()
    {
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService());
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());
        search.Layout.ControlsWidth = 390;
        search.Layout.InspectorWidth = 370;
        search.IncludeText = "hero";
        search.ExcludeText = "unused";
        SearchWorkspaceView view = new() { DataContext = search };
        Window window = new() { Width = 1400, Height = 760, Content = view };
        window.Show();

        try
        {
            Drag(
                window,
                Find<GridSplitter>(view, "SearchControlsSplitter"),
                SearchWorkspaceLayoutViewModel.ControlsMinimumWidth - search.Layout.ControlsWidth);
            Assert.True(search.Layout.AreControlsVisible);
            Assert.Equal(SearchWorkspaceLayoutViewModel.ControlsMinimumWidth, search.Layout.ControlsWidth);

            Drag(
                window,
                Find<GridSplitter>(view, "SearchInspectorSplitter"),
                search.Layout.InspectorWidth - SearchWorkspaceLayoutViewModel.InspectorMinimumWidth);
            Assert.True(search.Layout.IsInspectorVisible);
            Assert.Equal(SearchWorkspaceLayoutViewModel.InspectorMinimumWidth, search.Layout.InspectorWidth);

            search.Layout.ControlsWidth = 390;
            search.Layout.InspectorWidth = 370;
            view.UpdateLayout();
            Drag(
                window,
                Find<GridSplitter>(view, "SearchControlsSplitter"),
                -(390 - SearchWorkspaceLayoutViewModel.ControlsMinimumWidth
                  + SearchWorkspaceLayoutViewModel.CollapseThreshold + 8));

            Assert.False(search.Layout.AreControlsVisible);
            Assert.True(search.Layout.IsInspectorVisible);
            Assert.Equal(390, search.Layout.ControlsWidth);

            Drag(
                window,
                Find<GridSplitter>(view, "SearchInspectorSplitter"),
                370 - SearchWorkspaceLayoutViewModel.InspectorMinimumWidth
                + SearchWorkspaceLayoutViewModel.CollapseThreshold + 8);

            Assert.False(search.Layout.IsInspectorVisible);
            Assert.Equal(370, search.Layout.InspectorWidth);
            Assert.Equal("hero", search.IncludeText);
            Assert.Equal("unused", search.ExcludeText);
            Assert.True(Find<DataGrid>(view, "ResultsGrid").IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task CollapseRestore_PreservesSearchResultsSelectionAndControlsScrollPosition()
    {
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 1))))
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        SearchResultItemViewModel selected = Assert.Single(search.Results);
        search.SelectedResult = selected;
        SearchWorkspaceView view = new() { DataContext = search };
        Window window = new() { Width = 1200, Height = 360, Content = view };
        window.Show();

        try
        {
            view.UpdateLayout();
            ScrollViewer scroller = Find<ScrollViewer>(view, "SearchControlsScroller");
            scroller.Offset = new Vector(0, 80);
            view.UpdateLayout();
            double scrollOffset = scroller.Offset.Y;
            Assert.True(scrollOffset > 0);

            Find<Button>(view, "SearchControlsCollapseButton").Command!.Execute(null);
            Find<Button>(view, "SearchInspectorCollapseButton").Command!.Execute(null);
            view.UpdateLayout();
            Find<Button>(view, "SearchControlsRestoreHandle").Command!.Execute(null);
            Find<Button>(view, "SearchInspectorRestoreHandle").Command!.Execute(null);
            view.UpdateLayout();

            Assert.Equal("Hero", search.IncludeText);
            Assert.True(Assert.Single(search.PackageScopes).IsSelected);
            Assert.Same(selected, Assert.Single(search.Results));
            Assert.Same(selected, search.SelectedResult);
            Assert.Equal(scrollOffset, scroller.Offset.Y, precision: 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SearchControlsScrollbarAutoHidesWithContentGutter()
    {
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService());
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());
        SearchWorkspaceView view = new() { DataContext = search };
        Window window = new() { Width = 1000, Height = 360, Content = view };
        window.Show();

        try
        {
            view.UpdateLayout();
            ScrollViewer scroller = view.FindControl<ScrollViewer>("SearchControlsScroller")
                                    ?? throw new InvalidOperationException("Search controls scroller was not created.");

            Assert.True(scroller.AllowAutoHide);
            Assert.True(scroller.Extent.Height > scroller.Viewport.Height);
            ScrollBar scrollBar = scroller.GetVisualDescendants()
                .OfType<ScrollBar>()
                .Single(control => control.Name == "PART_VerticalScrollBar"
                                   && ReferenceEquals(
                                       control.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault(),
                                       scroller));
            TextBox include = view.FindControl<TextBox>("IncludeSearchTextBox")
                              ?? throw new InvalidOperationException("Include search field was not created.");
            Point scrollBarOrigin = scrollBar.TranslatePoint(default, scroller)
                                    ?? throw new InvalidOperationException("Scrollbar position was unavailable.");
            Point inputOrigin = include.TranslatePoint(default, scroller)
                                ?? throw new InvalidOperationException("Search input position was unavailable.");
            double gutter = scrollBarOrigin.X - (inputOrigin.X + include.Bounds.Width);

            Assert.True(gutter >= 4, $"Expected at least a 4 px content gutter; actual gutter was {gutter:N1} px.");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task EnterInCriteriaFieldExecutesSearchThroughViewWiring()
    {
        RecordingSearchService service = new();
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 1))))
        ]), service);
        using SearchWorkspaceViewModel search = new(packages, service);
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        SearchWorkspaceView view = new() { DataContext = search };
        Window window = new() { Width = 1200, Height = 700, Content = view };
        window.Show();

        try
        {
            TaskCompletionSource executionCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            search.SearchCommand.CanExecuteChanged += (_, _) =>
            {
                if (search.SearchCommand.CanExecute(null) && search.HasExecutedSearch)
                    executionCompleted.TrySetResult();
            };
            TextBox include = view.FindControl<TextBox>("IncludeSearchTextBox")
                              ?? throw new InvalidOperationException("Include search field was not created.");
            include.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter
            });
            await service.RequestObserved.Task;
            await executionCompleted.Task;

            Assert.Equal(["Hero"], service.LastRequest!.IncludeTerms);
            Assert.True(search.HasExecutedSearch);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ResultsGridKeepsSearchLocalSelectionAndVirtualizedRowParity()
    {
        PackageAssetReference[] assets = Enumerable.Range(0, 120)
            .Select(index => Asset($"Hero {index:D3}", index))
            .ToArray();
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", assets)))
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        SearchWorkspaceView view = new() { DataContext = search };
        Window window = new() { Width = 1200, Height = 420, Content = view };
        window.Show();

        try
        {
            view.UpdateLayout();
            DataGrid grid = view.FindControl<DataGrid>("ResultsGrid")
                            ?? throw new InvalidOperationException("Search results grid was not created.");
            Assert.Contains("assetList", grid.Classes);
            Assert.All(grid.Columns, column => Assert.True(column.CanUserSort));
            Assert.All(
                grid.GetVisualDescendants().OfType<DataGridRow>().Where(row => row.Index >= 0),
                row => Assert.Contains(row.Index % 2 == 0 ? "assetRowEven" : "assetRowOdd", row.Classes));

            SearchResultItemViewModel target = search.Results[80];
            grid.ScrollIntoView(target, null);
            view.UpdateLayout();
            DataGridRow targetRow = grid.GetVisualDescendants().OfType<DataGridRow>()
                .Single(row => ReferenceEquals(row.DataContext, target));
            grid.SelectedItem = target;
            view.UpdateLayout();

            Assert.Same(target, search.SelectedResult);
            Assert.Equal(80, targetRow.Index);
            Assert.Contains("assetRowEven", targetRow.Classes);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task PackageScope_FilterAndBulkSelectionRemainIndependent()
    {
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 1)))),
            Entry("Beta", Document("beta", Bundle("beta.ab", Asset("Villain", 2))))
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());

        await packages.LoadCatalogAsync("catalog");
        search.PackageFilter = "Alpha";

        Assert.Equal(2, search.SelectedPackageCount);
        Assert.Single(search.VisiblePackageScopes);
        search.ClearPackagesCommand.Execute(null);
        Assert.Equal(0, search.SelectedPackageCount);
        search.SelectAllPackagesCommand.Execute(null);
        Assert.Equal(2, search.SelectedPackageCount);
    }

    [Fact]
    public async Task CatalogRefresh_PreservesSelectionsAndDoesNotSelectNewPackagesSilently()
    {
        PackageCatalogEntry alpha = Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 1))));
        FakePackageService service = new([
            [alpha],
            [alpha, Entry("Beta", Document("beta", Bundle("beta.ab", Asset("Villain", 2))))]
        ]);
        using PackageManagementViewModel packages = CreatePackages(service);
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());

        await packages.LoadCatalogAsync("first");
        Assert.True(Assert.Single(search.PackageScopes).IsSelected);

        await packages.LoadCatalogAsync("second");

        Assert.Equal(2, search.PackageScopes.Count);
        Assert.True(search.PackageScopes.Single(item => item.Name == "Alpha").IsSelected);
        Assert.False(search.PackageScopes.Single(item => item.Name == "Beta").IsSelected);
    }

    [Fact]
    public async Task Search_MergesPackagesAndPreservesResultProvenance()
    {
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Shared Hero", 7)))),
            Entry("Beta", Document("beta", Bundle("beta.ab", Asset("Shared Hero", 7))))
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Shared Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;

        await search.SearchAsync();

        Assert.Equal(2, search.Results.Count);
        Assert.Equal(["Alpha", "Beta"], search.Results.Select(result => result.PackageName));
        Assert.All(search.Results, result => Assert.Equal(7, result.PathId));
    }

    [Fact]
    public async Task MemoOnlyAndMemoKeywordSearchProduceIndependentRequestFlags()
    {
        RecordingSearchService service = new();
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 1, "scarlet"))))
        ]), service);
        using SearchWorkspaceViewModel search = new(packages, service);
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.MemoOnly = true;

        await search.SearchAsync();

        Assert.True(service.LastRequest!.RequireNonEmptyMemo);
        Assert.Equal(PackageSearchFields.None, service.LastRequest.Fields & PackageSearchFields.Memo);

        search.SearchMemos = true;
        await search.SearchAsync();

        Assert.True(service.LastRequest.RequireNonEmptyMemo);
        Assert.NotEqual(PackageSearchFields.None, service.LastRequest.Fields & PackageSearchFields.Memo);
    }

    [Fact]
    public async Task CriteriaChangesPreserveResultsUntilExplicitSearchAndValidationClearsThem()
    {
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 1))))
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        SearchResultItemViewModel result = Assert.Single(search.Results);

        search.ExcludeText = "unused";

        Assert.True(search.CriteriaChanged);
        Assert.Same(result, Assert.Single(search.Results));

        search.IncludeText = string.Empty;
        search.PathIdText = string.Empty;
        await search.SearchAsync();

        Assert.Empty(search.Results);
        Assert.True(search.HasError);
        Assert.Contains("include keyword", search.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SupersededSearchCannotReplaceNewerResults()
    {
        ControlledSearchService service = new();
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Old Hero", 1), Asset("New Hero", 2))))
        ]), service);
        using SearchWorkspaceViewModel search = new(packages, service);
        await packages.LoadCatalogAsync("catalog");
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        search.IncludeText = "Old";

        Task stale = search.SearchAsync();
        await service.FirstCallStarted.Task;
        search.IncludeText = "New";
        await search.SearchAsync();
        service.ReleaseFirstCall.SetResult();
        await stale;

        PackageSearchResult result = Assert.Single(search.Results).Result;
        Assert.Equal("New Hero", result.AssetName);
        Assert.True(service.FirstToken.IsCancellationRequested);
        Assert.False(search.IsSearching);
    }

    [Fact]
    public async Task InvalidSearchSupersedingActiveRequestClearsLoadingState()
    {
        ControlledSearchService service = new();
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 1))))
        ]), service);
        using SearchWorkspaceViewModel search = new(packages, service);
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";

        Task stale = search.SearchAsync();
        await service.FirstCallStarted.Task;
        search.IncludeText = string.Empty;
        await search.SearchAsync();

        Assert.False(search.IsSearching);
        Assert.True(search.HasError);
        Assert.True(service.FirstToken.IsCancellationRequested);

        service.ReleaseFirstCall.SetResult();
        await stale;
        Assert.False(search.IsSearching);
    }

    [AvaloniaFact]
    public async Task SelectedAssetResultShowsFullMemoAndExactPackageContext()
    {
        string fullMemo = string.Join(' ', Enumerable.Repeat("unabridged memo text", 12));
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 7, fullMemo))))
        ]));
        using SearchWorkspaceViewModel search = new(
            packages,
            new PackageSearchService(),
            memoDraftCoordinator: new RecordingDraftCoordinator(),
            editorSessionGuard: new PackageEditorSessionGuard());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        search.SelectedResult = Assert.Single(search.Results);

        SearchWorkspaceView view = new() { DataContext = search };
        Window window = new() { Width = 1400, Height = 760, Content = view };
        window.Show();
        try
        {
            view.UpdateLayout();
            EditableMemoInspectorView memoInspector = Find<EditableMemoInspectorView>(view, "SearchMemoInspector");
            TextBox memo = Find<TextBox>(memoInspector, "MemoEditor");
            Assert.Equal(fullMemo, memo.Text);
            Assert.True(memo.IsEnabled);
            Assert.Equal(PackageMemoResolutionStatus.Available, search.MemoInspector.Status);
            Assert.Contains("Alpha", search.MemoInspector.PackageContext);
            Assert.Contains(@"C:\packages\alpha", search.MemoInspector.PackageContext);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task CatalogReloadMarksSelectedResultStaleAndNonAssetResultsUnsupported()
    {
        PackageDocument package = Document("alpha", Bundle("alpha.ab", Asset("Hero", 7, "memo")));
        FakePackageService service = new([
            Entry("Alpha", package)
        ], []);
        using PackageManagementViewModel packages = CreatePackages(service);
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        search.SelectedResult = Assert.Single(search.Results);
        Assert.Equal(PackageMemoResolutionStatus.Available, search.MemoInspector.Status);

        await packages.LoadCatalogAsync("catalog");

        Assert.Equal(PackageMemoResolutionStatus.MissingOrStale, search.MemoInspector.Status);

        FakePackageService packageOnlyService = new([Entry("Alpha", package)]);
        using PackageManagementViewModel packageOnlyPackages = CreatePackages(packageOnlyService);
        using SearchWorkspaceViewModel packageOnlySearch = new(packageOnlyPackages, new PackageSearchService());
        await packageOnlyPackages.LoadCatalogAsync("catalog");
        packageOnlySearch.IncludeText = "Alpha";
        packageOnlySearch.IncludeSourceResults = false;
        packageOnlySearch.IncludeAssetResults = false;
        await packageOnlySearch.SearchAsync();
        packageOnlySearch.SelectedResult = Assert.Single(packageOnlySearch.Results);

        Assert.Equal(PackageMemoResolutionStatus.Unsupported, packageOnlySearch.MemoInspector.Status);
    }

    [AvaloniaFact]
    public async Task EligibleAssetEditsSharedDraftAndSaveNowUsesExactIdentity()
    {
        RecordingDraftCoordinator drafts = new();
        PackageEditorSessionGuard guard = new();
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 7, "old memo"))))
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService(),
            memoDraftCoordinator: drafts, editorSessionGuard: guard);
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();

        search.SelectedResult = Assert.Single(search.Results);
        await search.MemoTransitionTask;
        SearchResultItemViewModel row = search.SelectedResult;
        List<string?> notifications = [];
        row.PropertyChanged += (_, eventArgs) => notifications.Add(eventArgs.PropertyName);

        Assert.True(search.MemoInspector.IsEditable);
        Assert.Equal("old memo", search.MemoInspector.MemoText);
        search.MemoInspector.MemoText = "new memo";
        Assert.Equal(MemoDraftSaveState.Unsaved, search.MemoInspector.SaveState);
        Assert.True(search.MemoInspector.SaveNowCommand.CanExecute(null));

        await search.MemoInspector.SaveNowCommand.ExecuteAsync();

        Assert.Equal(MemoDraftSaveState.Saved, search.MemoInspector.SaveState);
        Assert.Equal("new memo", search.MemoInspector.MemoText);
        Assert.Equal("new memo", row.Memo);
        Assert.Equal("new memo", row.MemoSummary);
        Assert.Contains(nameof(SearchResultItemViewModel.Memo), notifications);
        Assert.Contains(nameof(SearchResultItemViewModel.MemoSummary), notifications);
        PackageMemoIdentity saved = Assert.Single(drafts.SaveNowCalls);
        Assert.Equal(7, saved.PathId);
        Assert.Equal("Hero", saved.AssetName);
        Assert.Equal(@"C:\packages\alpha", saved.PackagePath);
    }

    [AvaloniaFact]
    public async Task SwitchingResultsFlushesPreviousIdentityAndStaleCompletionCannotReplaceCurrentDraft()
    {
        RecordingDraftCoordinator drafts = new();
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Same", 1, "one"), Asset("Same", 2, "two"))))
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService(),
            memoDraftCoordinator: drafts, editorSessionGuard: new PackageEditorSessionGuard());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Same";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();

        SearchResultItemViewModel first = search.Results.Single(result => result.PathId == 1);
        SearchResultItemViewModel second = search.Results.Single(result => result.PathId == 2);
        search.SelectedResult = first;
        await search.MemoTransitionTask;
        search.MemoInspector.MemoText = "draft one";

        search.SelectedResult = second;
        await search.MemoTransitionTask;

        Assert.Equal(1, Assert.Single(drafts.FlushCalls).PathId);
        Assert.Equal("two", search.MemoInspector.MemoText);
        drafts.Publish(first.Result, "confirmed late", MemoDraftSaveState.Saved, PackageMemoPersistenceStatus.Saved);
        Assert.Equal(2, search.MemoInspector.Identity!.PathId);
        Assert.Equal("two", search.MemoInspector.MemoText);
        Assert.Equal("confirmed late", first.Memo);
        Assert.Equal("two", second.Memo);
        Assert.Equal(MemoDraftSaveState.Saved, search.MemoInspector.SaveState);
    }

    [Fact]
    public async Task ActivePackageEditorBlocksMemoAndClosingItRestoresExistingDraft()
    {
        RecordingDraftCoordinator drafts = new();
        PackageEditorSessionGuard guard = new();
        PackageDocument document = Document("alpha", Bundle("alpha.ab", Asset("Hero", 7, "memo")));
        using PackageManagementViewModel packages = CreatePackages(
            new FakePackageService([Entry("Alpha", document)]), editorSessionController: guard);
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService(),
            memoDraftCoordinator: drafts, editorSessionGuard: guard);
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        search.SelectedResult = Assert.Single(search.Results);
        await search.MemoTransitionTask;
        search.MemoInspector.MemoText = "preserved draft";

        Assert.True(await packages.EditSelectedPackageAsync());
        await search.MemoTransitionTask;
        Assert.False(search.MemoInspector.IsEditable);
        Assert.Contains("Package Editor", search.MemoInspector.Explanation);
        Assert.False(packages.Editor.IsDirty);

        await packages.Editor.DiscardAsync();
        await search.MemoTransitionTask;
        Assert.True(search.MemoInspector.IsEditable);
        Assert.Equal("preserved draft", search.MemoInspector.MemoText);
        Assert.False(packages.Editor.IsEditing);
    }

    [AvaloniaFact]
    public async Task FailedSaveKeepsDraftAndRetryPersistsIt()
    {
        RecordingDraftCoordinator drafts = new() { FailNextSave = true };
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", Document("alpha", Bundle("alpha.ab", Asset("Hero", 7, "old"))))
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService(),
            memoDraftCoordinator: drafts, editorSessionGuard: new PackageEditorSessionGuard());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        search.SelectedResult = Assert.Single(search.Results);
        await search.MemoTransitionTask;
        search.MemoInspector.MemoText = "keep this draft";

        await search.MemoInspector.SaveNowCommand.ExecuteAsync();

        Assert.Equal(MemoDraftSaveState.Failed, search.MemoInspector.SaveState);
        Assert.Equal("keep this draft", search.MemoInspector.MemoText);
        Assert.Equal("old", search.SelectedResult!.Memo);
        Assert.True(search.MemoInspector.RetryCommand.CanExecute(null));
        await search.MemoInspector.RetryCommand.ExecuteAsync();
        Assert.Equal(MemoDraftSaveState.Saved, search.MemoInspector.SaveState);
        Assert.Equal("keep this draft", search.MemoInspector.MemoText);
        Assert.Equal("keep this draft", search.SelectedResult.Memo);
    }

    [AvaloniaFact]
    public async Task VerifiedMemoUpdatesAllExactRowsWithoutTouchingDuplicateNamesInOtherPackages()
    {
        RecordingDraftCoordinator drafts = new();
        PackageDocument alpha = Document("alpha", Bundle("shared.ab", Asset("Same", 7, null), Asset("Same", 7, null)));
        PackageDocument beta = Document("beta", Bundle("shared.ab", Asset("Same", 7, "beta")));
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService([
            Entry("Alpha", alpha), Entry("Beta", beta)
        ]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService(),
            memoDraftCoordinator: drafts, editorSessionGuard: new PackageEditorSessionGuard());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Same";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        SearchResultItemViewModel[] alphaRows = search.Results.Where(row => row.PackageName == "Alpha").ToArray();
        SearchResultItemViewModel betaRow = search.Results.Single(row => row.PackageName == "Beta");
        Assert.Equal(2, alphaRows.Length);

        drafts.Publish(alphaRows[0].Result, string.Empty, MemoDraftSaveState.Saved, PackageMemoPersistenceStatus.Saved);

        Assert.All(alphaRows, row => Assert.Equal(string.Empty, row.Memo));
        Assert.Equal("beta", betaRow.Memo);
        Assert.Equal(3, search.Results.Count);
        Assert.False(search.CriteriaChanged);
    }

    [Fact]
    public async Task CatalogPublicationRefreshPreservesResultsSelectionAndPendingDraft()
    {
        RecordingDraftCoordinator drafts = new();
        PackageDocument initial = Document("alpha", Bundle("alpha.ab", Asset("Hero", 7, "old")));
        PackageDocument published = Document("alpha", Bundle("alpha.ab", Asset("Hero", 7, "saved elsewhere")));
        using PackageManagementViewModel packages = CreatePackages(new FakePackageService(
            [Entry("Alpha", initial)],
            [Entry("Alpha", published)]));
        using SearchWorkspaceViewModel search = new(packages, new PackageSearchService(),
            memoDraftCoordinator: drafts, editorSessionGuard: new PackageEditorSessionGuard());
        await packages.LoadCatalogAsync("catalog");
        search.IncludeText = "Hero";
        search.IncludePackageResults = false;
        search.IncludeSourceResults = false;
        await search.SearchAsync();
        SearchResultItemViewModel selected = Assert.Single(search.Results);
        search.SelectedResult = selected;
        await search.MemoTransitionTask;
        search.MemoInspector.MemoText = "pending local draft";

        await packages.LoadCatalogAsync("catalog");
        await search.MemoTransitionTask;

        Assert.Same(selected, search.SelectedResult);
        Assert.Same(selected, Assert.Single(search.Results));
        Assert.Equal("pending local draft", search.MemoInspector.MemoText);
        Assert.Equal(MemoDraftSaveState.Unsaved, search.MemoInspector.SaveState);
        Assert.False(search.CriteriaChanged);
    }

    private static PackageManagementViewModel CreatePackages(
        IPackageService packageService,
        IPackageSearchService? searchService = null,
        IPackageEditorSessionController? editorSessionController = null) => new(
            new NullPicker(),
            packageService,
            searchService ?? new PackageSearchService(),
            new PackageDependencyResolver(),
            editorSessionController: editorSessionController);

    private static T Find<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name)
        ?? throw new InvalidOperationException($"Control '{name}' was not created.");

    private static void AssertIconCentered(Button button)
    {
        Assert.Equal(HorizontalAlignment.Center, button.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, button.VerticalContentAlignment);

        PathIcon icon = Assert.IsType<PathIcon>(button.Content);
        Point origin = icon.TranslatePoint(default, button)
                       ?? throw new InvalidOperationException("Could not locate chevron icon.");
        double horizontalDifference = Math.Abs((origin.X + (icon.Bounds.Width / 2)) - (button.Bounds.Width / 2));
        double verticalDifference = Math.Abs((origin.Y + (icon.Bounds.Height / 2)) - (button.Bounds.Height / 2));

        Assert.InRange(horizontalDifference, 0, 0.5);
        Assert.InRange(verticalDifference, 0, 0.5);
    }

    private static void Drag(Window window, Control control, double horizontalDelta)
    {
        Point start = control.TranslatePoint(
                          new Point(Math.Max(1, control.Bounds.Width / 2), Math.Max(1, control.Bounds.Height / 2)),
                          window)
                      ?? throw new InvalidOperationException("Could not locate splitter.");
        Point end = new(start.X + horizontalDelta, start.Y);
        window.MouseDown(start, MouseButton.Left, RawInputModifiers.None);
        window.MouseMove(end, RawInputModifiers.LeftMouseButton);
        window.MouseUp(end, MouseButton.Left, RawInputModifiers.None);
    }

    private static PackageCatalogEntry Entry(string name, PackageDocument document) =>
        new(name, document.PackagePath!, document, null);

    private static PackageDocument Document(string name, params PackageBundleReference[] bundles) =>
        new([new PackageIndex(string.Empty, bundles)], $@"C:\packages\{name}");

    private static PackageBundleReference Bundle(string name, params PackageAssetReference[] assets) => new()
    {
        Name = name,
        Path = $@"C:\game\{name}",
        CabCode = $"CAB-{name}",
        AssetInfos = assets.ToList()
    };

    private static PackageAssetReference Asset(string name, long pathId, string? memo = null) =>
        new() { Name = name, PathId = pathId, Memo = memo };

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }

    private sealed class FakePackageService(params IReadOnlyList<PackageCatalogEntry>[] catalogs) : IPackageService
    {
        private readonly Queue<IReadOnlyList<PackageCatalogEntry>> _catalogs = new(catalogs.Length == 0 ? [[]] : catalogs);

        public Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(string packageParentPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(_catalogs.Count > 1 ? _catalogs.Dequeue() : _catalogs.Peek());
        public Task<PackageDocument> LoadAsync(string packagePath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> CreateAsync(string sourceRoot, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageBundleReference> CreateReferenceAsync(string sourcePath, IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, PackageFolderSearchMode searchMode, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> BuildAndSaveAsync(string sourceRoot, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PackageSummary>> ListAsync(string packageParentPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingSearchService : IPackageSearchService
    {
        private readonly PackageSearchService _inner = new();
        public PackageSearchRequest? LastRequest { get; private set; }
        public TaskCompletionSource RequestObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyList<PackageSearchResult> Search(IReadOnlyList<PackageSearchSource> sources, PackageSearchRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            RequestObserved.TrySetResult();
            return _inner.Search(sources, request, cancellationToken);
        }

        public Task<IReadOnlyList<PackageSearchResult>> SearchAsync(IReadOnlyList<PackageSearchSource> sources, PackageSearchRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            Task<IReadOnlyList<PackageSearchResult>> result = _inner.SearchAsync(sources, request, cancellationToken);
            RequestObserved.TrySetResult();
            return result;
        }
    }

    private sealed class ControlledSearchService : IPackageSearchService
    {
        private readonly PackageSearchService _inner = new();
        private int _calls;
        public TaskCompletionSource FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirstCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken FirstToken { get; private set; }

        public IReadOnlyList<PackageSearchResult> Search(IReadOnlyList<PackageSearchSource> sources, PackageSearchRequest request, CancellationToken cancellationToken = default) =>
            _inner.Search(sources, request, cancellationToken);

        public async Task<IReadOnlyList<PackageSearchResult>> SearchAsync(IReadOnlyList<PackageSearchSource> sources, PackageSearchRequest request, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                FirstToken = cancellationToken;
                FirstCallStarted.SetResult();
                await ReleaseFirstCall.Task;
            }
            return _inner.Search(sources, request, CancellationToken.None);
        }
    }

    private sealed class RecordingDraftCoordinator : IMemoDraftCoordinator
    {
        private readonly Dictionary<PackageMemoIdentity, MemoDraftSnapshot> _snapshots = [];
        public event EventHandler<MemoDraftChangedEventArgs>? DraftChanged;
        public List<PackageMemoIdentity> SaveNowCalls { get; } = [];
        public List<PackageMemoIdentity> FlushCalls { get; } = [];
        public bool FailNextSave { get; set; }

        public MemoDraftSnapshot Track(PackageMemoIdentity identity, string? persistedMemo, string? packageRevision)
        {
            if (!_snapshots.TryGetValue(identity, out MemoDraftSnapshot? snapshot))
            {
                snapshot = new MemoDraftSnapshot(identity, persistedMemo, persistedMemo, packageRevision, 0, MemoDraftSaveState.Saved);
                _snapshots.Add(identity, snapshot);
            }
            return snapshot;
        }

        public Task<MemoDraftSnapshot> TrackAsync(PackageMemoIdentity identity, string? persistedMemo, CancellationToken cancellationToken = default) =>
            Task.FromResult(Track(identity, persistedMemo, "revision"));

        public MemoDraftSnapshot Edit(PackageMemoIdentity identity, string? memo)
        {
            MemoDraftSnapshot current = _snapshots[identity];
            MemoDraftSnapshot updated = current with
            {
                DraftMemo = memo,
                DraftRevision = current.DraftRevision + 1,
                State = MemoDraftSaveState.Unsaved,
                Error = null
            };
            return Store(updated);
        }

        public MemoDraftSnapshot GetSnapshot(PackageMemoIdentity? identity) =>
            identity is not null && _snapshots.TryGetValue(identity, out MemoDraftSnapshot? snapshot)
                ? snapshot
                : MemoDraftSnapshot.Unavailable;

        public Task<MemoDraftSnapshot> SaveNowAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default)
        {
            SaveNowCalls.Add(identity);
            if (FailNextSave)
            {
                FailNextSave = false;
                return Task.FromResult(Store(_snapshots[identity] with
                {
                    State = MemoDraftSaveState.Failed,
                    LastPersistenceStatus = PackageMemoPersistenceStatus.WriteFailure,
                    Error = "simulated failure"
                }));
            }
            return Task.FromResult(Store(_snapshots[identity] with
            {
                ConfirmedMemo = _snapshots[identity].DraftMemo,
                State = MemoDraftSaveState.Saved,
                LastPersistenceStatus = PackageMemoPersistenceStatus.Saved,
                Error = null
            }));
        }

        public Task<MemoDraftSnapshot> RetryAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) =>
            SaveNowAsync(identity, cancellationToken);

        public Task<MemoDraftSnapshot> FlushForNavigationAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default)
        {
            FlushCalls.Add(identity);
            return SaveNowAsync(identity, cancellationToken);
        }

        public Task<MemoDraftDrainResult> DrainForShutdownAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new MemoDraftDrainResult(_snapshots.Values.ToArray()));

        public void Publish(
            PackageSearchResult result,
            string? memo,
            MemoDraftSaveState state,
            PackageMemoPersistenceStatus? persistenceStatus = null)
        {
            PackageMemoIdentity identity = new(
                result.PackagePath,
                result.IndexDirectory!,
                result.SourcePath!,
                result.AssetName!,
                result.PathId!.Value);
            MemoDraftSnapshot current = _snapshots.TryGetValue(identity, out MemoDraftSnapshot? existing)
                ? existing
                : new MemoDraftSnapshot(
                    identity,
                    result.Memo,
                    result.Memo,
                    "revision",
                    0,
                    MemoDraftSaveState.Saved);
            Store(current with
            {
                DraftMemo = memo,
                ConfirmedMemo = persistenceStatus is PackageMemoPersistenceStatus.Saved or PackageMemoPersistenceStatus.Unchanged
                    ? memo
                    : current.ConfirmedMemo,
                State = state,
                LastPersistenceStatus = persistenceStatus,
                Error = state == MemoDraftSaveState.Failed ? "failed" : null
            });
        }

        private MemoDraftSnapshot Store(MemoDraftSnapshot snapshot)
        {
            _snapshots[snapshot.Identity!] = snapshot;
            DraftChanged?.Invoke(this, new MemoDraftChangedEventArgs(snapshot));
            return snapshot;
        }
    }
}
