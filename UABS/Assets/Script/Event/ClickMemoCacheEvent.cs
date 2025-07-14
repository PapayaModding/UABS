namespace UABS.Assets.Script.Event
{
    public class ClickMemoCacheEvent : AppEvent
    {
        public string ShortPath { get; }
        public bool IsSelected { get; }
        public ClickMemoCacheEvent(string shortPath, bool isSelected)
        {
            ShortPath = shortPath;
            IsSelected = isSelected;
        }
    }
}