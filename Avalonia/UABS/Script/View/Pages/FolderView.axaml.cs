using System.Collections.ObjectModel;
using AssetsTools.NET.Extra;
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
            TypeClass = AssetClassID.Sprite,
            Type = "Sprite"
        });

        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            TypeClass = AssetClassID.Texture2D,
            Type = "Texture2D"
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            TypeClass = AssetClassID.Texture2D,
            Type = "Texture2D"
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            TypeClass = AssetClassID.Texture2D,
            Type = "Texture2D"
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            TypeClass = AssetClassID.Texture2D,
            Type = "Texture2D"
        });
        ItemsSource.Add(new()
        {
            Name = "MainMenu.unity",
            TypeClass = AssetClassID.Texture2D,
            Type = "Texture2D"
        });

        DataContext = this;
    }
}
