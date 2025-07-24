using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class ExportAssetsEvent : AppEvent
    {
        public ExportMethod ExportMethod;

        public ExportAssetsEvent(ExportMethod exportMethod)
        {
            ExportMethod = exportMethod;
        }
    }
}