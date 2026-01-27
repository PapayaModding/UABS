using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using UABS.Script.View.ViewModels;

namespace UABS.Script.View.Options
{
    public static class SearchDropdownPanels
    {
        public static Border BuildByKeywordsPanel(ToolbarViewModel vm, 
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

            Label searchHintLabel = new() { Content = "Search Keywords, split by comma. \n(E.g. hero,weapon,skill)" };
            searchHintLabel.Classes.Add("smallToolbarLabel");

            TextBox searchHintTextBox = new()
            {
                Watermark = "Enter Text...",
                Margin = new Thickness(5, 0, 5, 0)
            };

            Label excludeHintLabel = new() { Content = "Exclude Keywords, split by comma." };
            excludeHintLabel.Classes.Add("smallToolbarLabel");

            TextBox excludeHintTextBox = new()
            {
                Watermark = "Enter Text...",
                Margin = new Thickness(5, 0, 5, 0)
            };

            Button searchButton = new() { Content = "Search" };
            searchButton.Classes.Add("toolbarButton");

            StackPanel stack = new()
            {
                Children =
                {
                    buildNewPackageButton,
                    thinBlackLine,
                    foundPackagesLabel,
                    foundPackagesScrollView,
                    searchHintLabel,
                    searchHintTextBox,
                    excludeHintLabel,
                    excludeHintTextBox,
                    searchButton
                }
            };

            buildNewPackageButton.HorizontalAlignment = HorizontalAlignment.Center;
            searchButton.HorizontalAlignment = HorizontalAlignment.Center;

            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }

        public static Border BuildByImagePanel(ToolbarViewModel vm, 
                                                Func<Border> makeDropdownPanel,
                                                int nested_dropdown_panel_width)
        {
            Label testButton2 = new() { Content = "Unavailable." };
            testButton2.Classes.Add("smallToolbarLabel");

            StackPanel stack = new()
            {
                Children = { testButton2 }
            };

            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }

        public static Border BuildByMemoPanel(ToolbarViewModel vm, 
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

            Label searchHintLabel = new() { Content = "Search Memos, split by comma. \n(E.g. hero,weapon,skill)" };
            searchHintLabel.Classes.Add("smallToolbarLabel");

            TextBox searchHintTextBox = new()
            {
                Watermark = "Enter Text...",
                Margin = new Thickness(5, 0, 5, 0)
            };

            Label excludeHintLabel = new() { Content = "Exclude Memos, split by comma." };
            excludeHintLabel.Classes.Add("smallToolbarLabel");

            TextBox excludeHintTextBox = new()
            {
                Watermark = "Enter Text...",
                Margin = new Thickness(5, 0, 5, 0)
            };

            Button searchButton = new() { Content = "Search" };
            searchButton.Classes.Add("toolbarButton");

            StackPanel stack = new()
            {
                Children =
                {
                    buildNewPackageButton,
                    thinBlackLine,
                    foundPackagesLabel,
                    foundPackagesScrollView,
                    searchHintLabel,
                    searchHintTextBox,
                    excludeHintLabel,
                    excludeHintTextBox,
                    searchButton
                }
            };

            buildNewPackageButton.HorizontalAlignment = HorizontalAlignment.Center;
            searchButton.HorizontalAlignment = HorizontalAlignment.Center;

            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }
    }
}
