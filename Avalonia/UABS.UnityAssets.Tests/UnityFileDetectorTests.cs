namespace UABS.UnityAssets.Tests;

public sealed class UnityFileDetectorTests
{
    [Fact]
    public void Detect_RecognizesRepresentativeBundle()
    {
        Assert.Equal(UnityFileKind.Bundle, UnityFileDetector.Detect(TestPaths.BundleFixture));
    }

    [Fact]
    public void Detect_RecognizesRepresentativeSerializedFile()
    {
        Assert.Equal(UnityFileKind.SerializedFile, UnityFileDetector.Detect(TestPaths.SerializedFixture));
    }

    [Fact]
    public void Detect_RejectsInvalidFile()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not a Unity file");
            Assert.Equal(UnityFileKind.Unknown, UnityFileDetector.Detect(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Detect_ReportsMissingFile()
    {
        Assert.Throws<FileNotFoundException>(() => UnityFileDetector.Detect(TestPaths.MissingFile));
    }
}
