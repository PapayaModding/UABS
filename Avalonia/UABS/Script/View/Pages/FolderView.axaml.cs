using System.Collections.Generic;
using System.Collections.ObjectModel;
using AssetsTools.NET.Extra;
using Avalonia.Controls;
using UABS.Component;

namespace UABS.Pages;

public partial class FolderView : UserControl
{
    private readonly ObservableCollection<FolderViewEntry> _dataGridItems = [];
    
    public FolderView()
    {
        InitializeComponent();

        dataGrid.ItemsSource = _dataGridItems;

        _dataGridItems.Add(new()
        {
            Name = "Player.prefab",
            TypeClass = AssetClassID.Sprite,
            Type = "Sprite"
        });

        _dataGridItems.Add(new()
        {
            Name = "MainMenu.unity",
            TypeClass = AssetClassID.Texture2D,
            Type = "Texture2D"
        });
    }
}
