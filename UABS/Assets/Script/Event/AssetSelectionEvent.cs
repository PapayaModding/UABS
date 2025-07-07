namespace UABS.Assets.Script.Event
{
    public class AssetSelectionEvent : AppEvent // On focus item's path id. (Only applicable in AssetBundle view)
    {
        public long PathID { get; }

        public int Index { get; }

        public int TotalNumOfAssets { get; }

        public bool IsHoldingShift { get; }

        public bool IsHoldingCtrl { get; }

        public bool UseJump { get; }

        public AssetSelectionEvent(long pathID, int index = 0,
                                    int totalNumOfAssets = 0,
                                    bool isHoldingShift = false,
                                    bool isHoldingCtrl = false,
                                    bool useJump = false)
        {
            PathID = pathID;
            Index = index;
            TotalNumOfAssets = totalNumOfAssets;
            IsHoldingShift = isHoldingShift;
            IsHoldingCtrl = isHoldingCtrl;
            UseJump = useJump;
        }
    }
}