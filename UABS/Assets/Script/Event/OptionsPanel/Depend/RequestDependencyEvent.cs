namespace UABS.Assets.Script.Event
{
    public class RequestDependencyEvent : AppEvent
    {
        public string ReadFromCachePath { get; }

        public RequestDependencyEvent(string readFromCachePath)
        {
            ReadFromCachePath = readFromCachePath;
        }
    }
}