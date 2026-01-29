namespace UABS.Data
{
    public sealed class FolderLocation : Location
    {
        public FolderLocation(FsNode node) : base(node) { }
        public override string Kind => "Folder";
    }
}