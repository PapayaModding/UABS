namespace UABS.UnityAssets;

public interface IAssetSessionFactory
{
    Task<IAssetSession> OpenAsync(string path, CancellationToken cancellationToken = default);
}
