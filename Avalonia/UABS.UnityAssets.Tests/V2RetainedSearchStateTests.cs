using Avalonia.Headless.XUnit;
using UABS.PackageV2.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed partial class V2BundleSearchWorkspaceTests
{
    [AvaloniaFact]
    public async Task NondefaultCriteriaAndScopeCatalogTransitionsRetainStaleRowsWithoutExecutingSearch()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(2);
        using var packages = V2SearchIntegrationTests.Packages(); int executions = 0;
        using var search = Create(packages, () => { executions++; return new V2SearchService(); });
        await packages.V2.OpenAsync(corpus.Packages[0].Path, select: false); Configure(search); await search.SearchAsync();
        Assert.Null(search.Error); Assert.Equal(1, executions);
        var rows = search.Results; var selected = search.SelectedResult = Assert.Single(rows);
        var identity = selected.V2Result!.Key; string executed = search.LastExecutedCriteria;
        string fresh = search.CountSummary; Assert.Equal("1–1 displayed / 1 exact indexed matches", fresh);
        search.IncludeText = "hero_retention"; search.ExcludeText = "excluded 保持";
        search.PathIdText = "-42424241"; search.ClassIdText = "114"; search.TypeNameText = "CP21_Type_テスト";
        search.FieldSelection = SearchWorkspaceFieldSelection.Names; search.IncludePackageResults = true;
        search.ExactText = true; search.SearchMemos = true; search.MemoOnly = true;
        Assert.False(search.IsStale); Assert.Equal(fresh, search.CountSummary); // Editing alone need not invalidate the snapshot.

        void Retained(bool selectedScope)
        {
            Assert.Equal(1, executions); Assert.False(search.IsSearching); Assert.Null(search.Error);
            Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult);
            Assert.Equal(identity, Assert.Single(search.Results).V2Result!.Key);
            Assert.Equal(executed, search.LastExecutedCriteria); Assert.True(search.CriteriaChanged);
            Assert.True(search.IsStale); Assert.Equal("1 displayed · stale count", search.CountSummary);
            Assert.Equal(selectedScope, Assert.Single(search.PackageScopes).IsSelected);
            Assert.Equal("hero_retention", search.IncludeText); Assert.Equal("excluded 保持", search.ExcludeText);
            Assert.Equal("-42424241", search.PathIdText); Assert.Equal("114", search.ClassIdText); Assert.Equal("CP21_Type_テスト", search.TypeNameText);
            Assert.Equal(SearchWorkspaceFieldSelection.Names, search.FieldSelection);
            Assert.True(search.IncludePackageResults); Assert.True(search.IncludeSourceResults); Assert.False(search.IncludeAssetResults);
            Assert.True(search.ExactText); Assert.True(search.SearchMemos); Assert.True(search.MemoOnly);
        }
        Assert.Single(search.PackageScopes).IsSelected = false; Retained(false);
        Assert.Single(search.PackageScopes).IsSelected = true; Retained(true);
        search.Deactivate(); await search.ActivateAsync(); Retained(true);
        await packages.LoadCatalogAsync(corpus.DirectoryPath); Retained(true);
        await packages.RefreshCatalogCommand.ExecuteAsync(); Retained(true);

        Configure(search); search.ExcludeText = ""; search.PathIdText = ""; search.ClassIdText = ""; search.TypeNameText = "";
        search.ExactText = false; search.SearchMemos = false; search.MemoOnly = false;
        await search.RefreshSearchCommand.ExecuteAsync();
        Assert.Equal(2, executions); Assert.False(search.IsStale); Assert.Null(search.Error);
        Assert.Equal(fresh, search.CountSummary); Assert.Equal(identity, Assert.Single(search.Results).V2Result!.Key);
    }
}
