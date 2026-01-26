using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace UABS.Script.Wrapper.FileBrowser
{
    public class AvaloniaFileBrowserWrapper : IFileBrowser
    {
        public async Task<string[]> OpenFilePanelAsync(Window owner, string title, string[] extensions)
        {
            var options = new FilePickerOpenOptions
            {
                Title = title,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Supported Files") { Patterns = extensions.Select(e => $"*.{e}").ToArray() }
                }
            };

            var files = await owner.StorageProvider.OpenFilePickerAsync(options);
            return files?.Select(f => f.Path.LocalPath).ToArray() ?? [];
        }

        public async Task<string[]> OpenFilePanelAsync(Window owner, string title)
        {
            var options = new FilePickerOpenOptions
            {
                Title = title
            };

            var files = await owner.StorageProvider.OpenFilePickerAsync(options);
            return files?.Select(f => f.Path.LocalPath).ToArray() ?? Array.Empty<string>();
        }

        public async Task<string[]> OpenFolderPanelAsync(Window owner, string title)
        {
            var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title
            });

            return folders?.Select(f => f.Path.LocalPath).ToArray() ?? [];
        }

        public async Task<string?> SaveFilePanelAsync(Window owner, string title, string defaultName, string extension)
        {
            var options = new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = defaultName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Supported File") { Patterns = new[] { $"*.{extension}" } }
                }
            };

            var file = await owner.StorageProvider.SaveFilePickerAsync(options);
            return file?.Path.LocalPath;
        }
    }
}