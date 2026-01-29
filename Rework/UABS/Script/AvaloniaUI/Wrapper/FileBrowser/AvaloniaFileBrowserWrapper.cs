using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using UABS.Util;
using UABS.Wrapper;

namespace UABS.AvaloniaUI
{
    public class AvaloniaFileBrowserWrapper : IFileBrowser
    {
        public Window? Owner {private get; set;}

        public async Task<string[]> OpenFilePanelAsync(string title, string[] extensions)
        {
            var options = new FilePickerOpenOptions
            {
                Title = title,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Supported Files") { Patterns = extensions.Select(e => $"*.{e}").ToArray() }
                }
            };

            if (Owner is Window owner)
            {
                var files = await owner.StorageProvider.OpenFilePickerAsync(options);
                return files?.Select(f => f.Path.LocalPath).ToArray() ?? [];
            }
            else
            {
                Log.Error("Missing Window reference.", "AvaloniaFileBrowserWrapper.cs");
                return [];
            }
        }

        public async Task<string[]> OpenFilePanelAsync(string title)
        {
            var options = new FilePickerOpenOptions
            {
                Title = title
            };

            if (Owner is Window owner)
            {
                var files = await Owner.StorageProvider.OpenFilePickerAsync(options);
                return files?.Select(f => f.Path.LocalPath).ToArray() ?? Array.Empty<string>();
            }
            else
            {
                Log.Error("Missing Window reference.", "AvaloniaFileBrowserWrapper.cs");
                return [];
            }
        }

        public async Task<string[]> OpenFolderPanelAsync(string title)
        {
            IReadOnlyList<IStorageFolder>? folders = null;
            
            if (Owner is Window owner)
            {
                folders = await Owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = title
                });
            }
            else
            {
                Log.Error("Missing Window reference.", "AvaloniaFileBrowserWrapper.cs");
            }

            return folders?.Select(f => f.Path.LocalPath).ToArray() ?? [];
        }

        public async Task<string?> SaveFilePanelAsync(string title, string defaultName, string extension)
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

            if (Owner is Window owner)
            {
                var file = await Owner.StorageProvider.SaveFilePickerAsync(options);
                return file?.Path.LocalPath;
            }
            else
            {
                Log.Error("Missing Window reference.", "AvaloniaFileBrowserWrapper.cs");
                return null;
            }
        }
    }
}