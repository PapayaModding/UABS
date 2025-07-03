using System.Collections.Generic;

namespace UABS.Assets.Script.Event
{
    public class AssetMultiSelectionEvent : AppEvent
    {
        public HashSet<long> SelectedPathIDs;

        public AssetMultiSelectionEvent(HashSet<long> _selectedPathIDs)
        {
            SelectedPathIDs = _selectedPathIDs;
        }
    }
}