using Avalonia.Controls;
using UABS.Util;
using UABS.ViewModel;

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
                    this.DataContext = mainVm.FolderWindowVM;
                }
                else
                {
                    Log.Error("Folder Window must be placed inside a MainWindow with MainViewModel as DataContext.");
                }
            };
        }
    }
}