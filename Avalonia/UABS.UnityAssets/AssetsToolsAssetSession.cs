using AssetsTools.NET.Extra;

namespace UABS.UnityAssets;

internal sealed class AssetsToolsAssetSession : IAssetSession
{
    private readonly AssetsManager _assetsManager;
    private readonly IReadOnlyList<Stream> _ownedStreams;
    private bool _disposed;

    public AssetsToolsAssetSession(
        AssetsManager assetsManager,
        IReadOnlyList<Stream> ownedStreams,
        AssetSessionMetadata metadata,
        IReadOnlyList<AssetEntry> assets,
        IReadOnlyList<string> warnings)
    {
        _assetsManager = assetsManager;
        _ownedStreams = ownedStreams;
        Metadata = metadata;
        Assets = assets;
        Warnings = warnings;
    }

    public AssetSessionMetadata Metadata { get; }

    public IReadOnlyList<AssetEntry> Assets { get; }

    public IReadOnlyList<string> Warnings { get; }

    public Task<AssetDetails> GetDetailsAsync(AssetEntry asset, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Assets.Contains(asset))
            throw new ArgumentException("The requested asset does not belong to this session.", nameof(asset));

        AssetDetails details = new(
            asset.Name,
            asset.Type,
            asset.ClassId,
            asset.PathId,
            asset.ByteSize,
            asset.LogicalFile,
            asset.SourcePath,
            Metadata.Kind,
            Metadata.UnityVersion,
            Metadata.LogicalFileCount);
        return Task.FromResult(details);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _assetsManager.UnloadAll(true);
        }
        catch
        {
            // Individual stream cleanup below is the final ownership boundary.
        }

        try
        {
            _assetsManager.UnloadClassDatabase();
            _assetsManager.UnloadClassPackage();
        }
        catch
        {
            // Disposal must remain safe and idempotent.
        }

        foreach (Stream stream in _ownedStreams)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
                // Best-effort cleanup after AssetsTools.NET has unloaded its files.
            }
        }
    }
}
