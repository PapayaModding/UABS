using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UABS.Script.View.ViewModels;

namespace UABS.Options;

public partial class Toolbar : UserControl
{
    class DropdownWrapper
    {
        public Border Panel { get; init; } = null!;
        public Control Trigger { get; init; } = null!;
        public DropdownWrapper? Parent { get; init; }

        public CancellationTokenSource? HideCts;

        private int _visibleChildren = 0;
        private DropdownWrapper? _activeChild;

        public bool IsPointerOverSelf()
            => Panel.IsPointerOver || Trigger.IsPointerOver;

        public bool ShouldStayOpen()
            => IsPointerOverSelf() || _visibleChildren > 0;

        public void OnChildShown(DropdownWrapper child)
        {
            if (_activeChild != null && _activeChild != child)
                _activeChild.ForceHide();

            _activeChild = child;
            _visibleChildren = 1;
        }

        public void OnChildHidden(DropdownWrapper child)
        {
            if (_activeChild == child)
            {
                _activeChild = null;
                _visibleChildren = 0;
            }

            if (!ShouldStayOpen())
                Hide();
        }

        public void Show()
        {
            Panel.IsVisible = true;
            Parent?.OnChildShown(this);
        }

        public void Hide()
        {
            Panel.IsVisible = false;
            Parent?.OnChildHidden(this);
        }

        public void ForceHide()
        {
            HideCts?.Cancel();
            Panel.IsVisible = false;
            Parent?.OnChildHidden(this);
        }
    }

    public Toolbar()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, __) =>
        {
            // Get the parent DataContext (MainViewModel)
            if (this.Parent?.DataContext is MainViewModel mainVm)
            {
                this.DataContext = mainVm.Toolbar;
            }
            else
            {
                Log.Error("Toolbar must be placed inside a MainWindow with MainViewModel as DataContext.");
            }

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
            PutExportDropdownPanel(dropdownLayer, trigger: ExportButton);
            PutSearchDropdownPanel(dropdownLayer, trigger: SearchButton);
            PutDependDropdownPanel(dropdownLayer, trigger: DependButton);
            PutFilterDropdownPanel(dropdownLayer, trigger: FilterButton);
            PutMemoDropdownPanel(dropdownLayer, trigger: MemoButton);
            PutPackageDropdownPanel(dropdownLayer, trigger: PackageButton);
        };
    }

    private void PutFileDropdownPanel(Canvas dropdownLayer, Control trigger)
    {
        Border dropdownPanel = MakeDropdownPanel();

        var toolbarVm = this.DataContext as ToolbarViewModel;
        Button openFileButton = new()
        {
            Content = "Open File",
            Classes = { "toolbarButton" },
        };
        openFileButton.Click += (_, __) =>
        {
            if (toolbarVm == null) return;
            var owner = this.VisualRoot as Window;
            toolbarVm.OpenFileCommand.Execute(owner);
        };
        Button openFolderButton = new() 
        { 
            Content = "Open Folder",
            Classes = { "toolbarButton" },
        };
        openFolderButton.Click += (_, __) =>
        {
            if (toolbarVm == null) return;
            var owner = this.VisualRoot as Window;
            toolbarVm.OpenFolderCommand.Execute(owner);
        };

        StackPanel stack = new()
        {
            Children =
            {
                openFileButton,
                openFolderButton
            }
        };

        foreach (Control component in stack.Children)
        {
            component.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        }

        dropdownPanel.Child = stack;

        AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
            ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
    }

    private static void PutExportDropdownPanel(Canvas dropdownLayer, Control trigger)
    {
        Border dropdownPanel = MakeDropdownPanel();

        Button exportAllAssetsButton = new() { Content = "Export All Assets" };
        exportAllAssetsButton.Classes.Add("toolbarButton");
        Button exportSelectedAssetsButton = new() { Content = "Export Selected Assets" };
        exportSelectedAssetsButton.Classes.Add("toolbarButton");
        Button exportFilteredAssetsButton = new() { Content = "Export Filtered Assets" };
        exportFilteredAssetsButton.Classes.Add("toolbarButton");

        StackPanel stack = new()
        {
            Children =
            {
                exportAllAssetsButton,
                exportSelectedAssetsButton,
                exportFilteredAssetsButton
            }
        };

        foreach (Control component in stack.Children)
        {
            component.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        }

        dropdownPanel.Child = stack;

        AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
            ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
    }

    private static void PutSearchDropdownPanel(Canvas dropdownLayer, Control trigger)
    {
        Border dropdownPanel = MakeDropdownPanel();

        Button byKeywordsButton = new() { Content = "By Keywords ▶" };
        byKeywordsButton.Classes.Add("toolbarButton");

        Button testButton = new() { Content = "Test" };
        testButton.Classes.Add("toolbarButton");
        Border byKeywordsDropdownPanel = MakeDropdownPanel();
        StackPanel stack1 = new()
        {
            Children =
            {
                testButton
            }
        };
        byKeywordsDropdownPanel.Child = stack1;

        Button byImageButton = new() { Content = "By Image ▶" };
        byImageButton.Classes.Add("toolbarButton");

        Button testButton2 = new() { Content = "Test" };
        testButton2.Classes.Add("toolbarButton");
        Border byImageDropdownPanel = MakeDropdownPanel();
        StackPanel stack2 = new()
        {
            Children =
            {
                testButton2
            }
        };
        byImageDropdownPanel.Child = stack2;

        Button byMemoButton = new() { Content = "By Memo ▶" };
        byMemoButton.Classes.Add("toolbarButton");

        StackPanel stack = new()
        {
            Children =
            {
                byKeywordsButton,
                byImageButton,
                byMemoButton
            }
        };

        foreach (Control component in stack.Children)
        {
            component.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        }

        dropdownPanel.Child = stack;

        DropdownWrapper parentWrapper = AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
            ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));

        AttachDropdown(byKeywordsDropdownPanel, byKeywordsButton, dropdownLayer, layer =>
            ShowDropdownRightOfParent(byKeywordsButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
        
        AttachDropdown(byImageDropdownPanel, byImageButton, dropdownLayer, layer =>
            ShowDropdownRightOfParent(byImageButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
    }

    private static void PutDependDropdownPanel(Canvas dropdownLayer, Control trigger)
    {
        Border dropdownPanel = MakeDropdownPanel();

        Button findDependenciesButton = new() { Content = "Find Dependencies ▶" };
        findDependenciesButton.Classes.Add("toolbarButton");
        Button findDependentsButton = new() { Content = "Find Dependents ▶" };
        findDependentsButton.Classes.Add("toolbarButton");

        StackPanel stack = new()
        {
            Children =
            {
                findDependenciesButton,
                findDependentsButton
            }
        };

        foreach (Control component in stack.Children)
        {
            component.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        }

        dropdownPanel.Child = stack;

        AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
            ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
    }

    private static void PutFilterDropdownPanel(Canvas dropdownLayer, Control trigger)
    {
        Border dropdownPanel = MakeDropdownPanel();

        Button byTypeButton = new() { Content = "By Type ▶" };
        byTypeButton.Classes.Add("toolbarButton");
        Button byNameButton = new() { Content = "By Name ▶" };
        byNameButton.Classes.Add("toolbarButton");

        StackPanel stack = new()
        {
            Children =
            {
                byTypeButton,
                byNameButton
            }
        };

        foreach (Control component in stack.Children)
        {
            component.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        }

        dropdownPanel.Child = stack;

        AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
            ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
    }

    private static void PutMemoDropdownPanel(Canvas dropdownLayer, Control trigger)
    {
        Border dropdownPanel = MakeDropdownPanel();

        Button selectPackageButton = new() { Content = "Select Package ▶" };
        selectPackageButton.Classes.Add("toolbarButton");
        Button inheritMemoButton = new() { Content = "Inherit Memo ▶" };
        inheritMemoButton.Classes.Add("toolbarButton");

        StackPanel stack = new()
        {
            Children =
            {
                selectPackageButton,
                inheritMemoButton
            }
        };

        foreach (Control component in stack.Children)
        {
            component.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        }

        dropdownPanel.Child = stack;

        AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
            ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
    }

    private static void PutPackageDropdownPanel(Canvas dropdownLayer, Control trigger)
    {
        Border dropdownPanel = MakeDropdownPanel();

        Button buildNewButton = new() { Content = "Build New ▶" };
        buildNewButton.Classes.Add("toolbarButton");
        Button removeButton = new() { Content = "Remove ▶" };
        removeButton.Classes.Add("toolbarButton");

        StackPanel stack = new()
        {
            Children =
            {
                buildNewButton,
                removeButton
            }
        };

        foreach (Control component in stack.Children)
        {
            component.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        }

        dropdownPanel.Child = stack;

        AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
            ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
    }

#region Component

    private static Point? ShowDropdownBelowTrigger(Control trigger, Canvas dropdownLayer)
    {
        return trigger.TranslatePoint(new Point(0, trigger.Bounds.Height), dropdownLayer);
    }

    private static Point? ShowDropdownRightOfParent(Control trigger, Canvas dropdownLayer)
    {
        return trigger.TranslatePoint(new Point(trigger.Bounds.Width + 8, -8), dropdownLayer);
    }

    private static DropdownWrapper AttachDropdown(
        Border dropdown,
        Control trigger,
        Canvas layer,
        Func<Canvas, Point> getPosition,
        DropdownWrapper? parentWrapper = null)
    {
        var wrapper = new DropdownWrapper
        {
            Panel = dropdown,
            Trigger = trigger,
            Parent = parentWrapper
        };

        void ScheduleHide()
        {
            wrapper.HideCts?.Cancel();
            wrapper.HideCts = new CancellationTokenSource();
            var token = wrapper.HideCts.Token;

            _ = Task.Delay(150).ContinueWith(_ =>
            {
                if (token.IsCancellationRequested)
                    return;

                Dispatcher.UIThread.Post(() =>
                {
                    if (!wrapper.ShouldStayOpen())
                        wrapper.Hide();
                });
            });
        }

        void CancelHide() => wrapper.HideCts?.Cancel();

        trigger.PointerEntered += (_, __) =>
        {
            var pos = getPosition(layer);
            Canvas.SetLeft(dropdown, pos.X);
            Canvas.SetTop(dropdown, pos.Y);
            wrapper.Show();
            CancelHide();
        };

        trigger.PointerExited += (_, __) => ScheduleHide();
        dropdown.PointerEntered += (_, __) => CancelHide();
        dropdown.PointerExited += (_, __) => ScheduleHide();

        layer.Children.Add(dropdown);

        return wrapper;
    }

    private static Border MakeDropdownPanel()
    {
        return new() {
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            IsVisible = false,
        }; 
    }

#endregion
}
