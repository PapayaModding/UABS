using System;
using Avalonia.Controls;

namespace UABS.ViewModels;

public sealed class SearchWorkspaceLayoutViewModel : ViewModelBase
{
    public const double ControlsMinimumWidth = 240;
    public const double InspectorMinimumWidth = 260;
    public const double PanelMaximumWidth = 640;
    public const double SplitterWidth = 5;
    public const double CollapseThreshold = 36;

    private double _controlsWidth = 290;
    private double _inspectorWidth = 310;
    private bool _areControlsVisible = true;
    private bool _isInspectorVisible = true;

    public SearchWorkspaceLayoutViewModel()
    {
        ToggleControlsCommand = new RelayCommand(() => AreControlsVisible = !AreControlsVisible);
        ToggleInspectorCommand = new RelayCommand(() => IsInspectorVisible = !IsInspectorVisible);
    }

    public double ControlsWidth
    {
        get => _controlsWidth;
        set
        {
            if (SetField(ref _controlsWidth, ClampWidth(value, ControlsMinimumWidth)))
                OnPropertyChanged(nameof(ControlsColumnWidth));
        }
    }

    public double InspectorWidth
    {
        get => _inspectorWidth;
        set
        {
            if (SetField(ref _inspectorWidth, ClampWidth(value, InspectorMinimumWidth)))
                OnPropertyChanged(nameof(InspectorColumnWidth));
        }
    }

    public bool AreControlsVisible
    {
        get => _areControlsVisible;
        private set
        {
            if (!SetField(ref _areControlsVisible, value))
                return;

            OnPropertyChanged(nameof(ControlsColumnWidth));
            OnPropertyChanged(nameof(ControlsColumnMinimumWidth));
            OnPropertyChanged(nameof(ControlsSplitterColumnWidth));
            OnPropertyChanged(nameof(IsControlsRestoreHandleVisible));
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

    public GridLength ControlsColumnWidth => new(AreControlsVisible ? ControlsWidth : 0);
    public GridLength InspectorColumnWidth => new(IsInspectorVisible ? InspectorWidth : 0);
    public GridLength ControlsSplitterColumnWidth => new(AreControlsVisible ? SplitterWidth : 0);
    public GridLength InspectorSplitterColumnWidth => new(IsInspectorVisible ? SplitterWidth : 0);
    public double ControlsColumnMinimumWidth => AreControlsVisible ? ControlsMinimumWidth : 0;
    public double InspectorColumnMinimumWidth => IsInspectorVisible ? InspectorMinimumWidth : 0;
    public bool IsControlsRestoreHandleVisible => !AreControlsVisible;
    public bool IsInspectorRestoreHandleVisible => !IsInspectorVisible;

    public RelayCommand ToggleControlsCommand { get; }
    public RelayCommand ToggleInspectorCommand { get; }

    private static double ClampWidth(double value, double minimum)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return minimum;

        return Math.Clamp(value, minimum, PanelMaximumWidth);
    }
}
