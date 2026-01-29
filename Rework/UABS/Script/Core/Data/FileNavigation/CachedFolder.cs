using System.Collections.Generic;

namespace UABS.Data
{
    public class CachedFolder
    {
        public string Name { get; set; }
        public FsNode Parent { get; set; } // parent folder in the hierarchy
        public bool IsLeaf { get; set; } = true;
        public List<FsNode> ResultNodes { get; set; } = new();

        public CachedFolder(string name, FsNode parent, List<FsNode> results)
        {
            Name = name;
            Parent = parent;
            ResultNodes = results;
        }

        public string GetFullPath()
        {
            // Cached folder path is parent path + cached folder name
            if (Parent != null)
                return Parent.GetFullPath() + "/" + Name;
            return Name;
        }
    }
}