using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace UABS.Services;

public sealed class AvaloniaUserConfirmationService(Window owner) : IUserConfirmationService
{
    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool confirmed = false;
        var confirm = new Button { Content = confirmText, MinWidth = 100 };
        var cancel = new Button { Content = cancelText, MinWidth = 100 };
        var dialog = new Window
        {
            Title = title,
            Width = 480,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancel, confirm }
                    }
                }
            }
        };

        confirm.Click += (_, _) =>
        {
            confirmed = true;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();
        using CancellationTokenRegistration registration = cancellationToken.Register(
            () => Dispatcher.UIThread.Post(dialog.Close));
        await dialog.ShowDialog(owner);
        cancellationToken.ThrowIfCancellationRequested();
        return confirmed;
    }

    public async Task<UnsavedChangesChoice> ConfirmSaveDiscardCancelAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UnsavedChangesChoice choice = UnsavedChangesChoice.Cancel;
        var save = new Button { Content = "Save", MinWidth = 100, IsDefault = true };
        var discard = new Button { Content = "Discard", MinWidth = 100 };
        var cancel = new Button { Content = "Cancel", MinWidth = 100, IsCancel = true };
        var dialog = new Window
        {
            Title = title,
            Width = 520,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancel, discard, save }
                    }
                }
            }
        };

        save.Click += (_, _) =>
        {
            choice = UnsavedChangesChoice.Save;
            dialog.Close();
        };
        discard.Click += (_, _) =>
        {
            choice = UnsavedChangesChoice.Discard;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();
        using CancellationTokenRegistration registration = cancellationToken.Register(
            () => Dispatcher.UIThread.Post(dialog.Close));
        await dialog.ShowDialog(owner);
        cancellationToken.ThrowIfCancellationRequested();
        return choice;
    }
}
