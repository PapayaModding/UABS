namespace UABS.Assets.Script.Event
{
    public class ControlSearchGoBackEvent : AppEvent
    {
        // * If the only record path is Search Cache, disable go back in option controller
        public bool Enable { get; }
        public ControlSearchGoBackEvent(bool enable)
        {
            Enable = enable;
        }
    }
}