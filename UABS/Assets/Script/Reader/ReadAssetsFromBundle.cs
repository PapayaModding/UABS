using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.Reader
{
    public class ReadAssetsFromBundle
    {
        private AssetsManager _assetsManager;

        public ReadAssetsFromBundle(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public AssetsFileInstance ReadAssetsFileInst(BundleFileInstance bunInst)
        {
            AssetBundleFile bundle = bunInst.file;
            if (bunInst.file.DataIsCompressed)
            {
                MemoryStream bundleStream = new();
                bundle.Unpack(new AssetsFileWriter(bundleStream));
                bundleStream.Position = 0;

                AssetBundleFile newBundle = new();
                newBundle.Read(new AssetsFileReader(bundleStream));

                bundle.Close();
                bunInst.file = newBundle;
            }

            return _assetsManager.LoadAssetsFileFromBundle(bunInst, 0, false);
        }
    }
}