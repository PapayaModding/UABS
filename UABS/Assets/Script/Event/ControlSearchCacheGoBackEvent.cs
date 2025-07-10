namespace UABS.Assets.Script.Event
{
    public class ControlSearchCacheGoBackEvent : AppEvent
    {
        // * If the only record path is Search Cache, disable go back in option controller
        public bool Enable { get; }
        public ControlSearchCacheGoBackEvent(bool enable)
        {
            Enable = enable;
        }
    }
}