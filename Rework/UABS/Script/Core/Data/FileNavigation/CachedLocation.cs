namespace UABS.Data
{
    public sealed class CachedLocation : Location
    {
        public CachedLocation(FsNode node) : base(node) { }
        public override string Kind => "Cached";
    }
}