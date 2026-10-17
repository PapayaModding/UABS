using System.Threading;
using System.Threading.Tasks;

namespace UABS.Services;

public interface IUserConfirmationService
{
    Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default);
}
