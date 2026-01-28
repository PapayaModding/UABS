using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UABS.Data;

namespace UABS.Service
{
    public class FolderWindowService
    {
        public static IReadOnlyList<FolderWindowEntry> GetEntries(string path, string? cachedFolder=null)
        {
            if (!Directory.Exists(path))
                return Array.Empty<FolderWindowEntry>();

            var dirs = Directory.EnumerateDirectories(path)
            .Select(d => new FolderWindowEntry(
                d,
                Path.Combine(cachedFolder ?? string.Empty, Path.GetFileName(d))
            ));

            var files = Directory.EnumerateFiles(path)
            .Select(d => new FolderWindowEntry(
                d,
                Path.Combine(cachedFolder ?? string.Empty, Path.GetFileName(d))
            ));

            return dirs.Concat(files).ToList();
        }
    }
}