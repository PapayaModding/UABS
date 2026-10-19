using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using UABS.Models;
using UABS.Page;
using UABS.Services;
using UABS.ViewModels;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

namespace UABS.UnityAssets.Tests;

public sealed class NavigatorRowStyleTests
{
    [AvaloniaFact]
    public async Task ThemeResourcesAndAlternatingRows_ResolveForLightAndDarkThemes()
    {
        using BrowserViewModel browser = CreateBrowser(new SequencedBrowserService(
            new DirectorySnapshot("A", CreateEntries("A", 20))));
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out BrowserView view, out DataGrid grid);

        try
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
            view.UpdateLayout();
            AssertAlternation(grid, "#F5F9F5", "#EEF5EE");

            window.RequestedThemeVariant = ThemeVariant.Dark;
            view.UpdateLayout();
            AssertAlternation(grid, "#1A271E", "#203024");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task SortingNavigationRefreshAndVirtualizedScrolling_ReapplyVisibleParity()
    {
        IReadOnlyList<BrowserEntry> initialEntries = CreateEntries("A", 120).Reverse().ToArray();
        IReadOnlyList<BrowserEntry> navigatedEntries = CreateEntries("B", 24);
        IReadOnlyList<BrowserEntry> refreshedEntries = navigatedEntries.Reverse().ToArray();
        var service = new SequencedBrowserService(
            new DirectorySnapshot("A", initialEntries),
            new DirectorySnapshot("B", navigatedEntries),
            new DirectorySnapshot("B", refreshedEntries));
        using BrowserViewModel browser = CreateBrowser(service);
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out BrowserView view, out DataGrid grid, height: 310);

        try
        {
            DataGridColumnHeader nameHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .Single(header => Equals(header.Content, "Name"));
            Click(window, nameHeader);
            view.UpdateLayout();

            Assert.Equal("Entry 000", RealizedRows(grid).First().DataContext is BrowserEntry sorted ? sorted.Name : null);
            AssertRealizedParity(grid);

            BrowserEntry target = browser.Entries.Single(entry => entry.Name == "Entry 080");
            grid.ScrollIntoView(target, null);
            view.UpdateLayout();
            DataGridRow targetRow = FindRow(grid, target);
            Assert.Equal(80, targetRow.Index);
            Assert.Contains("navigatorRowEven", targetRow.Classes);
            AssertRealizedParity(grid);

            await browser.NavigateAsync("B");
            view.UpdateLayout();
            AssertRealizedParity(grid);

            await browser.RefreshAsync();
            view.UpdateLayout();
            Assert.Same(refreshedEntries[0], browser.Entries[0]);
            AssertRealizedParity(grid);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task HoverSelectionAndKeyboardFocus_KeepDistinctReadableStates()
    {
        IReadOnlyList<BrowserEntry> entries = CreateEntries("A", 20);
        using BrowserViewModel browser = CreateBrowser(new SequencedBrowserService(
            new DirectorySnapshot("A", entries)));
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out BrowserView view, out DataGrid grid);

        try
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
            view.UpdateLayout();
            DataGridRow firstRow = FindRow(grid, entries[0]);

            MovePointerTo(window, firstRow);
            view.UpdateLayout();
            Assert.Equal(Color.Parse("#CCE3D0"), AsSolidColor(BackgroundLayer(firstRow).Fill).Color);

            grid.SelectedItem = entries[0];
            window.MouseMove(new Point(window.Bounds.Width - 2, window.Bounds.Height - 2), RawInputModifiers.None);
            view.UpdateLayout();
            Assert.Equal(Color.Parse("#39724D"), AsSolidColor(BackgroundLayer(firstRow).Fill).Color);
            Assert.All(
                firstRow.GetVisualDescendants().OfType<DataGridCell>(),
                cell => Assert.Equal(Color.Parse("#FFFFFF"), AsSolidColor(cell.Foreground).Color));

            MovePointerTo(window, firstRow);
            view.UpdateLayout();
            Assert.Equal(Color.Parse("#2F613F"), AsSolidColor(BackgroundLayer(firstRow).Fill).Color);

            DataGridCell focusedCell = firstRow.GetVisualDescendants().OfType<DataGridCell>().First();
            Assert.True(focusedCell.Focus(NavigationMethod.Tab, KeyModifiers.None));
            view.UpdateLayout();
            Assert.True(focusedCell.IsFocused);
            Grid focusVisual = focusedCell.GetVisualDescendants()
                .OfType<Grid>()
                .Single(control => control.Name == "FocusVisual");
            Assert.Equal(2, focusVisual.Children.OfType<Rectangle>().Count());
            Assert.All(focusVisual.Children.OfType<Rectangle>(), rectangle => Assert.NotNull(rectangle.Stroke));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task KindCells_RenderCenteredAccessibleFolderAndFileIcons()
    {
        BrowserEntry directory = Entry("Folder", BrowserEntryKind.Directory);
        BrowserEntry file = Entry("bundle", BrowserEntryKind.File);
        using BrowserViewModel browser = CreateBrowser(new SequencedBrowserService(
            new DirectorySnapshot("A", [directory, file])));
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out BrowserView view, out DataGrid grid);

        try
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
            view.UpdateLayout();

            DataGridRow directoryRow = FindRow(grid, directory);
            DataGridRow fileRow = FindRow(grid, file);
            PathIcon directoryIcon = VisibleKindIcon(directoryRow);
            PathIcon fileIcon = VisibleKindIcon(fileRow);

            Assert.Contains("navigatorFolderKindIcon", directoryIcon.Classes);
            Assert.Contains("navigatorFileKindIcon", fileIcon.Classes);
            Assert.NotEqual(directoryIcon.Data, fileIcon.Data);
            Assert.Equal(Color.Parse("#A66B00"), AsSolidColor(directoryIcon.Foreground).Color);
            Assert.Equal(Color.Parse("#2563A8"), AsSolidColor(fileIcon.Foreground).Color);
            AssertKindAccessibilityAndAlignment(directoryRow, directoryIcon, "Directory");
            AssertKindAccessibilityAndAlignment(fileRow, fileIcon, "File");

            DataGridTemplateColumn kindColumn = Assert.IsType<DataGridTemplateColumn>(grid.Columns[0]);
            TextBlock kindHeader = Assert.IsType<TextBlock>(kindColumn.Header);
            Assert.Equal(0, kindColumn.DisplayIndex);
            Assert.Equal(string.Empty, kindHeader.Text);
            Assert.Equal("Kind", AutomationProperties.GetName(kindHeader));
            Assert.Equal("Kind", kindColumn.SortMemberPath);
            Assert.True(kindColumn.CanUserResize);
            Assert.False(kindColumn.CanUserSort);
            Assert.True(grid.CanUserResizeColumns);
            Assert.All(grid.Columns.Skip(1), column => Assert.True(column.CanUserResize));
            Assert.Equal(52, kindColumn.ActualWidth, precision: 1);

            grid.Columns[1].Width = new DataGridLength(280);
            view.UpdateLayout();
            Assert.Equal(52, kindColumn.ActualWidth, precision: 1);
            KindColumnAssertions.VerifyBoundedResizing(grid);

            window.RequestedThemeVariant = ThemeVariant.Dark;
            view.UpdateLayout();
            Assert.Equal(Color.Parse("#F1B84B"), AsSolidColor(directoryIcon.Foreground).Color);
            Assert.Equal(Color.Parse("#8CB4E8"), AsSolidColor(fileIcon.Foreground).Color);

            grid.SelectedItem = directory;
            view.UpdateLayout();
            Assert.Equal(Color.Parse("#FFFFFF"), AsSolidColor(directoryIcon.Foreground).Color);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task BlankKindHeaderDisablesUserSort_WhileProgrammaticSortAndDeepScrollRemainSafe()
    {
        IReadOnlyList<BrowserEntry> entries = CreateEntries("A", 120).Reverse().ToArray();
        using BrowserViewModel browser = CreateBrowser(new SequencedBrowserService(
            new DirectorySnapshot("A", entries)));
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out BrowserView view, out DataGrid grid, height: 310);

        try
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
            DataGridTemplateColumn kindColumn = Assert.IsType<DataGridTemplateColumn>(grid.Columns[0]);
            TextBlock headerContent = Assert.IsType<TextBlock>(kindColumn.Header);
            DataGridColumnHeader kindHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .Single(header => ReferenceEquals(header.Content, headerContent));
            Click(window, kindHeader);
            view.UpdateLayout();

            Assert.Same(entries[0], RealizedRows(grid).First().DataContext);

            grid.CollectionView.SortDescriptions.Add(DataGridSortDescription.FromPath(
                "Kind",
                ListSortDirection.Ascending,
                CultureInfo.InvariantCulture));
            view.UpdateLayout();

            BrowserEntryKind[] realizedKinds = RealizedRows(grid)
                .Select(row => Assert.IsType<BrowserEntry>(row.DataContext).Kind)
                .ToArray();
            Assert.Equal(realizedKinds.OrderBy(kind => kind), realizedKinds);
            AssertRealizedParity(grid);

            BrowserEntry target = entries.Last(entry => entry.Kind == BrowserEntryKind.File);
            grid.ScrollIntoView(target, null);
            view.UpdateLayout();
            DataGridRow targetRow = FindRow(grid, target);

            Assert.Contains("navigatorFileKindIcon", VisibleKindIcon(targetRow).Classes);
            Assert.Equal(targetRow.Index % 2 == 0, targetRow.Classes.Contains("navigatorRowEven"));
            Assert.Equal(
                Color.Parse(targetRow.Index % 2 == 0 ? "#F5F9F5" : "#EEF5EE"),
                AsSolidColor(targetRow.Background).Color);
            AssertRealizedParity(grid);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ClickingKindIcon_PreservesRowSelectionAndFileActivation()
    {
        BrowserEntry file = Entry("bundle", BrowserEntryKind.File);
        string? openedPath = null;
        using BrowserViewModel browser = CreateBrowser(
            new SequencedBrowserService(new DirectorySnapshot("A", [file])),
            (path, _) =>
            {
                openedPath = path;
                return Task.CompletedTask;
            });
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out _, out DataGrid grid);

        try
        {
            Click(window, VisibleKindIcon(FindRow(grid, file)));

            Assert.Same(file, browser.SelectedEntry);
            Assert.Same(file, grid.SelectedItem);
            Assert.Equal(file.FullPath, openedPath);
        }
        finally
        {
            window.Close();
        }
    }

    private static BrowserViewModel CreateBrowser(
        IDirectoryBrowserService service,
        Func<string, CancellationToken, Task>? openFile = null) =>
        new(service, new NullPicker(), openFile ?? ((_, _) => Task.CompletedTask), "A");

    private static Window ShowBrowser(
        BrowserViewModel browser,
        out BrowserView view,
        out DataGrid grid,
        double height = 700)
    {
        view = new BrowserView { DataContext = browser };
        var window = new Window
        {
            Width = 900,
            Height = height,
            Content = view
        };
        window.Show();
        view.UpdateLayout();
        grid = view.FindControl<DataGrid>("EntriesGrid")
               ?? throw new InvalidOperationException("Navigator entries grid was not created.");
        return window;
    }

    private static void AssertAlternation(DataGrid grid, string even, string odd)
    {
        IReadOnlyList<DataGridRow> rows = RealizedRows(grid);
        Assert.True(rows.Count >= 2);

        foreach (DataGridRow row in rows)
        {
            string expected = row.Index % 2 == 0 ? even : odd;
            Assert.Equal(Color.Parse(expected), AsSolidColor(row.Background).Color);
        }
    }

    private static void AssertRealizedParity(DataGrid grid)
    {
        foreach (DataGridRow row in RealizedRows(grid))
        {
            bool shouldBeEven = row.Index % 2 == 0;
            Assert.Equal(shouldBeEven, row.Classes.Contains("navigatorRowEven"));
            Assert.Equal(!shouldBeEven, row.Classes.Contains("navigatorRowOdd"));
        }
    }

    private static IReadOnlyList<DataGridRow> RealizedRows(DataGrid grid) =>
        grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .Where(row => row.Index >= 0)
            .OrderBy(row => row.Index)
            .ToArray();

    private static DataGridRow FindRow(DataGrid grid, BrowserEntry entry) =>
        RealizedRows(grid).Single(row => ReferenceEquals(row.DataContext, entry));

    private static Rectangle BackgroundLayer(DataGridRow row) =>
        row.GetVisualDescendants()
            .OfType<Rectangle>()
            .Single(control => control.Name == "BackgroundRectangle");

    private static PathIcon VisibleKindIcon(DataGridRow row) =>
        row.GetVisualDescendants()
            .OfType<PathIcon>()
            .Single(icon => icon.Classes.Contains("navigatorKindIcon") && icon.IsVisible);

    private static void AssertKindAccessibilityAndAlignment(
        DataGridRow row,
        PathIcon icon,
        string expectedKind)
    {
        Grid host = row.GetVisualDescendants()
            .OfType<Grid>()
            .Single(control => control.Name == "KindIconHost");
        DataGridCell kindCell = row.GetVisualDescendants().OfType<DataGridCell>().First();

        Assert.Equal(expectedKind, ToolTip.GetTip(host)?.ToString());
        Assert.Equal(expectedKind, AutomationProperties.GetName(host));
        Assert.Equal(expectedKind, AutomationProperties.GetName(icon));

        Point iconOrigin = icon.TranslatePoint(default, host)
                           ?? throw new InvalidOperationException("Could not locate Kind icon in its host.");
        Point hostOrigin = host.TranslatePoint(default, kindCell)
                           ?? throw new InvalidOperationException("Could not locate Kind icon host in its cell.");
        Assert.InRange(Math.Abs((iconOrigin.X + (icon.Bounds.Width / 2)) - (host.Bounds.Width / 2)), 0, 0.5);
        Assert.InRange(Math.Abs((iconOrigin.Y + (icon.Bounds.Height / 2)) - (host.Bounds.Height / 2)), 0, 0.5);
        Assert.InRange(Math.Abs((hostOrigin.X + (host.Bounds.Width / 2)) - (kindCell.Bounds.Width / 2)), 0, 1.5);
        Assert.InRange(Math.Abs((hostOrigin.Y + (host.Bounds.Height / 2)) - (kindCell.Bounds.Height / 2)), 0, 1.5);
    }

    private static void Click(Window window, Control control)
    {
        Point point = control.TranslatePoint(
                          new Point(Math.Max(1, control.Bounds.Width / 2), Math.Max(1, control.Bounds.Height / 2)),
                          window)
                      ?? throw new InvalidOperationException("Could not locate control.");
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
    }

    private static void MovePointerTo(Window window, Control control)
    {
        Point point = control.TranslatePoint(
                          new Point(Math.Max(1, control.Bounds.Width / 2), Math.Max(1, control.Bounds.Height / 2)),
                          window)
                      ?? throw new InvalidOperationException("Could not locate row.");
        window.MouseMove(point, RawInputModifiers.None);
    }

    private static IReadOnlyList<BrowserEntry> CreateEntries(string parent, int count) =>
        Enumerable.Range(0, count)
            .Select(index => new BrowserEntry(
                $"Entry {index:000}",
                $"{parent}/Entry {index:000}",
                index % 3 == 0 ? BrowserEntryKind.Directory : BrowserEntryKind.File,
                index,
                DateTime.UnixEpoch.AddMinutes(index)))
            .ToArray();

    private static BrowserEntry Entry(string name, BrowserEntryKind kind) =>
        new(name, $"A/{name}", kind, 1, DateTime.UnixEpoch);

    private static SolidColorBrush AsSolidColor(IBrush? brush) =>
        Assert.IsType<SolidColorBrush>(brush);

    private sealed class SequencedBrowserService(params DirectorySnapshot[] snapshots) : IDirectoryBrowserService
    {
        private readonly Dictionary<string, Queue<DirectorySnapshot>> _snapshots = snapshots
            .GroupBy(snapshot => snapshot.Path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new Queue<DirectorySnapshot>(group),
                StringComparer.OrdinalIgnoreCase);

        public Task<DirectorySnapshot> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            Queue<DirectorySnapshot> matches = _snapshots[path];
            DirectorySnapshot snapshot = matches.Count > 1 ? matches.Dequeue() : matches.Peek();
            return Task.FromResult(snapshot);
        }
    }

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }
}
