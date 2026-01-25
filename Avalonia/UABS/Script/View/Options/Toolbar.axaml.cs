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

    public Toolbar()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, __) =>
        {
            // Get reference to MainWindow
            if (this.GetVisualRoot() is not Window window)
            {
                Log.Error("The control isn't ready yet.", 
                            file: "Toolbar.axaml.cs");
                return;
            }

            // Find the one-and-only overlay canvas in MainWindow
            var dropdownLayer = window.Find<Canvas>("DropdownLayer");
            if (dropdownLayer == null)
            {
                Log.Error("DropdownLayer canvas not found in MainWindow. Cannot attach dropdowns.", 
                            file: "Toolbar.axaml.cs");
                return;
            }

            // * Add styles here
            StyleInclude style1 = new(new Uri("avares://UABS")) 
            {
                Source = new Uri("avares://UABS/Script/Styles/ToolbarStyle.axaml")
            };
            dropdownLayer.Styles.Add(style1);

            PutFileDropdownPanel(dropdownLayer, trigger: FileButton);
        };
    }

    private static void PutFileDropdownPanel(Canvas dropdownLayer, Control trigger)
    {
        Border fileDropdown = MakeDropdownPanel();

        var openFileButton = new Button { Content = "Open File" };
        openFileButton.Classes.Add("toolbarButton");
        var openFolderButton = new Button { Content = "Open Folder" };
        openFolderButton.Classes.Add("toolbarButton");

        var stack = new StackPanel
        {
            Children =
            {
                openFileButton,
                openFolderButton
            }
        };

        fileDropdown.Child = stack;

        AttachDropdown(fileDropdown, trigger, dropdownLayer, layer =>
            ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
    }

#region Component

    private static Point? ShowDropdownBelowTrigger(Control trigger, Canvas dropdownLayer)
    {
        return trigger.TranslatePoint(new Point(0, trigger.Bounds.Height), dropdownLayer);
    }

    private static Point? ShowDropdownRightOfParent(Control trigger, Canvas dropdownLayer)
    {
        return trigger.TranslatePoint(new Point(trigger.Bounds.Width, 0), dropdownLayer);
    }

    private static void AttachDropdown(
                                Border dropdown, 
                                Control trigger, 
                                Canvas layer, 
                                Func<Canvas, Point> getPosition)
    {
        CancellationTokenSource? hideCts = null;

        void ScheduleHide()
        {
            hideCts?.Cancel();
            hideCts = new CancellationTokenSource();
            var token = hideCts.Token;

            _ = Task.Delay(150).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested &&
                    !trigger.IsPointerOver &&
                    !dropdown.IsPointerOver)
                {
                    Dispatcher.UIThread.Post(() => dropdown.IsVisible = false);
                }
            });
        }

        void CancelHide() => hideCts?.Cancel();

        trigger.PointerEntered += (_, __) =>
        {
            var pos = getPosition(layer);
            Canvas.SetLeft(dropdown, pos.X);
            Canvas.SetTop(dropdown, pos.Y);
            dropdown.IsVisible = true;
        };
        trigger.PointerExited += (_, __) => ScheduleHide();
        dropdown.PointerEntered += (_, __) => CancelHide();
        dropdown.PointerExited += (_, __) => ScheduleHide();
        layer.Children.Add(dropdown);
    }

    private static Border MakeDropdownPanel(int width=140)
    {
        return new() {
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            Width = width,
            IsVisible = false,
        }; 
    }

#endregion
}
