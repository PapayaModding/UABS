using Avalonia.Controls;
using System.Threading.Tasks;

namespace UABS.Wrapper
{
    public interface IFileBrowser
    {
        Task<string[]> OpenFilePanelAsync(Window owner, string title, string[] extensions);
        Task<string[]> OpenFilePanelAsync(Window owner, string title);
        Task<string[]> OpenFolderPanelAsync(Window owner, string title);
        Task<string?> SaveFilePanelAsync(Window owner, string title, string defaultName, string extension);
    }
}