namespace UABS.UnityAssets.Tests;

public sealed class UnityFileRecognitionServiceTests
{
    private readonly UnityFileRecognitionService _service = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Recognize_BundleContentIsSupportedRegardlessOfExtension(bool includeExtension)
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            includeExtension ? $"uabs-{Guid.NewGuid():N}.unusual" : $"uabs-{Guid.NewGuid():N}");
        try
        {
            File.Copy(TestPaths.BundleFixture, path);

            UnityFileRecognition result = _service.Recognize(path);

            Assert.Equal(UnityFileRecognitionStatus.Supported, result.Status);
            Assert.Equal(UnityFileKind.Bundle, result.Kind);
            Assert.Null(result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Recognize_SerializedContentIsSupported()
    {
        UnityFileRecognition result = _service.Recognize(TestPaths.SerializedFixture);

        Assert.Equal(UnityFileRecognitionStatus.Supported, result.Status);
        Assert.Equal(UnityFileKind.SerializedFile, result.Kind);
    }

    [Fact]
    public void Recognize_InvalidUnityLookingFileIsInvalid()
    {
        string path = Path.Combine(Path.GetTempPath(), $"uabs-{Guid.NewGuid():N}.bundle");
        try
        {
            File.WriteAllText(path, "not a Unity file");

            UnityFileRecognition result = _service.Recognize(path);

            Assert.Equal(UnityFileRecognitionStatus.Invalid, result.Status);
            Assert.Equal(UnityFileKind.Unknown, result.Kind);
            Assert.Null(result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Recognize_MissingFileIsUnreadableAndKeepsDiagnostic()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}");

        UnityFileRecognition result = _service.Recognize(path);

        Assert.Equal(UnityFileRecognitionStatus.Unreadable, result.Status);
        Assert.IsType<FileNotFoundException>(result.Error);
    }

    [Fact]
    public void Recognize_HonorsCancellationBeforeOpeningFile()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            _service.Recognize(TestPaths.BundleFixture, cancellation.Token));
    }
}
