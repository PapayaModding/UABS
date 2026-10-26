using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Uabs.UiPoc.Fixtures;
using UABS.Page;
using UABS.PackageV2.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class IndeterminateUiAdmissionTests
{
    [AvaloniaFact]
    public async Task SupportedCatalogAdmitsSyntheticPartial_RealSearchRejectsAndRecovers()
    {
        string root = Path.Combine(Path.GetTempPath(), "uabs-g5-admission-" + Guid.NewGuid().ToString("N"));
        try
        {
            var fixtures = await IndeterminateFixtureBuilder.BuildAsync(root);
            var hashes = fixtures.Select(f => f.Sha256).ToArray();
            using var packages = V2SearchIntegrationTests.Packages();
            await packages.LoadCatalogAsync(root);
            Assert.Null(packages.CatalogError); Assert.Equal(2, packages.V2.Registrations.Count);
            Assert.Contains(packages.V2.Registrations, r => r.Status == "partial");
            using var search = new SearchWorkspaceViewModel(packages, new UABS.UnityAssets.Packages.Search.PackageSearchService(), useBundleProjection: true);
            var view = new SearchWorkspaceView { DataContext = search }; var window = new Window { Content = view, Width = 1300, Height = 800 }; window.Show();
            try
            {
                search.SelectAllPackagesCommand.Execute(null); search.IncludeText = IndeterminateFixtureBuilder.Keyword;
                search.FieldSelection = SearchWorkspaceFieldSelection.Assets; search.IncludePackageResults = false; search.IncludeAssetResults = false; search.IncludeSourceResults = true;
                await search.SearchAsync(); Assert.Null(search.Error); Assert.Equal(2, search.Results.Count);
                Assert.Contains(search.Results, r => r.Name == "g5-partial.metadata.json");
                var rows = search.Results; var selected = search.SelectedResult = rows[0]; string details = selected.V2Details!;
                string count = search.CountSummary, confirmed = search.LastExecutedCriteria;
                search.ExcludeText = "g5_absent_exclusion"; await search.SearchAsync();
                Assert.Equal("Exclusions cannot be verified against incomplete asset/name evidence. Select fully indexed packages or clear exclusions.", search.Error);
                Assert.Equal("InvalidRequest: " + IndeterminateFixtureBuilder.Backend, search.ErrorDetails);
                Assert.Same(rows, search.Results); Assert.Same(selected, search.SelectedResult); Assert.Equal(details, selected.V2Details);
                Assert.Equal(count, search.CountSummary); Assert.Equal(confirmed, search.LastExecutedCriteria); Assert.Equal("No new rows published.", search.Status);
                Assert.Equal("g5_absent_exclusion", search.ExcludeText); Assert.True(search.CriteriaChanged);
                foreach (var scope in search.PackageScopes) scope.IsSelected = scope.Path == fixtures.Single(f => !f.Partial).Path;
                await search.SearchAsync(); Assert.Null(search.Error); Assert.Null(search.ErrorDetails); Assert.Single(search.Results);
                Assert.Equal("g5-complete.metadata.json", search.Results[0].Name);
                search.SelectAllPackagesCommand.Execute(null);
                search.ExcludeText = ""; await search.SearchAsync(); Assert.Null(search.Error); Assert.Null(search.ErrorDetails); Assert.Equal(2, search.Results.Count);
            }
            finally { window.Close(); }
            Assert.Equal(hashes, fixtures.Select(f => IndeterminateFixtureBuilder.Hash(f.Path)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact]
    public async Task PartialContractCannotPublishWithoutAcknowledgement()
    {
        string root = Path.Combine(Path.GetTempPath(), "uabs-g5-no-ack-" + Guid.NewGuid().ToString("N"));
        try
        {
            var error = await Assert.ThrowsAsync<InvalidDataException>(() => IndeterminateFixtureBuilder.BuildAsync(root, acknowledge: false));
            Assert.Contains("acknowledgement refusal", error.Message); Assert.False(File.Exists(Path.Combine(root, "G5-partial.uabs.sqlite")));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
