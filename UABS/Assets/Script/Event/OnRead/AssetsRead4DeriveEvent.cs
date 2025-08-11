using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.Event
{
    // Bundles in dependency folder / search folder
    public class AssetsRead4DeriveEvent : AppEvent
    {
        public BundleFileInstance Bundle { get; }
        public AssetsFileInstance AssetsInst { get; }
        public string FilePath { get; }
        public string OverridePath { get; }

        public AssetsRead4DeriveEvent(BundleFileInstance bundle, string filePath, string overridePath, AssetsFileInstance assetsInst)
        {
            Bundle = bundle;
            FilePath = filePath;
            OverridePath = overridePath;
            AssetsInst = assetsInst;
        }
    }
}