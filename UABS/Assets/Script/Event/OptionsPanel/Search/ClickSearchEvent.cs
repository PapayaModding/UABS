namespace UABS.Assets.Script.Event
{
    public class ClickSearchEvent : AppEvent
    {
        public string ShortPath { get; }
        public bool IsIncluded { get; }
        public ClickSearchEvent(string shortPath, bool isIncluded)
        {
            ShortPath = shortPath;
            IsIncluded = isIncluded;
        }
    }
}