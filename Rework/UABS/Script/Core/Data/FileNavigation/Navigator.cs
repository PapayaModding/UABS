using System;
using System.Collections.Generic;
using UABS.Util;

namespace UABS.Data
{
    public class Navigator
    {
        private List<Location> stack = new();
        private int currentIndex = -1;

        public Location? Current => (currentIndex >= 0 && currentIndex < stack.Count) ? stack[currentIndex] : null;

        private void PushLocation(Location loc)
        {
            if (currentIndex < stack.Count - 1)
                stack.RemoveRange(currentIndex + 1, stack.Count - currentIndex - 1);
            stack.Add(loc);
            currentIndex = stack.Count - 1;
        }

        // -------------------------
        // Navigation methods
        // -------------------------
        public void OpenFolder(FsNode node)
        {
            if (node == null || !node.IsFolder) return;
            PushLocation(new FolderLocation(node));
        }

        public void OpenCachedFolder(CachedFolder cached)
        {
            if (cached == null) return;
            PushLocation(new CachedLocation(cached));
        }

        public void OpenFile(FsNode file)
        {
            if (file == null || file.IsFolder) return;
            Console.WriteLine($"Opening file: {file.GetFullPath()}");
        }

        public void Back()
        {
            if (currentIndex > 0) currentIndex--;
        }

        public void Up()
        {
            if (Current == null) return;

            if (Current is FolderLocation folder)
            {
                if (folder.Node.Parent != null)
                {
                    // find index of parent if already in stack
                    int parentIndex = -1;
                    for (int i = currentIndex; i >= 0; i--)
                    {
                        if (stack[i] is FolderLocation f && f.Node == folder.Node.Parent)
                        {
                            parentIndex = i;
                            break;
                        }
                    }
                    if (parentIndex >= 0)
                    {
                        currentIndex = parentIndex;
                    }
                    else
                    {
                        OpenFolder(folder.Node.Parent); // push if not found
                    }
                }
            }
            else if (Current is CachedLocation)
            {
                // Move to the last real folder before currentIndex
                for (int i = currentIndex - 1; i >= 0; i--)
                {
                    if (stack[i] is FolderLocation)
                    {
                        currentIndex = i; // move index instead of pushing new folder
                        break;
                    }
                }
            }
        }

        // -------------------------
        // Debug / display
        // -------------------------
        public void PrintStack()
        {
            Log.Info("Navigation Stack:");
            for (int i = 0; i < stack.Count; i++)
            {
                string marker = (i == currentIndex) ? "-> " : "   ";
                switch (stack[i])
                {
                    case FolderLocation f:
                        Log.Info($"{marker}Folder: {f.Node.GetFullPath()}");
                        break;
                    case CachedLocation c:
                        Log.Info($"{marker}Cached: {c.Cached.Name}");
                        break;
                }
            }
            Log.Info("");
        }
    }
}