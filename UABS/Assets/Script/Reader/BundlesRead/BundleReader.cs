using System.Collections.Generic;
using AssetsTools.NET.Extra;
using UABS.Assets.Script.DataStruct;
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

        public (List<AssetsFileInstance>, List<ParsedAssetAndEntry>) ReadBundle(string path)
        {
            (List<AssetsFileInstance> assetsInsts, List<ParsedAssetAndEntry> parsedAE) = _assetsReader.ReadAssets(path);
            _dispatcher.Dispatch(new AssetsReadEvent(path, assetsInsts, parsedAE));
            return (assetsInsts, parsedAE);
        }

        public (List<AssetsFileInstance>, List<ParsedAssetAndEntry>) ReadBundle4Derive(string path, string overridePath)
        {
            (List<AssetsFileInstance> assetsInsts, List<ParsedAssetAndEntry> parsedAE) = _assetsReader.ReadAssets(path);
            _dispatcher.Dispatch(new AssetsRead4DeriveEvent(path, overridePath, assetsInsts, parsedAE));
            return (assetsInsts, parsedAE);
        }
    }
}