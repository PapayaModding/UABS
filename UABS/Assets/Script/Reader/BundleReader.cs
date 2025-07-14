using System;
using System.IO;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.Dispatcher;
using UABS.Assets.Script.Event;

namespace UABS.Assets.Script.Reader
{
    public class BundleReader
    {
        private readonly AssetsManager _assetsManager;
        private readonly EventDispatcher _dispatcher;
        private ReadAssetsFromBundle _readAssetsFromBundle;

        public BundleReader(AssetsManager assetsManager, EventDispatcher dispatcher)
        {
            _assetsManager = assetsManager;
            _dispatcher = dispatcher;
            _readAssetsFromBundle = new ReadAssetsFromBundle(_assetsManager);
        }

        public (BundleFileInstance, AssetsFileInstance) ReadBundle(string path)
        {
            if (Path.GetExtension(path).Equals(".assets", StringComparison.OrdinalIgnoreCase))
            {
                // Load as standalone .assets file
                var assetsInst = _assetsManager.LoadAssetsFile(path, true);
                _dispatcher.Dispatch(new BundleReadEvent(null, path, assetsInst));
                return (null, assetsInst);
            }
            else
            {
                // Load as bundle
                var bunInst = _assetsManager.LoadBundleFile(path, true);
                var assetsInst = _readAssetsFromBundle.ReadAssetsFileInst(bunInst);
                _dispatcher.Dispatch(new BundleReadEvent(bunInst, path, assetsInst));
                return (bunInst, assetsInst);
            }
        }

        public (BundleFileInstance, AssetsFileInstance) ReadBundle4Derive(string path, string overridePath)
        {
            if (Path.GetExtension(path).Equals(".assets", StringComparison.OrdinalIgnoreCase))
            {
                // Load as standalone .assets file
                var assetsInst = _assetsManager.LoadAssetsFile(path, true);
                _dispatcher.Dispatch(new BundleRead4DeriveEvent(null, path, overridePath, assetsInst));
                return (null, assetsInst);
            }
            else
            {
                // Load as bundle
                var bunInst = _assetsManager.LoadBundleFile(path, true);
                var assetsInst = _readAssetsFromBundle.ReadAssetsFileInst(bunInst);
                _dispatcher.Dispatch(new BundleRead4DeriveEvent(bunInst, path, overridePath, assetsInst));
                return (bunInst, assetsInst);
            }
        }
    }
}