using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Uabs.UiPoc.Fixtures;
using UABS.Page;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed partial class V2BundleSearchWorkspaceTests
{
    [AvaloniaTheory]
    [InlineData(900, 560)]
    [InlineData(1300, 700)]
    public async Task ResultsHeadingErrorAndWarningsHaveSeparateBoundedRows_KeepConfirmedState(int width, int height)
    {
        string root = Path.Combine(Path.GetTempPath(), "uabs-search-layout-" + Guid.NewGuid().ToString("N"));
        try
        {
            await IndeterminateFixtureBuilder.BuildAsync(root);
            using var packages = V2SearchIntegrationTests.Packages(); using var search = Create(packages);
            var view = new SearchWorkspaceView { DataContext = search };
            var window = new Window { Content = view, Width = width, Height = height }; window.Show();
            try
            {
                Layout(window);
                var region = view.FindControl<Grid>("SearchResultsRegion")!;
                var heading = view.FindControl<TextBlock>("SearchResultsHeading")!;
                var errorRegion = view.FindControl<Border>("SearchErrorRegion")!;
                var error = view.FindControl<TextBlock>("SearchErrorText")!;
                var info = view.FindControl<ScrollViewer>("SearchResultsInfo")!;
                var warnings = view.FindControl<Expander>("SearchIndexingWarnings")!;
                var warningText = view.FindControl<TextBlock>("SearchIndexingWarningsText")!;
                var count = view.FindControl<TextBlock>("SearchResultCount")!;
                var retained = view.FindControl<TextBlock>("SearchRetainedResultsNotice")!;
                var grid = view.FindControl<DataGrid>("ResultsGrid")!;
                Assert.True(heading.IsEffectivelyVisible); Assert.False(info.IsEffectivelyVisible);
                Assert.False(errorRegion.IsEffectivelyVisible); Assert.False(warnings.IsEffectivelyVisible);

                await packages.LoadCatalogAsync(root); search.SelectAllPackagesCommand.Execute(null);
                Configure(search); search.IncludeText = IndeterminateFixtureBuilder.Keyword;
                search.FieldSelection = SearchWorkspaceFieldSelection.Names;
                await search.SearchAsync(); Layout(window);
                Assert.True(errorRegion.IsEffectivelyVisible); Assert.True(heading.IsEffectivelyVisible);
                Assert.False(search.HasExecutedSearch); Assert.False(info.IsEffectivelyVisible); Assert.False(retained.IsEffectivelyVisible);
                search.FieldSelection = SearchWorkspaceFieldSelection.Assets;
                await search.SearchAsync(); Layout(window);
                Assert.Null(search.Error); Assert.Equal(2, search.Results.Count); Assert.True(search.HasWarnings);
                Assert.True(info.IsEffectivelyVisible); Assert.True(warnings.IsEffectivelyVisible); Assert.False(warnings.IsExpanded);
                Assert.Equal(search.Warnings, warningText.Text); Assert.Equal(search.CountSummary, count.Text);

                var rows = search.Results; var selected = search.SelectedResult = rows[0];
                string inspector = selected.V2Details!, confirmed = search.LastExecutedCriteria, confirmedCount = search.CountSummary;
                string longWarnings = string.Join("\n", Enumerable.Range(0, 150).Select(i => $"Warning {i}: complete indexed diagnostic content テスト — retained without truncation."));
                search.ExcludeText = "g5_absent_exclusion"; await search.SearchAsync(); Layout(window);
                Assert.Equal("InvalidRequest: " + IndeterminateFixtureBuilder.Backend, search.ErrorDetails);
                Assert.True(errorRegion.IsEffectivelyVisible); Assert.Equal(search.Error, error.Text);
                Assert.Equal(search.ErrorDetails, ToolTip.GetTip(error)); Assert.True(retained.IsEffectivelyVisible);
                Assert.Contains("stale for the current Search", retained.Text);
                Assert.False(search.IsStale); // Presentation distinguishes rejected criteria without changing session semantics.
                Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult); Assert.Same(selected, grid.SelectedItem);
                Assert.Equal(inspector, selected.V2Details); Assert.Equal(confirmed, search.LastExecutedCriteria);
                Assert.Equal(confirmedCount, search.CountSummary); Assert.Equal(confirmedCount, count.Text);
                Assert.Equal(IndeterminateFixtureBuilder.Keyword, search.IncludeText); Assert.Equal("g5_absent_exclusion", search.ExcludeText);
                Assert.Equal(search.Warnings, warningText.Text); Assert.False(warnings.IsExpanded);
                // Presentation-only stress values; never alter backend state or fixture coverage.
                warningText.SetCurrentValue(TextBlock.TextProperty, longWarnings);

                Rect InRegion(Control control) => new(control.TranslatePoint(default, region)!.Value, control.Bounds.Size);
                void SeparateRows()
                {
                    var h = InRegion(heading); var e = InRegion(errorRegion); var i = InRegion(info); var g = InRegion(grid);
                    Assert.True(heading.IsEffectivelyVisible); Assert.True(heading.DesiredSize.Width <= heading.Bounds.Width + 1);
                    Assert.True(h.Bottom <= e.Top && e.Bottom <= i.Top && i.Bottom <= g.Top);
                    Assert.True(g.Height > 32, $"DataGrid needs a usable body, not an error overlay: {g}");
                    Assert.True(g.Bottom <= region.Bounds.Height + 1);
                    Assert.True(e.Right <= region.Bounds.Width + 1 && i.Right <= region.Bounds.Width + 1);
                }
                SeparateRows();
                error.SetCurrentValue(TextBlock.TextProperty, string.Join(" ", Enumerable.Repeat(search.Error, 60)));
                Layout(window); SeparateRows();
                var errorScroll = view.FindControl<ScrollViewer>("SearchErrorScroller")!;
                Assert.True(errorScroll.Extent.Height > errorScroll.Viewport.Height); Assert.True(errorScroll.Viewport.Height <= 96);
                Assert.Equal(search.ErrorDetails, ToolTip.GetTip(error));
                error.SetCurrentValue(TextBlock.TextProperty, search.Error);
                warnings.IsExpanded = true; Layout(window); SeparateRows();
                Assert.Equal(longWarnings, warningText.Text); Assert.True(info.Extent.Height > info.Viewport.Height);
                Assert.True(info.Viewport.Height <= 120); Assert.Equal(1, Grid.GetRow(errorRegion));
                info.Offset = new Vector(0, info.Extent.Height); Layout(window);
                Assert.True(info.Offset.Y > 0); Assert.Equal(longWarnings, warningText.Text);
                warnings.IsExpanded = false; Layout(window); SeparateRows();
                warningText.SetCurrentValue(TextBlock.TextProperty, search.Warnings);

                search.ExcludeText = ""; Layout(window);
                Assert.False(search.CriteriaChanged); Assert.True(errorRegion.IsEffectivelyVisible); Assert.False(retained.IsEffectivelyVisible);
                await search.SearchAsync(); Layout(window);
                Assert.Null(search.Error); Assert.Null(search.ErrorDetails); Assert.Null(ToolTip.GetTip(error));
                Assert.False(errorRegion.IsEffectivelyVisible); Assert.False(retained.IsEffectivelyVisible);
                Assert.True(heading.IsEffectivelyVisible); Assert.Equal(2, search.Results.Count);
                Assert.Equal(search.Warnings, warningText.Text); Assert.False(warnings.IsExpanded);
                search.IncludeText = "absent-layout-asset"; await search.SearchAsync(); Layout(window);
                Assert.Null(search.Error); Assert.Empty(search.Results); Assert.True(search.ShowEmptyState);
                Assert.True(info.IsEffectivelyVisible); Assert.Equal(search.CountSummary, count.Text);
                Assert.True(heading.IsEffectivelyVisible); Assert.False(retained.IsEffectivelyVisible);
            }
            finally { window.Close(); }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
