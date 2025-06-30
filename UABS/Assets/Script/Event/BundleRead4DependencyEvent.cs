using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.Event
{
    public class BundleRead4DependencyEvent : AppEvent
    {
        public BundleFileInstance Bundle { get; }
        public string FilePath { get; }
        public string RealFilPath { get; }

        public BundleRead4DependencyEvent(BundleFileInstance bundle, string filePath, string realFilePath)
        {
            Bundle = bundle;
            FilePath = filePath;
            RealFilPath = realFilePath;
        }
    }
}