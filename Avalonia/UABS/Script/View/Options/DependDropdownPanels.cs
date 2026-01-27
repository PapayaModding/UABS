using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using UABS.Script.View.ViewModels;

namespace UABS.Script.View.Options
{
    public static class DependDropdownPanels
    {
        public static Border BuildDependenciesPanel(ToolbarViewModel vm,
                                                    Func<Border> makeDropdownPanel,
                                                    int nested_dropdown_panel_width)
        {
            Button buildNewPackageButton = new() { Content = "BuildNewPackage" };
            buildNewPackageButton.Classes.Add("toolbarButton");

            Border thinBlackLine = new()
            {
                Height = 1,
                Background = Brushes.Black
            };

            Label foundPackagesLabel = new() { Content = "Found Packages" };
            foundPackagesLabel.Classes.Add("smallToolbarLabel");

            StackPanel foundPackagesScrollViewContainer = new();
            for (int i = 0; i < 10; i++)
                foundPackagesScrollViewContainer.Children.Add(new TextBlock { Text = $"{i}" });

            ScrollViewer foundPackagesScrollView = new()
            {
                Content = foundPackagesScrollViewContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Height = 100,
                Background = Brushes.LightGray,
                Margin = new Thickness(10, 0, 10, 10)
            };

            StackPanel stack = new()
            {
                Children =
                {
                    buildNewPackageButton,
                    thinBlackLine,
                    foundPackagesLabel,
                    foundPackagesScrollView
                }
            };

            buildNewPackageButton.HorizontalAlignment = HorizontalAlignment.Center;
            
            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }

        public static Border BuildDependentsPanel(ToolbarViewModel vm,
                                                    Func<Border> makeDropdownPanel,
                                                    int nested_dropdown_panel_width)
        {
            Button buildNewPackageButton = new() { Content = "BuildNewPackage" };
            buildNewPackageButton.Classes.Add("toolbarButton");

            Border thinBlackLine = new()
            {
                Height = 1,
                Background = Brushes.Black
            };

            Label foundPackagesLabel = new() { Content = "Found Packages" };
            foundPackagesLabel.Classes.Add("smallToolbarLabel");

            StackPanel foundPackagesScrollViewContainer = new();
            for (int i = 0; i < 10; i++)
                foundPackagesScrollViewContainer.Children.Add(new TextBlock { Text = $"{i}" });

            ScrollViewer foundPackagesScrollView = new()
            {
                Content = foundPackagesScrollViewContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Height = 100,
                Background = Brushes.LightGray,
                Margin = new Thickness(10, 0, 10, 10)
            };

            StackPanel stack = new()
            {
                Children =
                {
                    buildNewPackageButton,
                    thinBlackLine,
                    foundPackagesLabel,
                    foundPackagesScrollView
                }
            };

            buildNewPackageButton.HorizontalAlignment = HorizontalAlignment.Center;
            
            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }
    }
}