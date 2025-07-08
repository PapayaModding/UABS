using AssetsTools.NET.Extra;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Event;

namespace UABS.Assets.Script.Reader
{
    public class BundleReader
    {
        private readonly AssetsManager _assetsManager;
        private readonly EventDispatcher _dispatcher;

        public BundleReader(AssetsManager assetsManager, EventDispatcher dispatcher)
        {
            _assetsManager = assetsManager;
            _dispatcher = dispatcher;
        }

        public BundleFileInstance ReadBundle(string path)
        {
            BundleFileInstance bunInst = _assetsManager.LoadBundleFile(@path, true);
            _dispatcher.Dispatch(new BundleReadEvent(bunInst, path));
            return bunInst;
        }

        public BundleFileInstance ReadBundle4Derive(string path, string overridePath)
        {
            BundleFileInstance bunInst = _assetsManager.LoadBundleFile(@path, true);
            _dispatcher.Dispatch(new BundleRead4DeriveEvent(bunInst, path, overridePath));
            return bunInst;
        }
    }
}