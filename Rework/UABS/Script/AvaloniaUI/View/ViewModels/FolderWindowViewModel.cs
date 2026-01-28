using System.Collections.ObjectModel;
using System.Linq;
using UABS.App;
using UABS.Data;
using UABS.Service;

namespace UABS.AvaloniaUI
{
    public class FolderWindowViewModel
    {
        public FolderWindow FolderWindow { get; }
        public ObservableCollection<FolderWindowEntry> ItemsSource { get; } = new();
        public ObservableCollection<FolderWindowEntry> SelectedItems { get; } = new();

        public FolderWindowViewModel(FolderWindow folderWindow)
        {
            FolderWindow = folderWindow;
        }

        public void Refresh(string path, string? cachedFolder = null)
        {
            var newEntries = FolderWindowService.GetEntries(path, cachedFolder);
            ItemsSource.Clear();

            for (int i = 0; i < newEntries.Count; i++)
            {
                var entry = newEntries[i];
                entry.RowBackground =
                    i % 2 == 0 ? "#d6ffd7" : "#FFFFFF";
                if (SelectedItems.Contains(entry))
                    entry.RowBackground = "#00FF00";
                ItemsSource.Add(entry);
            }

            FolderWindow.Items = ItemsSource.ToList();
        }

        public void SelectItem(FolderWindowEntry entry)
        {
            if (!SelectedItems.Contains(entry))
                SelectedItems.Add(entry);

            UpdateRowBackgrounds();
        }

        public void DeselectItem(FolderWindowEntry entry)
        {
            if (SelectedItems.Contains(entry))
                SelectedItems.Remove(entry);

            UpdateRowBackgrounds();
        }

        private void UpdateRowBackgrounds()
        {
            for (int i = 0; i < ItemsSource.Count; i++)
            {
                var entry = ItemsSource[i];
                if (SelectedItems.Contains(entry))
                    entry.RowBackground = "#00FF00";
                else
                    entry.RowBackground = i % 2 == 0 ? "#d6ffd7" : "#FFFFFF";
            }
        }
    }
}