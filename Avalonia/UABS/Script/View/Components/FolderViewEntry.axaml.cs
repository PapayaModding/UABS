using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace UABS.Component;

public partial class FolderViewEntry : UserControl
{
    public static readonly StyledProperty<Bitmap?> IconProperty =
        AvaloniaProperty.Register<FolderViewEntry, Bitmap?>(nameof(Icon));

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<FolderViewEntry, string>(nameof(Text));

    public Bitmap? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public FolderViewEntry()
    {
        InitializeComponent();
        DataContext = this;

        // Example click handler for the whole entry
        this.PointerPressed += (s, e) => 
        {
            // Handle click here or raise your own event
        };
    }
}