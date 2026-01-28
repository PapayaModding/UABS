using Avalonia.Controls;
using UABS.Script.View.Pages;

namespace UABS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainContent.Content = new FolderView();
        }
    }
}
