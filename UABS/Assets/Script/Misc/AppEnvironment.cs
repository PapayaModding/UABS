using AssetsTools.NET.Extra;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Reader.BundlesRead;

namespace UABS.Assets.Script.Misc
{
    public class AppEnvironment
    {
        public EventDispatcher Dispatcher { get; } = new();
        public AssetsManager AssetsManager { get; } = new();
        public AssetReader AssetReader { get; }
        public AppWrapper Wrapper { get; } = new();
        public BundleReader BundleReader { get; }

        public AppEnvironment()
        {
            AssetsManager.LoadClassPackage(PredefinedPaths.ClassDataPath);
            AssetReader = new(AssetsManager);
            BundleReader = new(AssetsManager, Dispatcher);
        }
    }
}