using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UABS.Models;
using UABS.Services;

namespace UABS.ViewModels;

public sealed class BrowserViewModel : ViewModelBase, IDisposable
{
    private readonly IDirectoryBrowserService _browserService;
    private readonly IFilePickerService _filePicker;
    private readonly Func<string, CancellationToken, Task> _openFile;
    private readonly string _initialPath;
    private readonly List<string> _backHistory = [];
    private readonly List<string> _forwardHistory = [];
    private CancellationTokenSource? _navigationCancellation;
    private IReadOnlyList<BrowserEntry> _entries = [];
    private BrowserEntry? _selectedEntry;
    private string _currentPath = string.Empty;
    private string _locationInput = string.Empty;
    private string? _errorMessage;
    private bool _isLoading;
    private int _navigationVersion;
    private bool _initialized;
    private bool _disposed;

    public BrowserViewModel(
        IDirectoryBrowserService browserService,
        IFilePickerService filePicker,
        Func<string, CancellationToken, Task> openFile,
        string? initialPath = null)
    {
        _browserService = browserService;
        _filePicker = filePicker;
        _openFile = openFile;
        _initialPath = string.IsNullOrWhiteSpace(initialPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : initialPath;

        BackCommand = new AsyncCommand(NavigateBackAsync, () => CanGoBack);
        ForwardCommand = new AsyncCommand(NavigateForwardAsync, () => CanGoForward);
        UpCommand = new AsyncCommand(NavigateUpAsync, () => CanGoUp);
        RefreshCommand = new AsyncCommand(RefreshAsync, () => HasCurrentPath);
        GoCommand = new AsyncCommand(() => NavigateAsync(LocationInput), () => !string.IsNullOrWhiteSpace(LocationInput));
        ChooseFolderCommand = new AsyncCommand(ChooseFolderAsync);
        OpenSelectedCommand = new AsyncCommand(OpenSelectedAsync, () => SelectedEntry is not null);
    }

    public IReadOnlyList<BrowserEntry> Entries
    {
        get => _entries;
        private set => SetField(ref _entries, value);
    }

    public BrowserEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetField(ref _selectedEntry, value))
                OpenSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    public string CurrentPath
    {
        get => _currentPath;
        private set
        {
            if (SetField(ref _currentPath, value))
            {
                OnPropertyChanged(nameof(HasCurrentPath));
                OnPropertyChanged(nameof(CanGoUp));
                RefreshCommand.NotifyCanExecuteChanged();
                UpCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string LocationInput
    {
        get => _locationInput;
        set
        {
            if (SetField(ref _locationInput, value))
                GoCommand.NotifyCanExecuteChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public bool HasCurrentPath => !string.IsNullOrWhiteSpace(CurrentPath);
    public bool CanGoBack => _backHistory.Count > 0;
    public bool CanGoForward => _forwardHistory.Count > 0;
    public bool CanGoUp => HasCurrentPath && Directory.GetParent(CurrentPath) is not null;

    public AsyncCommand BackCommand { get; }
    public AsyncCommand ForwardCommand { get; }
    public AsyncCommand UpCommand { get; }
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand GoCommand { get; }
    public AsyncCommand ChooseFolderCommand { get; }
    public AsyncCommand OpenSelectedCommand { get; }

    public Task InitializeAsync()
    {
        if (_initialized)
            return Task.CompletedTask;

        _initialized = true;
        return NavigateCoreAsync(_initialPath, NavigationKind.Initial);
    }

    public Task NavigateAsync(string path) => NavigateCoreAsync(path, NavigationKind.New);

    public Task RefreshAsync() => HasCurrentPath
        ? NavigateCoreAsync(CurrentPath, NavigationKind.Refresh)
        : Task.CompletedTask;

    public Task NavigateBackAsync() => CanGoBack
        ? NavigateCoreAsync(_backHistory[^1], NavigationKind.Back)
        : Task.CompletedTask;

    public Task NavigateForwardAsync() => CanGoForward
        ? NavigateCoreAsync(_forwardHistory[^1], NavigationKind.Forward)
        : Task.CompletedTask;

    public Task NavigateUpAsync()
    {
        DirectoryInfo? parent = HasCurrentPath ? Directory.GetParent(CurrentPath) : null;
        return parent is null ? Task.CompletedTask : NavigateCoreAsync(parent.FullName, NavigationKind.New);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _navigationCancellation?.Cancel();
        _navigationCancellation?.Dispose();
    }

    private async Task ChooseFolderAsync()
    {
        try
        {
            string? path = await _filePicker.PickFolderAsync();
            if (!string.IsNullOrWhiteSpace(path))
                await NavigateAsync(path);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = $"The folder picker could not be opened: {exception.Message}";
        }
    }

    private async Task OpenSelectedAsync()
    {
        BrowserEntry? selected = SelectedEntry;
        if (selected is null)
            return;

        if (selected.IsDirectory)
            await NavigateAsync(selected.FullPath);
        else
            await _openFile(selected.FullPath, CancellationToken.None);
    }

    private async Task NavigateCoreAsync(string path, NavigationKind kind)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(path))
            return;

        _navigationCancellation?.Cancel();
        _navigationCancellation?.Dispose();
        _navigationCancellation = new CancellationTokenSource();
        CancellationToken token = _navigationCancellation.Token;
        int version = ++_navigationVersion;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            DirectorySnapshot snapshot = await _browserService.ReadAsync(path, token);
            token.ThrowIfCancellationRequested();
            if (version != _navigationVersion)
                return;

            CommitHistory(kind, snapshot.Path);
            CurrentPath = snapshot.Path;
            LocationInput = snapshot.Path;
            Entries = snapshot.Entries;
            SelectedEntry = null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            if (version == _navigationVersion)
                ErrorMessage = exception.Message;
        }
        finally
        {
            if (version == _navigationVersion)
                IsLoading = false;
        }
    }

    private void CommitHistory(NavigationKind kind, string destination)
    {
        if (kind == NavigationKind.Refresh || string.Equals(CurrentPath, destination, StringComparison.OrdinalIgnoreCase))
            return;

        switch (kind)
        {
            case NavigationKind.New when HasCurrentPath:
                _backHistory.Add(CurrentPath);
                _forwardHistory.Clear();
                break;
            case NavigationKind.Back:
                _backHistory.RemoveAt(_backHistory.Count - 1);
                if (HasCurrentPath)
                    _forwardHistory.Add(CurrentPath);
                break;
            case NavigationKind.Forward:
                _forwardHistory.RemoveAt(_forwardHistory.Count - 1);
                if (HasCurrentPath)
                    _backHistory.Add(CurrentPath);
                break;
        }

        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
        BackCommand.NotifyCanExecuteChanged();
        ForwardCommand.NotifyCanExecuteChanged();
    }

    private enum NavigationKind
    {
        Initial,
        New,
        Back,
        Forward,
        Refresh
    }
}
