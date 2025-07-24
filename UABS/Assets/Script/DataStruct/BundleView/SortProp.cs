namespace UABS.Assets.Script.DataStruct
{
    public enum SortByType
    {
        None,
        Name,
        Type,
        PathID
    }

    public enum SortOrder
    {
        None,
        Up,
        Down
    }

    public struct SortProp
    {
        public SortByType sortByType;
        public SortOrder sortOrder;
    }
}