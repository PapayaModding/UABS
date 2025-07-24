namespace UABS.Assets.Script.Event
{
    public class MemoPackageEvent : AppEvent
    {
        public string SelectedShortPath { get; }

        public MemoPackageEvent(string selectedShortPath)
        {
            SelectedShortPath = selectedShortPath;
        }
    }
}