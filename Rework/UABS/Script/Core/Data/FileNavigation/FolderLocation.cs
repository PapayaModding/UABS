namespace UABS.Data
{
    public class FolderLocation : Location
    {
        public FsNode Folder { get; }
        public override string Name => Folder.Name;
        public override FsNode? ParentNode => Folder.Parent;

        public FolderLocation(FsNode folder)
        {
            Folder = folder;
        }
    }
}