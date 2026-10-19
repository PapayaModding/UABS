using System;
using Avalonia.Controls;

namespace UABS.ViewModels;

public sealed class PackagesWorkspaceLayoutViewModel : ViewModelBase
{
    public const double SidebarMinimumWidth = 260;
    public const double SidebarMaximumWidth = 640;
    public const double SplitterWidth = 5;
    public const double CollapseThreshold = 36;

    private double _sidebarWidth = 330;
    private bool _isSidebarVisible = true;

    public PackagesWorkspaceLayoutViewModel()
    {
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarVisible = !IsSidebarVisible);
    }

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set
        {
            double clamped = double.IsNaN(value) || double.IsInfinity(value)
                ? SidebarMinimumWidth
                : Math.Clamp(value, SidebarMinimumWidth, SidebarMaximumWidth);
            if (SetField(ref _sidebarWidth, clamped))
                OnPropertyChanged(nameof(SidebarColumnWidth));
        }
    }

    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        private set
        {
            if (!SetField(ref _isSidebarVisible, value))
                return;

            OnPropertyChanged(nameof(SidebarColumnWidth));
            OnPropertyChanged(nameof(SidebarColumnMinimumWidth));
            OnPropertyChanged(nameof(SplitterColumnWidth));
            OnPropertyChanged(nameof(IsRestoreHandleVisible));
        }
    }

    public GridLength SidebarColumnWidth => new(IsSidebarVisible ? SidebarWidth : 0);
    public GridLength SplitterColumnWidth => new(IsSidebarVisible ? SplitterWidth : 0);
    public double SidebarColumnMinimumWidth => IsSidebarVisible ? SidebarMinimumWidth : 0;
    public bool IsRestoreHandleVisible => !IsSidebarVisible;

    public RelayCommand ToggleSidebarCommand { get; }
}
