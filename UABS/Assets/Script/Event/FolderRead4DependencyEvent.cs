using System.Collections.Generic;
using UABS.Assets.Script.DataStruct;

namespace UABS.Assets.Script.Event
{
    public class FolderRead4DependencyEvent : AppEvent
    {
        public string FolderPath { get; }

        public List<DependencyInfo> DependencyInfos { get; }

        public string OverrideBundlePath { get; }

        public FolderRead4DependencyEvent(string folderPath, List<DependencyInfo> dependencyInfos = null, string overrideBundlePath = "")
        {
            FolderPath = folderPath;
            DependencyInfos = dependencyInfos;
            OverrideBundlePath = overrideBundlePath;
        }
    }
}