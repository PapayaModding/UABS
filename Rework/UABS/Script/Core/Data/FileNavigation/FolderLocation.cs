namespace UABS.Data
{
    public class FolderLocation : Location
    {
        public FsNode Node { get; set; }
        public FolderLocation(FsNode node) { Node = node; }
    }
}