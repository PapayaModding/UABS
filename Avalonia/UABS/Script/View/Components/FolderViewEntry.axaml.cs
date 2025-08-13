using System;
using System.ComponentModel;
using AssetsTools.NET.Extra;

namespace UABS.Component;

public class FolderViewEntry : INotifyPropertyChanged
{
    public required string Name { get; set; }
    public AssetClassID TypeClass { get; set; }
    public required string Type { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update(string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}