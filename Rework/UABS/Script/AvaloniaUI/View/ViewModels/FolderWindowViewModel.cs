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
        
        public FolderWindowViewModel(FolderWindow folderWindow)
        {
            FolderWindow = folderWindow;
        }

        public void Refresh(string path, string? cachedFolder = null)
        {
            var newEntries = FolderWindowService.GetEntries(path, cachedFolder);
            ItemsSource.Clear();

            foreach (var e in newEntries)
                ItemsSource.Add(e);

            FolderWindow.Items = ItemsSource.ToList();
        }
    }
}