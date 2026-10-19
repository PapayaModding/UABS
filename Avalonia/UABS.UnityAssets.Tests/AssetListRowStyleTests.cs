using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using UABS.Page;
using UABS.Services;
using UABS.UnityAssets;
using UABS.ViewModels;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

namespace UABS.UnityAssets.Tests;

public sealed class AssetListRowStyleTests
{
    [AvaloniaFact]
    public async Task ThemeResourcesAndAlternatingRows_ResolveForLightAndDarkThemes()
    {
        using MainWindowViewModel viewModel = await CreateViewModelAsync(CreateAssets(20));
        Window window = ShowAssetList(viewModel, out FolderView view, out DataGrid grid);

        try
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
            view.UpdateLayout();
            AssertAlternation(grid, "#F3F8F3", "#E8F2E9");

            window.RequestedThemeVariant = ThemeVariant.Dark;
            view.UpdateLayout();
            AssertAlternation(grid, "#18231B", "#1E2C22");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task SortingFilteringAndVirtualizedScrolling_ReapplyParityFromVisibleIndex()
    {
        IReadOnlyList<AssetEntry> assets = CreateAssets(120);
        using MainWindowViewModel viewModel = await CreateViewModelAsync(assets);
        Window window = ShowAssetList(viewModel, out FolderView view, out DataGrid grid, height: 300);

        try
        {
            viewModel.AssetList.SortField = AssetSortField.PathId;
            viewModel.AssetList.SortDescending = true;
            viewModel.AssetList.NameFilter = "Asset 0";
            view.UpdateLayout();

            Assert.Equal(99, viewModel.AssetList.Assets[0].PathId);
            AssertRealizedParity(grid);

            AssetEntry target = viewModel.AssetList.Assets[80];
            grid.ScrollIntoView(target, null);
            view.UpdateLayout();

            DataGridRow targetRow = FindRow(grid, target);
            Assert.Equal(80, targetRow.Index);
            Assert.Contains("assetRowEven", targetRow.Classes);
            Assert.DoesNotContain("assetRowOdd", targetRow.Classes);
            AssertRealizedParity(grid);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task HoverSelectionMultiSelectionAndKeyboardFocus_KeepDistinctVisualStates()
    {
        using MainWindowViewModel viewModel = await CreateViewModelAsync(CreateAssets(20));
        Window window = ShowAssetList(viewModel, out FolderView view, out DataGrid grid);

        try
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
            view.UpdateLayout();

            AssetEntry firstAsset = viewModel.AssetList.Assets[0];
            AssetEntry secondAsset = viewModel.AssetList.Assets[1];
            DataGridRow firstRow = FindRow(grid, firstAsset);
            DataGridRow secondRow = FindRow(grid, secondAsset);

            MovePointerTo(window, firstRow);
            view.UpdateLayout();
            Assert.Equal(Color.Parse("#CCE3D0"), AsSolidColor(BackgroundLayer(firstRow).Fill).Color);

            grid.SelectedItem = firstAsset;
            window.MouseMove(new Point(window.Bounds.Width - 2, window.Bounds.Height - 2), RawInputModifiers.None);
            view.UpdateLayout();
            Assert.Equal(Color.Parse("#39724D"), AsSolidColor(BackgroundLayer(firstRow).Fill).Color);

            MovePointerTo(window, firstRow);
            view.UpdateLayout();
            Assert.Equal(Color.Parse("#2F613F"), AsSolidColor(BackgroundLayer(firstRow).Fill).Color);

            grid.SelectedItems.Add(secondAsset);
            view.UpdateLayout();
            Assert.True(firstRow.IsSelected);
            Assert.True(secondRow.IsSelected);
            Assert.Equal(Color.Parse("#39724D"), AsSolidColor(BackgroundLayer(secondRow).Fill).Color);
            Assert.All(
                secondRow.GetVisualDescendants().OfType<DataGridCell>(),
                cell => Assert.Equal(Color.Parse("#FFFFFF"), AsSolidColor(cell.Foreground).Color));

            DataGridCell focusedCell = firstRow.GetVisualDescendants().OfType<DataGridCell>().First();
            Assert.True(focusedCell.Focus(NavigationMethod.Tab, KeyModifiers.None));
            view.UpdateLayout();
            Assert.True(focusedCell.IsFocused);
            Grid focusVisual = focusedCell.GetVisualDescendants()
                .OfType<Grid>()
                .Single(control => control.Name == "FocusVisual");
            Assert.Equal(2, focusVisual.Children.OfType<Rectangle>().Count());
            Assert.All(
                focusVisual.Children.OfType<Rectangle>(),
                rectangle => Assert.NotNull(rectangle.Stroke));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task AssetKindIconColumn_IsFixedWhileOtherColumnsRemainResizable()
    {
        IReadOnlyList<AssetEntry> assets = CreateAssets(4);
        using MainWindowViewModel viewModel = await CreateViewModelAsync(assets);
        Window window = ShowAssetList(viewModel, out FolderView view, out DataGrid grid);

        try
        {
            DataGridTemplateColumn kindColumn = Assert.IsType<DataGridTemplateColumn>(grid.Columns[0]);
            Assert.Equal(string.Empty, kindColumn.Header);
            Assert.False(kindColumn.CanUserResize);
            Assert.True(grid.CanUserResizeColumns);
            Assert.All(grid.Columns.Skip(1), column => Assert.True(column.CanUserResize));
            Assert.Equal(42, kindColumn.ActualWidth, precision: 1);

            grid.Columns[1].Width = new DataGridLength(320);
            view.UpdateLayout();

            Assert.Equal(42, kindColumn.ActualWidth, precision: 1);
            DataGridRow firstRow = FindRow(grid, assets[0]);
            DataGridCell kindCell = firstRow.GetVisualDescendants().OfType<DataGridCell>().First();
            Image icon = kindCell.GetVisualDescendants().OfType<Image>().Single();
            Assert.Equal(24, icon.Bounds.Width, precision: 1);
            Assert.Equal(24, icon.Bounds.Height, precision: 1);
        }
        finally
        {
            window.Close();
        }
    }

    private static async Task<MainWindowViewModel> CreateViewModelAsync(IReadOnlyList<AssetEntry> assets)
    {
        var session = new TestSession(assets);
        var viewModel = new MainWindowViewModel(new NullPicker(), new SingleSessionFactory(session));
        await viewModel.LoadFileAsync(session.Metadata.SourcePath);
        return viewModel;
    }

    private static Window ShowAssetList(
        MainWindowViewModel viewModel,
        out FolderView view,
        out DataGrid grid,
        double height = 700)
    {
        view = new FolderView { DataContext = viewModel };
        var window = new Window
        {
            Width = 1100,
            Height = height,
            Content = view
        };
        window.Show();
        view.UpdateLayout();
        grid = view.FindControl<DataGrid>("AssetsGrid")
               ?? throw new InvalidOperationException("Assets grid was not created.");
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
            Assert.Equal(shouldBeEven, row.Classes.Contains("assetRowEven"));
            Assert.Equal(!shouldBeEven, row.Classes.Contains("assetRowOdd"));
        }
    }

    private static IReadOnlyList<DataGridRow> RealizedRows(DataGrid grid) =>
        grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .Where(row => row.Index >= 0)
            .OrderBy(row => row.Index)
            .ToArray();

    private static DataGridRow FindRow(DataGrid grid, AssetEntry asset) =>
        RealizedRows(grid).Single(row => ReferenceEquals(row.DataContext, asset));

    private static Rectangle BackgroundLayer(DataGridRow row) =>
        row.GetVisualDescendants()
            .OfType<Rectangle>()
            .Single(control => control.Name == "BackgroundRectangle");

    private static void MovePointerTo(Window window, Control control)
    {
        Point point = control.TranslatePoint(
                          new Point(Math.Max(1, control.Bounds.Width / 2), Math.Max(1, control.Bounds.Height / 2)),
                          window)
                      ?? throw new InvalidOperationException("Could not locate row.");
        window.MouseMove(point, RawInputModifiers.None);
    }

    private static IReadOnlyList<AssetEntry> CreateAssets(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new AssetEntry(
                $"Asset {index:000}",
                index % 2 == 0 ? "Texture2D" : "Sprite",
                index % 2 == 0 ? 28 : 213,
                index,
                (uint)(index + 1),
                "fixture.assets",
                "fixture.assets"))
            .ToArray();

    private static SolidColorBrush AsSolidColor(IBrush? brush) =>
        Assert.IsType<SolidColorBrush>(brush);

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class SingleSessionFactory(IAssetSession session) : IAssetSessionFactory
    {
        public Task<IAssetSession> OpenAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(session);
    }

    private sealed class TestSession(IReadOnlyList<AssetEntry> assets) : IAssetSession
    {
        public AssetSessionMetadata Metadata { get; } = new(
            Path.GetFullPath("fixture.assets"),
            "fixture.assets",
            UnityFileKind.SerializedFile,
            "2021.3.0f1",
            1);

        public IReadOnlyList<AssetEntry> Assets { get; } = assets;
        public IReadOnlyList<string> Warnings { get; } = [];

        public Task<AssetDetails> GetDetailsAsync(
            AssetEntry entry,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AssetDetails(
                entry.Name,
                entry.Type,
                entry.ClassId,
                entry.PathId,
                entry.ByteSize,
                entry.LogicalFile,
                entry.SourcePath,
                Metadata.Kind,
                Metadata.UnityVersion,
                Metadata.LogicalFileCount));

        public void Dispose() { }
    }
}
