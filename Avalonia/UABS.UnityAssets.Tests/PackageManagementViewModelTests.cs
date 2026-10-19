using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using UABS.Page;
using UABS.Services;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Dependencies;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class PackageManagementViewModelTests
{
    [AvaloniaFact]
    public void LoadedPackagesSidebar_UsesMinimumOvershootAndKeyboardRestore()
    {
        using PackageManagementViewModel viewModel = CreateViewModel();
        viewModel.Layout.SidebarWidth = 390;
        PackageManagementView view = new() { DataContext = viewModel };
        Window window = new() { Width = 1200, Height = 760, Content = view };
        window.Show();

        try
        {
            Grid regions = Find<Grid>(view, "PackagesRegionsGrid");
            GridSplitter splitter = Find<GridSplitter>(view, "PackagesSidebarSplitter");
            Button collapse = Find<Button>(view, "PackagesSidebarCollapseButton");
            Button restore = Find<Button>(view, "PackagesSidebarRestoreHandle");

            Assert.Equal("Collapse Loaded Packages", AutomationProperties.GetName(collapse));
            Assert.Equal("Restore Loaded Packages", AutomationProperties.GetName(restore));
            Assert.NotNull(ToolTip.GetTip(collapse));
            Assert.NotNull(ToolTip.GetTip(restore));
            AssertIconCentered(collapse);

            Drag(
                window,
                splitter,
                PackagesWorkspaceLayoutViewModel.SidebarMinimumWidth - viewModel.Layout.SidebarWidth);
            Assert.True(viewModel.Layout.IsSidebarVisible);
            Assert.Equal(PackagesWorkspaceLayoutViewModel.SidebarMinimumWidth, viewModel.Layout.SidebarWidth);

            viewModel.Layout.SidebarWidth = 390;
            view.UpdateLayout();
            Drag(
                window,
                splitter,
                -(390 - PackagesWorkspaceLayoutViewModel.SidebarMinimumWidth
                  + PackagesWorkspaceLayoutViewModel.CollapseThreshold + 8));

            Assert.False(viewModel.Layout.IsSidebarVisible);
            Assert.Equal(390, viewModel.Layout.SidebarWidth);
            Assert.False(splitter.IsVisible);
            Assert.Equal(0, regions.ColumnDefinitions[0].ActualWidth);
            Assert.Equal(0, regions.ColumnDefinitions[1].ActualWidth);
            Assert.True(restore.IsVisible);
            AssertIconCentered(restore);
            Assert.True(Find<Grid>(view, "PackageEditorRegion").Bounds.Width >= 520);

            Assert.True(restore.Focus());
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            view.UpdateLayout();

            Assert.True(viewModel.Layout.IsSidebarVisible);
            Assert.Equal(390, regions.ColumnDefinitions[0].ActualWidth, precision: 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task SidebarCollapseRestore_PreservesSelectionAndDirtyEditorState()
    {
        PackageDocument package = Document("one", Bundle("one.ab", "CAB-ONE", Asset("Hero", 7, "original")));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]));
        await viewModel.LoadCatalogAsync("catalog");
        await viewModel.EditSelectedPackageAsync();
        PackageCatalogItemViewModel selectedPackage = viewModel.SelectedPackage!;
        PackageEditorItemViewModel selectedItem = viewModel.Editor.Items.Single(item =>
            item.Kind == PackageEditorItemKind.Asset);
        viewModel.Editor.SelectedItem = selectedItem;
        viewModel.Editor.PackageName = "renamed";
        viewModel.Editor.MemoText = "unsaved memo";
        PackageManagementView view = new() { DataContext = viewModel };
        Window window = new() { Width = 1200, Height = 760, Content = view };
        window.Show();

        try
        {
            Find<Button>(view, "PackagesSidebarCollapseButton").Command!.Execute(null);
            view.UpdateLayout();
            Find<Button>(view, "PackagesSidebarRestoreHandle").Command!.Execute(null);
            view.UpdateLayout();

            Assert.Same(selectedPackage, viewModel.SelectedPackage);
            Assert.True(viewModel.Editor.IsEditing);
            Assert.True(viewModel.Editor.IsDirty);
            Assert.Equal("renamed", viewModel.Editor.PackageName);
            Assert.Same(selectedItem, viewModel.Editor.SelectedItem);
            Assert.Equal("unsaved memo", viewModel.Editor.MemoText);
            Assert.Equal("unsaved memo", selectedItem.Memo);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task PackageGrids_UseSimplifiedColumnsAndRetainTechnicalDetails()
    {
        PackageDocument package = Document("one", Bundle("one.ab", "CAB-ONE", Asset("Hero", 7, "memo")));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]));
        await viewModel.LoadCatalogAsync("catalog");
        PackageManagementView view = new() { DataContext = viewModel };
        Window window = new() { Width = 1200, Height = 760, Content = view };
        window.Show();

        try
        {
            DataGrid contentsGrid = Find<DataGrid>(view, "PackageContentsGrid");
            view.UpdateLayout();
            AssertPackageKindColumn(contentsGrid);
            AssertPackageKindIcons(contentsGrid);
            Assert.Equal(["Source", "Asset (type unavailable)"],
                viewModel.Contents.Select(item => item.TypeLabel));
            PackageContentItem contentAsset = viewModel.Contents.Single(item => !item.IsSource);
            viewModel.SelectedContent = contentAsset;
            view.UpdateLayout();
            string contentDetails = Find<TextBlock>(view, "SelectedContentTechnicalDetails").Text!;
            Assert.Contains("CAB-ONE", contentDetails);
            Assert.Contains("Path ID: 7", contentDetails);
            Assert.Contains(contentAsset.SourcePath!, contentDetails);

            await viewModel.EditSelectedPackageAsync();
            view.UpdateLayout();

            DataGrid editorGrid = Find<DataGrid>(view, "PackageEditorItemsGrid");
            AssertPackageKindColumn(editorGrid);
            AssertPackageKindIcons(editorGrid);
            Button closeEditor = Find<Button>(view, "ClosePackageEditorButton");
            Assert.Equal("Close Editor", closeEditor.Content);
            Assert.Same(viewModel.Editor.CloseEditorCommand, closeEditor.Command);
            PackageEditorItemViewModel editorAsset = viewModel.Editor.Items.Single(item => !item.IsSource);
            viewModel.Editor.SelectedItem = editorAsset;
            view.UpdateLayout();
            string editorDetails = Find<TextBlock>(view, "EditorSelectedTechnicalDetails").Text!;
            Assert.Contains("CAB-ONE", editorDetails);
            Assert.Contains("Path ID: 7", editorDetails);
            Assert.Contains(editorAsset.SourcePath, editorDetails);
            Assert.Equal("memo", viewModel.Editor.MemoText);
            Assert.False(viewModel.Editor.IsDirty);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task PackageAssetAndDependencyGrids_UseSharedThemeAwareAlternatingRowsAfterSort()
    {
        PackageBundleReference source = Bundle("zeta.ab", "CAB-Z", Asset("Zulu", 3), ["CAB-X", "CAB-DUP"]);
        source.AssetInfos.AddRange(Enumerable.Range(10, 120).Select(index => Asset($"Asset {index:000}", index)));
        PackageDocument package = Document(
            "request",
            source,
            Bundle("alpha.ab", "CAB-A", Asset("Alpha", 1), ["CAB-MISSING"]));
        PackageDocument candidates = Document(
            "targets",
            Bundle("x.ab", "CAB-X"),
            Bundle("dup-one.ab", "CAB-DUP"),
            Bundle("dup-two.ab", "CAB-DUP"));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("request", package), Entry("targets", candidates)]));
        await viewModel.LoadCatalogAsync("catalog");
        PackageManagementView view = new() { DataContext = viewModel };
        Window window = new() { Width = 1400, Height = 850, Content = view };
        window.Show();

        try
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
            view.UpdateLayout();
            DataGrid contents = Find<DataGrid>(view, "PackageContentsGrid");
            Assert.Contains("assetList", contents.Classes);
            AssertAlternatingRows(contents, "#F3F8F3", "#E8F2E9");
            AssertBothThemeRows(window, view, contents);

            contents.CollectionView.SortDescriptions.Add(DataGridSortDescription.FromPath(
                "Name",
                ListSortDirection.Ascending,
                CultureInfo.InvariantCulture));
            view.UpdateLayout();
            AssertAlternatingRows(contents, "#F3F8F3", "#E8F2E9");

            PackageContentItem distantItem = viewModel.Contents[90];
            contents.ScrollIntoView(distantItem, null);
            view.UpdateLayout();
            Assert.Contains(contents.GetVisualDescendants().OfType<DataGridRow>(),
                row => ReferenceEquals(row.DataContext, distantItem) && row.Index > 20);
            AssertBothThemeRows(window, view, contents);

            await viewModel.RefreshCatalogAsync();
            view.UpdateLayout();
            AssertAlternatingRows(contents, "#F3F8F3", "#E8F2E9");

            Find<TabControl>(view, "PackageOverviewTabs").SelectedIndex = 1;
            view.UpdateLayout();
            DataGrid dependencies = Find<DataGrid>(view, "PackageDependenciesGrid");
            Assert.Contains("assetList", dependencies.Classes);
            AssertAlternatingRows(dependencies, "#F3F8F3", "#E8F2E9");
            AssertBothThemeRows(window, view, dependencies);

            viewModel.SelectedDependency = viewModel.DependencyResults.Single(result => result.CandidateCount == 2);
            view.UpdateLayout();
            DataGrid candidateGrid = Find<DataGrid>(view, "PackageDependencyCandidatesGrid");
            Assert.Contains("assetList", candidateGrid.Classes);
            AssertAlternatingRows(candidateGrid, "#F3F8F3", "#E8F2E9");
            AssertBothThemeRows(window, view, candidateGrid);

            await viewModel.EditSelectedPackageAsync();
            view.UpdateLayout();
            DataGrid editor = Find<DataGrid>(view, "PackageEditorItemsGrid");
            Assert.Contains("assetList", editor.Classes);
            AssertAlternatingRows(editor, "#F3F8F3", "#E8F2E9");
            AssertBothThemeRows(window, view, editor);

            DataGridRow editorRow = editor.GetVisualDescendants().OfType<DataGridRow>()
                .First(row => row.Index >= 0);
            Point hoverPoint = editorRow.TranslatePoint(new Point(70, editorRow.Bounds.Height / 2), window)!.Value;
            window.MouseMove(hoverPoint, RawInputModifiers.None);
            view.UpdateLayout();
            Assert.True(editorRow.IsPointerOver);
            AssertAlternatingRows(editor, "#F3F8F3", "#E8F2E9");
            editor.SelectedItem = editorRow.DataContext;
            view.UpdateLayout();
            Assert.True(editorRow.IsSelected);
            AssertAlternatingRows(editor, "#F3F8F3", "#E8F2E9");
            window.MouseMove(new Point(window.Bounds.Width - 2, window.Bounds.Height - 2), RawInputModifiers.None);
            view.UpdateLayout();
            Assert.False(editorRow.IsPointerOver);
            AssertAlternatingRows(editor, "#F3F8F3", "#E8F2E9");

            window.RequestedThemeVariant = ThemeVariant.Dark;
            view.UpdateLayout();
            AssertAlternatingRows(editor, "#18231B", "#1E2C22");
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task ChoosePackageDirectory_LoadsPickerSelectionAndListsPackages()
    {
        FakePackageService packages = new([Entry("one", Document("one"))]);
        using PackageManagementViewModel viewModel = CreateViewModel(new QueuePicker("catalog"), packages);

        await viewModel.ChoosePackageDirectoryAsync();

        Assert.EndsWith("catalog", viewModel.PackageDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("one", Assert.Single(viewModel.Packages).Name);
        Assert.Equal("one", viewModel.SelectedPackage!.Name);
        Assert.Contains("1 package", viewModel.CatalogStatus, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadCatalog_ReportsEmptyDirectory()
    {
        using PackageManagementViewModel viewModel = CreateViewModel(packageService: new FakePackageService([]));

        await viewModel.LoadCatalogAsync("empty");

        Assert.Empty(viewModel.Packages);
        Assert.Null(viewModel.SelectedPackage);
        Assert.Equal("No package directories were found.", viewModel.CatalogStatus);
    }

    [Fact]
    public async Task LoadCatalog_ShowsMalformedAndWarningEntriesWithoutLosingValidPackages()
    {
        PackageDocument warning = Document("warning");
        warning.ValidationIssues.Add(new PackageValidationIssue(
            PackageIssueSeverity.Error, "missing-marker", "Validation marker missing."));
        FakePackageService packages = new([
            new PackageCatalogEntry("broken", "broken", null, "Malformed JSON."),
            Entry("warning", warning)
        ]);
        using PackageManagementViewModel viewModel = CreateViewModel(packageService: packages);

        await viewModel.LoadCatalogAsync("catalog");

        Assert.Equal(2, viewModel.Packages.Count);
        Assert.Equal("Error", viewModel.Packages[0].Status);
        Assert.Equal("Warning", viewModel.Packages[1].Status);
        Assert.Equal("warning", viewModel.SelectedPackage!.Name);
    }

    [Fact]
    public async Task RefreshCatalog_PreservesSelectionByPackagePath()
    {
        PackageCatalogEntry[] first = [Entry("one", Document("one")), Entry("two", Document("two"))];
        PackageCatalogEntry[] second = [Entry("one", Document("one")), Entry("two", Document("two"))];
        FakePackageService packages = new(first, second);
        using PackageManagementViewModel viewModel = CreateViewModel(packageService: packages);
        await viewModel.LoadCatalogAsync("catalog");
        await viewModel.SelectPackageAsync(viewModel.Packages[1]);

        await viewModel.RefreshCatalogAsync();

        Assert.Equal("two", viewModel.SelectedPackage!.Name);
        Assert.Equal(2, packages.ReadCatalogCalls);
    }

    [Fact]
    public async Task SwitchingPackages_ReplacesContentsAndClearsStaleSelection()
    {
        PackageDocument first = Document("one", Bundle("first.ab", "CAB-FIRST", Asset("First", 1)));
        PackageDocument second = Document("two", Bundle("second.ab", "CAB-SECOND", Asset("Second", 2)));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", first), Entry("two", second)]));
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SelectedContent = viewModel.Contents[1];

        await viewModel.SelectPackageAsync(viewModel.Packages[1]);

        Assert.Null(viewModel.SelectedContent);
        Assert.Equal(["second.ab", "Second"], viewModel.Contents.Select(item => item.Name));
    }

    [Fact]
    public async Task ContentSelection_FiltersDependenciesToItsSourceWithoutPathIdAssumptions()
    {
        PackageDocument request = Document("request",
            Bundle("a.ab", "CAB-A", dependencies: ["CAB-X"]),
            Bundle("b.ab", "CAB-B", dependencies: ["CAB-Y"]));
        PackageDocument candidates = Document("targets",
            Bundle("x.ab", "CAB-X"), Bundle("y.ab", "CAB-Y"));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("request", request), Entry("targets", candidates)]));
        await viewModel.LoadCatalogAsync("catalog");

        viewModel.SelectedContent = viewModel.Contents.Single(item => item.SourceName == "b.ab");

        PackageDependencyResultViewModel result = Assert.Single(viewModel.DependencyResults);
        Assert.Equal("CAB-Y", result.RequestedCabCode);
        Assert.Equal("b.ab", result.RequestingSource);
    }

    [Fact]
    public async Task Search_UsesExistingSyntaxAndReturnsProvenance()
    {
        PackageDocument package = Document("one", Bundle("source.ab", "CAB-A", Asset("Hero", 42, "important")));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]));
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SearchText = "hero";
        viewModel.SearchPackages = false;
        viewModel.SearchSources = false;

        await viewModel.SearchAsync();

        PackageSearchResult result = Assert.Single(viewModel.SearchResults);
        Assert.Equal(PackageSearchResultKind.Asset, result.Kind);
        Assert.Equal("one", result.PackageName);
        Assert.Equal("source.ab", result.SourceName);
        Assert.Equal(42, result.PathId);
    }

    [Fact]
    public async Task Search_FieldAndResultFiltersAreForwardedToSearchService()
    {
        PackageDocument package = Document("one", Bundle("source.ab", "CAB-A", Asset("Hero", 42, "needle")));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]));
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SearchText = "needle";
        viewModel.SearchField = PackageSearchFieldSelection.Memos;
        viewModel.SearchPackages = false;
        viewModel.SearchSources = false;
        viewModel.SearchAssets = true;

        await viewModel.SearchAsync();

        Assert.Equal(PackageSearchMatchCategory.Memo, Assert.Single(viewModel.SearchResults).MatchCategories);
    }

    [Fact]
    public async Task Search_InvalidPathIdShowsActionableError()
    {
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", Document("one"))]));
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SearchPathIdText = "not-a-number";

        await viewModel.SearchAsync();

        Assert.True(viewModel.HasSearchError);
        Assert.Empty(viewModel.SearchResults);
    }

    [Fact]
    public async Task Search_CancelsAndSuppressesStaleResults()
    {
        PackageBundleReference source = Bundle("source.ab", "CAB-A", Asset("Alpha", 1));
        source.AssetInfos.Add(Asset("Beta", 2));
        PackageDocument package = Document("one", source);
        ControlledSearchService search = new(blockCallNumber: 1);
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]), searchService: search);
        await viewModel.LoadCatalogAsync("catalog");
        viewModel.SearchText = "Alpha";
        Task first = viewModel.SearchAsync();
        await search.BlockedCallStarted.Task;

        viewModel.SearchText = "Beta";
        await viewModel.SearchAsync();
        search.ReleaseBlockedCall.SetResult();
        await first;

        Assert.True(search.BlockedToken.IsCancellationRequested);
        Assert.Equal("Beta", Assert.Single(viewModel.SearchResults).AssetName);
    }

    [Fact]
    public async Task DependencyInspection_DisplaysResolvedUnresolvedAndAmbiguousWithoutChoosingWinner()
    {
        PackageDocument request = Document("request", Bundle("request.ab", "CAB-A",
            dependencies: ["CAB-ONE", "CAB-MISSING", "CAB-DUP"]));
        PackageDocument targets = Document("targets",
            Bundle("one.ab", "CAB-ONE"),
            Bundle("dup-1.ab", "CAB-DUP"),
            Bundle("dup-2.ab", "CAB-DUP"));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("request", request), Entry("targets", targets)]));

        await viewModel.LoadCatalogAsync("catalog");

        Assert.Equal(3, viewModel.DependencyResults.Count);
        Assert.Equal(PackageDependencyStatus.Resolved, viewModel.DependencyResults[0].Status);
        Assert.Equal(PackageDependencyStatus.Unresolved, viewModel.DependencyResults[1].Status);
        PackageDependencyResultViewModel ambiguous = viewModel.DependencyResults[2];
        Assert.Equal(PackageDependencyStatus.Ambiguous, ambiguous.Status);
        Assert.Equal(2, ambiguous.CandidateCount);
        Assert.Equal(2, ambiguous.Candidates.Count);
    }

    [Fact]
    public async Task RapidPackageSelection_PreventsStaleDependencyPublication()
    {
        PackageDocument first = Document("first", Bundle("first.ab", "CAB-FIRST", dependencies: ["CAB-X"]));
        PackageDocument second = Document("second", Bundle("second.ab", "CAB-SECOND", dependencies: ["CAB-Y"]));
        PackageDocument targets = Document("targets", Bundle("x.ab", "CAB-X"), Bundle("y.ab", "CAB-Y"));
        ControlledDependencyResolver resolver = new(blockCallNumber: 2);
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([
                Entry("first", first), Entry("second", second), Entry("targets", targets)]),
            dependencyResolver: resolver);
        await viewModel.LoadCatalogAsync("catalog");

        Task stale = viewModel.SelectPackageAsync(viewModel.Packages[1]);
        await resolver.BlockedCallStarted.Task;
        await viewModel.SelectPackageAsync(viewModel.Packages[0]);
        resolver.ReleaseBlockedCall.SetResult();
        await stale;

        Assert.True(resolver.BlockedToken.IsCancellationRequested);
        Assert.Equal("first", viewModel.SelectedPackage!.Name);
        Assert.Equal("CAB-X", Assert.Single(viewModel.DependencyResults).RequestedCabCode);
    }

    [Fact]
    public async Task RefreshFailure_PreservesLastGoodCatalog()
    {
        FakePackageService packages = new(
            (IReadOnlyList<PackageCatalogEntry>)[Entry("one", Document("one"))],
            new IOException("disk unavailable"));
        using PackageManagementViewModel viewModel = CreateViewModel(packageService: packages);
        await viewModel.LoadCatalogAsync("catalog");

        await viewModel.RefreshCatalogAsync();

        Assert.Equal("one", Assert.Single(viewModel.Packages).Name);
        Assert.True(viewModel.HasCatalogError);
        Assert.Contains("previous", viewModel.CatalogStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NewPackage_WorksWithoutCatalogOrSelectedDirectory()
    {
        using PackageManagementViewModel viewModel = CreateViewModel();

        Assert.True(await viewModel.StartNewPackageAsync());

        Assert.True(viewModel.Editor.IsEditing);
        Assert.True(viewModel.Editor.IsNew);
        Assert.True(viewModel.Editor.IsDirty);
        Assert.Empty(viewModel.Editor.Items);
        Assert.Equal(string.Empty, viewModel.Editor.DestinationDirectory);
    }

    [Fact]
    public async Task SwitchingPackages_WithUnsavedEditor_RequiresDiscardConfirmation()
    {
        QueueConfirmation confirmations = new(false, true);
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", Document("one")), Entry("two", Document("two"))]),
            confirmations: confirmations);
        await viewModel.LoadCatalogAsync("catalog");
        await viewModel.StartNewPackageAsync();

        await viewModel.SelectPackageAsync(viewModel.Packages[1]);
        Assert.Equal("one", viewModel.SelectedPackage!.Name);
        Assert.True(viewModel.Editor.IsDirty);

        await viewModel.SelectPackageAsync(viewModel.Packages[1]);
        Assert.Equal("two", viewModel.SelectedPackage!.Name);
        Assert.False(viewModel.Editor.IsEditing);
        Assert.Equal(2, confirmations.Calls);
    }

    [Fact]
    public async Task TransientNullCatalogSelection_DoesNotCloseCleanEditor()
    {
        PackageDocument package = Document("one", Bundle("one.ab", "CAB-ONE", Asset("One", 1)));
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", package)]));
        await viewModel.LoadCatalogAsync("catalog");
        await viewModel.EditSelectedPackageAsync();
        PackageEditorItemViewModel selected = viewModel.Editor.Items.Single(item =>
            item.Kind == PackageEditorItemKind.Asset);
        viewModel.Editor.SelectedItem = selected;

        viewModel.SelectedPackage = null;
        await Task.Yield();

        Assert.Equal("one", viewModel.SelectedPackage!.Name);
        Assert.True(viewModel.Editor.IsEditing);
        Assert.False(viewModel.Editor.IsDirty);
        Assert.Equal(2, viewModel.Editor.Items.Count);
        Assert.Same(selected, viewModel.Editor.SelectedItem);
    }

    [Fact]
    public async Task AddSelectedAsset_UsesSelectedPackageWhenNoEditorIsOpen()
    {
        using PackageManagementViewModel viewModel = CreateViewModel(
            packageService: new FakePackageService([Entry("one", Document("one"))]));
        await viewModel.LoadCatalogAsync("catalog");
        AssetEntry selected = new("Hero", "Texture2D", 28, 42, 100, "main.assets", @"C:\game\new.assets");

        Assert.True(await viewModel.AddSelectedAssetAsync(selected));

        Assert.True(viewModel.Editor.IsEditing);
        Assert.False(viewModel.Editor.IsNew);
        Assert.Contains(viewModel.Editor.Items, item => item.PathId == 42 && item.SourcePath == selected.SourcePath);
    }

    private static PackageManagementViewModel CreateViewModel(
        IFilePickerService? picker = null,
        IPackageService? packageService = null,
        IPackageSearchService? searchService = null,
        IPackageDependencyResolver? dependencyResolver = null,
        IUserConfirmationService? confirmations = null) =>
        new(
            picker ?? new QueuePicker((string?[])[null]),
            packageService ?? new FakePackageService(),
            searchService ?? new PackageSearchService(),
            dependencyResolver ?? new PackageDependencyResolver(),
            confirmations);

    private static T Find<T>(Control root, string name) where T : Control =>
        root.FindControl<T>(name)
        ?? throw new InvalidOperationException($"Control '{name}' was not created.");

    private static void AssertPackageKindColumn(DataGrid grid)
    {
        DataGridTemplateColumn kindColumn = Assert.IsType<DataGridTemplateColumn>(grid.Columns[0]);
        TextBlock header = Assert.IsType<TextBlock>(kindColumn.Header);

        Assert.Equal(0, kindColumn.DisplayIndex);
        Assert.Equal(string.Empty, header.Text);
        Assert.Equal("Kind", AutomationProperties.GetName(header));
        Assert.Equal("Kind", kindColumn.SortMemberPath);
        Assert.False(kindColumn.CanUserResize);
        Assert.True(grid.CanUserResizeColumns);
        Assert.All(grid.Columns.Skip(1), column => Assert.True(column.CanUserResize));
        Assert.Equal(52, kindColumn.ActualWidth, precision: 1);
        Assert.Equal(4, grid.Columns.Count);
        Assert.Equal("Name", grid.Columns[1].Header);
        Assert.Equal("Type", grid.Columns[2].Header);
        Assert.Equal("Memo", grid.Columns[3].Header);

        grid.Columns[1].Width = new DataGridLength(280);
        grid.UpdateLayout();
        Assert.Equal(52, kindColumn.ActualWidth, precision: 1);
    }

    private static void AssertPackageKindIcons(DataGrid grid)
    {
        DataGridRow[] rows = grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .Where(row => row.Index >= 0)
            .OrderBy(row => row.Index)
            .ToArray();
        Assert.Equal(2, rows.Length);

        foreach (DataGridRow row in rows)
        {
            string kind = row.DataContext switch
            {
                PackageContentItem content => content.Kind.ToString(),
                PackageEditorItemViewModel item => item.Kind.ToString(),
                _ => throw new InvalidOperationException("Unexpected package grid row type.")
            };
            Grid host = row.GetVisualDescendants()
                .OfType<Grid>()
                .Single(control => control.Name == "PackageKindIconHost");
            PathIcon icon = host.GetVisualDescendants()
                .OfType<PathIcon>()
                .Single(control => control.IsVisible);

            Assert.Equal(kind, ToolTip.GetTip(host)?.ToString());
            Assert.Equal(kind, AutomationProperties.GetName(host));
            Assert.Equal(kind, AutomationProperties.GetName(icon));
            Assert.Contains(kind == "Source" ? "packageSourceKindIcon" : "packageAssetKindIcon", icon.Classes);
            Assert.Equal(18, icon.Bounds.Width, precision: 1);
            Assert.Equal(18, icon.Bounds.Height, precision: 1);
        }
    }

    private static void AssertAlternatingRows(DataGrid grid, string even, string odd)
    {
        DataGridRow[] rows = grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .Where(row => row.Index >= 0)
            .OrderBy(row => row.Index)
            .ToArray();
        Assert.True(rows.Length >= 2);

        foreach (DataGridRow row in rows)
        {
            bool isEven = row.Index % 2 == 0;
            Assert.Equal(isEven, row.Classes.Contains("assetRowEven"));
            Assert.Equal(!isEven, row.Classes.Contains("assetRowOdd"));
            SolidColorBrush background = Assert.IsType<SolidColorBrush>(row.Background);
            Assert.Equal(Color.Parse(isEven ? even : odd), background.Color);

            // Validate the effective Fluent drawing layers, not just the row property:
            // RowBorder paints the stripe, while transparent cells leave it visible.
            Border rowBorder = row.GetVisualDescendants().OfType<Border>()
                .Single(border => border.Name == "RowBorder");
            Assert.Equal(background.Color, Assert.IsType<SolidColorBrush>(rowBorder.Background).Color);
            Assert.Equal(1, rowBorder.Opacity);
            Assert.True(rowBorder.Bounds.Width > 0);
            foreach (DataGridCell cell in row.GetVisualDescendants().OfType<DataGridCell>())
            {
                AssertTransparent(cell.Background);
                foreach (Border border in cell.GetVisualDescendants().OfType<Border>()
                             .Where(border => border.Name == "CellBorder"))
                    AssertTransparent(border.Background);
            }

            var overlay = row.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Rectangle>()
                .Single(rectangle => rectangle.Name == "BackgroundRectangle");
            if (row.IsSelected || row.IsPointerOver)
            {
                string expected = row.IsSelected
                    ? row.IsPointerOver ? "#2F613F" : "#39724D"
                    : grid.ActualThemeVariant == ThemeVariant.Dark ? "#2C4633" : "#CCE3D0";
                Assert.Equal(Color.Parse(expected), Assert.IsType<SolidColorBrush>(overlay.Fill).Color);
                Assert.Equal(1, overlay.Opacity);
            }
            else
            {
                AssertTransparent(overlay.Fill);
            }
        }
    }

    private static void AssertBothThemeRows(Window window, Control view, DataGrid grid)
    {
        window.RequestedThemeVariant = ThemeVariant.Dark;
        view.UpdateLayout();
        AssertAlternatingRows(grid, "#18231B", "#1E2C22");
        window.RequestedThemeVariant = ThemeVariant.Light;
        view.UpdateLayout();
        AssertAlternatingRows(grid, "#F3F8F3", "#E8F2E9");
    }

    private static void AssertTransparent(IBrush? brush)
    {
        if (brush is not null)
            Assert.Equal(0, Assert.IsType<SolidColorBrush>(brush).Color.A);
    }

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

    private static PackageBundleReference Bundle(
        string name,
        string cabCode,
        PackageAssetReference? asset = null,
        string[]? dependencies = null) =>
        new()
        {
            Name = name,
            Path = $@"C:\game\{name}",
            CabCode = cabCode,
            AssetInfos = asset is null ? [] : [asset],
            Dependencies = dependencies?.ToList() ?? []
        };

    private static PackageAssetReference Asset(string name, long pathId, string? memo = null) =>
        new() { Name = name, PathId = pathId, Memo = memo };

    private sealed class QueuePicker(params string?[] paths) : IFilePickerService
    {
        private readonly Queue<string?> _paths = new(paths);
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_paths.Count == 0 ? null : _paths.Dequeue());
    }

    private sealed class QueueConfirmation(params bool[] answers) : IUserConfirmationService
    {
        private readonly Queue<bool> _answers = new(answers);
        public int Calls { get; private set; }

        public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_answers.Count > 0 && _answers.Dequeue());
        }
    }

    private sealed class FakePackageService(params object[] responses) : IPackageService
    {
        private readonly Queue<object> _responses = new(NormalizeResponses(responses));
        public int ReadCatalogCalls { get; private set; }

        public Task<IReadOnlyList<PackageCatalogEntry>> ReadCatalogAsync(string packageParentPath, CancellationToken cancellationToken = default)
        {
            ReadCatalogCalls++;
            object response = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return response is Exception exception
                ? Task.FromException<IReadOnlyList<PackageCatalogEntry>>(exception)
                : Task.FromResult((IReadOnlyList<PackageCatalogEntry>)response);
        }

        public Task<PackageDocument> LoadAsync(string packagePath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> CreateAsync(string sourceRoot, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageBundleReference> CreateReferenceAsync(string sourcePath, IReadOnlyCollection<PackageAssetSelection>? selectedAssets = null, CancellationToken cancellationToken = default)
        {
            PackageAssetSelection selection = selectedAssets?.Single() ?? new PackageAssetSelection("Asset", 1, "main.assets");
            return Task.FromResult(new PackageBundleReference
            {
                Name = Path.GetFileName(sourcePath),
                Path = sourcePath,
                AssetInfos = [new PackageAssetReference { Name = selection.Name, PathId = selection.PathId }]
            });
        }
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageFolderImportResult> CreateReferencesFromFolderAsync(string folderPath, PackageFolderSearchMode searchMode, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(PackageDocument package, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageDocument> BuildAndSaveAsync(string sourceRoot, string targetPath, bool overwrite = false, IProgress<PackageProgress>? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PackageSummary>> ListAsync(string packageParentPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        private static IEnumerable<object> NormalizeResponses(object[] values)
        {
            if (values.Length == 0)
                return [(IReadOnlyList<PackageCatalogEntry>)Array.Empty<PackageCatalogEntry>()];
            if (values.All(value => value is PackageCatalogEntry))
                return [(IReadOnlyList<PackageCatalogEntry>)values.Cast<PackageCatalogEntry>().ToArray()];
            return values;
        }
    }

    private sealed class ControlledSearchService(int blockCallNumber) : IPackageSearchService
    {
        private readonly PackageSearchService _inner = new();
        private int _calls;
        public TaskCompletionSource BlockedCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseBlockedCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken BlockedToken { get; private set; }

        public IReadOnlyList<PackageSearchResult> Search(IReadOnlyList<PackageSearchSource> sources, PackageSearchRequest request, CancellationToken cancellationToken = default) =>
            _inner.Search(sources, request, cancellationToken);

        public async Task<IReadOnlyList<PackageSearchResult>> SearchAsync(IReadOnlyList<PackageSearchSource> sources, PackageSearchRequest request, CancellationToken cancellationToken = default)
        {
            int call = Interlocked.Increment(ref _calls);
            if (call == blockCallNumber)
            {
                BlockedToken = cancellationToken;
                BlockedCallStarted.SetResult();
                await ReleaseBlockedCall.Task;
            }
            return _inner.Search(sources, request, CancellationToken.None);
        }
    }

    private sealed class ControlledDependencyResolver(int blockCallNumber) : IPackageDependencyResolver
    {
        private readonly PackageDependencyResolver _inner = new();
        private int _calls;
        public TaskCompletionSource BlockedCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseBlockedCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken BlockedToken { get; private set; }

        public IReadOnlyList<PackageDependencyResult> Resolve(IReadOnlyList<PackageDependencySource> sources, CancellationToken cancellationToken = default) => _inner.Resolve(sources, cancellationToken);
        public Task<IReadOnlyList<PackageDependencyResult>> ResolveAsync(IReadOnlyList<PackageDependencySource> sources, CancellationToken cancellationToken = default) => _inner.ResolveAsync(sources, cancellationToken);
        public IReadOnlyList<PackageDependencyResult> ResolveForPackage(IReadOnlyList<PackageDependencySource> catalog, PackageDependencySource requester, CancellationToken cancellationToken = default) => _inner.ResolveForPackage(catalog, requester, cancellationToken);

        public async Task<IReadOnlyList<PackageDependencyResult>> ResolveForPackageAsync(IReadOnlyList<PackageDependencySource> catalog, PackageDependencySource requester, CancellationToken cancellationToken = default)
        {
            int call = Interlocked.Increment(ref _calls);
            if (call == blockCallNumber)
            {
                BlockedToken = cancellationToken;
                BlockedCallStarted.SetResult();
                await ReleaseBlockedCall.Task;
            }
            return _inner.ResolveForPackage(catalog, requester, CancellationToken.None);
        }
    }
}
