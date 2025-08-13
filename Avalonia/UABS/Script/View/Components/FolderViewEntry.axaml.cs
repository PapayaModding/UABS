using System.ComponentModel;
using AssetsTools.NET.Extra;

namespace UABS.Component;

public class FolderViewEntry : INotifyPropertyChanged
{
    private string name = string.Empty;
    private AssetClassID typeClass;
    private string type = string.Empty;

    public required string Name 
    {
        get => name;
        set
        {
            if (name != value)
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }

    public AssetClassID TypeClass
    {
        get => typeClass;
        set
        {
            if (typeClass != value)
            {
                typeClass = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeClass)));
            }
        }
    }

    public required string Type
    {
        get => type;
        set
        {
            if (type != value)
            {
                type = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
