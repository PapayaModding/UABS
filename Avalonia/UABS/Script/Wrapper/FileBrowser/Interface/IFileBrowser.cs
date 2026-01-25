using Avalonia.Controls;
using System.Threading.Tasks;

namespace UABS.Script.Wrapper.FileBrowser
{
    public interface IFileBrowser
    {
        Task<string[]> OpenFilePanelAsync(Window owner, string title, string directory, string[] extensions, bool multiselect);
        Task<string[]> OpenFilePanelAsync(Window owner, string title, string directory, bool multiselect);
        Task<string[]> OpenFolderPanelAsync(Window owner, string title, string directory, bool multiselect);
        Task<string?> SaveFilePanelAsync(Window owner, string title, string directory, string defaultName, string extension);
    }
}