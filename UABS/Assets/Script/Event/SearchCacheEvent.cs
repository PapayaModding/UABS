using System.Collections.Generic;

namespace UABS.Assets.Script.Event
{
    public class SearchCacheEvent : AppEvent
    {
        public HashSet<string> IncludePaths { get; }
        
        public SearchCacheEvent(HashSet<string> includePaths)
        {
            IncludePaths = includePaths;
        }
    }
}