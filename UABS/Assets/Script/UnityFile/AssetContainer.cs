using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UABS.Assets.Script.UnityFile
{
    // From UABEA
    public class AssetContainer
    {
        public long PathId { get; }
        public int ClassId { get; }
        public ushort MonoId { get; }
        public uint Size { get; }
        public string FilePath { get; }
        public AssetsFileInstance FileInstance { get; }
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public AssetTypeValueField? BaseValueField { get; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        public long FilePosition { get; private set; }
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public AssetsFileReader? FileReader { get; private set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        public AssetPPtr AssetPPtr => new(FileInstance.path, 0, PathId);
        public bool HasValueField => BaseValueField != null;

        public AssetContainer(AssetsManager assetsManager, string filePath, long pathID)
        {
            
        }
    }
}
