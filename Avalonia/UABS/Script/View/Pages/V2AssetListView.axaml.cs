using Avalonia.Controls;
namespace UABS.Page;
public partial class V2AssetListView : UserControl
{
    public V2AssetListView() => InitializeComponent();
    private void OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Classes.Remove("assetRowEven"); e.Row.Classes.Remove("assetRowOdd");
        e.Row.Classes.Add(e.Row.Index % 2 == 0 ? "assetRowEven" : "assetRowOdd");
    }
}
