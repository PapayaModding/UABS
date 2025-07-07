using System.Collections.Generic;

namespace UABS.Assets.Script.Event
{
    public class AssetMultiSelectionEvent : AppEvent
    {
        public HashSet<long> SelectedPathIDs;
        public int StartIndex;

        public AssetMultiSelectionEvent(HashSet<long> _selectedPathIDs, int startIndex)
        {
            SelectedPathIDs = _selectedPathIDs;
            StartIndex = startIndex;
        }
    }
}