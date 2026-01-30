using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using UABS.ViewModel;

namespace UABS.AvaloniaUI
{
    public static class FilterDropdownPanels
    {
        public static Border BuildFilterByTypePanel(ToolbarViewModel vm, 
                                                    Func<Border> makeDropdownPanel,
                                                    int nested_dropdown_panel_width)
        {
            Label filterByTypeLabel = new() { Content = "Filter By Type" };
            filterByTypeLabel.Classes.Add("smallToolbarLabel");

            StackPanel typesScrollViewContainer = new();
            for (int i = 0; i < 10; i++)
                typesScrollViewContainer.Children.Add(new TextBlock { Text = $"{i}" });

            ScrollViewer typesScrollView = new()
            {
                Content = typesScrollViewContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Height = 100,
                Background = Brushes.LightGray,
                Margin = new Thickness(10, 0, 10, 10)
            };

            Button enableAllButton = new() { Content = "Enable All" };
            enableAllButton.Classes.Add("toolbarButton");

            Button disableAllButton = new() { Content = "Disable All" };
            disableAllButton.Classes.Add("toolbarButton");

            StackPanel stack = new()
            {
                Children =
                {
                    filterByTypeLabel,
                    typesScrollView,
                    enableAllButton,
                    disableAllButton
                }
            };

            enableAllButton.HorizontalAlignment = HorizontalAlignment.Center;
            disableAllButton.HorizontalAlignment = HorizontalAlignment.Center;

            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }

        public static Border BuildFilterByNamePanel(ToolbarViewModel vm, 
                                                Func<Border> makeDropdownPanel,
                                                int nested_dropdown_panel_width)
        {
            Label filterByNameLabel = new() { Content = "Filter By Name" };
            filterByNameLabel.Classes.Add("smallToolbarLabel");

            StackPanel stack = new()
            {
                Children =
                {
                    filterByNameLabel
                }
            };

            Border dropdownPanel = makeDropdownPanel();
            dropdownPanel.Width = nested_dropdown_panel_width;
            dropdownPanel.Child = stack;

            return dropdownPanel;
        }
    }
}