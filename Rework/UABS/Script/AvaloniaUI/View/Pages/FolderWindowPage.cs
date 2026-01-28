using System.Linq;
using Avalonia.Controls;
using UABS.Data;
using UABS.Util;

namespace UABS.AvaloniaUI
{
    public partial class FolderWindowPage : UserControl
    {
        public FolderWindowPage()
        {
            InitializeComponent();

            this.AttachedToVisualTree += (_, __) =>
            {
                if (this.Parent?.DataContext is MainViewModel mainVm)
                {
                    this.DataContext = mainVm.FolderWindow;
                }
                else
                {
                    Log.Error("Folder Window must be placed inside a MainWindow with MainViewModel as DataContext.");
                }

                dataGrid.SelectionMode = DataGridSelectionMode.Extended;
                dataGrid.SelectionChanged += (s, e) =>
                {
                    if (this.DataContext is FolderWindowViewModel vm)
                    {
                        // Add newly selected items
                        foreach (var item in e.AddedItems.OfType<FolderWindowEntry>())
                            vm.SelectItem(item);

                        // Remove newly deselected items
                        foreach (var item in e.RemovedItems.OfType<FolderWindowEntry>())
                            vm.DeselectItem(item);
                    }
                };
            };
        }
    }
}