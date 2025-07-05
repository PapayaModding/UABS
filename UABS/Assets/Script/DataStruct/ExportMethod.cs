namespace UABS.Assets.Script.DataStruct
{
    public enum ExportType
    {
        All,
        FilterByType,
        Selected
    }

    public enum ExportKind
    {
        Asset,
        Dump
    }

    public struct ExportMethod
    {
        public ExportType exportType;
        public ExportKind exportKind;
        public string destination;
    }
}