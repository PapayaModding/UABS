using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UABS.Util;

namespace UABS.AvaloniaUI
{
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

        private static readonly int NESTED_DROPDOWN_PANEL_WIDTH = 240;

        public Toolbar()
        {
            InitializeComponent();

            this.AttachedToVisualTree += (_, __) =>
            {
                // Get the parent DataContext (MainViewModel)
                if (this.Parent?.DataContext is MainViewModel mainVm)
                {
                    this.DataContext = mainVm.ToolbarVM;
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

    #region Styles
                StyleInclude style1 = new(new Uri("avares://UABS")) 
                {
                    Source = new Uri(Path.Combine("avares://UABS/Script/AvaloniaUI/Styles/ToolbarStyle.axaml"))
                };
                dropdownLayer.Styles.Add(style1);
    #endregion

                PutFileDropdownPanel(dropdownLayer, trigger: FileButton);
                PutExportDropdownPanel(dropdownLayer, trigger: ExportButton);
                PutSearchDropdownPanel(dropdownLayer, trigger: SearchButton);
                PutDependDropdownPanel(dropdownLayer, trigger: DependButton);
                PutFilterDropdownPanel(dropdownLayer, trigger: FilterButton);
                PutMemoDropdownPanel(dropdownLayer, trigger: MemoButton);
                PutPackageDropdownPanel(dropdownLayer, trigger: PackageButton);

                if (this.DataContext is ToolbarViewModel toolbarVm)
                {
                    ExportButton.Bind(Button.IsEnabledProperty, 
                    new Avalonia.Data.Binding
                    {
                        Path = nameof(ToolbarViewModel.CanExport),
                        Source = toolbarVm,
                        Mode = Avalonia.Data.BindingMode.OneWay
                    });

                    DependButton.Bind(Button.IsEnabledProperty, 
                    new Avalonia.Data.Binding
                    {
                        Path = nameof(ToolbarViewModel.CanDepend),
                        Source = toolbarVm,
                        Mode = Avalonia.Data.BindingMode.OneWay
                    });

                    FilterButton.Bind(Button.IsEnabledProperty, 
                    new Avalonia.Data.Binding
                    {
                        Path = nameof(ToolbarViewModel.CanFilter),
                        Source = toolbarVm,
                        Mode = Avalonia.Data.BindingMode.OneWay
                    });

                    BackButton.Bind(Button.IsEnabledProperty, 
                    new Avalonia.Data.Binding
                    {
                        Path = nameof(ToolbarViewModel.CanBack),
                        Source = toolbarVm,
                        Mode = Avalonia.Data.BindingMode.OneWay
                    });
                };
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
                component.HorizontalAlignment = HorizontalAlignment.Stretch;
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
                component.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            dropdownPanel.Child = stack;

            AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
                ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
        }

        private void PutSearchDropdownPanel(Canvas dropdownLayer, Control trigger)
        {
            Button byKeywordsButton = CreateArrowButton("By Keywords");
            Button byImageButton = CreateArrowButton("By Image");
            Button byMemoButton = CreateArrowButton("By Memo");

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
                component.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            Border dropdownPanel = MakeDropdownPanel();
            dropdownPanel.Child = stack;

            DropdownWrapper parentWrapper = AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
                ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));

            if (this.DataContext is ToolbarViewModel toolbarVm)
            {
                AttachDropdown(SearchDropdownPanels.BuildByKeywordsPanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH), 
                    byKeywordsButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(byKeywordsButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
                AttachDropdown(SearchDropdownPanels.BuildByImagePanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH), 
                    byImageButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(byImageButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
                AttachDropdown(SearchDropdownPanels.BuildByMemoPanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH), 
                    byMemoButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(byMemoButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
            }
        }

        private void PutDependDropdownPanel(Canvas dropdownLayer, Control trigger)
        {
            Button findDependenciesButton = CreateArrowButton("Find Dependencies");
            Button findDependentsButton = CreateArrowButton("Find Dependents");

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
                component.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            Border dropdownPanel = MakeDropdownPanel();
            dropdownPanel.Child = stack;

            DropdownWrapper parentWrapper = AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
                ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));

            if (this.DataContext is ToolbarViewModel toolbarVm)
            {
                AttachDropdown(DependDropdownPanels.BuildDependenciesPanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH),
                    findDependenciesButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(findDependenciesButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);

                AttachDropdown(DependDropdownPanels.BuildDependentsPanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH),
                    findDependentsButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(findDependentsButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
            }
        }

        private void PutFilterDropdownPanel(Canvas dropdownLayer, Control trigger)
        {
            Button byTypeButton = CreateArrowButton("By Type");
            Button byNameButton = CreateArrowButton("By Name");

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
                component.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            Border dropdownPanel = MakeDropdownPanel();
            dropdownPanel.Child = stack;

            DropdownWrapper parentWrapper = AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
                ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));

            if (this.DataContext is ToolbarViewModel toolbarVm)
            {
                AttachDropdown(FilterDropdownPanels.BuildFilterByTypePanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH),
                    byTypeButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(byTypeButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);

                AttachDropdown(FilterDropdownPanels.BuildFilterByNamePanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH),
                    byNameButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(byNameButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
            }
        }

        private void PutMemoDropdownPanel(Canvas dropdownLayer, Control trigger)
        {
            Button selectPackageButton = CreateArrowButton("Select Package");
            Button inheritMemoButton = CreateArrowButton("Inherit Memo");

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
                component.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            Border dropdownPanel = MakeDropdownPanel();
            dropdownPanel.Child = stack;

            DropdownWrapper parentWrapper = AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
                ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
            
            if (this.DataContext is ToolbarViewModel toolbarVm)
            {
                AttachDropdown(MemoDropdownPanels.BuildSelectPackagePanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH),
                    selectPackageButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(selectPackageButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
            
                AttachDropdown(MemoDropdownPanels.BuildInheritMemoPanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH),
                    inheritMemoButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(inheritMemoButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
            }
        }

        private void PutPackageDropdownPanel(Canvas dropdownLayer, Control trigger)
        {
            Border dropdownPanel = MakeDropdownPanel();

            Button buildNewButton = CreateArrowButton("Build New");
            Button removeButton = CreateArrowButton("Remove");

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
                component.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            dropdownPanel.Child = stack;

            DropdownWrapper parentWrapper = AttachDropdown(dropdownPanel, trigger, dropdownLayer, layer =>
                ShowDropdownBelowTrigger(trigger, layer) ?? new Point(0, 0));
            
            if (this.DataContext is ToolbarViewModel toolbarVm)
            {
                AttachDropdown(PackageDropdownPanels.BuildNewPackagePanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH),
                    buildNewButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(buildNewButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
            
                AttachDropdown(PackageDropdownPanels.BuildRemovePackagePanel(toolbarVm, MakeDropdownPanel, NESTED_DROPDOWN_PANEL_WIDTH),
                    removeButton, dropdownLayer, layer =>
                    ShowDropdownRightOfParent(removeButton, layer) ?? new Point(0, 0), parentWrapper: parentWrapper);
            }
        }

    #region Component

        private static Button CreateArrowButton(string text)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };

            var mainText = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(mainText, 0);

            var arrow = new TextBlock
            {
                Text = "▶",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5, 0, 0, 0) // 5 pixels of space on the left
            };
            Grid.SetColumn(arrow, 1);

            grid.Children.Add(mainText);
            grid.Children.Add(arrow);

            return new Button
            {
                Classes = { "toolbarButton" },
                Content = grid
            };
        }

        private static Point? ShowDropdownBelowTrigger(Control trigger, Canvas dropdownLayer)
        {
            return trigger.TranslatePoint(new Point(0, trigger.Bounds.Height), dropdownLayer);
        }

        private static Point? ShowDropdownRightOfParent(Control trigger, Canvas dropdownLayer)
        {
            return trigger.TranslatePoint(new Point(trigger.Bounds.Width + 6, -6), dropdownLayer);
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
                if (!trigger.IsEnabled) return; // <-- ignore if disabled

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
                IsVisible = false,
            }; 
        }

    #endregion
    }
}