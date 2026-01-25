using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Media;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

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

            Border FileDropdown = new()
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15),
                Width = 180,
                IsVisible = false,
                Child = new StackPanel
                {
                    Children =
                    {
                        new Button { Content = "Open File", Classes = { "toolbarButton" } },
                        new Button { Content = "Open Folder", Classes = { "toolbarButton" } }
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