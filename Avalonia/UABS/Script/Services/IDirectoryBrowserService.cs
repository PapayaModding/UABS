using System.Threading;
using System.Threading.Tasks;
using UABS.Models;

namespace UABS.Services;

public interface IDirectoryBrowserService
{
    Task<DirectorySnapshot> ReadAsync(string path, CancellationToken cancellationToken = default);
}
