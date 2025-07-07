namespace UABS.Assets.Script.Event
{
    public class AssetSelectionJumpEvent : AppEvent
    {
        public int JumpToIndex { get; }

        public AssetSelectionJumpEvent(int jumpToIndex)
        {
            JumpToIndex = jumpToIndex;
        }
    }
}