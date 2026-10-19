using Avalonia.Controls;

namespace UABS.Page;

public partial class FolderView : UserControl
{
    private const string EvenRowClass = "assetRowEven";
    private const string OddRowClass = "assetRowOdd";

    public FolderView()
    {
        InitializeComponent();
    }

    private void OnAssetGridLoadingRow(object? sender, DataGridRowEventArgs eventArgs)
    {
        eventArgs.Row.Classes.Remove(EvenRowClass);
        eventArgs.Row.Classes.Remove(OddRowClass);
        eventArgs.Row.Classes.Add(eventArgs.Row.Index % 2 == 0 ? EvenRowClass : OddRowClass);
    }
}
