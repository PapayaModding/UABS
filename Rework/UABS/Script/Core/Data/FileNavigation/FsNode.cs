using System.Collections.Generic;

namespace UABS.Data
{
    public class FsNode
    {
        public string Name { get; set; }
        public FsNode? Parent { get; set; } // null for root
        public List<FsNode> Children { get; set; } = new();
        public bool IsFolder { get; set; } = true;

        public FsNode(string name, FsNode? parent = null)
        {
            Name = name;
            Parent = parent;
            Parent?.Children.Add(this);
        }

        public string GetFullPath()
        {
            var parts = new List<string>();
            var current = this;
            while (current != null)
            {
                parts.Add(current.Name);
                current = current.Parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}