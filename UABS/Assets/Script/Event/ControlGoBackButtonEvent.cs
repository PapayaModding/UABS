namespace UABS.Assets.Script.Event
{
    public class ControlGoBackButtonEvent : AppEvent
    {
        public bool Enable { get; }
        
        public ControlGoBackButtonEvent(bool enable)
        {
            Enable = enable;
        }
    }
}