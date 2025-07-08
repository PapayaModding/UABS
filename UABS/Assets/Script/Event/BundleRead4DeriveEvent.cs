using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.Event
{
    // Bundles in dependency folder / search folder
    public class BundleRead4DeriveEvent : AppEvent
    {
        public BundleFileInstance Bundle { get; }
        public string FilePath { get; }
        public string OverridePath { get; }

        public BundleRead4DeriveEvent(BundleFileInstance bundle, string filePath, string overridePath)
        {
            Bundle = bundle;
            FilePath = filePath;
            OverridePath = overridePath;
        }
    }
}