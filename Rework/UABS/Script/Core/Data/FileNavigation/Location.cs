namespace UABS.Data
{
    public abstract class Location
    {
        public abstract string Name { get; }
        public abstract FsNode? ParentNode { get; }
    }
}