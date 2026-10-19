using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using UABS.Models;
using UABS.ViewModels;

namespace UABS.Page;

public partial class BrowserView : UserControl
{
    private const string EvenRowClass = "navigatorRowEven";
    private const string OddRowClass = "navigatorRowOdd";
    private string? _lastPointerActivationPath;
    private long _lastPointerActivationTimestamp;

    public BrowserView()
    {
        InitializeComponent();
        EntriesGrid.AddHandler(
            InputElement.KeyDownEvent,
            OnEntriesKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private void OnEntriesGridLoadingRow(object? sender, DataGridRowEventArgs eventArgs)
    {
        eventArgs.Row.Classes.Remove(EvenRowClass);
        eventArgs.Row.Classes.Remove(OddRowClass);
        eventArgs.Row.Classes.Add(eventArgs.Row.Index % 2 == 0 ? EvenRowClass : OddRowClass);
    }

    private async void OnEntriesTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (DataContext is not BrowserViewModel viewModel
            || FindEntry(eventArgs.Source) is not { } entry
            || IsRepeatedPointerActivation(entry, eventArgs))
        {
            return;
        }

        eventArgs.Handled = true;
        await viewModel.ActivateEntryAsync(entry);
    }

    private async void OnEntriesKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Enter
            || DataContext is not BrowserViewModel viewModel
            || viewModel.SelectedEntry is not { } entry)
        {
            return;
        }

        eventArgs.Handled = true;
        await viewModel.ActivateEntryAsync(entry);
    }

    private static BrowserEntry? FindEntry(object? source)
    {
        if (source is DataGridRow { DataContext: BrowserEntry rowEntry })
            return rowEntry;

        return (source as Visual)?.FindAncestorOfType<DataGridRow>()?.DataContext as BrowserEntry;
    }

    private bool IsRepeatedPointerActivation(BrowserEntry entry, TappedEventArgs eventArgs)
    {
        long now = Stopwatch.GetTimestamp();
        PointerType pointerType = eventArgs.Pointer?.Type ?? PointerType.Mouse;
        TimeSpan doubleTapTime = TopLevel.GetTopLevel(this)?.PlatformSettings?.GetDoubleTapTime(pointerType)
                                 ?? TimeSpan.FromMilliseconds(500);
        bool repeated = string.Equals(_lastPointerActivationPath, entry.FullPath, StringComparison.OrdinalIgnoreCase)
                        && Stopwatch.GetElapsedTime(_lastPointerActivationTimestamp, now) <= doubleTapTime;

        if (!repeated)
        {
            _lastPointerActivationPath = entry.FullPath;
            _lastPointerActivationTimestamp = now;
        }

        return repeated;
    }
}
