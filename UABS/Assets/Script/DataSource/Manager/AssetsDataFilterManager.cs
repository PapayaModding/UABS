using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
using UABS.Assets.Script.Event;
using UABS.Assets.Script.EventListener;

namespace UABS.Assets.Script.Misc
{
    public class AssetsDataFilterManager : IAppEventListener
    {
        private Dictionary<AssetClassID, bool> _isClassIDFiltered = new();
        
        public List<ParsedAssetAndEntry> originalEntryInfos;

        public Action<List<ParsedAssetAndEntry>> SetEntryInfosCallBack;

        public List<ParsedAssetAndEntry> FilterEntryInfoByType(List<ParsedAssetAndEntry> entryInfos)
        {
            return entryInfos.Where(x => !_isClassIDFiltered.ContainsKey(x.assetEntryInfo.classID) ||
                                            (_isClassIDFiltered.ContainsKey(x.assetEntryInfo.classID) &&
                                            !_isClassIDFiltered[x.assetEntryInfo.classID])).ToList();
        }

        public void OnEvent(AppEvent e)
        {
            if (e is FilterTypeEvent fte)
            {
                _isClassIDFiltered = fte.IsClassIDFiltered;
                SetEntryInfosCallBack(FilterEntryInfoByType(originalEntryInfos));
            }
        }
    }
}
