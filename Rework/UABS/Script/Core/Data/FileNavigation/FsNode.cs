using System.Collections.Generic;

namespace UABS.Data
{
    public sealed class FsNode
    {
        public string Name { get; }

        public FsNode(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
}