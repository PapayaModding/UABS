using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    private ScrollBar? _resultsScroll;
    private bool _viewportQueued;
    private bool _restoringWindow;
    private SearchResultItemViewModel? _anchor;
    private double _anchorY;
    private int _anchorVersion;
    public Task ViewportLoadTask { get; private set; } = Task.CompletedTask;

    public SearchWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SearchRegionsGrid.SizeChanged += (_, _) => UpdatePanelMaximumWidths();
        ResultsGrid.TemplateApplied += (_, _) => HookResultsScroll();
        ResultsGrid.SizeChanged += (_, _) => QueueViewport();
        AttachedToVisualTree += (_, _) => { HookResultsScroll(); QueueViewport(); };
        ObserveViewModel(DataContext as SearchWorkspaceViewModel);
    }

    private void OnDataContextChanged(object? sender, EventArgs eventArgs) =>
        ObserveViewModel(DataContext as SearchWorkspaceViewModel);

    private void ObserveViewModel(SearchWorkspaceViewModel? viewModel)
    {
        if (_observedViewModel is not null)
        {
            _observedViewModel.Layout.PropertyChanged -= OnLayoutPropertyChanged;
            _observedViewModel.PropertyChanged -= OnSearchPropertyChanged;
            _observedViewModel.ResultWindowChanging -= CaptureAnchor;
            _observedViewModel.ResultWindowChanged -= RestoreAnchor;
        }

        _observedViewModel = viewModel;
        _anchorVersion++; _restoringWindow = false; _anchor = null;

        if (_observedViewModel is not null)
        {
            _observedViewModel.Layout.PropertyChanged += OnLayoutPropertyChanged;
            _observedViewModel.PropertyChanged += OnSearchPropertyChanged;
            _observedViewModel.ResultWindowChanging += CaptureAnchor;
            _observedViewModel.ResultWindowChanged += RestoreAnchor;
        }

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
        QueueViewport();
    }

    private void OnSearchPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchWorkspaceViewModel.Results))
        {
            // An explicit new session replaces the collection. Its viewport must
            // never receive a queued restoration from the previous session.
            _anchorVersion++; _anchor = null; _restoringWindow = false;
        }
        if (e.PropertyName is nameof(SearchWorkspaceViewModel.IsSearching) or nameof(SearchWorkspaceViewModel.Results)) QueueViewport();
    }
    private void OnResultSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_restoringWindow || _observedViewModel is not { } vm || vm.IsUpdatingResultWindow) return;
        if (ResultsGrid.SelectedItem is SearchResultItemViewModel selected) vm.SelectedResult = selected;
        else if (!vm.IsV2Search || vm.SelectedResult is null || vm.Results.Contains(vm.SelectedResult)) vm.SelectedResult = null;
    }
    private void HookResultsScroll()
    {
        if (_resultsScroll is not null) _resultsScroll.PropertyChanged -= OnResultsScrollChanged;
        _resultsScroll = ResultsGrid.GetVisualDescendants().OfType<ScrollBar>()
            .FirstOrDefault(b => b.Orientation == Avalonia.Layout.Orientation.Vertical);
        if (_resultsScroll is not null) _resultsScroll.PropertyChanged += OnResultsScrollChanged;
    }
    private void OnResultsScrollChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    { if (e.Property == RangeBase.ValueProperty) QueueViewport(); }

    private DataGridRow[] ViewportRows()
    {
        double top = ResultsGrid.GetVisualDescendants().OfType<DataGridColumnHeader>()
            .Select(h => (h.TranslatePoint(default, ResultsGrid)?.Y ?? 0) + h.Bounds.Height).DefaultIfEmpty(0).Max();
        double bottom = ResultsGrid.GetVisualDescendants().OfType<ScrollBar>()
            .Where(b => b.Orientation == Avalonia.Layout.Orientation.Horizontal && b.IsVisible && b.Bounds.Height > 0)
            .Select(b => b.TranslatePoint(default, ResultsGrid)?.Y ?? ResultsGrid.Bounds.Height).DefaultIfEmpty(ResultsGrid.Bounds.Height).Min();
        return ResultsGrid.GetVisualDescendants().OfType<DataGridRow>()
            .Where(r => r.IsVisible && r.Bounds.Height > 0 && r.TranslatePoint(default, ResultsGrid) is { } p
                && p.Y + r.Bounds.Height > top && p.Y < bottom)
            .OrderBy(r => r.Index).ToArray();
    }

    private void QueueViewport()
    {
        if (_viewportQueued || _restoringWindow) return;
        _viewportQueued = true;
        Dispatcher.UIThread.Post(() => {
            _viewportQueued = false;
            if (!_restoringWindow)
            {
                var observed = ObserveViewportAsync();
                // A coalesced no-op must not hide the fetch that is still running.
                if (ViewportLoadTask.IsCompleted || !observed.IsCompleted) ViewportLoadTask = observed;
            }
        }, DispatcherPriority.Background);
    }
    private async Task ObserveViewportAsync()
    {
        try
        {
            if (!IsEffectivelyVisible || _observedViewModel is not { IsV2Search: true } vm) return;
            var rows = ViewportRows();
            if (rows.Length > 0) await vm.ReportViewportAsync(rows[0].Index, rows[^1].Index);
        }
        catch (Exception e) { Printer.PrintException("Search viewport loading failed.", e); }
    }
    private void CaptureAnchor()
    {
        _anchorVersion++;
        _restoringWindow = true;
        var row = ViewportRows().FirstOrDefault(); _anchor = row?.DataContext as SearchResultItemViewModel;
        _anchorY = row?.TranslatePoint(default, ResultsGrid)?.Y ?? 0;
    }
    private void RestoreAnchor()
    {
        int version = _anchorVersion;
        // Count/status/selection labels can change the viewport height after the
        // collection event. Restore only after the entire publication has laid out.
        Dispatcher.UIThread.Post(() => {
            if (version == _anchorVersion) RestoreAnchorAfterLayout();
        }, DispatcherPriority.Loaded);
    }
    private void RestoreAnchorAfterLayout()
    {
        // Restore the same visible immutable row at its previous pixel position;
        // collection indices change but the current viewport does not jump a page.
        ResultsGrid.UpdateLayout();
        if (_observedViewModel?.SelectedResult is { } selected && _observedViewModel.Results.Contains(selected))
        {
            ResultsGrid.SelectedItem = selected;
            ResultsGrid.UpdateLayout(); // drain selection's pending bring-into-view before restoring the viewport anchor
        }
        if (_anchor is not null && _observedViewModel?.Results.Contains(_anchor) == true)
        {
            var row = ViewportRows().FirstOrDefault(r => ReferenceEquals(r.DataContext, _anchor));
            if (row is null)
            {
                ResultsGrid.ScrollIntoView(_anchor, null); ResultsGrid.UpdateLayout();
                row = ViewportRows().FirstOrDefault(r => ReferenceEquals(r.DataContext, _anchor));
            }
            if (row?.TranslatePoint(default, ResultsGrid) is { } p && Math.Abs(p.Y - _anchorY) > 0.01)
            {
                // RangeBase.Value alone moves the thumb, not DataGrid's row offset.
                // Its rows presenter exposes the native DIP-based scroll gesture
                // route; use that route rather than private offsets or drag code.
                var gesture = new ScrollGestureEventArgs(0, new Vector(0, p.Y - _anchorY));
                ResultsGrid.GetVisualDescendants().OfType<DataGridRowsPresenter>().FirstOrDefault()?.RaiseEvent(gesture);
                ResultsGrid.UpdateLayout();
            }
        }
        _anchor = null; _restoringWindow = false;
        QueueViewport();
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
