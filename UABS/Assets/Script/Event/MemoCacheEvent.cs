namespace UABS.Assets.Script.Event
{
    public class MemoCacheEvent : AppEvent
    {
        public string SelectedShortPath { get; }

        public MemoCacheEvent(string selectedShortPath)
        {
            SelectedShortPath = selectedShortPath;
        }
    }
}