using System;
using System.IO;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.DataStruct._New
{
    public class FileInstanceLike
    {
        private readonly AssetsFileInstance _assetsInst;
        private readonly BundleFileInstance _bunInst;
        private readonly Stream _stream;
        private readonly int _typeIndex;

        public FileInstanceLike(AssetsFileInstance assetsInst)
        {
            _assetsInst = assetsInst;
            _bunInst = null;
            _stream = null;
            _typeIndex = 0;
        }

        public FileInstanceLike(BundleFileInstance bunInst)
        {
            _assetsInst = null;
            _bunInst = bunInst;
            _stream = null;
            _typeIndex = 1;
        }

        public FileInstanceLike(Stream stream)
        {
            _assetsInst = null;
            _bunInst = null;
            _stream = stream;
            _typeIndex = 2;
        }

        public bool IsAssetsFileInstance => _typeIndex == 0;
        public bool IsBundleFileInstance => _typeIndex == 1;
        public bool IsStream => _typeIndex == 2;

        public AssetsFileInstance AsAssetsFileInstance => IsAssetsFileInstance ? _assetsInst : throw new InvalidOperationException("Not an assets file instance.");
        public BundleFileInstance AsBundleFileInstace => IsBundleFileInstance ? _bunInst : throw new InvalidOperationException("Not a bundle file instance.");
        public Stream AsStream => IsStream ? _stream : throw new InvalidOperationException("Not a stream.");
    }
}
