using System;
using Avalonia.Controls;

namespace UABS.ViewModels;

public sealed class AssetsWorkspaceLayoutViewModel : ViewModelBase
{
    public const double NavigatorMinimumWidth = 220;
    public const double InspectorMinimumWidth = 260;
    public const double PanelMaximumWidth = 640;
    public const double SplitterWidth = 5;
    public const double CollapseThreshold = 36;

    private double _navigatorWidth = 340;
    private double _inspectorWidth = 320;
    private bool _isNavigatorVisible = true;
    private bool _isInspectorVisible = true;

    public AssetsWorkspaceLayoutViewModel()
    {
        ToggleNavigatorCommand = new RelayCommand(ToggleNavigator);
        ToggleInspectorCommand = new RelayCommand(ToggleInspector);
    }

    public double NavigatorWidth
    {
        get => _navigatorWidth;
        set
        {
            double width = ClampWidth(value, NavigatorMinimumWidth);
            if (SetField(ref _navigatorWidth, width))
                OnPropertyChanged(nameof(NavigatorColumnWidth));
        }
    }

    public double InspectorWidth
    {
        get => _inspectorWidth;
        set
        {
            double width = ClampWidth(value, InspectorMinimumWidth);
            if (SetField(ref _inspectorWidth, width))
                OnPropertyChanged(nameof(InspectorColumnWidth));
        }
    }

    public bool IsNavigatorVisible
    {
        get => _isNavigatorVisible;
        private set
        {
            if (!SetField(ref _isNavigatorVisible, value))
                return;

            OnPropertyChanged(nameof(NavigatorColumnWidth));
            OnPropertyChanged(nameof(NavigatorColumnMinimumWidth));
            OnPropertyChanged(nameof(NavigatorSplitterColumnWidth));
            OnPropertyChanged(nameof(IsNavigatorRestoreHandleVisible));
        }
    }

    public bool IsInspectorVisible
    {
        get => _isInspectorVisible;
        private set
        {
            if (!SetField(ref _isInspectorVisible, value))
                return;

            OnPropertyChanged(nameof(InspectorColumnWidth));
            OnPropertyChanged(nameof(InspectorColumnMinimumWidth));
            OnPropertyChanged(nameof(InspectorSplitterColumnWidth));
            OnPropertyChanged(nameof(IsInspectorRestoreHandleVisible));
        }
    }

    public GridLength NavigatorColumnWidth => new(IsNavigatorVisible ? NavigatorWidth : 0);
    public GridLength InspectorColumnWidth => new(IsInspectorVisible ? InspectorWidth : 0);
    public GridLength NavigatorSplitterColumnWidth => new(IsNavigatorVisible ? SplitterWidth : 0);
    public GridLength InspectorSplitterColumnWidth => new(IsInspectorVisible ? SplitterWidth : 0);
    public double NavigatorColumnMinimumWidth => IsNavigatorVisible ? NavigatorMinimumWidth : 0;
    public double InspectorColumnMinimumWidth => IsInspectorVisible ? InspectorMinimumWidth : 0;
    public bool IsNavigatorRestoreHandleVisible => !IsNavigatorVisible;
    public bool IsInspectorRestoreHandleVisible => !IsInspectorVisible;

    public RelayCommand ToggleNavigatorCommand { get; }
    public RelayCommand ToggleInspectorCommand { get; }

    private void ToggleNavigator() => IsNavigatorVisible = !IsNavigatorVisible;

    private void ToggleInspector() => IsInspectorVisible = !IsInspectorVisible;

    private static double ClampWidth(double value, double minimum)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return minimum;

        return Math.Clamp(value, minimum, PanelMaximumWidth);
    }
}
