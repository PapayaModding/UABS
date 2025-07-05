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
        private string _realPath = "";
        public string RealPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_realPath))
                    return path;
                return _realPath;
            }
            set
            {
                _realPath = value;
            }
        }
        public bool IsDependencyFolder => !string.IsNullOrEmpty(_realPath);
    }
}