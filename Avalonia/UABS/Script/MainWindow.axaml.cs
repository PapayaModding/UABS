using Avalonia.Controls;
using UABS.Page;

namespace UABS;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MainContent.Content = new FolderView();
    }
}
