using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class AssetsWorkspaceLayoutViewModelTests
{
    [Fact]
    public void Widths_AreIndependentAndClampedToSafeBounds()
    {
        var layout = new AssetsWorkspaceLayoutViewModel();

        layout.NavigatorWidth = 100;
        layout.InspectorWidth = 900;

        Assert.Equal(AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth, layout.NavigatorWidth);
        Assert.Equal(AssetsWorkspaceLayoutViewModel.PanelMaximumWidth, layout.InspectorWidth);

        layout.NavigatorWidth = 410;
        Assert.Equal(410, layout.NavigatorWidth);
        Assert.Equal(AssetsWorkspaceLayoutViewModel.PanelMaximumWidth, layout.InspectorWidth);
    }

    [Fact]
    public void ToggleNavigator_CollapsesSplitterAndRestoresPreviousWidth()
    {
        var layout = new AssetsWorkspaceLayoutViewModel { NavigatorWidth = 412 };

        layout.ToggleNavigatorCommand.Execute(null);

        Assert.False(layout.IsNavigatorVisible);
        Assert.Equal(0, layout.NavigatorColumnWidth.Value);
        Assert.Equal(0, layout.NavigatorSplitterColumnWidth.Value);
        Assert.Equal(0, layout.NavigatorColumnMinimumWidth);

        layout.ToggleNavigatorCommand.Execute(null);

        Assert.True(layout.IsNavigatorVisible);
        Assert.Equal(412, layout.NavigatorColumnWidth.Value);
        Assert.Equal(AssetsWorkspaceLayoutViewModel.SplitterWidth, layout.NavigatorSplitterColumnWidth.Value);
    }

    [Fact]
    public void ToggleInspector_CollapsesSplitterAndRestoresPreviousWidth()
    {
        var layout = new AssetsWorkspaceLayoutViewModel { InspectorWidth = 438 };

        layout.ToggleInspectorCommand.Execute(null);

        Assert.False(layout.IsInspectorVisible);
        Assert.Equal(0, layout.InspectorColumnWidth.Value);
        Assert.Equal(0, layout.InspectorSplitterColumnWidth.Value);
        Assert.Equal(0, layout.InspectorColumnMinimumWidth);

        layout.ToggleInspectorCommand.Execute(null);

        Assert.True(layout.IsInspectorVisible);
        Assert.Equal(438, layout.InspectorColumnWidth.Value);
        Assert.Equal(AssetsWorkspaceLayoutViewModel.SplitterWidth, layout.InspectorSplitterColumnWidth.Value);
    }

    [Fact]
    public void BothPanels_CollapseAndRestoreIndependently()
    {
        var layout = new AssetsWorkspaceLayoutViewModel
        {
            NavigatorWidth = 390,
            InspectorWidth = 370
        };

        layout.ToggleNavigatorCommand.Execute(null);
        layout.ToggleInspectorCommand.Execute(null);

        Assert.False(layout.IsNavigatorVisible);
        Assert.False(layout.IsInspectorVisible);
        Assert.Equal(0, layout.NavigatorColumnWidth.Value);
        Assert.Equal(0, layout.InspectorColumnWidth.Value);

        layout.ToggleInspectorCommand.Execute(null);

        Assert.False(layout.IsNavigatorVisible);
        Assert.True(layout.IsInspectorVisible);
        Assert.Equal(370, layout.InspectorWidth);
        Assert.Equal(390, layout.NavigatorWidth);
    }
}
