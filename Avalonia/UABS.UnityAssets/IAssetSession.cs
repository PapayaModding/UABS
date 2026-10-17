namespace UABS.UnityAssets;

public interface IAssetSession : IDisposable
{
    AssetSessionMetadata Metadata { get; }

    IReadOnlyList<AssetEntry> Assets { get; }

    IReadOnlyList<string> Warnings { get; }

    Task<AssetDetails> GetDetailsAsync(AssetEntry asset, CancellationToken cancellationToken = default);
}
