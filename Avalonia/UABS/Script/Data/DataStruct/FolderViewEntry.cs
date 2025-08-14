using System.ComponentModel;

namespace UABS.Component;

public class FolderViewEntry : INotifyPropertyChanged
{
    private string name = string.Empty;
    private FolderViewType type;

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

    public required FolderViewType Type
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