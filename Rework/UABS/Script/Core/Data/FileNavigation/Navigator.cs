using System.Collections.Generic;
using UABS.Util;

namespace UABS.Data
{
    public class Navigator
    {
        private readonly List<Location> stack = new();
        private int currentIndex = -1;

        public Location? Current => currentIndex >= 0 ? stack[currentIndex] : null;

        public void Push(Location loc)
        {
            // Remove anything ahead if we navigated back
            if (currentIndex < stack.Count - 1)
                stack.RemoveRange(currentIndex + 1, stack.Count - currentIndex - 1);

            stack.Add(loc);
            currentIndex = stack.Count - 1;
        }

        // Back moves to previous history location
        public void Back()
        {
            if (currentIndex > 0)
                currentIndex--;
        }

        // Up moves forward to last child cached folder, or backward to parent
        public void Up()
        {
            if (Current == null) return;

            FsNode? currentNode = Current is FolderLocation f ? f.Folder
                            : Current is CachedLocation c ? c.Cached
                            : null;
            if (currentNode == null) return;

            // 1️⃣ Move forward to child cached folder
            for (int i = currentIndex + 1; i < stack.Count; i++)
            {
                if (stack[i] is CachedLocation child && child.ParentNode == currentNode)
                {
                    currentIndex = i;
                    return;
                }
            }

            // 2️⃣ Move backward to most recent parent
            var parentNode = Current.ParentNode;
            if (parentNode == null) return; // root

            for (int i = currentIndex - 1; i >= 0; i--)
            {
                if (stack[i] is FolderLocation pf && pf.Folder == parentNode)
                {
                    currentIndex = i;
                    return;
                }
                else if (stack[i] is CachedLocation pc && pc.ParentNode == parentNode)
                {
                    currentIndex = i;
                    return;
                }
            }

            // 3️⃣ Nothing found → Up has no effect
        }

        // Debug: print stack
        public void PrintStack()
        {
            Log.Info("Navigation Stack:");
            for (int i = 0; i < stack.Count; i++)
            {
                var loc = stack[i];
                string type = loc is FolderLocation ? "Folder" : "Cached";
                string marker = i == currentIndex ? "->" : "  ";
                Log.Info($"{marker} {loc.Name} ({type})");
            }
            Log.Info("");
        }
    }
}