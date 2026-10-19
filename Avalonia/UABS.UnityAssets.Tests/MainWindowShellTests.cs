using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class MainWindowShellTests
{
    [AvaloniaFact]
    public void WorkspaceTabsRemainTheCompactNavigationAfterToolbarRemoval()
    {
        MainWindow window = new();
        window.Show();

        try
        {
            TabControl tabs = Assert.IsType<TabControl>(window.Content);
            Assert.Equal("WorkspaceTabs", tabs.Name);
            Assert.Equal(
                ["Assets", "Search", "Packages"],
                tabs.Items.OfType<TabItem>().Select(item => item.Header?.ToString()));

            MainWindowViewModel viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
            viewModel.ShowSearchCommand.Execute(null);
            Assert.Equal(1, tabs.SelectedIndex);
            viewModel.ShowPackagesCommand.Execute(null);
            Assert.Equal(2, tabs.SelectedIndex);
            viewModel.ShowAssetsCommand.Execute(null);
            Assert.Equal(0, tabs.SelectedIndex);
        }
        finally
        {
            window.Close();
        }
    }
}
