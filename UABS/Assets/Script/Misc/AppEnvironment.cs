using AssetsTools.NET.Extra;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Reader;

namespace UABS.Assets.Script.Misc
{
    public class AppEnvironment
    {
        public EventDispatcher Dispatcher { get; } = new();
        public AssetsManager AssetsManager { get; } = new();
        public AssetReader AssetReader { get; }

        public AppEnvironment()
        {
            AssetReader = new(AssetsManager);
        }
    }
}