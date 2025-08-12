using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UABS;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MainContent.Content = new TextBlock { Text = "Welcome!" };
    }
}