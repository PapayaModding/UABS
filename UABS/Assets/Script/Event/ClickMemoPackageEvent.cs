namespace UABS.Assets.Script.Event
{
    public class ClickMemoPackageEvent : AppEvent
    {
        public string ShortPath { get; }
        public bool IsSelected { get; }
        public ClickMemoPackageEvent(string shortPath, bool isSelected)
        {
            ShortPath = shortPath;
            IsSelected = isSelected;
        }
    }
}