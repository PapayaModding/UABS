using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class FolderViewInfosEvent : AppEvent
    {
        public List<FolderViewInfo> FoldViewInfos { get; }

        public FolderViewInfosEvent(List<FolderViewInfo> folderViewInfos)
        {
            FoldViewInfos = folderViewInfos;
        }
    }
}