namespace UABS.Assets.Script.DataStruct
{
    public enum FolderViewType
    {
        Folder,
        Bundle
    }

    public class FolderViewInfo
    {
        public FolderViewType type;
        public string name;
        public long size;
        public string path;

        public string overrideDerivePath;
        public bool IsDeriveFolder => !string.IsNullOrEmpty(overrideDerivePath);
    }
}