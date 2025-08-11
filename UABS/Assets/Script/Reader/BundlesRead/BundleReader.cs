using System;
using System.IO;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Event;

namespace UABS.Assets.Script.Reader.BundlesRead
{
    public class BundleReader
    {
        private readonly AssetsManager _assetsManager;
        private readonly EventDispatcher _dispatcher;
        private readonly AssetsReader _assetsReader;

        public BundleReader(AssetsManager assetsManager, EventDispatcher dispatcher)
        {
            _assetsManager = assetsManager;
            _dispatcher = dispatcher;
            _assetsReader = new AssetsReader(_assetsManager);
        }

        public (BundleFileInstance, AssetsFileInstance) ReadBundle(string path)
        {
            if (Path.GetExtension(path).Equals(".assets", StringComparison.OrdinalIgnoreCase))
            {
                // Load as standalone .assets file
                var assetsInst = _assetsReader.ReadValidAssetsFileInst(path, true);
                _dispatcher.Dispatch(new AssetsReadEvent(null, path, assetsInst));
                return (null, assetsInst);
            }
            else
            {
                // Load as bundle
                var bunInst = _assetsManager.LoadBundleFile(path, true);
                var assetsInst = _assetsReader.ReadAssetsFileInstFromBundle(bunInst);
                _dispatcher.Dispatch(new AssetsReadEvent(bunInst, path, assetsInst));
                return (bunInst, assetsInst);
            }
        }

        public (BundleFileInstance, AssetsFileInstance) ReadBundle4Derive(string path, string overridePath)
        {
            if (Path.GetExtension(path).Equals(".assets", StringComparison.OrdinalIgnoreCase))
            {
                // Load as standalone .assets file
                var assetsInst = _assetsReader.ReadValidAssetsFileInst(path, true);
                _dispatcher.Dispatch(new AssetsRead4DeriveEvent(null, path, overridePath, assetsInst));
                return (null, assetsInst);
            }
            else
            {
                // Load as bundle
                var bunInst = _assetsManager.LoadBundleFile(path, true);
                var assetsInst = _assetsReader.ReadAssetsFileInstFromBundle(bunInst);
                _dispatcher.Dispatch(new AssetsRead4DeriveEvent(bunInst, path, overridePath, assetsInst));
                return (bunInst, assetsInst);
            }
        }
    }
}