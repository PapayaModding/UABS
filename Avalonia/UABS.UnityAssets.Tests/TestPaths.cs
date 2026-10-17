namespace UABS.UnityAssets.Tests;

internal static class TestPaths
{
    public static string BundleFixture => Path.Combine(AppContext.BaseDirectory, "Fixtures", "aigirl.ab");

    public static string SerializedFixture => Path.Combine(AppContext.BaseDirectory, "Fixtures", "aigirl.assets");

    public static string LegacyPackageFixture => Path.Combine(AppContext.BaseDirectory, "Fixtures", "LegacyPackage");

    public static string ClassData => Path.Combine(AppContext.BaseDirectory, "Resources", "classdata.tpk");

    public static string MissingFile => Path.Combine(AppContext.BaseDirectory, "Fixtures", "missing.bundle");

    public static string MissingClassData => Path.Combine(AppContext.BaseDirectory, "Resources", "missing-classdata.tpk");
}
