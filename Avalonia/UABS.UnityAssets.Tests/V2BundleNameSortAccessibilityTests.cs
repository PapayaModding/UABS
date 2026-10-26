using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UABS.Page;
using UABS.ViewModels;
using UABS.PackageV2.Search;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

public sealed partial class V2BundleSearchWorkspaceTests
{
    private static void Layout(Window window)
    { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }

    private static void InvokeSort(Button button)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(button)!;
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("SearchNameSortButton", peer.GetAutomationId());
        Assert.Equal("Sort Name", peer.GetName());
        Assert.NotNull(peer.GetProvider<IInvokeProvider>());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTheory]
    [InlineData(900, true)]
    [InlineData(1300, false)]
    public async Task AccessibleNameButton_InvokeAndKeyboardToggleGlobalOrderAndRetainSelection(int width, bool invokeFirst)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 3);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var selected = search.SelectedResult = search.Results[1];
        var identity = selected.V2Result!.Key;
        string? packagePath = selected.PackagePath, sourcePath = selected.SourcePath;
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = width, Height = 700 }; window.Show();
        try
        {
            Layout(window);
            var button = view.FindControl<Button>("SearchNameSortButton")!;
            var refresh = view.FindControl<Button>("RefreshSearchButton")!;
            Assert.True(button.IsEffectivelyVisible); Assert.True(button.IsEnabled); Assert.True(button.Focusable);
            Assert.Same(refresh.Parent, button.Parent); Assert.IsType<WrapPanel>(button.Parent);
            var footer = (Control)button.Parent!;
            Assert.True(button.Bounds.Right <= footer.Bounds.Width && button.Bounds.Bottom <= footer.Bounds.Height);
            Assert.True(refresh.Bounds.Right <= footer.Bounds.Width && refresh.Bounds.Bottom <= footer.Bounds.Height);
            // Exercise actual wrapping in a narrow results region, not clipped horizontal children.
            footer.Width = 150; Layout(window);
            Assert.True(button.Bounds.Top >= refresh.Bounds.Bottom);
            Assert.True(button.Bounds.Bottom <= footer.Bounds.Height);
            footer.ClearValue(Control.WidthProperty); Layout(window);
            string[] ascending = ["source-0.bundle", "source-0.bundle-1", "source-0.bundle-2"];
            for (int action = 0; action < 2; action++)
            {
                string direction = action == 0 ? "Ascending" : "Descending";
                Assert.Contains("Next Name order: " + direction + ".", AutomationProperties.GetHelpText(button));
                if ((action == 0) == invokeFirst) InvokeSort(button);
                else
                {
                    Assert.True(button.Focus()); Assert.True(button.IsFocused);
                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
                    window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
                    Dispatcher.UIThread.RunJobs();
                }
                await view.HeaderSortTask; Layout(window);
                Assert.Null(search.Error); Assert.Equal(PackageSearchSortField.SourceName, search.DisplayedSortBy);
                Assert.Equal(action == 0 ? ascending : ascending.Reverse(), search.Results.Select(row => row.Name));
                Assert.All(search.Results, row => Assert.Equal("Package", row.PackageName));
                Assert.Same(selected, search.SelectedResult); Assert.Equal(identity, search.SelectedResult!.V2Result!.Key);
                Assert.Equal(packagePath, selected.PackagePath); Assert.Equal(sourcePath, selected.SourcePath);
                // Each global sort re-admits registrations; compare immutable identity, not the ephemeral registration ID.
                foreach (var id in new[] { identity.PackageId, identity.GenerationId, identity.SourceId!.Value, identity.SourceRevision!.Value })
                    Assert.Contains(id.ToString(), selected.V2Details);
                var grid = view.FindControl<DataGrid>("ResultsGrid")!;
                Assert.Same(selected, grid.SelectedItem); Assert.Empty(grid.CollectionView.SortDescriptions);
                var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, "Name"));
                Assert.Contains(action == 0 ? ":sortascending" : ":sortdescending", header.Classes);
                Assert.Equal($"Current Name order: {direction}. Next Name order: {(action == 0 ? "Descending" : "Ascending")}.",
                    ControlAutomationPeer.CreatePeerForElement(button)!.GetHelpText());
                Assert.Equal("hero", search.IncludeText); Assert.Equal(SearchWorkspaceFieldSelection.Assets, search.FieldSelection);
                Assert.True(search.IncludeSourceResults); Assert.False(search.IncludePackageResults); Assert.False(search.IncludeAssetResults);
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task AccessibleNameButton_DisabledForStaleSession_AndHiddenWithoutBundleResults()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 3);
        using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try
        {
            Layout(window); var button = view.FindControl<Button>("SearchNameSortButton")!;
            Assert.False(button.IsVisible); Assert.False(button.IsEnabled);
            await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync(); Layout(window);
            Assert.True(button.IsVisible); Assert.True(button.IsEnabled);
            var rows = search.Results; var selected = search.SelectedResult = rows[0];
            Edit(corpus.Packages[0].Path, "UPDATE asset SET memo='changed'");
            await search.ActivateAsync(); Layout(window);
            Assert.True(search.IsStale); Assert.False(button.IsEnabled);
            var task = view.HeaderSortTask;
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); // guard also rejects stale routed events
            Assert.Same(task, view.HeaderSortTask); Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccessibleNameButton_QueuedToggleUsesPendingDirectionAndRetainsCriteria(bool cancel)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2, sourceCount: 3);
        using var packages = V2SearchIntegrationTests.Packages(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int pages = 0;
        using var search = Create(packages, () => new V2SearchService(observe: (boundary, _) => {
            if (boundary == SearchBoundary.BeforePublication && Interlocked.Increment(ref pages) == 2)
            { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException(); }
        }));
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        var rows = search.Results; var selected = search.SelectedResult = rows[0];
        var view = new SearchWorkspaceView { DataContext = search };
        var window = new Window { Content = view, Width = 1300, Height = 700 }; window.Show();
        try
        {
            Layout(window); var button = view.FindControl<Button>("SearchNameSortButton")!;
            InvokeSort(button); Task first = view.HeaderSortTask;
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(15)); Layout(window);
            Assert.True(button.IsEnabled);
            Assert.Contains("Sorting in progress.", AutomationProperties.GetHelpText(button));
            Assert.Contains("Next Name order: Descending.", AutomationProperties.GetHelpText(button));
            search.IncludeText = "unexecuted criteria";
            Task latest;
            try { InvokeSort(button); latest = view.HeaderSortTask; if (cancel) search.CancelSearchCommand.Execute(null); }
            finally { release.Set(); }
            await Task.WhenAll(first, latest); Layout(window);
            Assert.Null(search.Error); Assert.Same(selected, search.SelectedResult);
            Assert.Equal("unexecuted criteria", search.IncludeText); Assert.True(search.CriteriaChanged);
            if (cancel) { Assert.Same(rows, search.Results); Assert.Equal(PackageSearchSortField.PackageName, search.DisplayedSortBy); }
            else { Assert.Equal(PackageSearchSortField.SourceName, search.DisplayedSortBy); Assert.Equal(PackageSearchSortDirection.Descending, search.DisplayedSortDirection); }
            Assert.Same(selected, view.FindControl<DataGrid>("ResultsGrid")!.SelectedItem);
        }
        finally { release.Set(); window.Close(); }
    }
}
