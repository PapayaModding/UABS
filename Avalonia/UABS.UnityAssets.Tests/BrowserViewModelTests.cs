using UABS.Models;
using UABS.Services;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class BrowserViewModelTests
{
    [Fact]
    public async Task NavigationHistory_BackForwardAndBranchingAreDeterministic()
    {
        FakeBrowserService service = new("A", "B", "C", "D");
        using BrowserViewModel browser = new(service, new NullPicker(), (_, _) => Task.CompletedTask, "A");

        await browser.InitializeAsync();
        await browser.NavigateAsync("B");
        await browser.NavigateAsync("C");
        await browser.NavigateBackAsync();
        Assert.Equal("B", browser.CurrentPath);
        Assert.True(browser.CanGoForward);

        await browser.NavigateForwardAsync();
        Assert.Equal("C", browser.CurrentPath);
        await browser.NavigateBackAsync();
        await browser.NavigateAsync("D");

        Assert.Equal("D", browser.CurrentPath);
        Assert.False(browser.CanGoForward);
        Assert.True(browser.CanGoBack);
    }

    [Fact]
    public async Task Refresh_ReenumeratesWithoutChangingHistory()
    {
        FakeBrowserService service = new("A");
        using BrowserViewModel browser = new(service, new NullPicker(), (_, _) => Task.CompletedTask, "A");
        await browser.InitializeAsync();

        await browser.RefreshAsync();

        Assert.Equal(2, service.Calls["A"]);
        Assert.False(browser.CanGoBack);
        Assert.False(browser.CanGoForward);
    }

    [Fact]
    public async Task FailedNavigation_PreservesCurrentDirectoryAndHistory()
    {
        FakeBrowserService service = new("A", "B") { FailurePath = "missing" };
        using BrowserViewModel browser = new(service, new NullPicker(), (_, _) => Task.CompletedTask, "A");
        await browser.InitializeAsync();
        await browser.NavigateAsync("B");

        await browser.NavigateAsync("missing");

        Assert.Equal("B", browser.CurrentPath);
        Assert.True(browser.CanGoBack);
        Assert.True(browser.HasError);
    }

    [Fact]
    public async Task OpeningFile_DelegatesWithoutChangingDirectory()
    {
        string? opened = null;
        FakeBrowserService service = new("A");
        using BrowserViewModel browser = new(service, new NullPicker(), (path, _) =>
        {
            opened = path;
            return Task.CompletedTask;
        }, "A");
        await browser.InitializeAsync();
        browser.SelectedEntry = new BrowserEntry("x.assets", "A/x.assets", BrowserEntryKind.File, 10, DateTime.Now);

        browser.OpenSelectedCommand.Execute(null);
        await Task.Yield();

        Assert.Equal("A/x.assets", opened);
        Assert.Equal("A", browser.CurrentPath);
    }

    private sealed class FakeBrowserService(params string[] paths) : IDirectoryBrowserService
    {
        private readonly HashSet<string> _paths = new(paths, StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> Calls { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string? FailurePath { get; init; }

        public Task<DirectorySnapshot> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            Calls[path] = Calls.GetValueOrDefault(path) + 1;
            if (string.Equals(path, FailurePath, StringComparison.OrdinalIgnoreCase) || !_paths.Contains(path))
                throw new DirectoryNotFoundException($"Missing {path}");
            return Task.FromResult(new DirectorySnapshot(path, []));
        }
    }

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
}
