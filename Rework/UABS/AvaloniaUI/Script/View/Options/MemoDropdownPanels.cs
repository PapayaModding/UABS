using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using UABS.ViewModel;

namespace UABS.AvaloniaUI
{
    public class MemoDropdownPanels
    {
        public static Border BuildSelectPackagePanel(ToolbarViewModel vm, 
                                                    Func<Border> makeDropdownPanel,
                                                    int nested_dropdown_panel_width)
        {
            Label selectPackageHintLabel = new() { Content = "Select Package to enable Memo" };
            selectPackageHintLabel.Classes.Add("smallToolbarLabel");

            StackPanel selectPackageScrollViewContainer = new();
            for (int i = 0; i < 10; i++)
                selectPackageScrollViewContainer.Children.Add(new TextBlock { Text = $"{i}" });

            ScrollViewer selectPackageScrollView = new()
            {
                Content = selectPackageScrollViewContainer,
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
                    selectPackageHintLabel,
                    selectPackageScrollView
                }
            };

            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }

        public static Border BuildInheritMemoPanel(ToolbarViewModel vm, 
                                                    Func<Border> makeDropdownPanel,
                                                    int nested_dropdown_panel_width)
        {
            Label inheritMemoFromHintLabel = new() { Content = "Select a package folder to inherit \nmemo from" };
            inheritMemoFromHintLabel.Classes.Add("smallToolbarLabel");

            Grid inheritMemoFromGrid = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            TextBox inheritMemoFromTextBox = new()
            {
                Watermark = "",
                Padding = new Thickness(6, 6),
                Margin = new Thickness(5, 0, 5, 0),
                Width = 150,
                Height = 12
            };
            Button inheritMemoFromButton = new() { Content = "Pick" };
            inheritMemoFromButton.Classes.Add("smallToolbarButton");
            inheritMemoFromButton.Margin = new Thickness(0, 0, 15, 0);
            Grid.SetColumn(inheritMemoFromTextBox, 0);
            Grid.SetColumn(inheritMemoFromButton, 1);
            inheritMemoFromGrid.Children.Add(inheritMemoFromTextBox);
            inheritMemoFromGrid.Children.Add(inheritMemoFromButton);

            Label inheritMemoToHintLabel = new() { Content = "\t\t\t\t\t\t⬇️\nSelect a package folder to inherit \nmemo to" };
            inheritMemoToHintLabel.Classes.Add("smallToolbarLabel");

            Grid inheritMemoToGrid = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            TextBox inheritMemToTextBox = new()
            {
                Watermark = "",
                Padding = new Thickness(6, 6),
                Margin = new Thickness(5, 0, 5, 0),
                Width = 150,
                Height = 12
            };
            Button inheritMemoToButton = new() { Content = "Pick" };
            inheritMemoToButton.Classes.Add("smallToolbarButton");
            inheritMemoToButton.Margin = new Thickness(0, 0, 15, 0);
            Grid.SetColumn(inheritMemToTextBox, 0);
            Grid.SetColumn(inheritMemoToButton, 1);
            inheritMemoToGrid.Children.Add(inheritMemToTextBox);
            inheritMemoToGrid.Children.Add(inheritMemoToButton);

            Label modeHintLabel = new() { Content = "Mode" };
            inheritMemoFromHintLabel.Classes.Add("smallToolbarLabel");

            StackPanel modesScrollViewContainer = new();
            for (int i = 0; i < 3; i++)
                modesScrollViewContainer.Children.Add(new TextBlock { Text = $"{i}" });

            ScrollViewer modesScrollView = new()
            {
                Content = modesScrollViewContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Height = 100,
                Background = Brushes.LightGray,
                Margin = new Thickness(10, 0, 10, 10)
            };

            Button runButton = new() { Content = "Run" };
            runButton.Classes.Add("toolbarButton");

            StackPanel stack = new()
            {
                Children =
                {
                    inheritMemoFromHintLabel,
                    inheritMemoFromGrid,
                    inheritMemoToHintLabel,
                    inheritMemoToGrid,
                    modeHintLabel,
                    modesScrollView,
                    runButton
                }
            };

            runButton.HorizontalAlignment = HorizontalAlignment.Center;

            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }
    }
}