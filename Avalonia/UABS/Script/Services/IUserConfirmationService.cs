using System.Threading;
using System.Threading.Tasks;

namespace UABS.Services;

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel
}

public interface IUserConfirmationService
{
    Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default);

    Task<UnsavedChangesChoice> ConfirmSaveDiscardCancelAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnsavedChangesChoice.Cancel);
}
