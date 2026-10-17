using UABS.UnityAssets.Packages;

namespace UABS.UnityAssets.Tests;

public sealed class PackagePathComparerTests
{
    [Theory]
    [InlineData(@"C:\Game\Data\file.bundle", @"\\?\c:\game\data\file.bundle")]
    [InlineData(@"C:\Game\Data\", @"c:/game/data")]
    [InlineData(@"\\server\share\folder\file.assets", @"\\?\UNC\SERVER\share\folder\file.assets")]
    public void Equals_RecognizesSafeWindowsPathSpellings(string left, string right)
    {
        Assert.True(PackagePathComparer.Equals(left, right));
        Assert.Equal(PackagePathComparer.GetIdentity(left), PackagePathComparer.GetIdentity(right),
            StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(@"C:\Game\one.bundle", @"C:\Game\two.bundle")]
    [InlineData(@"\\server\share\one", @"\\server\other\one")]
    [InlineData(@"\\?\Volume{00000000-0000-0000-0000-000000000000}\file", @"C:\file")]
    public void Equals_DoesNotCollapseDistinctOrUnsupportedDevicePaths(string left, string right)
    {
        Assert.False(PackagePathComparer.Equals(left, right));
    }

    [Fact]
    public void Equals_InvalidPathsDoNotThrowOrBecomeEquivalent()
    {
        string first = "invalid\0one";
        string second = "invalid\0two";

        Assert.True(PackagePathComparer.Equals(first, first));
        Assert.False(PackagePathComparer.Equals(first, second));
        Assert.False(PackagePathComparer.Equals(null, string.Empty));
    }

    [Fact]
    public void PackageDocument_DetectsExtendedPathDuplicateAndPreservesOriginalSpelling()
    {
        PackageBundleReference existing = new() { Name = "one", Path = @"\\?\C:\Game\one.bundle" };
        PackageDocument document = new([new PackageIndex(string.Empty, [existing])]);

        Assert.Throws<InvalidOperationException>(() => document.AddReference(
            "nested",
            new PackageBundleReference { Name = "duplicate", Path = @"c:\game\one.bundle" }));
        Assert.Equal(@"\\?\C:\Game\one.bundle", Assert.Single(document.Bundles).Path);
    }
}
