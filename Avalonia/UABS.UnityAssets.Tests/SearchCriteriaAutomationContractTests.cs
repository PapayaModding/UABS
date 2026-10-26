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
    public void CriteriaExposeUniqueAccessibleNamesAndIdsWithStableValueProvidersAndBindings()
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
            (string Id, string Name, string Watermark, string Property, string Value)[] fields = [
                (Id: "IncludeSearchTextBox", Name: "Include terms", Watermark: "Include terms (comma = OR)", Property: "IncludeText", Value: "テスト"),
                ("ExcludeSearchTextBox", "Exclude terms", "Exclude terms", "ExcludeText", "excluded 保持"),
                ("PathIdSearchTextBox", "Exact Path ID", "Exact Path ID (optional)", "PathIdText", "-42424241"),
                ("ClassIdSearchTextBox", "V2 Class ID", "V2 Class ID (asset-only, optional)", "ClassIdText", "114"),
                ("TypeNameSearchTextBox", "V2 exact type name", "V2 exact type name (asset-only, optional)", "TypeNameText", "CP21_Type_テスト")
            ];
            foreach (var field in fields)
            {
                var edit = Assert.Single(Descendants(root), peer => peer.GetAutomationControlType() == AutomationControlType.Edit
                    && (peer.GetAutomationId() == field.Id || peer.GetName() == field.Name));
                Assert.Equal(field.Id, edit.GetAutomationId()); Assert.Equal(field.Name, edit.GetName());
                Assert.Equal(field.Watermark, Assert.IsType<TextBoxAutomationPeer>(edit).Owner.Watermark);
                var value = edit.GetProvider<IValueProvider>(); Assert.NotNull(value); Assert.False(value.IsReadOnly);
                Assert.True(string.IsNullOrEmpty(value.Value)); value.SetValue(field.Value); Dispatcher.UIThread.RunJobs();
                Assert.Equal(field.Value, value.Value);
                Assert.Equal(field.Value, search.GetType().GetProperty(field.Property)!.GetValue(search));
                tabs.SelectedIndex = 1; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
                tabs.SelectedIndex = 0; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
                Assert.Contains(edit, Descendants(root));
                Assert.Equal(field.Value, edit.GetProvider<IValueProvider>()!.Value);
                Assert.Equal(field.Id, edit.GetAutomationId()); Assert.Equal(field.Name, edit.GetName());
            }
            Assert.Equal(SearchWorkspaceFieldSelection.Names, search.FieldSelection);
            Assert.True(search.IncludePackageResults); Assert.True(search.IncludeSourceResults); Assert.True(search.IncludeAssetResults);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TemplateWatermarkPeerPresenceDoesNotEstablishControlViewIdentity()
    {
        var textBox = new TextBox { Watermark = "Template-only placeholder" };
        var window = new Window { Content = textBox, Width = 400, Height = 200 }; window.Show();
        try
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            var peer = ControlAutomationPeer.CreatePeerForElement(textBox)!;
            var watermark = Assert.Single(Descendants(peer), child => child.GetAutomationId() == "PART_Watermark");
            Assert.Equal("Template-only placeholder", watermark.GetName());
            Assert.False(watermark.IsControlElement()); // Internal template text is not a stable public field identity.
            Assert.True(string.IsNullOrEmpty(peer.GetName())); // Watermark does not label the Edit.
            Assert.Null(peer.GetLabeledBy());
        }
        finally { window.Close(); }
    }
}
