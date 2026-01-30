namespace UABS.Data
{
    public abstract class Location
    {
        public FsNode Node { get; }

        protected Location(FsNode node)
        {
            Node = node;
        }

        public abstract string Kind { get; }
    }
}