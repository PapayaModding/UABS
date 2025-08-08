namespace UABS.Assets.Script.DataStruct._New
{
    public enum UnityAssetContainer
    {
        UnityFSBundle,     // Modern bundle format (Unity 5+)
        UnityRawBundle,    // Old bundle format (Unity 3.x–4.x)
        CabContainer,      // CAB-xxxx container inside older bundles
        AssetsFile,        // .assets, sharedassets, levelX.assets, globalgamemanagers
        ResourceStream,    // .resS
        LegacyResource,    // .resource (Unity 4.x legacy)
        Unknown
    }
}
