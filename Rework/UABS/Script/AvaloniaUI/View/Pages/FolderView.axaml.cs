using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using UABS.Data;

namespace UABS.AvaloniaUI
{
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
}