using System.Collections.Generic;

namespace UABS.Data
{
    public class CachedFolder
    {
        public string Name { get; set; }
        public List<FsNode> ResultNodes { get; set; } = new();
        public CachedFolder(string name, List<FsNode> results)
        {
            Name = name;
            ResultNodes = results;
        }
    }
}