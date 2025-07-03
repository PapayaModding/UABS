namespace UABS.Assets.Script.Event
{
    public class AssetSelectionEvent : AppEvent // On focus item's path id. (Only applicable in AssetBundle view)
    {
        public long PathID { get; }

        public int CurrIndex { get; }

        public int TotalNumOfAssets { get; }

        public bool UseJump { get; }

        public bool IsHoldingShift { get; }

        public AssetSelectionEvent(long pathID, int currIndex = 0,
                                    int totalNumOfAssets = 0, bool useJump = false,
                                    bool isHoldingShift = false)
        {
            PathID = pathID;
            CurrIndex = currIndex;
            TotalNumOfAssets = totalNumOfAssets;
            UseJump = useJump;
            IsHoldingShift = isHoldingShift;
        }
    }
}