namespace UABS.Assets.Script.Event
{
    public class FilterAllEvent : AppEvent
    {
        public bool Config;

        public FilterAllEvent(bool config)
        {
            Config = config;
        }
    }
}