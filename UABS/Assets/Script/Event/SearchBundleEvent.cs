using System.Collections.Generic;

namespace UABS.Assets.Script.Event
{
    public class SearchBundleEvent : AppEvent
    {
        public HashSet<string> IncludePaths { get; }
        
        public SearchBundleEvent(HashSet<string> includePaths)
        {
            IncludePaths = includePaths;
        }
    }
}