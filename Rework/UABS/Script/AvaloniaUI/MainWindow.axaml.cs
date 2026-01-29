using Avalonia.Controls;
using UABS.Util;

namespace UABS.AvaloniaUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainViewModel mainVM)
            {
                // mainVM.CurrentViewModel = mainVM.FolderWindowVM;
            }
            else
            {
                Log.Error("MainWindow must have MainViewModel as DataContext set in App.axaml.cs");
            }
        }
    }
}
