namespace UABS.UnityAssets.Packages;

public class PackageException : Exception
{
    public PackageException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class PackageFormatException : PackageException
{
    public PackageFormatException(string message, string? filePath = null, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }

    public string? FilePath { get; }
}

public sealed class PackageAlreadyExistsException : PackageException
{
    public PackageAlreadyExistsException(string packagePath)
        : base($"A package already exists at '{packagePath}'. Explicit overwrite approval is required.")
    {
        PackagePath = packagePath;
    }

    public string PackagePath { get; }
}

public sealed class PackageBuildException : PackageException
{
    public PackageBuildException(string sourcePath, Exception innerException)
        : base($"Could not index Unity file '{sourcePath}': {innerException.Message}", innerException)
    {
        SourcePath = sourcePath;
    }

    public string SourcePath { get; }
}
