using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using UABS.ViewModels;

namespace UABS.Page;

public partial class SearchWorkspaceView : UserControl
{
    private const string EvenRowClass = "assetRowEven";
    private const string OddRowClass = "assetRowOdd";
    private const double ResultsMinimumWidth = 280;
    private readonly SplitterCollapseGesture _controlsGesture = new(collapseWithPositiveDelta: false);
    private readonly SplitterCollapseGesture _inspectorGesture = new(collapseWithPositiveDelta: true);
    private SearchWorkspaceViewModel? _observedViewModel;

    public SearchWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SearchRegionsGrid.SizeChanged += (_, _) => UpdatePanelMaximumWidths();
        ObserveViewModel(DataContext as SearchWorkspaceViewModel);
    }

    private void OnDataContextChanged(object? sender, EventArgs eventArgs) =>
        ObserveViewModel(DataContext as SearchWorkspaceViewModel);

    private void ObserveViewModel(SearchWorkspaceViewModel? viewModel)
    {
        if (_observedViewModel is not null)
            _observedViewModel.Layout.PropertyChanged -= OnLayoutPropertyChanged;

        _observedViewModel = viewModel;

        if (_observedViewModel is not null)
            _observedViewModel.Layout.PropertyChanged += OnLayoutPropertyChanged;

        UpdatePanelMaximumWidths();
    }

    private void OnLayoutPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs) =>
        UpdatePanelMaximumWidths();

    private void OnSearchControlsSplitterDragStarted(object? sender, VectorEventArgs eventArgs) =>
        _controlsGesture.Begin(SearchRegionsGrid.ColumnDefinitions[0].ActualWidth);

    private void OnSearchControlsSplitterDragDelta(object? sender, VectorEventArgs eventArgs) =>
        _controlsGesture.Track(eventArgs.Vector.X);

    private void OnSearchControlsSplitterDragCompleted(object? sender, VectorEventArgs eventArgs)
    {
        if (DataContext is not SearchWorkspaceViewModel { Layout.AreControlsVisible: true } viewModel)
            return;

        if (_controlsGesture.ShouldCollapse(SearchWorkspaceLayoutViewModel.ControlsMinimumWidth))
            viewModel.Layout.ToggleControlsCommand.Execute(null);
        else
            viewModel.Layout.ControlsWidth = SearchRegionsGrid.ColumnDefinitions[0].ActualWidth;

        UpdatePanelMaximumWidths();
    }

    private void OnSearchInspectorSplitterDragStarted(object? sender, VectorEventArgs eventArgs) =>
        _inspectorGesture.Begin(SearchRegionsGrid.ColumnDefinitions[4].ActualWidth);

    private void OnSearchInspectorSplitterDragDelta(object? sender, VectorEventArgs eventArgs) =>
        _inspectorGesture.Track(eventArgs.Vector.X);

    private void OnSearchInspectorSplitterDragCompleted(object? sender, VectorEventArgs eventArgs)
    {
        if (DataContext is not SearchWorkspaceViewModel { Layout.IsInspectorVisible: true } viewModel)
            return;

        if (_inspectorGesture.ShouldCollapse(SearchWorkspaceLayoutViewModel.InspectorMinimumWidth))
            viewModel.Layout.ToggleInspectorCommand.Execute(null);
        else
            viewModel.Layout.InspectorWidth = SearchRegionsGrid.ColumnDefinitions[4].ActualWidth;

        UpdatePanelMaximumWidths();
    }

    private void UpdatePanelMaximumWidths()
    {
        if (_observedViewModel is null || SearchRegionsGrid.Bounds.Width <= 0)
            return;

        SearchWorkspaceLayoutViewModel layout = _observedViewModel.Layout;
        double splitterWidth = (layout.AreControlsVisible ? SearchWorkspaceLayoutViewModel.SplitterWidth : 0)
                               + (layout.IsInspectorVisible ? SearchWorkspaceLayoutViewModel.SplitterWidth : 0);
        double availableForPanels = Math.Max(0, SearchRegionsGrid.Bounds.Width - ResultsMinimumWidth - splitterWidth);

        double controlsMaximum = SearchWorkspaceLayoutViewModel.PanelMaximumWidth;
        double inspectorMaximum = SearchWorkspaceLayoutViewModel.PanelMaximumWidth;

        if (layout.AreControlsVisible && layout.IsInspectorVisible
            && layout.ControlsWidth + layout.InspectorWidth > availableForPanels)
        {
            double minimumTotal = SearchWorkspaceLayoutViewModel.ControlsMinimumWidth
                                  + SearchWorkspaceLayoutViewModel.InspectorMinimumWidth;
            double distributable = Math.Max(0, availableForPanels - minimumTotal);
            double controlsExtra = layout.ControlsWidth - SearchWorkspaceLayoutViewModel.ControlsMinimumWidth;
            double inspectorExtra = layout.InspectorWidth - SearchWorkspaceLayoutViewModel.InspectorMinimumWidth;
            double requestedExtra = controlsExtra + inspectorExtra;
            double controlsShare = requestedExtra > 0 ? controlsExtra / requestedExtra : 0.5;

            controlsMaximum = SearchWorkspaceLayoutViewModel.ControlsMinimumWidth
                              + (distributable * controlsShare);
            inspectorMaximum = SearchWorkspaceLayoutViewModel.InspectorMinimumWidth
                               + (distributable * (1 - controlsShare));
        }
        else if (layout.AreControlsVisible && !layout.IsInspectorVisible)
        {
            controlsMaximum = Math.Max(
                SearchWorkspaceLayoutViewModel.ControlsMinimumWidth,
                Math.Min(SearchWorkspaceLayoutViewModel.PanelMaximumWidth, availableForPanels));
        }
        else if (!layout.AreControlsVisible && layout.IsInspectorVisible)
        {
            inspectorMaximum = Math.Max(
                SearchWorkspaceLayoutViewModel.InspectorMinimumWidth,
                Math.Min(SearchWorkspaceLayoutViewModel.PanelMaximumWidth, availableForPanels));
        }

        SearchRegionsGrid.ColumnDefinitions[0].MaxWidth = controlsMaximum;
        SearchRegionsGrid.ColumnDefinitions[4].MaxWidth = inspectorMaximum;
    }

    private void OnResultsGridLoadingRow(object? sender, DataGridRowEventArgs eventArgs)
    {
        eventArgs.Row.Classes.Remove(EvenRowClass);
        eventArgs.Row.Classes.Remove(OddRowClass);
        eventArgs.Row.Classes.Add(eventArgs.Row.Index % 2 == 0 ? EvenRowClass : OddRowClass);
    }

    private async void OnSearchFieldKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Enter || DataContext is not SearchWorkspaceViewModel viewModel)
            return;

        eventArgs.Handled = true;
        await viewModel.SearchCommand.ExecuteAsync();
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
            return _collapseDistance >= distanceToMinimum + SearchWorkspaceLayoutViewModel.CollapseThreshold;
        }
    }
}
