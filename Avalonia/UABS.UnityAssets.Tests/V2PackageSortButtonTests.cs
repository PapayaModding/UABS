using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using UABS.Page;
using UABS.PackageV2.Search;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

public sealed partial class V2BundleSearchWorkspaceTests
{
    private static void InvokePackage(Button button)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(button)!;
        Assert.Equal("Sort Package", peer.GetName()); Assert.Equal("SearchPackageSortButton", peer.GetAutomationId());
        peer.GetProvider<IInvokeProvider>()!.Invoke(); Dispatcher.UIThread.RunJobs();
    }
    [AvaloniaTheory]
    [InlineData(900)]
    [InlineData(1300)]
    public async Task PackageButtonSupportsInvokeTabEnterSpaceAndKeepsAscendingTies(int width)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, "A", sourceCount: 2); await corpus.Add(2, "Z");
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        foreach (var package in corpus.Packages) await packages.V2.OpenAsync(package.Path, select: false);
        search.SelectAllPackagesCommand.Execute(null); Configure(search); await search.SearchAsync();
        var selected = search.SelectedResult = search.Results[0]; var key = selected.V2Result!.Key;
        var view = new SearchWorkspaceView { DataContext = search }; var window = new Window { Content = view, Width = width, Height = 700 }; window.Show();
        try
        {
            Layout(window); var button = view.FindControl<Button>("SearchPackageSortButton")!;
            var name = view.FindControl<Button>("SearchNameSortButton")!;
            var footer = Assert.IsType<WrapPanel>(button.Parent);
            footer.Width = 150; Layout(window);
            Assert.True(button.Bounds.Top >= name.Bounds.Bottom); Assert.True(button.Bounds.Right <= footer.Bounds.Width && button.Bounds.Bottom <= footer.Bounds.Height);
            footer.ClearValue(Control.WidthProperty); Layout(window);
            search.IncludeText = "unexecuted criteria"; string confirmed = search.LastExecutedCriteria;
            string[] ascending = ["A/source-0.bundle", "A/source-0.bundle-1", "Z/source-1.bundle"];
            string[] descending = ["Z/source-1.bundle", "A/source-0.bundle", "A/source-0.bundle-1"];
            for (int action = 0; action < 3; action++)
            {
                bool desc = action != 1;
                if (action == 0) InvokePackage(button);
                else
                {
                    if (action == 1)
                    {
                        Assert.True(name.Focus());
                        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t"); window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
                        Assert.True(button.IsFocused);
                    }
                    else Assert.True(button.Focus());
                    var input = action == 1 ? Key.Enter : Key.Space;
                    var physical = action == 1 ? PhysicalKey.Enter : PhysicalKey.Space;
                    window.KeyPress(input, RawInputModifiers.None, physical, action == 1 ? "\r" : " ");
                    window.KeyRelease(input, RawInputModifiers.None, physical, action == 1 ? "\r" : " "); Dispatcher.UIThread.RunJobs();
                }
                await view.HeaderSortTask; Layout(window);
                Assert.Null(search.Error); Assert.Equal(PackageSearchSortField.PackageName, search.DisplayedSortBy);
                Assert.Equal(desc ? PackageSearchSortDirection.Descending : PackageSearchSortDirection.Ascending, search.DisplayedSortDirection);
                Assert.Equal(desc ? descending : ascending, search.Results.Select(row => row.PackageName + "/" + row.Name));
                Assert.Same(selected, search.SelectedResult); Assert.Equal(key, selected.V2Result!.Key); Assert.Same(selected, view.FindControl<DataGrid>("ResultsGrid")!.SelectedItem);
                Assert.Empty(view.FindControl<DataGrid>("ResultsGrid")!.CollectionView.SortDescriptions);
                Assert.Equal("unexecuted criteria", search.IncludeText); Assert.Equal(confirmed, search.LastExecutedCriteria); Assert.True(search.CriteriaChanged);
                Assert.Equal($"Current Package order: {(desc ? "Descending" : "Ascending")}. Next Package order: {(desc ? "Ascending" : "Descending")}.",
                    ControlAutomationPeer.CreatePeerForElement(button)!.GetHelpText());
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task PackageButtonDisabledForStaleOrDisabledGridAndHiddenWithoutBundles()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        var view = new SearchWorkspaceView { DataContext = search }; var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try
        {
            Layout(window); var button = view.FindControl<Button>("SearchPackageSortButton")!;
            Assert.False(button.IsVisible); Assert.False(button.IsEnabled);
            await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync(); Layout(window);
            Assert.True(button.IsEnabled); var grid = view.FindControl<DataGrid>("ResultsGrid")!;
            grid.CanUserSortColumns = false; Layout(window); Assert.False(button.IsEnabled);
            var task = view.HeaderSortTask; button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); Assert.Same(task, view.HeaderSortTask);
            grid.CanUserSortColumns = true; Layout(window); Assert.True(button.IsEnabled);
            Edit(corpus.Packages[0].Path, "UPDATE asset SET memo='changed'"); await search.ActivateAsync(); Layout(window);
            Assert.True(search.IsStale); Assert.False(button.IsEnabled);
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); Assert.Same(task, view.HeaderSortTask);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueuedPackageButtonRetainsOwnershipCriteriaAndSelection(bool cancel)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 3);
        using var packages = V2SearchIntegrationTests.Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int publications = 0;
        using var search = Create(packages, () => new V2SearchService(observe: (boundary, _) => {
            if (boundary == SearchBoundary.BeforePublication && Interlocked.Increment(ref publications) == 2)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); }
        }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var rows = search.Results; var selected = search.SelectedResult = rows[0];
        var view = new SearchWorkspaceView { DataContext = search }; var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try
        {
            Layout(window); var button = view.FindControl<Button>("SearchPackageSortButton")!; InvokePackage(button); var first = view.HeaderSortTask;
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(15)); Layout(window);
            Assert.Contains("Sorting in progress.", ControlAutomationPeer.CreatePeerForElement(button)!.GetHelpText());
            Assert.Contains("Next Package order: Ascending.", ControlAutomationPeer.CreatePeerForElement(button)!.GetHelpText());
            search.IncludeText = "unexecuted"; Task last;
            try { InvokePackage(button); last = view.HeaderSortTask; if (cancel) search.CancelSearchCommand.Execute(null); }
            finally { release.Set(); }
            await Task.WhenAll(first, last); Layout(window);
            Assert.Null(search.Error); Assert.Same(selected, search.SelectedResult); Assert.Equal("unexecuted", search.IncludeText);
            Assert.Equal(PackageSearchSortField.PackageName, search.DisplayedSortBy); Assert.Equal(PackageSearchSortDirection.Ascending, search.DisplayedSortDirection);
            if (cancel) Assert.Same(rows, search.Results);
            Assert.Same(selected, view.FindControl<DataGrid>("ResultsGrid")!.SelectedItem);
        }
        finally { release.Set(); window.Close(); }
    }
}
