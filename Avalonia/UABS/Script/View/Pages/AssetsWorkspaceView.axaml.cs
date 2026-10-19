using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using UABS.ViewModels;

namespace UABS.Page;

public partial class AssetsWorkspaceView : UserControl
{
    private readonly SplitterCollapseGesture _navigatorGesture = new(collapseWithPositiveDelta: false);
    private readonly SplitterCollapseGesture _inspectorGesture = new(collapseWithPositiveDelta: true);
    private MainWindowViewModel? _observedViewModel;

    public AssetsWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        RegionsGrid.SizeChanged += (_, _) => UpdatePanelMaximumWidths();
        ObserveViewModel(DataContext as MainWindowViewModel);
    }

    private void OnDataContextChanged(object? sender, EventArgs eventArgs) =>
        ObserveViewModel(DataContext as MainWindowViewModel);

    private void ObserveViewModel(MainWindowViewModel? viewModel)
    {
        if (_observedViewModel is not null)
            _observedViewModel.AssetsLayout.PropertyChanged -= OnLayoutPropertyChanged;

        _observedViewModel = viewModel;

        if (_observedViewModel is not null)
            _observedViewModel.AssetsLayout.PropertyChanged += OnLayoutPropertyChanged;

        UpdatePanelMaximumWidths();
    }

    private void OnLayoutPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs) =>
        UpdatePanelMaximumWidths();

    private void OnNavigatorSplitterDragStarted(object? sender, VectorEventArgs eventArgs) =>
        _navigatorGesture.Begin(RegionsGrid.ColumnDefinitions[0].ActualWidth);

    private void OnNavigatorSplitterDragDelta(object? sender, VectorEventArgs eventArgs) =>
        _navigatorGesture.Track(eventArgs.Vector.X);

    private void OnNavigatorSplitterDragCompleted(object? sender, VectorEventArgs eventArgs)
    {
        if (DataContext is not MainWindowViewModel { AssetsLayout.IsNavigatorVisible: true } viewModel)
            return;

        if (_navigatorGesture.ShouldCollapse(AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth))
            viewModel.AssetsLayout.ToggleNavigatorCommand.Execute(null);
        else
            viewModel.AssetsLayout.NavigatorWidth = RegionsGrid.ColumnDefinitions[0].ActualWidth;

        UpdatePanelMaximumWidths();
    }

    private void OnInspectorSplitterDragStarted(object? sender, VectorEventArgs eventArgs) =>
        _inspectorGesture.Begin(RegionsGrid.ColumnDefinitions[4].ActualWidth);

    private void OnInspectorSplitterDragDelta(object? sender, VectorEventArgs eventArgs) =>
        _inspectorGesture.Track(eventArgs.Vector.X);

    private void OnInspectorSplitterDragCompleted(object? sender, VectorEventArgs eventArgs)
    {
        if (DataContext is not MainWindowViewModel { AssetsLayout.IsInspectorVisible: true } viewModel)
            return;

        if (_inspectorGesture.ShouldCollapse(AssetsWorkspaceLayoutViewModel.InspectorMinimumWidth))
            viewModel.AssetsLayout.ToggleInspectorCommand.Execute(null);
        else
            viewModel.AssetsLayout.InspectorWidth = RegionsGrid.ColumnDefinitions[4].ActualWidth;

        UpdatePanelMaximumWidths();
    }

    private void UpdatePanelMaximumWidths()
    {
        if (_observedViewModel is null || RegionsGrid.Bounds.Width <= 0)
            return;

        AssetsWorkspaceLayoutViewModel layout = _observedViewModel.AssetsLayout;
        double splitterWidth = (layout.IsNavigatorVisible ? AssetsWorkspaceLayoutViewModel.SplitterWidth : 0)
                               + (layout.IsInspectorVisible ? AssetsWorkspaceLayoutViewModel.SplitterWidth : 0);
        double availableForPanels = Math.Max(0, RegionsGrid.Bounds.Width - 280 - splitterWidth);

        double navigatorMaximum = AssetsWorkspaceLayoutViewModel.PanelMaximumWidth;
        double inspectorMaximum = AssetsWorkspaceLayoutViewModel.PanelMaximumWidth;

        if (layout.IsNavigatorVisible && layout.IsInspectorVisible
            && layout.NavigatorWidth + layout.InspectorWidth > availableForPanels)
        {
            double minimumTotal = AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth
                                  + AssetsWorkspaceLayoutViewModel.InspectorMinimumWidth;
            double distributable = Math.Max(0, availableForPanels - minimumTotal);
            double navigatorExtra = layout.NavigatorWidth - AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth;
            double inspectorExtra = layout.InspectorWidth - AssetsWorkspaceLayoutViewModel.InspectorMinimumWidth;
            double requestedExtra = navigatorExtra + inspectorExtra;
            double navigatorShare = requestedExtra > 0 ? navigatorExtra / requestedExtra : 0.5;

            navigatorMaximum = AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth
                               + (distributable * navigatorShare);
            inspectorMaximum = AssetsWorkspaceLayoutViewModel.InspectorMinimumWidth
                               + (distributable * (1 - navigatorShare));
        }
        else if (layout.IsNavigatorVisible && !layout.IsInspectorVisible)
        {
            navigatorMaximum = Math.Max(
                AssetsWorkspaceLayoutViewModel.NavigatorMinimumWidth,
                Math.Min(AssetsWorkspaceLayoutViewModel.PanelMaximumWidth, availableForPanels));
        }
        else if (!layout.IsNavigatorVisible && layout.IsInspectorVisible)
        {
            inspectorMaximum = Math.Max(
                AssetsWorkspaceLayoutViewModel.InspectorMinimumWidth,
                Math.Min(AssetsWorkspaceLayoutViewModel.PanelMaximumWidth, availableForPanels));
        }

        RegionsGrid.ColumnDefinitions[0].MaxWidth = navigatorMaximum;
        RegionsGrid.ColumnDefinitions[4].MaxWidth = inspectorMaximum;
    }

    private sealed class SplitterCollapseGesture(bool collapseWithPositiveDelta)
    {
        private double _startingWidth;
        private double _collapseDistance;

        public void Begin(double startingWidth)
        {
            _startingWidth = startingWidth;
            _collapseDistance = 0;
        }

        public void Track(double horizontalDelta)
        {
            _collapseDistance += collapseWithPositiveDelta ? horizontalDelta : -horizontalDelta;
        }

        public bool ShouldCollapse(double minimumWidth)
        {
            double distanceToMinimum = Math.Max(0, _startingWidth - minimumWidth);
            return _collapseDistance >= distanceToMinimum + AssetsWorkspaceLayoutViewModel.CollapseThreshold;
        }
    }
}
