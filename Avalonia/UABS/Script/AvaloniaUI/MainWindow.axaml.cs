using Avalonia.Controls;

namespace UABS.AvaloniaUI
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
