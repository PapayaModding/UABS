using System.Collections.Generic;

namespace UABS.Data
{
    public class FsNode
    {
        public string Name { get; }
        public FsNode? Parent { get; }

        public FsNode(string name, FsNode? parent = null)
        {
            Name = name;
            Parent = parent;
        }

        // Returns A/B/C for this node
        public string GetFullPath()
        {
            if (Parent == null) return Name;
            return Parent.GetFullPath() + "/" + Name;
        }
    }
}