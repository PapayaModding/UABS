using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class FolderRead4DeriveEvent : AppEvent
    {
        public string FolderPath { get; }

        public List<DeriveInfo> DeriveInfos { get; }

        public string OverrideBundlePath { get; }

        public FolderRead4DeriveEvent(string folderPath,
                                            List<DeriveInfo> deriveInfos = null,
                                            string overrideBundlePath = "")
        {
            FolderPath = folderPath;
            DeriveInfos = deriveInfos;
            OverrideBundlePath = overrideBundlePath;
        }
    }
}