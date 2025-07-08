namespace UABS.Assets.Script.Event
{
    public class RequestSearchEvent : AppEvent
    {
        public string ReadFromCachePath { get; }

        public string SearchKeywords { get; }

        public string ExcludeKeywords { get; }

        public RequestSearchEvent(string readFromCachePath,
                                    string searchKeywords,
                                    string excludeKeywords)
        {
            ReadFromCachePath = readFromCachePath;
            SearchKeywords = searchKeywords;
            ExcludeKeywords = excludeKeywords;
        }
    }
}