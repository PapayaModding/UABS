using Avalonia.Controls;
using Avalonia.Interactivity;
using UABS.Pages;

namespace UABS;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MainContent.Content = new FolderView();
    }
}
