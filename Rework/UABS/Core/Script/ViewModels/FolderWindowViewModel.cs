using System.Collections.Generic;
using System.Collections.ObjectModel;
using UABS.App;
using UABS.Data;

namespace UABS.ViewModel
{
    public class FolderWindowViewModel
    {
        private FolderWindow FolderWindow { get; }
        public ObservableCollection<FolderWindowEntry> Items { get; } = new();
        // public ObservableCollection<FolderWindowEntry> SelectedItems { get; } = new();

        public FolderWindowViewModel(FolderWindow folderWindow)
        {
            FolderWindow = folderWindow;
            FolderWindow.OnNewPathOpened += lst =>
            {
                Items.Clear();
                List<string> alt_row_bg_colors = FolderWindowEntry.Alternative_Row_Background_Colors;
                for (int i = 0; i < lst.Count; i++)
                {
                    var entry = lst[i];
                    entry.RowBackground = alt_row_bg_colors[i % alt_row_bg_colors.Count];
                    Items.Add(entry);
                }
            };
        }

        public void OpenNewPath(string path, string? cachedFolder = null)
        {
            FolderWindow.OpenNewFolder(path, cachedFolder);
        }

        // public void SelectItem(FolderWindowEntry entry)
        // {
        //     if (!SelectedItems.Contains(entry))
        //         SelectedItems.Add(entry);

        //     UpdateRowBackgrounds();
        // }

        // public void DeselectItem(FolderWindowEntry entry)
        // {
        //     if (SelectedItems.Contains(entry))
        //         SelectedItems.Remove(entry);

        //     UpdateRowBackgrounds();
        // }

        // private void UpdateRowBackgrounds()
        // {
        //     for (int i = 0; i < Items.Count; i++)
        //     {
        //         var entry = Items[i];
        //         if (SelectedItems.Contains(entry))
        //             entry.RowBackground = "#00FF00";
        //         else
        //             entry.RowBackground = i % 2 == 0 ? "#d6ffd7" : "#FFFFFF";
        //     }
        // }
    }
}