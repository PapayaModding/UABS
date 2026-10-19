using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using UABS.Page;
using UABS.Services;
using UABS.UnityAssets;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class AssetsWorkspaceViewInteractionTests
{
    [AvaloniaFact]
    public void Splitters_ResizeNavigatorAndInspectorIndependently()
    {
        using MainWindowViewModel viewModel = CreateViewModel();
        Window window = ShowWorkspace(viewModel, out AssetsWorkspaceView view);

        try
        {
            GridSplitter navigator = Find<GridSplitter>(view, "NavigatorSplitter");
            GridSplitter inspector = Find<GridSplitter>(view, "InspectorSplitter");
            double initialNavigator = viewModel.AssetsLayout.NavigatorWidth;
            double initialInspector = viewModel.AssetsLayout.InspectorWidth;

            Drag(window, navigator, 70);

            Assert.True(viewModel.AssetsLayout.NavigatorWidth > initialNavigator);
            Assert.Equal(initialInspector, viewModel.AssetsLayout.InspectorWidth);
            double resizedNavigator = viewModel.AssetsLayout.NavigatorWidth;

            Drag(window, inspector, -60);

            Assert.True(viewModel.AssetsLayout.InspectorWidth > initialInspector);
            Assert.Equal(resizedNavigator, viewModel.AssetsLayout.NavigatorWidth);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CompactControls_CollapseAndKeyboardRestoreBothPanelsWithAccessibleNames()
    {
        using MainWindowViewModel viewModel = CreateViewModel();
        viewModel.AssetsLayout.NavigatorWidth = 410;
        viewModel.AssetsLayout.InspectorWidth = 390;
        Window window = ShowWorkspace(viewModel, out AssetsWorkspaceView view);

        try
        {
            Grid regions = Find<Grid>(view, "RegionsGrid");
            Button navigatorCollapse = Find<Button>(view, "NavigatorCollapseButton");
            Button inspectorCollapse = Find<Button>(view, "InspectorCollapseButton");
            Button navigatorRestore = Find<Button>(view, "NavigatorRestoreHandle");
            Button inspectorRestore = Find<Button>(view, "InspectorRestoreHandle");
            GridSplitter navigatorSplitter = Find<GridSplitter>(view, "NavigatorSplitter");
            GridSplitter inspectorSplitter = Find<GridSplitter>(view, "InspectorSplitter");

            Assert.Equal("Collapse Navigator", AutomationProperties.GetName(navigatorCollapse));
            Assert.Equal("Collapse Inspector", AutomationProperties.GetName(inspectorCollapse));
            Assert.Equal("Restore Navigator", AutomationProperties.GetName(navigatorRestore));
            Assert.Equal("Restore Inspector", AutomationProperties.GetName(inspectorRestore));
            Assert.NotNull(ToolTip.GetTip(navigatorRestore));
            Assert.NotNull(ToolTip.GetTip(inspectorRestore));

            navigatorCollapse.Command!.Execute(null);
            inspectorCollapse.Command!.Execute(null);
            regions.UpdateLayout();

            Assert.False(navigatorSplitter.IsVisible);
            Assert.False(inspectorSplitter.IsVisible);
            Assert.True(navigatorRestore.IsVisible);
            Assert.True(inspectorRestore.IsVisible);
            Assert.Equal(0, regions.ColumnDefinitions[0].ActualWidth);
            Assert.Equal(0, regions.ColumnDefinitions[1].ActualWidth);
            Assert.Equal(0, regions.ColumnDefinitions[3].ActualWidth);
            Assert.Equal(0, regions.ColumnDefinitions[4].ActualWidth);

            Assert.True(navigatorRestore.Focus());
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            regions.UpdateLayout();

            Assert.True(navigatorSplitter.IsVisible);
            Assert.False(inspectorSplitter.IsVisible);
            Assert.Equal(410, regions.ColumnDefinitions[0].ActualWidth, precision: 1);
            Assert.Equal(390, viewModel.AssetsLayout.InspectorWidth);

            Assert.True(inspectorRestore.Focus());
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            regions.UpdateLayout();

            Assert.True(inspectorSplitter.IsVisible);
            Assert.Equal(390, regions.ColumnDefinitions[4].ActualWidth, precision: 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ChevronButtons_CenterTheirPathIconsAtExpandedAndCollapsedSizes()
    {
        using MainWindowViewModel viewModel = CreateViewModel();
        Window window = ShowWorkspace(viewModel, out AssetsWorkspaceView view);

        try
        {
            Button navigatorCollapse = Find<Button>(view, "NavigatorCollapseButton");
            Button inspectorCollapse = Find<Button>(view, "InspectorCollapseButton");

            AssertIconCentered(navigatorCollapse);
            AssertIconCentered(inspectorCollapse);

            navigatorCollapse.Command!.Execute(null);
            inspectorCollapse.Command!.Execute(null);
            view.UpdateLayout();

            AssertIconCentered(Find<Button>(view, "NavigatorRestoreHandle"));
            AssertIconCentered(Find<Button>(view, "InspectorRestoreHandle"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Splitters_StopAtMinimumWithoutCollapsing()
    {
        using MainWindowViewModel viewModel = CreateViewModel();
        Window window = ShowWorkspace(viewModel, out AssetsWorkspaceView view);

        try
        {
            double navigatorDelta = AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth
                                    - viewModel.AssetsLayout.NavigatorWidth;
            double inspectorDelta = viewModel.AssetsLayout.InspectorWidth
                                    - AssetsWorkspaceLayoutViewModel.InspectorMinimumWidth;

            Drag(window, Find<GridSplitter>(view, "NavigatorSplitter"), navigatorDelta);
            Drag(window, Find<GridSplitter>(view, "InspectorSplitter"), inspectorDelta);

            Assert.True(viewModel.AssetsLayout.IsNavigatorVisible);
            Assert.True(viewModel.AssetsLayout.IsInspectorVisible);
            Assert.Equal(
                AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth,
                viewModel.AssetsLayout.NavigatorWidth);
            Assert.Equal(
                AssetsWorkspaceLayoutViewModel.InspectorMinimumWidth,
                viewModel.AssetsLayout.InspectorWidth);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Splitters_SnapPanelsClosedOnlyAfterDeliberateMinimumOvershoot()
    {
        using MainWindowViewModel viewModel = CreateViewModel();
        viewModel.AssetsLayout.NavigatorWidth = 410;
        viewModel.AssetsLayout.InspectorWidth = 390;
        Window window = ShowWorkspace(viewModel, out AssetsWorkspaceView view);

        try
        {
            double navigatorCollapseDrag = -(
                410
                - AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth
                + AssetsWorkspaceLayoutViewModel.CollapseThreshold
                + 8);
            Drag(window, Find<GridSplitter>(view, "NavigatorSplitter"), navigatorCollapseDrag);

            Assert.False(viewModel.AssetsLayout.IsNavigatorVisible);
            Assert.True(viewModel.AssetsLayout.IsInspectorVisible);
            Assert.Equal(410, viewModel.AssetsLayout.NavigatorWidth);
            Assert.True(Find<Button>(view, "NavigatorRestoreHandle").IsVisible);

            double inspectorCollapseDrag = 390
                                           - AssetsWorkspaceLayoutViewModel.InspectorMinimumWidth
                                           + AssetsWorkspaceLayoutViewModel.CollapseThreshold
                                           + 8;
            Drag(window, Find<GridSplitter>(view, "InspectorSplitter"), inspectorCollapseDrag);

            Assert.False(viewModel.AssetsLayout.IsInspectorVisible);
            Assert.Equal(390, viewModel.AssetsLayout.InspectorWidth);
            Assert.True(Find<Button>(view, "InspectorRestoreHandle").IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RepeatedCollapseRestore_IsIndependentAndRetainsComfortableWidths()
    {
        using MainWindowViewModel viewModel = CreateViewModel();
        viewModel.AssetsLayout.NavigatorWidth = 405;
        viewModel.AssetsLayout.InspectorWidth = 375;
        Window window = ShowWorkspace(viewModel, out AssetsWorkspaceView view);

        try
        {
            Button navigatorCollapse = Find<Button>(view, "NavigatorCollapseButton");
            Button navigatorRestore = Find<Button>(view, "NavigatorRestoreHandle");

            for (int index = 0; index < 5; index++)
            {
                navigatorCollapse.Command!.Execute(null);
                navigatorRestore.Command!.Execute(null);
            }

            view.UpdateLayout();
            Assert.True(viewModel.AssetsLayout.IsNavigatorVisible);
            Assert.True(viewModel.AssetsLayout.IsInspectorVisible);
            Assert.Equal(405, viewModel.AssetsLayout.NavigatorWidth);
            Assert.Equal(375, viewModel.AssetsLayout.InspectorWidth);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NarrowSupportedWindow_KeepsCenterAndCompactControlsAccessible()
    {
        using MainWindowViewModel viewModel = CreateViewModel();
        viewModel.AssetsLayout.NavigatorWidth = 500;
        viewModel.AssetsLayout.InspectorWidth = 500;
        Window window = ShowWorkspace(viewModel, out AssetsWorkspaceView view);

        try
        {
            window.Width = 900;
            view.UpdateLayout();

            Grid regions = Find<Grid>(view, "RegionsGrid");
            Assert.True(regions.ColumnDefinitions[2].ActualWidth >= 279);
            Assert.True(Find<Button>(view, "NavigatorCollapseButton").Bounds.Width > 0);
            Assert.True(Find<Button>(view, "InspectorCollapseButton").Bounds.Width > 0);

            Find<Button>(view, "NavigatorCollapseButton").Command!.Execute(null);
            Find<Button>(view, "InspectorCollapseButton").Command!.Execute(null);
            view.UpdateLayout();

            Assert.True(Find<Button>(view, "NavigatorRestoreHandle").Bounds.Width > 0);
            Assert.True(Find<Button>(view, "InspectorRestoreHandle").Bounds.Width > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task InspectorAndNavigatorCollapse_PreserveSelectionDetailsAndSession()
    {
        AssetEntry asset = new("Hero", "Texture2D", 28, 42, 128, "main.assets", "fixture.assets");
        var session = new TestSession(asset);
        using MainWindowViewModel viewModel = new(new NullPicker(), new SingleSessionFactory(session));
        Window window = ShowWorkspace(viewModel, out AssetsWorkspaceView view);

        try
        {
            AssetInspectorView inspector = view.GetVisualDescendants().OfType<AssetInspectorView>().Single();
            TextBlock empty = Find<TextBlock>(inspector, "NoSelectionMessage");
            TextBlock selectedName = Find<TextBlock>(inspector, "SelectedAssetName");

            Assert.True(empty.IsVisible);
            await viewModel.LoadFileAsync(session.Metadata.SourcePath);
            viewModel.AssetList.SelectedAsset = asset;
            await viewModel.AssetList.DetailsLoadingTask;
            view.UpdateLayout();

            Assert.False(empty.IsVisible);
            Assert.Equal("Hero", selectedName.Text);
            Assert.Equal("Texture2D", Find<TextBlock>(inspector, "SelectedAssetType").Text);
            Assert.Equal("42", Find<TextBlock>(inspector, "SelectedAssetPathId").Text);
            Assert.Equal("SerializedFile", Find<TextBlock>(inspector, "SelectedFileKind").Text);
            Assert.Contains("not available yet", Find<TextBlock>(inspector, "PreviewPlaceholder").Text);

            Find<Button>(view, "InspectorCollapseButton").Command!.Execute(null);
            Find<Button>(view, "NavigatorCollapseButton").Command!.Execute(null);
            view.UpdateLayout();

            Assert.Same(asset, viewModel.AssetList.SelectedAsset);
            Assert.Equal(42, viewModel.AssetList.SelectedDetails?.PathId);
            Assert.False(session.IsDisposed);

            Find<Button>(view, "InspectorRestoreHandle").Command!.Execute(null);
            view.UpdateLayout();
            Assert.Equal("Hero", selectedName.Text);
        }
        finally
        {
            window.Close();
        }
    }

    private static MainWindowViewModel CreateViewModel() =>
        new(new NullPicker(), new EmptySessionFactory());

    private static Window ShowWorkspace(
        MainWindowViewModel viewModel,
        out AssetsWorkspaceView view)
    {
        view = new AssetsWorkspaceView { DataContext = viewModel };
        var window = new Window
        {
            Width = 1400,
            Height = 760,
            Content = view
        };
        window.Show();
        view.UpdateLayout();
        return window;
    }

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
        control.UpdateLayout();
    }

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class EmptySessionFactory : IAssetSessionFactory
    {
        public Task<IAssetSession> OpenAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class SingleSessionFactory(IAssetSession session) : IAssetSessionFactory
    {
        public Task<IAssetSession> OpenAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(session);
    }

    private sealed class TestSession(AssetEntry asset) : IAssetSession
    {
        public AssetSessionMetadata Metadata { get; } = new(
            Path.GetFullPath("fixture.assets"),
            "fixture.assets",
            UnityFileKind.SerializedFile,
            "2021.3.0f1",
            1);

        public IReadOnlyList<AssetEntry> Assets { get; } = [asset];
        public IReadOnlyList<string> Warnings { get; } = [];
        public bool IsDisposed { get; private set; }

        public Task<AssetDetails> GetDetailsAsync(AssetEntry entry, CancellationToken cancellationToken = default) =>
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

        public void Dispose() => IsDisposed = true;
    }
}
