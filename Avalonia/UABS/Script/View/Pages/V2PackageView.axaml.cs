using Avalonia.Controls;
namespace UABS.Page;
public partial class V2PackageView : UserControl
{
    public V2PackageView() => InitializeComponent();
    private void OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Classes.Remove("assetRowEven"); e.Row.Classes.Remove("assetRowOdd");
        e.Row.Classes.Add(e.Row.Index % 2 == 0 ? "assetRowEven" : "assetRowOdd");
    }
}
