using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UABS.UnityAssets.Packages;

internal sealed class LegacyPackageMetadataReader
{
    private readonly string _classDataPath;

    public LegacyPackageMetadataReader(string classDataPath)
    {
        _classDataPath = classDataPath;
    }

    public PackageFileMetadata Read(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string cabCode = ReadCabCode(path, cancellationToken);
        List<string> dependencies = ReadDependencies(path, cancellationToken);
        return new PackageFileMetadata(cabCode, dependencies);
    }

    private List<string> ReadDependencies(string path, CancellationToken cancellationToken)
    {
        AssetsManager manager = new();
        try
        {
            if (File.Exists(_classDataPath))
                manager.LoadClassPackage(_classDataPath);

            AssetsFileInstance fileInstance;
            UnityFileKind kind = UnityFileDetector.Detect(path);
            if (kind == UnityFileKind.Bundle)
            {
                BundleFileInstance bundle = manager.LoadBundleFile(path, true)
                    ?? throw new PackageFormatException("AssetsTools.NET returned no bundle instance.", path);
                cancellationToken.ThrowIfCancellationRequested();
                fileInstance = manager.LoadAssetsFileFromBundle(bundle, 0, false)
                    ?? throw new PackageFormatException("The bundle does not contain a readable first serialized file.", path);
            }
            else if (kind == UnityFileKind.SerializedFile)
            {
                fileInstance = manager.LoadAssetsFile(path, false)
                    ?? throw new PackageFormatException("AssetsTools.NET returned no serialized file instance.", path);
            }
            else
            {
                throw new PackageFormatException("The source is not a supported Unity bundle or serialized file.", path);
            }

            TryLoadClassDatabase(manager, fileInstance.file.Metadata.UnityVersion);
            cancellationToken.ThrowIfCancellationRequested();

            AssetFileInfo? assetBundleInfo = fileInstance.file
                .GetAssetsOfType(AssetClassID.AssetBundle)
                .FirstOrDefault();
            if (assetBundleInfo is null)
                return [];

            AssetTypeValueField baseField = manager.GetBaseField(fileInstance, assetBundleInfo);
            AssetTypeValueField dependencyField = baseField["m_Dependencies"];
            if (dependencyField is null || dependencyField.IsDummy || dependencyField.Children.Count == 0)
                return [];

            AssetTypeValueField dependenciesArray = dependencyField[0];
            List<string> result = new(dependenciesArray.Children.Count);
            foreach (AssetTypeValueField dependency in dependenciesArray.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(dependency.Value.AsString);
            }

            return result;
        }
        finally
        {
            try
            {
                manager.UnloadAll(true);
            }
            catch
            {
            }
        }
    }

    private void TryLoadClassDatabase(AssetsManager manager, string? unityVersion)
    {
        if (manager.ClassPackage is null || string.IsNullOrWhiteSpace(unityVersion) || unityVersion == "0.0.0")
            return;

        manager.LoadClassDatabaseFromPackage(unityVersion);
    }

    private static string ReadCabCode(string path, CancellationToken cancellationToken)
    {
        byte[] prefix = "CAB-"u8.ToArray();
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192, FileOptions.SequentialScan);
        int matched = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int value = stream.ReadByte();
            if (value < 0)
                return string.Empty;

            if (value == prefix[matched])
            {
                matched++;
                if (matched == prefix.Length)
                    break;
            }
            else
            {
                matched = value == prefix[0] ? 1 : 0;
            }
        }

        List<byte> bytes = [.. prefix];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int value = stream.ReadByte();
            if (value is < 0x21 or > 0x7e)
                break;
            bytes.Add((byte)value);
        }

        return Encoding.ASCII.GetString(bytes.ToArray());
    }
}

internal sealed record PackageFileMetadata(string CabCode, IReadOnlyList<string> Dependencies);
