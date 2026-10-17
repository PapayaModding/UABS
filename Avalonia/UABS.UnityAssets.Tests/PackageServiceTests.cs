using System.Text.Json;
using UABS.UnityAssets.Packages;

namespace UABS.UnityAssets.Tests;

public sealed class PackageServiceTests
{
    [Fact]
    public async Task ReadCatalogAsync_ReturnsLoadedMalformedAndIncompleteDirectories()
    {
        using TemporaryDirectory temporary = new();
        string valid = temporary.CreateDirectory("valid");
        string malformed = temporary.CreateDirectory("malformed");
        string incomplete = temporary.CreateDirectory("incomplete");
        await CreateService().SaveAsync(CreateDocument("valid.ab"), valid, overwrite: true);
        File.WriteAllText(Path.Combine(malformed, PackageService.ValidationFileName), string.Empty);
        File.WriteAllText(Path.Combine(malformed, PackageService.IndexFileName), "not-json");
        File.WriteAllText(Path.Combine(incomplete, PackageService.IndexFileName), "[]");

        IReadOnlyList<PackageCatalogEntry> catalog = await CreateService().ReadCatalogAsync(temporary.Path);

        Assert.Equal(["incomplete", "malformed", "valid"], catalog.Select(item => item.Name));
        PackageCatalogEntry incompleteEntry = catalog[0];
        Assert.True(incompleteEntry.IsLoaded);
        Assert.Contains(incompleteEntry.ValidationIssues, issue => issue.Code == "missing-validation-marker");
        Assert.False(catalog[1].IsLoaded);
        Assert.Contains("malformed", catalog[1].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(catalog[2].IsLoaded);
    }

    [Fact]
    public async Task ReadCatalogAsync_MissingParentReturnsEmptyCatalog()
    {
        using TemporaryDirectory temporary = new();
        string missing = Path.Combine(temporary.Path, "missing");

        IReadOnlyList<PackageCatalogEntry> catalog = await CreateService().ReadCatalogAsync(missing);

        Assert.Empty(catalog);
    }

    [Fact]
    public async Task LoadAsync_ReadsRepresentativeLegacyPackageAndPreservesUnknownFields()
    {
        PackageDocument package = await CreateService().LoadAsync(TestPaths.LegacyPackageFixture);

        Assert.Equal(2, package.Indexes.Count);
        PackageBundleReference bundle = Assert.Single(package.Bundles);
        Assert.Equal("character.ab", bundle.Name);
        Assert.Equal("C:\\LegacyGame\\Data\\character.ab", bundle.Path);
        Assert.Equal("CAB-0123456789abcdef", bundle.CabCode);
        Assert.Equal("CAB-fedcba9876543210", Assert.Single(bundle.Dependencies));
        PackageAssetReference asset = Assert.Single(bundle.AssetInfos);
        Assert.Equal((long)1001, asset.PathId);
        Assert.Equal("legacy memo", asset.Memo);
        Assert.Equal(42, bundle.ExtensionData!["LegacyBundleField"].GetInt32());
        Assert.Equal("preserve me", asset.ExtensionData!["LegacyAssetField"].GetString());
        Assert.Contains(package.ValidationIssues, issue => issue.Code == "missing-source-file");
    }

    [Fact]
    public async Task CreateAsync_IndexesRealBundleAndSerializedFixturesWithOriginalPaths()
    {
        using TemporaryDirectory temporary = new();
        string source = temporary.CreateDirectory("source");
        string bundlePath = CopyFixture(TestPaths.BundleFixture, source);
        string child = Directory.CreateDirectory(Path.Combine(source, "child")).FullName;
        string assetsPath = CopyFixture(TestPaths.SerializedFixture, child);

        PackageDocument package = await CreateService().CreateAsync(source);

        Assert.Equal(2, package.Indexes.Count);
        Assert.Equal(2, package.Bundles.Count());
        Assert.Contains(package.Bundles, item => item.Path == Path.GetFullPath(bundlePath));
        Assert.Contains(package.Bundles, item => item.Path == Path.GetFullPath(assetsPath));
        Assert.All(package.Bundles, item => Assert.NotEmpty(item.AssetInfos));
        Assert.All(package.Bundles.SelectMany(item => item.AssetInfos), asset => Assert.NotEqual(0, asset.PathId));
    }

    [Fact]
    public async Task CreateAsync_HonorsPreCanceledToken()
    {
        using TemporaryDirectory temporary = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService().CreateAsync(temporary.Path, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task CreateReferenceAsync_SelectedBundleAssetPreservesSourceIdentity()
    {
        using IAssetSession session = await new AssetsToolsAssetSessionFactory().OpenAsync(TestPaths.BundleFixture);
        AssetEntry selected = session.Assets.First(asset => asset.ClassId is 28 or 213);

        PackageBundleReference reference = await CreateService().CreateReferenceAsync(
            TestPaths.BundleFixture,
            [new PackageAssetSelection(selected.Name, selected.PathId, selected.LogicalFile)]);

        Assert.Equal(Path.GetFullPath(TestPaths.BundleFixture), reference.Path);
        Assert.Equal(Path.GetFileName(TestPaths.BundleFixture), reference.Name);
        PackageAssetReference asset = Assert.Single(reference.AssetInfos);
        Assert.Equal(selected.Name, asset.Name);
        Assert.Equal(selected.PathId, asset.PathId);
    }

    [Fact]
    public async Task CreateReferenceAsync_DetectsSupportedFilesByHeaderRegardlessOfExtension()
    {
        using TemporaryDirectory temporary = new();
        string[] names = ["source.assets", "source.bundle", "source.unity3d", "source.custom", "source"];

        foreach (string name in names)
        {
            string copied = Path.Combine(temporary.Path, name);
            File.Copy(TestPaths.SerializedFixture, copied);

            PackageBundleReference reference = await CreateService().CreateReferenceAsync(copied);

            Assert.Equal(Path.GetFullPath(copied), reference.Path);
            Assert.NotEmpty(reference.AssetInfos);
        }
    }

    [Fact]
    public async Task CreateReferenceAsync_UnsupportedHeaderProducesUsefulError()
    {
        using TemporaryDirectory temporary = new();
        string unsupported = Path.Combine(temporary.Path, "not-unity.bundle");
        await File.WriteAllTextAsync(unsupported, "ordinary text");

        PackageException exception = await Assert.ThrowsAsync<PackageException>(
            () => CreateService().CreateReferenceAsync(unsupported));

        Assert.Contains("header", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a supported Unity", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateReferencesFromFolderAsync_NonRecursiveMixedFilesReportsEveryOutcome()
    {
        using TemporaryDirectory temporary = new();
        string first = Path.Combine(temporary.Path, "first.unusual");
        string second = Path.Combine(temporary.Path, "second");
        File.Copy(TestPaths.SerializedFixture, first);
        File.Copy(TestPaths.BundleFixture, second);
        await File.WriteAllTextAsync(Path.Combine(temporary.Path, "unsupported.assets"), "ordinary text");
        string damaged = Path.Combine(temporary.Path, "damaged.data");
        byte[] damagedBytes = new byte[64];
        System.Text.Encoding.ASCII.GetBytes("UnityFS").CopyTo(damagedBytes, 0);
        await File.WriteAllBytesAsync(damaged, damagedBytes);
        string nested = temporary.CreateDirectory("nested");
        File.Copy(TestPaths.SerializedFixture, Path.Combine(nested, "not-scanned.assets"));
        List<PackageProgress> progress = [];

        PackageFolderImportResult result = await CreateService().CreateReferencesFromFolderAsync(
            temporary.Path,
            progress: new InlineProgress<PackageProgress>(progress.Add));

        Assert.Equal(4, result.ScannedFileCount);
        Assert.Equal(3, result.SupportedFileCount);
        Assert.Equal(2, result.ImportedFileCount);
        Assert.Equal(1, result.UnsupportedFileCount);
        Assert.Equal(1, result.FailedFileCount);
        Assert.False(result.IncludedSubfolders);
        Assert.Equal(1, result.SkippedSubdirectoryCount);
        Assert.Equal(Path.GetFullPath(damaged), Assert.Single(result.Failures).Path);
        Assert.Equal(
            [Path.GetFullPath(first), Path.GetFullPath(second)],
            result.References.Select(reference => reference.Path).OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        Assert.Contains(progress, item => item.Phase == PackageProgressPhase.Scanning);
        Assert.Equal(PackageProgressPhase.Complete, progress[^1].Phase);
    }

    [Fact]
    public async Task CreateReferencesFromFolderAsync_EmptyMissingAndCancellationAreSafe()
    {
        using TemporaryDirectory temporary = new();
        PackageFolderImportResult empty = await CreateService().CreateReferencesFromFolderAsync(temporary.Path);
        Assert.Equal(0, empty.ScannedFileCount);
        Assert.Empty(empty.References);

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            CreateService().CreateReferencesFromFolderAsync(Path.Combine(temporary.Path, "missing")));

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService().CreateReferencesFromFolderAsync(temporary.Path, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task CreateReferencesFromFolderAsync_RecursiveModeFindsNestedHeaderDetectedFiles()
    {
        using TemporaryDirectory temporary = new();
        string rootFile = Path.Combine(temporary.Path, "root.custom");
        string nestedDirectory = temporary.CreateDirectory(Path.Combine("nested", "deeper"));
        string nestedFile = Path.Combine(nestedDirectory, "extensionless");
        File.Copy(TestPaths.SerializedFixture, rootFile);
        File.Copy(TestPaths.BundleFixture, nestedFile);
        await File.WriteAllTextAsync(Path.Combine(temporary.Path, "nested", "notes.assets"), "not Unity data");

        PackageFolderImportResult currentOnly = await CreateService().CreateReferencesFromFolderAsync(
            temporary.Path,
            PackageFolderSearchMode.CurrentFolderOnly);
        PackageFolderImportResult recursive = await CreateService().CreateReferencesFromFolderAsync(
            temporary.Path,
            PackageFolderSearchMode.IncludeSubfolders);

        Assert.Single(currentOnly.References);
        Assert.Equal(Path.GetFullPath(rootFile), currentOnly.References[0].Path);
        Assert.Equal(3, recursive.ScannedFileCount);
        Assert.Equal(2, recursive.SupportedFileCount);
        Assert.Equal(2, recursive.ImportedFileCount);
        Assert.Equal(1, recursive.UnsupportedFileCount);
        Assert.True(recursive.IncludedSubfolders);
        Assert.Equal(0, recursive.SkippedSubdirectoryCount);
        Assert.Contains(recursive.References, reference => reference.Path == Path.GetFullPath(nestedFile));
        Assert.Contains(recursive.GetMappedReferences(), item =>
            item.RelativeDirectory == string.Empty && item.Reference.Path == Path.GetFullPath(rootFile));
        Assert.Contains(recursive.GetMappedReferences(), item =>
            item.RelativeDirectory == Path.Combine("nested", "deeper")
            && item.Reference.Path == Path.GetFullPath(nestedFile));
    }

    [Fact]
    public async Task SaveAndReopen_PreservesFlatNestedAndEmptyIndexesWithoutMovingRecords()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "mixed");
        PackageBundleReference root = CreateBundle("root.ab", "root memo");
        root.ExtensionData = new() { ["RootUnknown"] = JsonDocument.Parse("7").RootElement.Clone() };
        PackageBundleReference nested = CreateBundle("nested.ab", "nested memo");
        nested.Path = @"\\?\C:\LegacyGame\nested.ab";
        PackageDocument original = new([
            new PackageIndex(string.Empty, [root]),
            new PackageIndex(Path.Combine("characters", "hero"), [nested]),
            new PackageIndex(Path.Combine("characters", "empty"))
        ]);

        await CreateService().SaveAsync(original, target);
        PackageDocument reopened = await new PackageService(TestPaths.ClassData).LoadAsync(target);

        Assert.Equal(3, reopened.Indexes.Count);
        Assert.Equal("root.ab", Assert.Single(reopened.Indexes.Single(index => index.RelativeDirectory == string.Empty).Bundles).Name);
        Assert.Equal("nested.ab", Assert.Single(reopened.Indexes.Single(index =>
            index.RelativeDirectory == Path.Combine("characters", "hero")).Bundles).Name);
        Assert.Empty(reopened.Indexes.Single(index =>
            index.RelativeDirectory == Path.Combine("characters", "empty")).Bundles);
        Assert.Equal(@"\\?\C:\LegacyGame\nested.ab", reopened.Bundles.Single(bundle => bundle.Name == "nested.ab").Path);
        Assert.Equal("root memo", reopened.Bundles.Single(bundle => bundle.Name == "root.ab").AssetInfos.Single().Memo);
        Assert.Equal(7, reopened.Bundles.Single(bundle => bundle.Name == "root.ab")
            .ExtensionData!["RootUnknown"].GetInt32());
    }

    [Fact]
    public async Task SaveAsync_RejectsDuplicateNormalizedIndexDirectories()
    {
        using TemporaryDirectory temporary = new();
        PackageDocument duplicate = new([
            new PackageIndex(Path.Combine("Characters", "Hero")),
            new PackageIndex(Path.Combine("characters", "hero"))
        ]);

        await Assert.ThrowsAsync<PackageFormatException>(() =>
            CreateService().SaveAsync(duplicate, Path.Combine(temporary.Path, "duplicate")));
    }

    [Fact]
    public void PackageFolderScanner_ReportsInaccessibleSubdirectoryAndDoesNotFollowLinks()
    {
        var fileSystem = new ControlledFolderFileSystem(
            directories: new Dictionary<string, string[]>
            {
                ["root"] = ["root\\blocked", "root\\junction", "root\\link"],
                ["root\\blocked"] = [],
                ["root\\junction"] = ["root\\junction\\escape"],
                ["root\\link"] = ["root\\link\\escape"]
            },
            files: new Dictionary<string, string[]> { ["root"] = [] },
            attributes: new Dictionary<string, FileAttributes>
            {
                ["root\\blocked"] = FileAttributes.Directory,
                ["root\\junction"] = FileAttributes.Directory | FileAttributes.ReparsePoint,
                ["root\\link"] = FileAttributes.Directory | FileAttributes.ReparsePoint
            },
            inaccessibleDirectory: "root\\blocked");

        PackageFolderScanResult scan = new PackageFolderScanner(fileSystem).Scan(
            "root",
            PackageFolderSearchMode.IncludeSubfolders,
            progress: null,
            CancellationToken.None);

        Assert.Contains(scan.Failures, failure =>
            failure.Path == "root\\blocked" && failure.Kind == PackageFolderImportFailureKind.Directory);
        Assert.Single(scan.Failures);
        Assert.Equal(2, scan.SkippedLinkCount);
        Assert.DoesNotContain("root\\junction", fileSystem.EnumeratedDirectories);
        Assert.DoesNotContain("root\\link", fileSystem.EnumeratedDirectories);
    }

    [Fact]
    public void PackageFolderScanner_CancellationDuringEnumerationAbortsWithoutResult()
    {
        using CancellationTokenSource cancellation = new();
        var fileSystem = new CancellingFolderFileSystem(cancellation);

        Assert.Throws<OperationCanceledException>(() => new PackageFolderScanner(fileSystem).Scan(
            "root",
            PackageFolderSearchMode.IncludeSubfolders,
            progress: null,
            cancellation.Token));
    }

    [Fact]
    public async Task CreateReferencesFromFolderAsync_CancellationDuringAssetProcessingReturnsNoPartialResult()
    {
        using TemporaryDirectory temporary = new();
        File.Copy(TestPaths.SerializedFixture, Path.Combine(temporary.Path, "one.assets"));
        File.Copy(TestPaths.SerializedFixture, Path.Combine(temporary.Path, "two.assets"));
        using CancellationTokenSource cancellation = new();
        var progress = new InlineProgress<PackageProgress>(update =>
        {
            if (update.Phase == PackageProgressPhase.Reading && update.Completed == 1)
                cancellation.Cancel();
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService().CreateReferencesFromFolderAsync(
            temporary.Path,
            progress: progress,
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task EmptyPackage_SaveAndReopen_WritesLegacyRootIndex()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "first-package");
        PackageDocument empty = new([new PackageIndex(string.Empty)]);

        await CreateService().SaveAsync(empty, target);
        PackageDocument reopened = await CreateService().LoadAsync(target);

        Assert.Single(reopened.Indexes);
        Assert.Empty(reopened.Bundles);
        Assert.True(File.Exists(Path.Combine(target, PackageService.IndexFileName)));
        Assert.True(File.Exists(Path.Combine(target, PackageService.ValidationFileName)));
    }

    [Fact]
    public void CloneForEditing_DoesNotMutateOriginalAndPreservesExtensionData()
    {
        PackageDocument original = new([
            new PackageIndex(string.Empty, [new PackageBundleReference
            {
                Name = "source.ab",
                Path = @"C:\game\source.ab",
                ExtensionData = new() { ["Future"] = JsonDocument.Parse("42").RootElement.Clone() },
                AssetInfos = [new PackageAssetReference
                {
                    Name = "Hero",
                    PathId = 7,
                    ExtensionData = new() { ["Tag"] = JsonDocument.Parse("\"kept\"").RootElement.Clone() }
                }]
            }])
        ], @"C:\packages\original");

        PackageDocument clone = original.CloneForEditing();
        clone.Bundles.Single().AssetInfos.Clear();

        Assert.Single(original.Bundles.Single().AssetInfos);
        Assert.Equal(42, clone.Bundles.Single().ExtensionData!["Future"].GetInt32());
        Assert.Equal("kept", original.Bundles.Single().AssetInfos.Single().ExtensionData!["Tag"].GetString());
    }

    [Fact]
    public async Task BuildAndSaveAsync_CompletesAwaitablyAndReportsProgress()
    {
        using TemporaryDirectory temporary = new();
        string source = temporary.CreateDirectory("source");
        string sourcePath = CopyFixture(TestPaths.BundleFixture, source);
        string target = Path.Combine(temporary.Path, "package");
        List<PackageProgress> updates = [];

        PackageService service = CreateService();
        PackageDocument built = await service.BuildAndSaveAsync(
            source,
            target,
            progress: new InlineProgress<PackageProgress>(updates.Add));
        PackageDocument reopened = await service.LoadAsync(target);

        Assert.Equal(Path.GetFullPath(target), built.PackagePath);
        Assert.Equal(Path.GetFullPath(sourcePath), Assert.Single(reopened.Bundles).Path);
        Assert.Contains(updates, update => update.Phase == PackageProgressPhase.Reading);
        Assert.Contains(updates, update => update.Phase == PackageProgressPhase.Committing);
        Assert.Equal(PackageProgressPhase.Complete, updates[^1].Phase);
    }

    [Fact]
    public async Task SaveAndReopen_PreservesMetadataReferencesMemosAndExtensions()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "package");
        PackageDocument original = CreateDocument("bundle.ab", "memo");
        original.Bundles.Single().ExtensionData = new()
        {
            ["Custom"] = JsonDocument.Parse("{\"value\":7}").RootElement.Clone()
        };

        PackageService service = CreateService();
        await service.SaveAsync(original, target);
        PackageDocument reopened = await service.LoadAsync(target);

        PackageBundleReference bundle = Assert.Single(reopened.Bundles);
        Assert.Equal("bundle.ab", bundle.Name);
        Assert.Equal("memo", Assert.Single(bundle.AssetInfos).Memo);
        Assert.Equal(7, bundle.ExtensionData!["Custom"].GetProperty("value").GetInt32());
        Assert.True(File.Exists(Path.Combine(target, PackageService.ValidationFileName)));
    }

    [Fact]
    public async Task SaveAsync_DoesNotSilentlyOverwriteExistingPackage()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "package");
        PackageService service = CreateService();
        await service.SaveAsync(CreateDocument("first.ab"), target);

        await Assert.ThrowsAsync<PackageAlreadyExistsException>(() =>
            service.SaveAsync(CreateDocument("second.ab"), target));

        PackageDocument reopened = await service.LoadAsync(target);
        Assert.Equal("first.ab", Assert.Single(reopened.Bundles).Name);
    }

    [Fact]
    public async Task SaveAsync_ExplicitReplacementCommitsCompletePackageAndCleansArtifacts()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "package");
        PackageService service = CreateService();
        await service.SaveAsync(CreateDocument("first.ab"), target);

        await service.SaveAsync(CreateDocument("second.ab"), target, overwrite: true);

        PackageDocument reopened = await service.LoadAsync(target);
        Assert.Equal("second.ab", Assert.Single(reopened.Bundles).Name);
        Assert.DoesNotContain(Directory.EnumerateDirectories(temporary.Path), path =>
            path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".bak", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveAsync_CancellationLeavesExistingPackageUnchangedAndCleansStaging()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "package");
        PackageService service = CreateService();
        await service.SaveAsync(CreateDocument("first.ab"), target);

        PackageDocument replacement = new([
            new PackageIndex(string.Empty, [CreateBundle("second.ab")]),
            new PackageIndex("nested", [CreateBundle("third.ab")])
        ]);
        using CancellationTokenSource cancellation = new();
        InlineProgress<PackageProgress> progress = new(value =>
        {
            if (value.Phase == PackageProgressPhase.Writing && value.Completed == 1)
                cancellation.Cancel();
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SaveAsync(replacement, target, overwrite: true, progress, cancellation.Token));

        PackageDocument reopened = await service.LoadAsync(target);
        Assert.Equal("first.ab", Assert.Single(reopened.Bundles).Name);
        Assert.DoesNotContain(Directory.EnumerateDirectories(temporary.Path), path =>
            path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveAsync_ValidationFailureLeavesExistingPackageUnchanged()
    {
        using TemporaryDirectory temporary = new();
        string target = Path.Combine(temporary.Path, "package");
        PackageService service = CreateService();
        await service.SaveAsync(CreateDocument("first.ab"), target);
        PackageDocument invalid = CreateDocument("second.ab");
        invalid.Bundles.Single().AssetInfos = null!;

        await Assert.ThrowsAsync<PackageFormatException>(() =>
            service.SaveAsync(invalid, target, overwrite: true));

        PackageDocument reopened = await service.LoadAsync(target);
        Assert.Equal("first.ab", Assert.Single(reopened.Bundles).Name);
    }

    [Fact]
    public async Task LoadAsync_RejectsMalformedIndex()
    {
        using TemporaryDirectory temporary = new();
        string package = temporary.CreateDirectory("package");
        await File.WriteAllTextAsync(Path.Combine(package, PackageService.IndexFileName), "[{ invalid]");
        await File.WriteAllTextAsync(Path.Combine(package, PackageService.ValidationFileName), string.Empty);

        PackageFormatException exception = await Assert.ThrowsAsync<PackageFormatException>(() =>
            CreateService().LoadAsync(package));

        Assert.EndsWith(PackageService.IndexFileName, exception.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_ReportsMissingSourceWithoutDiscardingReference()
    {
        PackageDocument package = await CreateService().LoadAsync(TestPaths.LegacyPackageFixture);

        PackageBundleReference bundle = Assert.Single(package.Bundles);
        Assert.Equal("C:\\LegacyGame\\Data\\character.ab", bundle.Path);
        Assert.Contains(package.ValidationIssues, issue =>
            issue.Severity == PackageIssueSeverity.Warning && issue.Code == "missing-source-file");
    }

    [Fact]
    public async Task LoadAsync_SupportsRepeatedOperationsWithoutLockingIndexes()
    {
        PackageService service = CreateService();
        for (int index = 0; index < 3; index++)
            Assert.Single((await service.LoadAsync(TestPaths.LegacyPackageFixture)).Bundles);

        string indexPath = Path.Combine(TestPaths.LegacyPackageFixture, PackageService.IndexFileName);
        using FileStream exclusive = new(indexPath, FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.True(exclusive.Length > 0);
    }

    [Fact]
    public void Document_AddAndRemoveReference_UsesFullSourcePathNotPathIdAlone()
    {
        PackageDocument package = new([new PackageIndex(string.Empty)]);
        PackageBundleReference first = CreateBundle(Path.Combine("root", "first.ab"));
        PackageBundleReference second = CreateBundle(Path.Combine("root", "second.ab"));
        first.AssetInfos[0].PathId = 55;
        second.AssetInfos[0].PathId = 55;

        package.AddReference(string.Empty, first);
        package.AddReference("nested", second);

        Assert.Equal(2, package.Bundles.Count());
        Assert.True(package.RemoveReference(first.Path));
        Assert.Equal(second.Path, Assert.Single(package.Bundles).Path);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyMarkedPackages()
    {
        using TemporaryDirectory temporary = new();
        PackageService service = CreateService();
        await service.SaveAsync(CreateDocument("one.ab"), Path.Combine(temporary.Path, "one"));
        string unmarked = temporary.CreateDirectory("unmarked");
        await File.WriteAllTextAsync(Path.Combine(unmarked, PackageService.IndexFileName), "[]");

        IReadOnlyList<PackageSummary> packages = await service.ListAsync(temporary.Path);

        PackageSummary package = Assert.Single(packages);
        Assert.Equal("one", package.Name);
        Assert.Equal(1, package.BundleCount);
    }

    [Fact]
    public async Task SaveThenFreshService_RediscoversAndLoadsFromCatalogRootOrPackageDirectory()
    {
        using TemporaryDirectory temporary = new();
        string source = CopyFixture(TestPaths.SerializedFixture, temporary.Path);
        string catalogRoot = temporary.CreateDirectory("catalog");
        string emptyTarget = Path.Combine(catalogRoot, "empty");
        string populatedTarget = Path.Combine(catalogRoot, "populated");

        PackageService writer = CreateService();
        await writer.SaveAsync(new PackageDocument([new PackageIndex(string.Empty)]), emptyTarget);
        PackageBundleReference reference = await writer.CreateReferenceAsync(source);
        await writer.SaveAsync(
            new PackageDocument([new PackageIndex(string.Empty, [reference])]),
            populatedTarget);

        PackageService freshCatalogService = CreateService();
        IReadOnlyList<PackageCatalogEntry> catalog = await freshCatalogService.ReadCatalogAsync(catalogRoot);
        Assert.Equal(["empty", "populated"], catalog.Select(entry => entry.Name));
        Assert.All(catalog, entry => Assert.True(entry.IsLoaded, entry.ErrorMessage));

        PackageService freshPackageService = CreateService();
        PackageCatalogEntry direct = Assert.Single(await freshPackageService.ReadCatalogAsync(populatedTarget));
        Assert.Equal("populated", direct.Name);
        PackageBundleReference reopenedReference = Assert.Single(direct.Package!.Bundles);
        Assert.Equal(Path.GetFullPath(source), reopenedReference.Path);
        Assert.NotEmpty(reopenedReference.AssetInfos);

        string relativeCatalog = Path.GetRelativePath(Environment.CurrentDirectory, catalogRoot);
        IReadOnlyList<PackageCatalogEntry> relative = await new PackageService(TestPaths.ClassData)
            .ReadCatalogAsync(relativeCatalog);
        Assert.Equal(2, relative.Count);
    }

    [Fact]
    public async Task ReadCatalogAsync_IndividualMalformedOrMissingMarkerPackageRemainsVisibleWithDiagnostic()
    {
        using TemporaryDirectory temporary = new();
        string malformed = temporary.CreateDirectory("malformed");
        await File.WriteAllTextAsync(Path.Combine(malformed, PackageService.ValidationFileName), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(malformed, PackageService.IndexFileName), "not json");

        PackageCatalogEntry malformedEntry = Assert.Single(await CreateService().ReadCatalogAsync(malformed));
        Assert.False(malformedEntry.IsLoaded);
        Assert.Contains("malformed", malformedEntry.ErrorMessage!, StringComparison.OrdinalIgnoreCase);

        string missingMarker = temporary.CreateDirectory("missing-marker");
        await File.WriteAllTextAsync(Path.Combine(missingMarker, PackageService.IndexFileName), "[]");
        PackageCatalogEntry markerEntry = Assert.Single(await CreateService().ReadCatalogAsync(missingMarker));
        Assert.True(markerEntry.IsLoaded);
        Assert.Contains(markerEntry.ValidationIssues, issue => issue.Code == "missing-validation-marker");
    }

    private static PackageService CreateService() => new(TestPaths.ClassData);

    private static PackageDocument CreateDocument(string name, string? memo = null)
        => new([new PackageIndex(string.Empty, [CreateBundle(name, memo)])]);

    private static PackageBundleReference CreateBundle(string name, string? memo = null)
        => new()
        {
            Name = Path.GetFileName(name),
            Path = Path.GetFullPath(name),
            CabCode = "CAB-test",
            AssetInfos = [new PackageAssetReference { Name = "asset", PathId = 10, Memo = memo }],
            Dependencies = ["CAB-dependency"]
        };

    private static string CopyFixture(string fixture, string destinationDirectory)
    {
        string destination = Path.Combine(destinationDirectory, Path.GetFileName(fixture));
        File.Copy(fixture, destination);
        return destination;
    }

    private sealed class InlineProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }

    private sealed class ControlledFolderFileSystem(
        IReadOnlyDictionary<string, string[]> directories,
        IReadOnlyDictionary<string, string[]> files,
        IReadOnlyDictionary<string, FileAttributes> attributes,
        string inaccessibleDirectory) : IPackageFolderFileSystem
    {
        public List<string> EnumeratedDirectories { get; } = [];

        public IEnumerable<string> EnumerateFiles(string directory)
        {
            if (directory == inaccessibleDirectory)
                throw new UnauthorizedAccessException("Access denied for test directory.");
            return files.TryGetValue(directory, out string[]? entries) ? entries : [];
        }

        public IEnumerable<string> EnumerateDirectories(string directory)
        {
            EnumeratedDirectories.Add(directory);
            return directories.TryGetValue(directory, out string[]? entries) ? entries : [];
        }

        public FileAttributes GetAttributes(string path) => attributes[path];
    }

    private sealed class CancellingFolderFileSystem(CancellationTokenSource cancellation) : IPackageFolderFileSystem
    {
        public IEnumerable<string> EnumerateFiles(string directory)
        {
            cancellation.Cancel();
            yield return Path.Combine(directory, "never-returned.assets");
        }

        public IEnumerable<string> EnumerateDirectories(string directory) => [];
        public FileAttributes GetAttributes(string path) => FileAttributes.Normal;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"uabs-package-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateDirectory(string relativePath)
            => Directory.CreateDirectory(System.IO.Path.Combine(Path, relativePath)).FullName;

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
