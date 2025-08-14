using System.Collections.ObjectModel;
using Avalonia.Controls;
using UABS.Component;

namespace UABS.Page;

public partial class FolderView : UserControl
{
    public ObservableCollection<FolderViewEntry> ItemsSource { get; } = [];

    public FolderView()
    {
        InitializeComponent();

        ItemsSource.Add(new()
        {
            Name = "Player.prefab",
            Type = FolderViewType.Folder,
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            Type = FolderViewType.Folder,
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            Type = FolderViewType.File,
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            Type = FolderViewType.File,
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            Type = FolderViewType.File,
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            Type = FolderViewType.File,
        });
        
        DataContext = this;
    }
}
