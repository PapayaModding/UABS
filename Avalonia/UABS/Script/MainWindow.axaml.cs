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

    private void New_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new TextBlock { Text = "New File Page" };
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new TextBlock { Text = "Open File Page" };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new TextBlock { Text = "Saving..." };
    }
}