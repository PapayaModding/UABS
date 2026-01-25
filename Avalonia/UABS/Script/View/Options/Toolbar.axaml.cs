using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace UABS.Options;

public partial class Toolbar : UserControl
{
    private CancellationTokenSource? _hideCts;

    public Toolbar()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, __) =>
        {
            // Get reference to MainWindow
            var window = this.GetVisualRoot() as Window;
            if (window == null)
                return;

            // Find the overlay canvas in MainWindow
            var dropdownLayer = window.Find<Canvas>("DropdownLayer");
            if (dropdownLayer == null)
                return;
            
            var styleInclude = new StyleInclude(new Uri("avares://UABS")) 
            {
                Source = new Uri("avares://UABS/Script/Styles/ToolbarStyle.axaml")
            };
            dropdownLayer.Styles.Add(styleInclude);

            var openFileButton = new Button { Content = "Open File" };
            openFileButton.Classes.Add("toolbarButton");
            var openFolderButton = new Button { Content = "Open Folder" };
            openFolderButton.Classes.Add("toolbarButton");

            Border FileDropdown = new()
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(2),
                Width = 140,
                IsVisible = false,
                Child = new StackPanel
                {
                    Children =
                    {
                        openFileButton,
                        openFolderButton
                    }
                }
            };

            dropdownLayer.Children.Add(FileDropdown);

            FileButton.PointerEntered += (_, __) =>
            {
                ShowDropdown(dropdownLayer, FileDropdown);
            };

            FileButton.PointerExited += (_, __) =>
            {
                ScheduleHideDropdown(FileDropdown);
            };

            FileDropdown.PointerEntered += (_, __) => 
            {
                CancelHideDropdown();
            };

            FileDropdown.PointerExited += (_, __) => 
            {
                ScheduleHideDropdown(FileDropdown);
            };
        };
    }

    private void ShowDropdown(Canvas dropdownLayer, Border fileDropdown)
    {
        var pos = FileButton.TranslatePoint(new Point(0, FileButton.Bounds.Height), dropdownLayer);
        if (pos.HasValue)
        {
            Canvas.SetLeft(fileDropdown, pos.Value.X);
            Canvas.SetTop(fileDropdown, pos.Value.Y);
        }
        fileDropdown.IsVisible = true;
    }

    private void ScheduleHideDropdown(Border fileDropdown)
    {
        _hideCts?.Cancel();
        _hideCts = new CancellationTokenSource();
        var token = _hideCts.Token;

        // Delay 150ms before hiding
        _ = Task.Delay(150).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested && !FileButton.IsPointerOver && !fileDropdown.IsPointerOver)
            {
                Dispatcher.UIThread.Post(() => fileDropdown.IsVisible = false);
            }
        });
    }

    private void CancelHideDropdown()
    {
        _hideCts?.Cancel();
    }
}