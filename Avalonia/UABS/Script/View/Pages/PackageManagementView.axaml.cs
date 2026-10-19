using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using UABS.ViewModels;

namespace UABS.Page;

public partial class PackageManagementView : UserControl
{
    private const string EvenRowClass = "assetRowEven";
    private const string OddRowClass = "assetRowOdd";
    private const double EditorMinimumWidth = 520;
    private readonly SplitterCollapseGesture _sidebarGesture = new();
    private PackageManagementViewModel? _observedViewModel;

    public PackageManagementView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        PackagesRegionsGrid.SizeChanged += (_, _) => UpdateSidebarMaximumWidth();
        ObserveViewModel(DataContext as PackageManagementViewModel);
    }

    private void OnDataContextChanged(object? sender, EventArgs eventArgs) =>
        ObserveViewModel(DataContext as PackageManagementViewModel);

    private void ObserveViewModel(PackageManagementViewModel? viewModel)
    {
        if (_observedViewModel is not null)
            _observedViewModel.Layout.PropertyChanged -= OnLayoutPropertyChanged;

        _observedViewModel = viewModel;

        if (_observedViewModel is not null)
            _observedViewModel.Layout.PropertyChanged += OnLayoutPropertyChanged;

        UpdateSidebarMaximumWidth();
    }

    private void OnLayoutPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs) =>
        UpdateSidebarMaximumWidth();

    private void OnPackagesSidebarSplitterDragStarted(object? sender, VectorEventArgs eventArgs) =>
        _sidebarGesture.Begin(PackagesRegionsGrid.ColumnDefinitions[0].ActualWidth);

    private void OnPackagesSidebarSplitterDragDelta(object? sender, VectorEventArgs eventArgs) =>
        _sidebarGesture.Track(eventArgs.Vector.X);

    private void OnPackagesSidebarSplitterDragCompleted(object? sender, VectorEventArgs eventArgs)
    {
        if (DataContext is not PackageManagementViewModel { Layout.IsSidebarVisible: true } viewModel)
            return;

        if (_sidebarGesture.ShouldCollapse())
            viewModel.Layout.ToggleSidebarCommand.Execute(null);
        else
            viewModel.Layout.SidebarWidth = PackagesRegionsGrid.ColumnDefinitions[0].ActualWidth;

        UpdateSidebarMaximumWidth();
    }

    private void OnPackageGridLoadingRow(object? sender, DataGridRowEventArgs eventArgs)
    {
        eventArgs.Row.Classes.Remove(EvenRowClass);
        eventArgs.Row.Classes.Remove(OddRowClass);
        eventArgs.Row.Classes.Add(eventArgs.Row.Index % 2 == 0 ? EvenRowClass : OddRowClass);
    }

    private void UpdateSidebarMaximumWidth()
    {
        if (_observedViewModel is null || PackagesRegionsGrid.Bounds.Width <= 0)
            return;

        double splitterWidth = _observedViewModel.Layout.IsSidebarVisible
            ? PackagesWorkspaceLayoutViewModel.SplitterWidth
            : 0;
        double available = PackagesRegionsGrid.Bounds.Width - EditorMinimumWidth - splitterWidth;
        PackagesRegionsGrid.ColumnDefinitions[0].MaxWidth = Math.Max(
            PackagesWorkspaceLayoutViewModel.SidebarMinimumWidth,
            Math.Min(PackagesWorkspaceLayoutViewModel.SidebarMaximumWidth, available));
    }

    private sealed class SplitterCollapseGesture
    {
        private double _startingWidth;
        private double _collapseDistance;

        public void Begin(double startingWidth)
        {
            _startingWidth = startingWidth;
            _collapseDistance = 0;
        }

        public void Track(double horizontalDelta) => _collapseDistance -= horizontalDelta;

        public bool ShouldCollapse()
        {
            double distanceToMinimum = Math.Max(
                0,
                _startingWidth - PackagesWorkspaceLayoutViewModel.SidebarMinimumWidth);
            return _collapseDistance >= distanceToMinimum + PackagesWorkspaceLayoutViewModel.CollapseThreshold;
        }
    }
}
