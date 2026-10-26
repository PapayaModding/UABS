using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using UABS.Page;
using UABS.ViewModels;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

public sealed class SearchCriteriaAutomationContractTests
{
    private static IEnumerable<AutomationPeer> Descendants(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren() ?? [])
        { yield return child; foreach (var descendant in Descendants(child)) yield return descendant; }
    }

    [AvaloniaFact]
    public void EmptyCriteriaExposeUniqueSemanticWatermarksAndStandardValueProviders()
    {
        using var packages = V2SearchIntegrationTests.Packages();
        using var search = new SearchWorkspaceViewModel(packages, new PackageSearchService(), useBundleProjection: true);
        var view = new SearchWorkspaceView { DataContext = search };
        var tabs = new TabControl { Items = { new TabItem { Header = "Search", Content = view }, new TabItem { Header = "Other", Content = new TextBlock { Text = "Other" } } } };
        var window = new Window { Content = tabs, Width = 1300, Height = 700 }; window.Show();
        try
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            var root = ControlAutomationPeer.CreatePeerForElement(view)!;
            string[] watermarks = ["Include terms (comma = OR)", "Exclude terms", "Exact Path ID (optional)",
                "V2 Class ID (asset-only, optional)", "V2 exact type name (asset-only, optional)"];
            foreach (string watermark in watermarks)
            {
                var edit = Assert.Single(Descendants(root), peer => peer.GetAutomationControlType() == AutomationControlType.Edit
                    && Descendants(peer).Any(child => child.GetAutomationId() == "PART_Watermark" && child.GetName() == watermark));
                var value = edit.GetProvider<IValueProvider>(); Assert.NotNull(value); Assert.False(value.IsReadOnly);
                Assert.True(string.IsNullOrEmpty(value.Value)); value.SetValue("retained テスト " + watermark);
                Assert.Equal("retained テスト " + watermark, value.Value);
                tabs.SelectedIndex = 1; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
                tabs.SelectedIndex = 0; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
                Assert.Contains(edit, Descendants(root));
                Assert.Equal("retained テスト " + watermark, edit.GetProvider<IValueProvider>()!.Value);
            }
        }
        finally { window.Close(); }
    }
}
