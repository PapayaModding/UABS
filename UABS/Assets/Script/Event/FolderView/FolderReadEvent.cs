namespace UABS.Assets.Script.Event
{
    public class FolderReadEvent : AppEvent
    {
        public string FolderPath { get; }

        public FolderReadEvent(string folderPath)
        {
            FolderPath = folderPath;
        }
    }
}