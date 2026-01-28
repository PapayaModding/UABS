using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace UABS.AvaloniaUI
{
    public class PackageDropdownPanels
    {
        public static Border BuildNewPackagePanel(ToolbarViewModel vm, 
                                                    Func<Border> makeDropdownPanel,
                                                    int nested_dropdown_panel_width)
        {
            Button buildNewPackageButton = new() { Content = "Build New Package" };
            buildNewPackageButton.Classes.Add("toolbarButton");

            Border thinBlackLine = new()
            {
                Height = 1,
                Background = Brushes.Black
            };

            Label foundPackagesLabel = new() { Content = "Found Packages (View Only)" };
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

        public static Border BuildRemovePackagePanel(ToolbarViewModel vm, 
                                                    Func<Border> makeDropdownPanel,
                                                    int nested_dropdown_panel_width)
        {
            Label removePackageLabel = new() { Content = "Found Packages" };
            removePackageLabel.Classes.Add("smallToolbarLabel");

            StackPanel removePackageScrollViewContainer = new();
            for (int i = 0; i < 10; i++)
                removePackageScrollViewContainer.Children.Add(new TextBlock { Text = $"{i}" });

            ScrollViewer removePackageScrollView = new()
            {
                Content = removePackageScrollViewContainer,
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
                    removePackageLabel,
                    removePackageScrollView
                }
            };

            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }
    }
}