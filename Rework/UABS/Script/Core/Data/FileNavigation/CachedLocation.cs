namespace UABS.Data
{
    public class CachedLocation : Location
    {
        public CachedFolder Cached { get; set; }
        public CachedLocation(CachedFolder cached) { Cached = cached; }
    }
}