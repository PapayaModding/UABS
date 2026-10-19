using UABS;

namespace UABS.UnityAssets.Tests;

public sealed class PrinterTests
{
    [Fact]
    public void Print_CreatesAppendsAndReleasesLogFile()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "logs", "test.log");

        Printer.Print(path, "first");
        Printer.Print(path, "second");

        string contents = File.ReadAllText(path);
        Assert.Contains("first", contents, StringComparison.Ordinal);
        Assert.Contains("second", contents, StringComparison.Ordinal);
        string moved = Path.Combine(temporary.Path, "moved.log");
        File.Move(path, moved);
        Assert.True(File.Exists(moved));
    }

    [Fact]
    public async Task Print_SerializesConcurrentWrites()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "concurrent.log");

        await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(index => Task.Run(() => Printer.Print(path, $"message-{index}"))));

        string[] lines = File.ReadAllLines(path);
        Assert.Equal(20, lines.Length);
        for (int index = 0; index < 20; index++)
            Assert.Contains(lines, line => line.EndsWith($"message-{index}", StringComparison.Ordinal));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"uabs-printer-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
