using System.Threading.Tasks;

namespace UABS.Wrapper
{
    public interface IFileBrowser
    {
        Task<string[]> OpenFilePanelAsync(string title, string[] extensions);
        Task<string[]> OpenFilePanelAsync(string title);
        Task<string[]> OpenFolderPanelAsync(string title);
        Task<string?> SaveFilePanelAsync(string title, string defaultName, string extension);
    }
}