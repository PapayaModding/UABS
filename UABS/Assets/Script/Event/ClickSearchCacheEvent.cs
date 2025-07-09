namespace UABS.Assets.Script.Event
{
    public class ClickSearchCacheEvent : AppEvent
    {
        public string ShortPath { get; }
        public bool IsIncluded { get; }
        public ClickSearchCacheEvent(string shortPath, bool isIncluded)
        {
            ShortPath = shortPath;
            IsIncluded = isIncluded;
        }
    }
}