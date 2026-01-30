using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using UABS.ViewModel;

namespace UABS.AvaloniaUI
{
    public static class SearchDropdownPanels
    {
        public static Border BuildByKeywordsPanel(ToolbarViewModel vm, 
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
            Button buildNewPackageButton = new() { Content = "Build New Package" };
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

            Border imageContainer = new()
            {
                Width = 128,
                Height = 128,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent
            };
            Grid imageLayerGrid = new();
            Image backgroundImage = new()
            {
                Stretch = Stretch.Fill
            };
            var checker = new Bitmap(
                AssetLoader.Open(new Uri("avares://UABS/Resources/Images/transparent_bg_8x8.png"))
            );
            backgroundImage.Source = checker;
            Image userImage = new()
            {
                Stretch = Stretch.Uniform,
                IsVisible = false // hidden until user adds an image
            };
            imageLayerGrid.Children.Add(backgroundImage);
            imageLayerGrid.Children.Add(userImage);
            imageContainer.Child = imageLayerGrid;

            Label searchHintLabel = new() { Content = "Pick sprite/texture2d file. \n(Support .png, .jpeg)" };
            searchHintLabel.Classes.Add("smallToolbarLabel");

            Grid inputGrid = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            TextBox filePathTextBox = new()
            {
                Watermark = "File Path",
                Padding = new Thickness(6, 6),
                Margin = new Thickness(5, 0, 5, 0),
                Width = 150,
                Height = 12
            };
            Button filePathPickButton = new() { Content = "Pick" };
            filePathPickButton.Classes.Add("smallToolbarButton");
            filePathPickButton.Margin = new Thickness(0, 0, 15, 0);
            Grid.SetColumn(filePathTextBox, 0);
            Grid.SetColumn(filePathPickButton, 1);
            inputGrid.Children.Add(filePathTextBox);
            inputGrid.Children.Add(filePathPickButton);

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
                    imageContainer,
                    searchHintLabel,
                    inputGrid,
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

        public static Border BuildByMemoPanel(ToolbarViewModel vm, 
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
