namespace UABS.Assets.Script.Event
{
    public class RequestSearchEvent : AppEvent
    {
        public string ReadFromCachePath { get; }

        public string SearchKeywords { get; }

        public string ExcludeKeywords { get; }

        public bool ExactMatch { get; } // ! For image search only

        public bool SearchMemo { get; }

        public RequestSearchEvent(string readFromCachePath,
                                    string searchKeywords,
                                    string excludeKeywords,
                                    bool exactMatch = false,
                                    bool searchMemo = false)
        {
            ReadFromCachePath = readFromCachePath;
            SearchKeywords = searchKeywords;
            ExcludeKeywords = excludeKeywords;
            ExactMatch = exactMatch;
            SearchMemo = searchMemo;
        }
    }
}