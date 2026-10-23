using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using UABS.PackageV2;
using UABS.Services;

namespace UABS.ViewModels;

/// <summary>Retained creation form, independent of the V1 editor's working clone.</summary>
public sealed class V2CreationViewModel : ViewModelBase, IDisposable
{
    private readonly IFilePickerService _picker;
    private readonly V2PackageCreationService _service;
    private readonly V2FolderDiscoveryService _folderDiscovery;
    private readonly Func<V2CreationResult, Task<string?>> _register;
    private readonly Func<string, Task> _open;
    private CancellationTokenSource? _operation;
    private CancellationTokenSource? _selection;
    private TaskCompletionSource<bool>? _approval;
    private bool _isOpen, _busy, _awaiting, _disposed, _selecting;
    private int _selectedSourceIndex = -1;
    private string _name = "New Package", _directory = "", _status = "Select sources and destination.", _details = "";
    private IReadOnlyList<string> _sources = Array.Empty<string>();
    private readonly object _progressGate = new();
    private PackageBuildProgress? _latest;
    private bool _posted;
    private bool _folderPosted;
    private (V2FolderProgress Progress, CancellationTokenSource Owner)? _folderProgress;
    private bool _includeSubfolders = true;
    public V2CreationViewModel(IFilePickerService picker, Func<V2CreationResult, Task<string?>> register,
        Func<string, Task> open, V2PackageCreationService? service = null, V2FolderDiscoveryService? folderDiscovery = null)
    {
        _picker = picker; _register = register; _open = open; _service = service ?? new();
        _folderDiscovery = folderDiscovery ?? new();
        AddSourcesCommand = new AsyncCommand(AddSourcesAsync, () => CanEditSelection);
        AddFolderCommand = new AsyncCommand(AddFolderAsync, () => CanEditSelection);
        ClearSourcesCommand = new AsyncCommand(() => { Sources = Array.Empty<string>(); SelectedSourceIndex = -1; return Task.CompletedTask; }, () => CanEditSelection);
        RemoveSelectedSourceCommand = new AsyncCommand(() =>
        {
            var next = new List<string>(Sources); int index = SelectedSourceIndex; next.RemoveAt(index);
            Sources = next; SelectedSourceIndex = Math.Min(index, next.Count - 1); return Task.CompletedTask;
        }, () => CanEditSelection && SelectedSourceIndex >= 0 && SelectedSourceIndex < Sources.Count);
        DestinationCommand = new AsyncCommand(async () => { var path = await _picker.PickPackageDestinationAsync(); if (path is not null) Directory = path; }, () => CanEditSelection);
        CreateCommand = new AsyncCommand(CreateAsync, () => CanEditSelection && Result?.Published != true);
        CancelCommand = new AsyncCommand(() => { _selection?.Cancel(); _operation?.Cancel(); _approval?.TrySetResult(false); return Task.CompletedTask; }, () => IsBusy || IsSelecting);
        AcknowledgeCommand = new AsyncCommand(() => { _approval?.TrySetResult(true); return Task.CompletedTask; }, () => AwaitingAcknowledgement);
        CloseCommand = new AsyncCommand(() => { IsOpen = false; return Task.CompletedTask; }, () => CanEditSelection);
        OpenPackageCommand = new AsyncCommand(async () => { if (Result?.Verified == true) await _open(Result.Build!.PublishedPath!); }, () => !IsBusy && Result?.Verified == true);
    }
    public AsyncCommand AddSourcesCommand { get; }
    public AsyncCommand AddFolderCommand { get; }
    public bool IncludeSubfolders { get => _includeSubfolders; set => SetField(ref _includeSubfolders, value); }
    public AsyncCommand ClearSourcesCommand { get; }
    public AsyncCommand RemoveSelectedSourceCommand { get; }
    public AsyncCommand DestinationCommand { get; }
    public AsyncCommand CreateCommand { get; }
    public AsyncCommand CancelCommand { get; }
    public AsyncCommand AcknowledgeCommand { get; }
    public AsyncCommand CloseCommand { get; }
    public AsyncCommand OpenPackageCommand { get; }
    public V2CreationResult? Result { get; private set; }
    public bool IsOpen { get => _isOpen; private set => SetField(ref _isOpen, value); }
    public bool IsBusy { get => _busy; private set { SetField(ref _busy, value); OnPropertyChanged(nameof(CanEditSelection)); NotifyCommands(); } }
    public bool IsSelecting { get => _selecting; private set { SetField(ref _selecting, value); OnPropertyChanged(nameof(CanEditSelection)); NotifyCommands(); } }
    public bool CanEditSelection => !IsBusy && !IsSelecting && !_disposed;
    public bool AwaitingAcknowledgement { get => _awaiting; private set { SetField(ref _awaiting, value); NotifyCommands(); } }
    public string Name { get => _name; set { SetField(ref _name, value); OnPropertyChanged(nameof(DestinationPath)); } }
    public string Directory { get => _directory; set { SetField(ref _directory, value); OnPropertyChanged(nameof(DestinationPath)); } }
    public IReadOnlyList<string> Sources { get => _sources; private set { SetField(ref _sources, value); OnPropertyChanged(nameof(SelectedFileCount)); OnPropertyChanged(nameof(SelectionSummary)); RemoveSelectedSourceCommand.NotifyCanExecuteChanged(); } }
    public int SelectedFileCount => Sources.Count;
    public string SelectionSummary => $"Selected files: {SelectedFileCount:N0}. Duplicate physical selections are rejected at Create.";
    public int SelectedSourceIndex { get => _selectedSourceIndex; set { SetField(ref _selectedSourceIndex, value); RemoveSelectedSourceCommand.NotifyCanExecuteChanged(); } }
    public string DestinationPath { get { try { return V2PackageCreationService.Destination(new(Name, Directory, Sources)); } catch (Exception e) { return e.Message; } } }
    public string Status { get => _status; private set => SetField(ref _status, value); }
    public string Details { get => _details; private set => SetField(ref _details, value); }
    public void Show()
    {
        if (!IsOpen && Result?.Published == true)
        { Result = null; Status = "Select sources and destination for another new package."; Details = ""; NotifyCommands(); }
        IsOpen = true;
    }
    public async Task AddSourcesAsync()
    {
        if (!CanEditSelection) return;
        using var pending = new CancellationTokenSource(); _selection = pending; IsSelecting = true;
        try
        {
            var paths = await _picker.PickUnityFilesAsync(pending.Token);
            pending.Token.ThrowIfCancellationRequested();
            if (paths.Count == 0) return;
            Status = "Adding selected files...";
            var previous = Sources;
            var next = await Task.Run(() =>
            {
                var result = new List<string>();
                foreach (var path in previous.Concat(paths)) { pending.Token.ThrowIfCancellationRequested(); result.Add(path); }
                return result.ToArray();
            }, pending.Token);
            pending.Token.ThrowIfCancellationRequested();
            Sources = next; Status = SelectionSummary;
        }
        catch (OperationCanceledException) { Status = "Selection cancelled; existing sources preserved."; }
        catch (Exception e) { Status = "Cannot add sources: " + e.Message; }
        finally { _selection = null; IsSelecting = false; }
    }
    public async Task AddFolderAsync()
    {
        if (!CanEditSelection) return;
        using var pending = new CancellationTokenSource(); _selection = pending; IsSelecting = true;
        bool recursive = IncludeSubfolders;
        try
        {
            string? folder = await _picker.PickUnityFolderAsync(pending.Token);
            pending.Token.ThrowIfCancellationRequested();
            if (folder is null) return;
            Status = "Discovering Unity sources...";
            var previous = Sources;
            var result = await _folderDiscovery.DiscoverAsync(folder, recursive, previous,
                new InlineProgress<V2FolderProgress>(p => ReportFolder(p, pending)), pending.Token);
            pending.Token.ThrowIfCancellationRequested();
            Details = result.Summary + "\n" + (result.FirstError is null ? "" : "First error: " + result.FirstError + "\n")
                + string.Join("\n", result.Details);
            if (!result.Succeeded) { Status = "Folder discovery failed; existing selection preserved. " + result.Summary; return; }
            if (result.Sources.Count == 0) { Status = result.Summary + " " + SelectionSummary; return; }
            var next = await Task.Run(() =>
            {
                var merged = new List<string>();
                foreach (var path in previous.Concat(result.Sources)) { pending.Token.ThrowIfCancellationRequested(); merged.Add(path); }
                return merged.ToArray();
            }, pending.Token);
            pending.Token.ThrowIfCancellationRequested();
            Sources = next; Status = result.Summary + " " + SelectionSummary;
        }
        catch (OperationCanceledException) { Status = "Folder discovery cancelled; existing selection preserved."; }
        catch (Exception e) { Status = "Folder discovery failed; existing selection preserved: " + e.Message; }
        finally { _selection = null; IsSelecting = false; }
    }
    private void ReportFolder(V2FolderProgress progress, CancellationTokenSource owner)
    {
        lock (_progressGate)
        {
            _folderProgress = (progress, owner);
            if (_folderPosted) return;
            _folderPosted = true;
        }
        Dispatcher.UIThread.Post(() =>
        {
            (V2FolderProgress Progress, CancellationTokenSource Owner)? item;
            lock (_progressGate) { item = _folderProgress; _folderPosted = false; }
            if (!_disposed && IsSelecting && item is { } current && ReferenceEquals(_selection, current.Owner)
                && !current.Owner.IsCancellationRequested)
                Status = $"Discovering Unity sources: {current.Progress.Scanned:N0} files checked; {current.Progress.Supported:N0} new sources.";
        });
    }
    private void Report(PackageBuildProgress progress)
    {
        lock (_progressGate)
        {
            _latest = progress;
            if (_posted) return;
            _posted = true;
        }
        Dispatcher.UIThread.Post(() =>
        {
            PackageBuildProgress? p;
            lock (_progressGate) { p = _latest; _posted = false; }
            if (!_disposed && IsBusy && !AwaitingAcknowledgement && p is not null)
                Status = $"{p.Stage} · source {p.SourceNumber}/{p.SourceCount} · {p.Assets:N0} captured assets (current phase)";
        });
    }
    private async Task<bool> Acknowledge(PartialBuildSummary summary, CancellationToken token)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _approval = completion; AwaitingAcknowledgement = true;
            Status = "Incomplete asset-table coverage — publication paused. Explicit acknowledgement required.";
            Details = $"{summary.Assets:N0} captured assets; {summary.Issues:N0} issues. First 50 issue details:\n" + string.Join("\n", summary.Details);
        });
        try { return await completion.Task.WaitAsync(token).ConfigureAwait(false); }
        finally { await Dispatcher.UIThread.InvokeAsync(() => { _approval = null; AwaitingAcknowledgement = false; }); }
    }
    public async Task CreateAsync()
    {
        if (!CanEditSelection || Result?.Published == true) return;
        _operation = new(); IsBusy = true; Result = null; Details = "";
        try
        {
            Result = await _service.CreateAsync(new(Name, Directory, Sources), new InlineProgress<PackageBuildProgress>(Report), Acknowledge, _operation.Token);
            Status = Result.Message;
            if (Result.Build?.RetainedStagingPath is string retained)
                Details = "Owned staging retained for recovery (do not delete journals): " + retained + "\n" + Details;
            if (Result.Published)
            {
                Details = "Saved path: " + Result.Build!.PublishedPath + "\n" + Details;
                if (Result.Verified)
                {
                    Status = "Registering new package...";
                    string? error;
                    try { error = await _register(Result); } catch (Exception e) { error = e.Message; }
                    Status = Result.Message + (error is null ? " Registered; Open Package to browse." : " Saved and verified, but registration failed: " + error + ". Retry using Open V2.");
                }
            }
        }
        catch (Exception e) { Status = "Creation unavailable: " + e.Message; }
        finally { _operation.Dispose(); _operation = null; IsBusy = false; }
    }
    private void NotifyCommands()
    {
        foreach (var command in new[] { AddSourcesCommand, AddFolderCommand, ClearSourcesCommand, RemoveSelectedSourceCommand, DestinationCommand, CreateCommand,
            CancelCommand, AcknowledgeCommand, CloseCommand, OpenPackageCommand }) command?.NotifyCanExecuteChanged();
    }
    public void Dispose() { _disposed = true; _selection?.Cancel(); _operation?.Cancel(); _approval?.TrySetResult(false); }
}
