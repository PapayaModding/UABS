using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UABS.UnityAssets;

public sealed class AssetsToolsAssetSessionFactory : IAssetSessionFactory
{
    private readonly string _classDataPath;

    public AssetsToolsAssetSessionFactory(string? classDataPath = null)
    {
        _classDataPath = classDataPath
            ?? Path.Combine(AppContext.BaseDirectory, "Resources", "classdata.tpk");
    }

    public Task<IAssetSession> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Open(path, cancellationToken), cancellationToken);
    }

    private IAssetSession Open(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new AssetLoadException(
                AssetLoadErrorCode.FileNotFound,
                $"The selected file does not exist: {fullPath}");
        }

        UnityFileKind kind = UnityFileDetector.Detect(fullPath);
        if (kind == UnityFileKind.Unknown)
        {
            throw new AssetLoadException(
                AssetLoadErrorCode.UnsupportedFormat,
                "The selected file is not a supported Unity bundle or serialized asset file.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        AssetsManager assetsManager = new();
        List<Stream> ownedStreams = [];
        List<string> warnings = [];

        try
        {
            // Fingerprint the bytes actually enumerated. Pin only the loading interval;
            // existing session stream ownership and format parsing are unchanged.
            using FileStream memoEvidence = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            string sourceHash = HashEvidence(memoEvidence, cancellationToken);
            LoadClassPackage(assetsManager, warnings);

            List<(AssetsFileInstance Instance, string LogicalName)> files = kind switch
            {
                UnityFileKind.Bundle => LoadBundle(assetsManager, fullPath, ownedStreams, warnings, cancellationToken),
                UnityFileKind.SerializedFile => LoadSerializedFile(assetsManager, fullPath, ownedStreams, warnings),
                _ => throw new InvalidOperationException("Unsupported file kind reached the loader.")
            };

            List<AssetEntry> entries = EnumerateAssets(
                assetsManager,
                files,
                fullPath,
                warnings,
                cancellationToken);

            string? unityVersion = files.FirstOrDefault().Instance?.file?.Metadata?.UnityVersion;
            AssetSessionMetadata metadata = new(
                fullPath,
                Path.GetFileName(fullPath),
                kind,
                string.IsNullOrWhiteSpace(unityVersion) ? null : unityVersion,
                files.Count,
                sourceHash == HashEvidence(memoEvidence, cancellationToken) ? sourceHash : null);

            return new AssetsToolsAssetSession(
                assetsManager,
                ownedStreams,
                metadata,
                entries,
                warnings);
        }
        catch (OperationCanceledException)
        {
            CleanupFailedOpen(assetsManager, ownedStreams);
            throw;
        }
        catch (AssetLoadException)
        {
            CleanupFailedOpen(assetsManager, ownedStreams);
            throw;
        }
        catch (Exception exception)
        {
            CleanupFailedOpen(assetsManager, ownedStreams);
            throw new AssetLoadException(
                AssetLoadErrorCode.InvalidData,
                $"AssetsTools.NET could not open '{Path.GetFileName(fullPath)}'. The file may be invalid, truncated, encrypted, or from an unsupported Unity version.",
                exception);
        }
    }

    private static string HashEvidence(Stream stream, CancellationToken token)
    {
        stream.Position = 0;
        using var hash = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        byte[] buffer = new byte[65536]; int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) { token.ThrowIfCancellationRequested(); hash.AppendData(buffer, 0, read); }
        token.ThrowIfCancellationRequested(); return Convert.ToHexString(hash.GetHashAndReset());
    }
    private void LoadClassPackage(AssetsManager assetsManager, List<string> warnings)
    {
        if (!File.Exists(_classDataPath))
        {
            warnings.Add($"Class database package was not found at '{_classDataPath}'. Type and name resolution may be incomplete.");
            return;
        }

        try
        {
            assetsManager.LoadClassPackage(_classDataPath);
        }
        catch (Exception exception)
        {
            warnings.Add($"Class database package could not be loaded: {exception.Message}");
        }
    }

    private static List<(AssetsFileInstance, string)> LoadSerializedFile(
        AssetsManager assetsManager,
        string path,
        List<Stream> ownedStreams,
        List<string> warnings)
    {
        FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192, FileOptions.SequentialScan);
        ownedStreams.Add(stream);

        AssetsFileInstance instance = assetsManager.LoadAssetsFile(stream, false)
            ?? throw new AssetLoadException(AssetLoadErrorCode.InvalidData, "AssetsTools.NET returned no serialized file instance.");

        TryLoadClassDatabase(assetsManager, instance.file.Metadata.UnityVersion, warnings);
        return [(instance, instance.name ?? Path.GetFileName(path))];
    }

    private static List<(AssetsFileInstance, string)> LoadBundle(
        AssetsManager assetsManager,
        string path,
        List<Stream> ownedStreams,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192, FileOptions.SequentialScan);
        ownedStreams.Add(stream);

        BundleFileInstance bundleInstance = assetsManager.LoadBundleFile(stream, false)
            ?? throw new AssetLoadException(AssetLoadErrorCode.InvalidData, "AssetsTools.NET returned no bundle instance.");

        if (bundleInstance.file.DataIsCompressed)
        {
            MemoryStream unpackedStream = new();
            bundleInstance.file.Unpack(new AssetsFileWriter(unpackedStream));
            unpackedStream.Position = 0;

            AssetBundleFile unpackedBundle = new();
            unpackedBundle.Read(new AssetsFileReader(unpackedStream));
            bundleInstance.file.Close();
            bundleInstance.file = unpackedBundle;
            ownedStreams.Add(unpackedStream);
        }

        TryLoadClassDatabase(assetsManager, bundleInstance.file.Header.EngineVersion, warnings);

        List<(AssetsFileInstance, string)> files = [];
        int fileCount = bundleInstance.file.BlockAndDirInfo.DirectoryInfos.Count;
        for (int index = 0; index < fileCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!bundleInstance.file.IsAssetsFile(index))
                continue;

            string logicalName = bundleInstance.file.GetFileName(index);
            try
            {
                AssetsFileInstance? file = assetsManager.LoadAssetsFileFromBundle(bundleInstance, index, false);
                if (file is null)
                {
                    warnings.Add($"Serialized file '{logicalName}' could not be loaded from the bundle.");
                    continue;
                }

                TryLoadClassDatabase(assetsManager, file.file.Metadata.UnityVersion, warnings);
                files.Add((file, logicalName));
            }
            catch (Exception exception)
            {
                warnings.Add($"Serialized file '{logicalName}' was skipped: {exception.Message}");
            }
        }

        return files;
    }

    private static void TryLoadClassDatabase(AssetsManager assetsManager, string? unityVersion, List<string> warnings)
    {
        if (assetsManager.ClassPackage is null || assetsManager.ClassDatabase is not null || string.IsNullOrWhiteSpace(unityVersion) || unityVersion == "0.0.0")
            return;

        try
        {
            if (assetsManager.LoadClassDatabaseFromPackage(unityVersion) is null)
                warnings.Add($"No class database matched Unity version '{unityVersion}'.");
        }
        catch (Exception exception)
        {
            warnings.Add($"Class database for Unity version '{unityVersion}' could not be loaded: {exception.Message}");
        }
    }

    private static List<AssetEntry> EnumerateAssets(
        AssetsManager assetsManager,
        IEnumerable<(AssetsFileInstance Instance, string LogicalName)> files,
        string sourcePath,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        List<AssetEntry> entries = [];
        foreach ((AssetsFileInstance instance, string logicalName) in files)
        {
            foreach (AssetFileInfo assetInfo in instance.file.AssetInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                (string name, string type) = ReadIdentityFast(assetsManager, instance, assetInfo);
                entries.Add(new AssetEntry(
                    name,
                    type,
                    assetInfo.TypeId,
                    assetInfo.PathId,
                    assetInfo.ByteSize,
                    logicalName,
                    sourcePath));
            }
        }

        if (entries.Count == 0)
            warnings.Add("The file opened successfully but contained no enumerable serialized assets.");

        return entries;
    }

    private static (string Name, string Type) ReadIdentityFast(
        AssetsManager assetsManager,
        AssetsFileInstance instance,
        AssetFileInfo assetInfo)
    {
        string fallbackType = GetFallbackTypeName(assetInfo.TypeId);

        try
        {
            lock (instance.LockReader)
            {
                AssetsFile file = instance.file;
                AssetsFileReader reader = file.Reader;
                long position = assetInfo.GetAbsoluteByteOffset(file);
                int classId = assetInfo.TypeId;
                string? name = null;
                string type = fallbackType;

                if (file.Metadata.TypeTreeEnabled)
                {
                    TypeTreeType? tree = classId == 0x72 || classId < 0
                        ? file.Metadata.FindTypeTreeTypeByScriptIndex(assetInfo.ScriptTypeIndex)
                        : file.Metadata.FindTypeTreeTypeByID(classId);

                    if (tree is not null && tree.Nodes.Count > 0)
                    {
                        type = tree.Nodes[0].GetTypeString(tree.StringBufferBytes);
                        name = ReadKnownName(reader, position, file.Header.Version, tree.Nodes, tree.StringBufferBytes, type);
                    }
                }
                else if (assetsManager.ClassDatabase is not null)
                {
                    ClassDatabaseType? databaseType = assetsManager.ClassDatabase.FindAssetClassByID(classId);
                    if (databaseType is not null)
                    {
                        type = assetsManager.ClassDatabase.GetString(databaseType.Name);
                        List<ClassDatabaseTypeNode> nodes = databaseType.GetPreferredNode(false).Children;
                        name = ReadKnownName(reader, position, file.Header.Version, nodes, assetsManager.ClassDatabase, type);
                    }
                }

                return (string.IsNullOrWhiteSpace(name) ? "Unnamed asset" : name, type);
            }
        }
        catch
        {
            return ("Unnamed asset", fallbackType);
        }
    }

    private static string? ReadKnownName(
        AssetsFileReader reader,
        long position,
        uint fileVersion,
        IReadOnlyList<TypeTreeNode> nodes,
        byte[] stringBuffer,
        string type)
    {
        if (nodes.Count > 1 && nodes[1].GetNameString(stringBuffer) == "m_Name")
        {
            reader.Position = position;
            return reader.ReadCountStringInt32();
        }

        return ReadSpecialName(reader, position, fileVersion, type);
    }

    private static string? ReadKnownName(
        AssetsFileReader reader,
        long position,
        uint fileVersion,
        IReadOnlyList<ClassDatabaseTypeNode> nodes,
        ClassDatabaseFile database,
        string type)
    {
        if (nodes.Count > 0 && database.GetString(nodes[0].FieldName) == "m_Name")
        {
            reader.Position = position;
            return reader.ReadCountStringInt32();
        }

        return ReadSpecialName(reader, position, fileVersion, type);
    }

    private static string? ReadSpecialName(AssetsFileReader reader, long position, uint fileVersion, string type)
    {
        if (type == nameof(AssetClassID.GameObject))
        {
            reader.Position = position;
            int componentCount = reader.ReadInt32();
            int componentSize = fileVersion > 0x10 ? 0x0c : 0x10;
            reader.Position += componentCount * componentSize + 0x04;
            string objectName = reader.ReadCountStringInt32();
            return string.IsNullOrWhiteSpace(objectName) ? null : $"GameObject {objectName}";
        }

        if (type == nameof(AssetClassID.MonoBehaviour))
        {
            reader.Position = position + 0x1c;
            return reader.ReadCountStringInt32();
        }

        return null;
    }

    private static string GetFallbackTypeName(int classId)
    {
        if (classId < 0)
            return nameof(AssetClassID.MonoBehaviour);

        return Enum.IsDefined(typeof(AssetClassID), classId)
            ? ((AssetClassID)classId).ToString()
            : $"Class {classId}";
    }

    private static void CleanupFailedOpen(AssetsManager assetsManager, IEnumerable<Stream> streams)
    {
        try
        {
            assetsManager.UnloadAll(true);
        }
        catch
        {
        }

        foreach (Stream stream in streams)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
            }
        }
    }
}
