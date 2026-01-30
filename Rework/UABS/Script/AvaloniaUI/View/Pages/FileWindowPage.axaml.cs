using System.Linq;
using Avalonia.Controls;
using UABS.Data;
using UABS.Util;
using UABS.ViewModel;

namespace UABS.AvaloniaUI
{
    public partial class FileWindowPage : UserControl
    {
        public FileWindowPage()
        {
            InitializeComponent();

            this.AttachedToVisualTree += (_, __) =>
            {
                if (this.Parent?.DataContext is MainViewModel mainVm)
                {
                    this.DataContext = mainVm.FileWindowVM;
                }
                else
                {
                    Log.Error("Folder Window must be placed inside a MainWindow with MainViewModel as DataContext.");
                }
            };
        }
    }
}