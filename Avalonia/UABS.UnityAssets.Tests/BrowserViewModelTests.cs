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
        Assert.False(browser.ShowEmptyMessage);
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

    [Fact]
    public async Task RapidFolderActivation_IgnoresStaleResultAndKeepsHistoryCorrect()
    {
        var service = new ControllableBrowserService();
        using BrowserViewModel browser = new(service, new NullPicker(), (_, _) => Task.CompletedTask, "A");
        await browser.InitializeAsync();

        Task stale = browser.ActivateEntryAsync(
            new BrowserEntry("Slow", "Slow", BrowserEntryKind.Directory, 0, DateTime.UnixEpoch));
        await service.SlowStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task current = browser.ActivateEntryAsync(
            new BrowserEntry("Fast", "Fast", BrowserEntryKind.Directory, 0, DateTime.UnixEpoch));
        await current;
        service.CompleteSlow();
        await stale;

        Assert.Equal("Fast", browser.CurrentPath);
        Assert.True(browser.CanGoBack);
        await browser.NavigateBackAsync();
        Assert.Equal("A", browser.CurrentPath);
        Assert.False(browser.CanGoBack);
        Assert.True(browser.CanGoForward);
    }

    [Fact]
    public async Task RepeatedActivationWhileFileOpenIsRunning_ReusesOperation()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int openCount = 0;
        FakeBrowserService service = new("A");
        using BrowserViewModel browser = new(service, new NullPicker(), async (_, token) =>
        {
            openCount++;
            await release.Task.WaitAsync(token);
        }, "A");
        await browser.InitializeAsync();
        var file = new BrowserEntry("x.assets", "A/x.assets", BrowserEntryKind.File, 10, DateTime.UnixEpoch);

        Task first = browser.ActivateEntryAsync(file);
        Task second = browser.ActivateEntryAsync(file);
        release.SetResult();
        await Task.WhenAll(first, second);

        Assert.Same(first, second);
        Assert.Equal(1, openCount);
        Assert.False(browser.IsActivating);
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

    private sealed class ControllableBrowserService : IDirectoryBrowserService
    {
        private readonly TaskCompletionSource<DirectorySnapshot> _slow =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SlowStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<DirectorySnapshot> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            return path switch
            {
                "A" => Task.FromResult(new DirectorySnapshot("A", [])),
                "Fast" => Task.FromResult(new DirectorySnapshot("Fast", [])),
                "Slow" => StartSlow(),
                _ => throw new DirectoryNotFoundException(path)
            };
        }

        public void CompleteSlow() => _slow.TrySetResult(new DirectorySnapshot("Slow", []));

        private Task<DirectorySnapshot> StartSlow()
        {
            SlowStarted.TrySetResult();
            return _slow.Task;
        }
    }

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
}
