namespace UABS.Assets.Script.DataStruct
{
    public enum MemoInheritMode
    {
        Safe,  // inherit only if [TO] is empty
        Overwrite, // always inherit if [FROM] is not empty
        Force // always inherit [FROM] for any match
    }
}