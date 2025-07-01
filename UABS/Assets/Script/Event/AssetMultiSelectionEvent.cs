using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

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