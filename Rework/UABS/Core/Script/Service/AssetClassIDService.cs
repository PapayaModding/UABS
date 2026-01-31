using AssetsTools.NET.Extra;

namespace UABS.Service
{
    public class AssetClassIDService
    {
        public AssetClassID ClassID { get; }

        public AssetClassIDService(AssetClassID classID)
        {
            ClassID = classID;
        }

        public override string ToString()
        {
            return ClassID.ToString();
        }

        public int ToInt()
        {
            return (int)ClassID;
        }
    }
}