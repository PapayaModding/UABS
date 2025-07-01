using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class AssetSelectionEvent : AppEvent // On focus item's path id. (Only applicable in AssetBundle view)
    {
        public AssetSelectionInfo AssetSelectionInfo;

        public int TotalNumOfAssets { get; }

        public bool UseJump { get; }

        public bool IsHoldingShift { get; }

        public AssetSelectionEvent(long pathID, int currIndex = 0,
                                    int totalNumOfAssets = 0, bool useJump = false,
                                    bool isHoldingShift = false)
        {
            AssetSelectionInfo = new()
            {
                pathID = pathID,
                currIndex = currIndex
            };
            TotalNumOfAssets = totalNumOfAssets;
            UseJump = useJump;
            IsHoldingShift = isHoldingShift;
        }
    }
}