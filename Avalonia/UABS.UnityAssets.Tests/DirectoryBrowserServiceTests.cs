using UABS.Models;
using UABS.Services;

namespace UABS.UnityAssets.Tests;

public sealed class DirectoryBrowserServiceTests
{
    [Fact]
    public async Task ReadAsync_EnumeratesFoldersFirstAndFileMetadata()
    {
        string root = Path.Combine(Path.GetTempPath(), $"uabs-browser-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Zeta"));
            await File.WriteAllBytesAsync(Path.Combine(root, "alpha.assets"), [1, 2, 3]);

            DirectorySnapshot snapshot = await new DirectoryBrowserService().ReadAsync(root);

            Assert.Equal(Path.GetFullPath(root), snapshot.Path);
            Assert.Collection(snapshot.Entries,
                entry => Assert.Equal(BrowserEntryKind.Directory, entry.Kind),
                entry =>
                {
                    Assert.Equal(BrowserEntryKind.File, entry.Kind);
                    Assert.Equal(3, entry.Size);
                });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ReadAsync_ReportsMissingDirectory() =>
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            new DirectoryBrowserService().ReadAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
}
