namespace UABS.Data
{
    public class CachedLocation : Location
    {
        public FsNode Cached { get; }
        public override string Name => Cached.Name;
        public override FsNode ParentNode { get; }

        public CachedLocation(FsNode cached, FsNode parent)
        {
            Cached = cached;
            ParentNode = parent;
        }
    }
}