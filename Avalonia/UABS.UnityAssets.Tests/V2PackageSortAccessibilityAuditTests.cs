using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UABS.Page;

namespace UABS.UnityAssets.Tests;

public sealed partial class V2BundleSearchWorkspaceTests
{
    [AvaloniaFact]
    public async Task PackageHeaderKeepsPointerRoute_AndPackageButtonExposesSupportedInvoke()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 3);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            var grid = view.FindControl<DataGrid>("ResultsGrid")!;
            var column = Assert.Single(grid.Columns, item => Equals(item.Header, "Package"));
            Assert.True(column.IsVisible); Assert.True(column.CanUserSort); Assert.Equal("PackageName", column.SortMemberPath);
            var header = Assert.Single(grid.GetVisualDescendants().OfType<DataGridColumnHeader>(), item => Equals(item.Content, "Package"));
            var peer = ControlAutomationPeer.CreatePeerForElement(header)!;
            Assert.Equal(AutomationControlType.HeaderItem, peer.GetAutomationControlType());
            Assert.Null(peer.GetProvider<IInvokeProvider>());
            Assert.Null(peer.GetProvider<ISelectionItemProvider>());
            Assert.Contains("Sort all matching V2 results;", peer.GetHelpText()); // State/help is not an action.
            var nameButton = view.FindControl<Button>("SearchNameSortButton")!;
            var namePeer = ControlAutomationPeer.CreatePeerForElement(nameButton)!;
            Assert.Equal("Sort Name", namePeer.GetName()); Assert.NotNull(namePeer.GetProvider<IInvokeProvider>());
            var packageButton = view.FindControl<Button>("SearchPackageSortButton")!;
            var packagePeer = ControlAutomationPeer.CreatePeerForElement(packageButton)!;
            Assert.Equal("Sort Package", packagePeer.GetName()); Assert.NotNull(packagePeer.GetProvider<IInvokeProvider>());
            Assert.True(packageButton.IsVisible); Assert.True(packageButton.IsEnabled); Assert.True(packageButton.Focusable);
            Assert.Contains("Current Package order: Ascending. Next Package order: Descending.", packagePeer.GetHelpText());
        }
        finally { window.Close(); }
    }
}
