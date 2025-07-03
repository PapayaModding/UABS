using System;
using System.Collections.Generic;
using System.IO;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Reader
{
    public class ReadFolderContent
    {
        public List<FolderViewInfo> ReadAllReadable(string directory)
        {
            List<FolderViewInfo> result = new();
            result.AddRange(ReadAllTopDirectory(directory));
            result.AddRange(ReadAllTopBundle(directory));
            return result;
        }

        public List<FolderViewInfo> ReadAllTopDirectory(string directory)
        {
            List<FolderViewInfo> result = new();
            string[] allFolders = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

            foreach (string folder in allFolders)
            {
                result.Add(new()
                {
                    name = Path.GetFileName(folder),
                    path = folder,
                    type = FolderViewType.Folder
                });
            }
            return result;
        }

        public List<FolderViewInfo> ReadAllTopBundle(string directory)
        {
            List<FolderViewInfo> result = new();
            string[] allFiles = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string file in allFiles)
            {
                if (IsExtension(file, ".bundle") || IsExtension(file, ".ab"))
                    result.Add(new()
                    {
                        name = Path.GetFileName(file),
                        path = file,
                        type = FolderViewType.Bundle
                    });
            }

            return result;
        }

        private static bool IsExtension(string filePath, string expectedExtension)
        {
            // Compares extensions case-insensitively
            return string.Equals(Path.GetExtension(filePath), expectedExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}