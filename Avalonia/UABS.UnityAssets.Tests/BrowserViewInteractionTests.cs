using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using UABS.Models;
using UABS.Page;
using UABS.Services;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class BrowserViewInteractionTests
{
    [AvaloniaFact]
    public async Task EmptyFilteredDirectory_ShowsHelpfulNonErrorMessage()
    {
        StaticBrowserService service = new(new DirectorySnapshot("A", []));
        using BrowserViewModel browser = CreateBrowser(service);
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out _);

        try
        {
            BrowserView view = Assert.IsType<BrowserView>(window.Content);
            TextBlock message = view.FindControl<TextBlock>("EmptyFolderMessage")
                                ?? throw new InvalidOperationException("Empty-folder message was not created.");

            Assert.True(message.IsVisible);
            Assert.False(browser.HasError);
            Assert.Equal("No supported Unity asset files found in this folder.", message.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ClickingFolderRow_NavigatesAndAddsOneHistoryEntry()
    {
        BrowserEntry folder = Entry("Child", "A/Child", BrowserEntryKind.Directory);
        StaticBrowserService service = new(
            new DirectorySnapshot("A", [folder]),
            new DirectorySnapshot("A/Child", []));
        using BrowserViewModel browser = CreateBrowser(service);
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out DataGrid grid);

        try
        {
            Click(window, FindRow(grid, folder));

            Assert.Equal("A/Child", browser.CurrentPath);
            Assert.True(browser.CanGoBack);
            Assert.False(browser.CanGoForward);
            Assert.Equal(1, service.Calls["A/Child"]);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ClickingFileRow_InvokesExistingFileOpenCallback()
    {
        BrowserEntry file = Entry("sample.assets", "A/sample.assets", BrowserEntryKind.File);
        StaticBrowserService service = new(new DirectorySnapshot("A", [file]));
        string? opened = null;
        using BrowserViewModel browser = CreateBrowser(service, (path, _) =>
        {
            opened = path;
            return Task.CompletedTask;
        });
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out DataGrid grid);

        try
        {
            Click(window, FindRow(grid, file));

            Assert.Equal(file.FullPath, opened);
            Assert.Equal("A", browser.CurrentPath);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task EnterOnSelectedRow_ActivatesSelection()
    {
        BrowserEntry file = Entry("sample.assets", "A/sample.assets", BrowserEntryKind.File);
        StaticBrowserService service = new(new DirectorySnapshot("A", [file]));
        int openCount = 0;
        using BrowserViewModel browser = CreateBrowser(service, (_, _) =>
        {
            openCount++;
            return Task.CompletedTask;
        });
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out DataGrid grid);

        try
        {
            grid.SelectedItem = file;
            browser.SelectedEntry = file;
            grid.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter
            });

            Assert.Equal(1, openCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task SpaceOnSelectedRow_DoesNotActivateSelection()
    {
        BrowserEntry file = Entry("sample.assets", "A/sample.assets", BrowserEntryKind.File);
        StaticBrowserService service = new(new DirectorySnapshot("A", [file]));
        int openCount = 0;
        using BrowserViewModel browser = CreateBrowser(service, (_, _) =>
        {
            openCount++;
            return Task.CompletedTask;
        });
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out DataGrid grid);

        try
        {
            grid.SelectedItem = file;
            browser.SelectedEntry = file;
            grid.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Space
            });

            Assert.Equal(0, openCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task DoubleClickOnRow_StartsOnlyOneActivation()
    {
        BrowserEntry file = Entry("sample.assets", "A/sample.assets", BrowserEntryKind.File);
        StaticBrowserService service = new(new DirectorySnapshot("A", [file]));
        int openCount = 0;
        using BrowserViewModel browser = CreateBrowser(service, (_, _) =>
        {
            openCount++;
            return Task.CompletedTask;
        });
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out DataGrid grid);

        try
        {
            DataGridRow row = FindRow(grid, file);
            Click(window, row);
            Click(window, row);

            Assert.Equal(1, openCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task RapidClicks_DoNotAllowStaleDirectoryToReplaceNewerDirectory()
    {
        BrowserEntry slow = Entry("Slow", "Slow", BrowserEntryKind.Directory);
        BrowserEntry fast = Entry("Fast", "Fast", BrowserEntryKind.Directory);
        var service = new RacingBrowserService(slow, fast);
        using BrowserViewModel browser = CreateBrowser(service);
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out DataGrid grid);

        try
        {
            DataGridRow slowRow = FindRow(grid, slow);
            DataGridRow fastRow = FindRow(grid, fast);
            Click(window, slowRow);
            Click(window, fastRow);

            Assert.Equal("Fast", browser.CurrentPath);
            service.CompleteSlow();
            await service.SlowReturned.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Task.Yield();

            Assert.Equal("Fast", browser.CurrentPath);
            Assert.True(browser.CanGoBack);
            await browser.NavigateBackAsync();
            Assert.Equal("A", browser.CurrentPath);
            Assert.False(browser.CanGoBack);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task FailedFileClick_PreservesDirectoryAndReportsError()
    {
        BrowserEntry file = Entry("broken.assets", "A/broken.assets", BrowserEntryKind.File);
        StaticBrowserService service = new(new DirectorySnapshot("A", [file]));
        using BrowserViewModel browser = CreateBrowser(
            service,
            (_, _) => throw new IOException("Fixture load failure."));
        await browser.InitializeAsync();
        Window window = ShowBrowser(browser, out DataGrid grid);

        try
        {
            Click(window, FindRow(grid, file));

            Assert.Equal("A", browser.CurrentPath);
            Assert.Single(browser.Entries);
            Assert.True(browser.HasError);
            Assert.Contains("Fixture load failure", browser.ErrorMessage);
        }
        finally
        {
            window.Close();
        }
    }

    private static BrowserViewModel CreateBrowser(
        IDirectoryBrowserService service,
        Func<string, CancellationToken, Task>? openFile = null) =>
        new(service, new NullPicker(), openFile ?? ((_, _) => Task.CompletedTask), "A");

    private static Window ShowBrowser(BrowserViewModel browser, out DataGrid grid)
    {
        var view = new BrowserView { DataContext = browser };
        var window = new Window
        {
            Width = 900,
            Height = 600,
            Content = view
        };
        window.Show();
        grid = view.FindControl<DataGrid>("EntriesGrid")
               ?? throw new InvalidOperationException("Browser entries grid was not created.");
        grid.UpdateLayout();
        return window;
    }

    private static DataGridRow FindRow(DataGrid grid, BrowserEntry entry)
    {
        grid.ScrollIntoView(entry, null);
        grid.UpdateLayout();
        return grid.GetVisualDescendants()
                   .OfType<DataGridRow>()
                   .Single(row => ReferenceEquals(row.DataContext, entry));
    }

    private static void Click(Window window, Control control)
    {
        Point point = control.TranslatePoint(new Point(8, 8), window)
                      ?? throw new InvalidOperationException("Could not locate the browser row.");
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
    }

    private static BrowserEntry Entry(string name, string path, BrowserEntryKind kind) =>
        new(name, path, kind, 0, DateTime.UnixEpoch);

    private sealed class StaticBrowserService(params DirectorySnapshot[] snapshots) : IDirectoryBrowserService
    {
        private readonly Dictionary<string, DirectorySnapshot> _snapshots = snapshots.ToDictionary(
            snapshot => snapshot.Path,
            StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, int> Calls { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<DirectorySnapshot> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            Calls[path] = Calls.GetValueOrDefault(path) + 1;
            return Task.FromResult(_snapshots[path]);
        }
    }

    private sealed class RacingBrowserService(BrowserEntry slow, BrowserEntry fast) : IDirectoryBrowserService
    {
        private readonly TaskCompletionSource _releaseSlow =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SlowReturned { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<DirectorySnapshot> ReadAsync(string path, CancellationToken cancellationToken = default) =>
            path switch
            {
                "A" => Task.FromResult(new DirectorySnapshot("A", [slow, fast])),
                "Fast" => Task.FromResult(new DirectorySnapshot("Fast", [])),
                "Slow" => ReadSlowAsync(),
                _ => throw new DirectoryNotFoundException(path)
            };

        public void CompleteSlow() => _releaseSlow.TrySetResult();

        private async Task<DirectorySnapshot> ReadSlowAsync()
        {
            await _releaseSlow.Task;
            SlowReturned.TrySetResult();
            return new DirectorySnapshot("Slow", []);
        }
    }

    private sealed class NullPicker : IFilePickerService
    {
        public Task<string?> PickUnityFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }
}
