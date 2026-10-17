namespace UABS.UnityAssets.Tests;

public sealed class AssetSessionTests
{
    [Fact]
    public async Task OpenAsync_EnumeratesStandaloneSerializedFile()
    {
        using IAssetSession session = await new AssetsToolsAssetSessionFactory(TestPaths.ClassData)
            .OpenAsync(TestPaths.SerializedFixture);

        Assert.Equal(UnityFileKind.SerializedFile, session.Metadata.Kind);
        Assert.Equal(1, session.Metadata.LogicalFileCount);
        Assert.NotEmpty(session.Assets);
    }

    [Fact]
    public async Task OpenAsync_EnumeratesRealAssetsAndPreservesMetadata()
    {
        AssetsToolsAssetSessionFactory factory = new(TestPaths.ClassData);

        using IAssetSession session = await factory.OpenAsync(TestPaths.BundleFixture);

        Assert.Equal(UnityFileKind.Bundle, session.Metadata.Kind);
        Assert.Equal(Path.GetFullPath(TestPaths.BundleFixture), session.Metadata.SourcePath);
        Assert.True(session.Metadata.LogicalFileCount >= 1);
        Assert.NotEmpty(session.Assets);
        Assert.All(session.Assets, asset =>
        {
            Assert.False(string.IsNullOrWhiteSpace(asset.Name));
            Assert.False(string.IsNullOrWhiteSpace(asset.Type));
            Assert.False(string.IsNullOrWhiteSpace(asset.LogicalFile));
            Assert.Equal(session.Metadata.SourcePath, asset.SourcePath);
        });
        Assert.Contains(session.Assets, asset => asset.PathId != 0);
        Assert.Contains(session.Assets, asset => asset.ByteSize > 0);
    }

    [Fact]
    public async Task OpenAsync_ReturnsStablePathIdsAcrossSessions()
    {
        AssetsToolsAssetSessionFactory factory = new(TestPaths.ClassData);
        long[] first;
        long[] second;

        using (IAssetSession session = await factory.OpenAsync(TestPaths.BundleFixture))
            first = session.Assets.Select(asset => asset.PathId).ToArray();

        using (IAssetSession session = await factory.OpenAsync(TestPaths.BundleFixture))
            second = session.Assets.Select(asset => asset.PathId).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task GetDetailsAsync_ReturnsSessionAndEntryMetadata()
    {
        using IAssetSession session = await new AssetsToolsAssetSessionFactory(TestPaths.ClassData)
            .OpenAsync(TestPaths.BundleFixture);
        AssetEntry asset = session.Assets[0];

        AssetDetails details = await session.GetDetailsAsync(asset);

        Assert.Equal(asset.PathId, details.PathId);
        Assert.Equal(asset.LogicalFile, details.LogicalFile);
        Assert.Equal(session.Metadata.Kind, details.FileKind);
        Assert.Equal(session.Metadata.UnityVersion, details.UnityVersion);
        Assert.Equal(session.Metadata.LogicalFileCount, details.LogicalFileCount);
    }

    [Fact]
    public async Task Dispose_ReleasesSourceFileAndIsIdempotent()
    {
        AssetsToolsAssetSessionFactory factory = new(TestPaths.ClassData);
        IAssetSession session = await factory.OpenAsync(TestPaths.BundleFixture);

        session.Dispose();
        session.Dispose();

        using FileStream exclusive = new(TestPaths.BundleFixture, FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.True(exclusive.Length > 0);
    }

    [Fact]
    public async Task OpenAsync_AllowsRepeatedOpenAndClose()
    {
        AssetsToolsAssetSessionFactory factory = new(TestPaths.ClassData);

        for (int index = 0; index < 3; index++)
        {
            using IAssetSession session = await factory.OpenAsync(TestPaths.BundleFixture);
            Assert.NotEmpty(session.Assets);
        }
    }

    [Fact]
    public async Task OpenAsync_RejectsUnsupportedFile()
    {
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "not a Unity file with enough content to parse");
            AssetLoadException exception = await Assert.ThrowsAsync<AssetLoadException>(
                () => new AssetsToolsAssetSessionFactory(TestPaths.ClassData).OpenAsync(path));
            Assert.Equal(AssetLoadErrorCode.UnsupportedFormat, exception.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenAsync_ReportsNonexistentFile()
    {
        AssetLoadException exception = await Assert.ThrowsAsync<AssetLoadException>(
            () => new AssetsToolsAssetSessionFactory(TestPaths.ClassData).OpenAsync(TestPaths.MissingFile));

        Assert.Equal(AssetLoadErrorCode.FileNotFound, exception.Code);
    }

    [Fact]
    public async Task OpenAsync_MissingClassDataIsANonFatalWarningForTypeTreeBundle()
    {
        using IAssetSession session = await new AssetsToolsAssetSessionFactory(TestPaths.MissingClassData)
            .OpenAsync(TestPaths.BundleFixture);

        Assert.NotEmpty(session.Assets);
        Assert.Contains(session.Warnings, warning => warning.Contains("Class database package was not found", StringComparison.Ordinal));
    }
}
