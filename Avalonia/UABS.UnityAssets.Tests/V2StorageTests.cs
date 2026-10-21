using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using UABS.PackageV2;
using UABS.UnityAssets;
using UABS.UnityAssets.Metadata;

namespace UABS.UnityAssets.Tests;

public sealed class V2StorageTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private sealed class Fixture : IDisposable
    {
        public string DirectoryPath { get; } = Directory.CreateDirectory(Path.Combine(
            Environment.GetEnvironmentVariable("UABS_V2_TEST_ROOT") ?? Path.GetTempPath(), "uabs-v2-storage-" + Guid.NewGuid().ToString("N"))).FullName;
        public PackageBuildRequest Request(bool partial = false) => new(Path.Combine(DirectoryPath, "output.uabs.sqlite"),
            Guid.NewGuid(), Guid.NewGuid(), "test package", [new("game", DirectoryPath, OperatingSystem.IsWindows() ? "windows-lexical-v1" : "posix-lexical-v1")],
            [new(0, "source.bundle", "", new(Path.Combine(DirectoryPath, "source.bundle"), Guid.NewGuid(), Guid.NewGuid()))], partial);
        public void Dispose() => Directory.Delete(DirectoryPath, true);
    }
    private sealed class SyntheticExtractor(int assets = 6, int files = 2,
        V2ExtractionStatus status = V2ExtractionStatus.Complete, bool duplicates = true,
        Func<V2Dependency, V2Dependency>? dependencyMap = null) : IV2MetadataExtractor
    {
        public async Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
            Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive, V2ExtractionOptions? options = null,
            IProgress<V2ExtractionProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            List<V2MetadataRecord> batch = [];
            async ValueTask Emit(V2MetadataRecord row)
            {
                cancellationToken.ThrowIfCancellationRequested(); batch.Add(row);
                if (batch.Count == 128) { await receive(batch.ToArray(), cancellationToken); batch.Clear(); }
            }
            for (int f = 0; f < files; f++)
            {
                int ordinal = f * 2 + 1;
                await Emit(new V2AssetType(ordinal, 0, 28, null, "Texture2D"));
                await Emit(new V2AssetType(ordinal, 1, 1234567, null, null));
                for (int i = 0; i < assets; i++)
                {
                    long id = i == 0 ? -77 : i == 1 ? long.MaxValue : duplicates && i is 3 or 4 ? 900 : i;
                    string? name = i == 0 ? null : i == 1 ? "" : "duplicate";
                    await Emit(new V2Asset(ordinal, i, id, i == 2 ? 1234567 : 28, i == 2 ? 1 : 0, null, i * 32L, 16,
                        V2IdentityStatus.Addressable, name, name is null ? V2NameStatus.Failed : V2NameStatus.Present, i == 2 ? null : "Texture2D"));
                }
                V2Dependency dependency = new(ordinal, "external_file", 0, null, "CAB-duplicate", "CAB-duplicate", "", "CAB-duplicate", "010203048090A0B0FFFFFFFF11223344", 1);
                dependency = dependencyMap is null ? dependency : dependencyMap(dependency);
                await Emit(dependency);
                await Emit(new V2Dependency(ordinal, "external_file", dependency.Kind == "external_file" ? 1 : 0, null, "", "", "", null, new string('0', 32), 0));
                await Emit(new V2Diagnostic(ordinal, 0, "name", "name-failed", "Synthetic optional-name failure."));
                await Emit(new V2LogicalFile(ordinal, "CAB-duplicate", "CAB-duplicate", "2022.3.57f1", 22, true, assets, assets,
                    status == V2ExtractionStatus.Partial ? status : V2ExtractionStatus.Complete, V2DependencyStatus.Complete));
            }
            if (batch.Count > 0) await receive(batch.ToArray(), cancellationToken);
            return new(new(request.SourceId, request.RevisionId, request.Path, Path.GetFullPath(request.Path), UnityFileKind.Bundle,
                1234, DateTime.UnixEpoch, new string('A', 64), status is V2ExtractionStatus.Complete or V2ExtractionStatus.Partial),
                status, files, (long)assets * files, new(default, default, default, default, default, 0, 0, 128));
        }
    }
    private sealed class MappedExtractor(IV2MetadataExtractor inner, Func<V2ExtractionResult, V2ExtractionResult> map) : IV2MetadataExtractor
    {
        public async Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request, Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive,
            V2ExtractionOptions? options = null, IProgress<V2ExtractionProgress>? progress = null, CancellationToken cancellationToken = default)
            => map(await inner.ExtractAsync(request, receive, options, progress, cancellationToken));
    }
    private static async Task<PackageReader> Open(string path)
    {
        AdmissionResult result = await PackageReader.OpenAsync(path);
        Assert.Equal(StorageStatus.Success, result.Status); Assert.NotNull(result.Reader); return result.Reader;
    }
    private static void Edit(string path, string command)
    {
        using SqliteConnection c = new(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, ForeignKeys = false }.ToString());
        c.Open(); c.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
        using SqliteCommand sql = c.CreateCommand(); sql.CommandText = command; sql.ExecuteNonQuery();
    }

    [Fact]
    public async Task SyntheticRoundTrip_PreservesEveryIdentityAndMetadataCategory()
    {
        using Fixture f = new(); PackageBuildRequest request = f.Request();
        PackageBuildResult result = await new PackageBuilder(new SyntheticExtractor()).BuildAsync(request);
        Assert.True(result.Succeeded, result.Message);
        using PackageReader reader = await Open(request.Destination);
        Assert.Equal(request.PackageId, reader.Info.PackageId); Assert.Equal(request.GenerationId, reader.Info.GenerationId);
        Assert.Equal(2, reader.Info.SchemaMajor); Assert.Equal(0, reader.Info.SchemaMinor);
        Assert.Equal("dotnet-ordinal-v1", reader.Info.TextContract); Assert.NotEmpty(reader.Info.CreatedUtc); Assert.NotEmpty(reader.Info.IndexerVersion);
        Assert.Equal("complete", reader.Info.IndexStatus); Assert.Equal(12, reader.Info.Assets);
        var assets = await reader.ReadAssetsAsync(); Assert.Equal(12, assets.Count);
        Assert.All(assets, a => { Assert.Equal(request.Sources[0].Request.SourceId, a.Key.SourceId); Assert.Equal(request.Sources[0].Request.RevisionId, a.Key.SourceRevision); });
        Assert.Equal(new[] { 1, 3 }, assets.Select(a => a.Key.DirectoryOrdinal).Distinct());
        Assert.Contains(assets, a => a.Key.PathId == -77); Assert.Contains(assets, a => a.Key.PathId == long.MaxValue);
        Assert.Contains(assets, a => a.ClassId == 1234567); Assert.Contains(assets, a => a.Name is null); Assert.Contains(assets, a => a.Name == "");
        Assert.All(assets.Where(a => a.Key.PathId == 900), a => Assert.Equal("ambiguous", a.IdentityStatus));
        Assert.All(assets, a => Assert.Null(a.Memo));
        var dependencies = await reader.ReadMetadataAsync(MetadataTable.Dependencies);
        Assert.Equal(new long[] { 0, 1, 0, 1 }, dependencies.Select(d => (long)d.Values["declaration_ordinal"]!));
        Assert.Equal("010203048090A0B0FFFFFFFF11223344", Convert.ToHexString((byte[])dependencies[0].Values["guid_bytes"]!));
        Assert.Single(await reader.ReadMetadataAsync(MetadataTable.SourceRoots));
        Assert.Single(await reader.ReadMetadataAsync(MetadataTable.Sources));
        Assert.Equal(2, (await reader.ReadMetadataAsync(MetadataTable.LogicalFiles)).Count);
        Assert.Equal(4, (await reader.ReadMetadataAsync(MetadataTable.Types)).Count);
        Assert.Equal(2, (await reader.ReadMetadataAsync(MetadataTable.Issues)).Count);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task GenuineFixture_RoundTrips43Assets(bool bundle)
    {
        using Fixture f = new(); var request = f.Request();
        string path = bundle ? TestPaths.BundleFixture : TestPaths.SerializedFixture;
        request = request with { Roots = [request.Roots[0] with { OriginalBasePath = Path.GetDirectoryName(path)! }], Sources = [request.Sources[0] with { RelativePath = Path.GetFileName(path), Request = request.Sources[0].Request with { Path = path } }] };
        var result = await new PackageBuilder(new AssetsToolsV2MetadataExtractor(TestPaths.ClassData)).BuildAsync(request);
        Assert.True(result.Succeeded, result.Message);
        using PackageReader reader = await Open(request.Destination);
        Assert.Equal(43, reader.Info.Assets); Assert.Equal(5, (await reader.ReadAssetsAsync()).Select(a => a.ClassId).Distinct().Count());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PartialCoverage_RequiresExplicitAcknowledgement(bool acknowledged)
    {
        using Fixture f = new(); var request = f.Request(acknowledged);
        var result = await new PackageBuilder(new SyntheticExtractor(status: V2ExtractionStatus.Partial)).BuildAsync(request);
        Assert.Equal(acknowledged ? StorageStatus.Success : StorageStatus.PartialAcknowledgementRequired, result.Status);
        Assert.Equal(acknowledged, File.Exists(request.Destination));
        if (acknowledged) { using PackageReader reader = await Open(request.Destination); Assert.Equal("partial", reader.Info.IndexStatus); }
    }

    [Theory]
    [InlineData(V2ExtractionStatus.Failed)] [InlineData(V2ExtractionStatus.RevisionMismatch)] [InlineData(V2ExtractionStatus.Unsupported)]
    public async Task RejectedSource_DiscardsAllProvisionalRowsButKeepsFailureEvidence(V2ExtractionStatus sourceStatus)
    {
        using Fixture f = new(); var request = f.Request(true);
        var result = await new PackageBuilder(new SyntheticExtractor(status: sourceStatus)).BuildAsync(request);
        Assert.True(result.Succeeded, result.Message);
        using PackageReader reader = await Open(request.Destination);
        Assert.Equal(0, reader.Info.Assets); Assert.Empty(await reader.ReadMetadataAsync(MetadataTable.LogicalFiles));
        Assert.Empty(await reader.ReadMetadataAsync(MetadataTable.Types)); Assert.Empty(await reader.ReadMetadataAsync(MetadataTable.Dependencies));
        Assert.NotEmpty(await reader.ReadMetadataAsync(MetadataTable.Issues));
        var source = Assert.Single(await reader.ReadMetadataAsync(MetadataTable.Sources)); Assert.Null(source.Values["content_sha256"]);
    }

    [Theory]
    [InlineData(BuildBoundary.BeforeSchema)] [InlineData(BuildBoundary.AfterSchema)] [InlineData(BuildBoundary.BeforeBatch)]
    [InlineData(BuildBoundary.BeforeValidation)] [InlineData(BuildBoundary.AfterValidation)] [InlineData(BuildBoundary.BeforePublication)]
    public async Task FaultBoundary_NeverPublishesIncompleteOutput(BuildBoundary boundary)
    {
        using Fixture f = new(); var request = f.Request();
        var result = await new PackageBuilder(new SyntheticExtractor(), (point, _) => { if (point == boundary) throw new IOException("injected fault"); }).BuildAsync(request);
        Assert.False(result.Succeeded); Assert.False(File.Exists(request.Destination)); Assert.Empty(Directory.GetFiles(f.DirectoryPath));
    }

    [Fact]
    public async Task DestinationRace_UsesNoOverwritePublication()
    {
        using Fixture f = new(); var request = f.Request();
        var result = await new PackageBuilder(new SyntheticExtractor(), (point, _) =>
        { if (point == BuildBoundary.BeforePublication) File.WriteAllText(request.Destination, "other owner's file"); }).BuildAsync(request);
        Assert.Equal(StorageStatus.DestinationExists, result.Status); Assert.Equal("other owner's file", File.ReadAllText(request.Destination));
        Assert.Single(Directory.GetFiles(f.DirectoryPath));
    }

    [Fact]
    public async Task ExistingDestination_IsUntouched()
    {
        using Fixture f = new(); var request = f.Request(); File.WriteAllText(request.Destination, "sentinel");
        var result = await new PackageBuilder(new SyntheticExtractor()).BuildAsync(request);
        Assert.Equal(StorageStatus.DestinationExists, result.Status); Assert.Equal("sentinel", File.ReadAllText(request.Destination));
    }

    [Fact]
    public async Task CancellationDuringIngestion_CleansOnlyOwnedStaging()
    {
        using Fixture f = new(); using CancellationTokenSource cancellation = new(); var request = f.Request();
        var result = await new PackageBuilder(new SyntheticExtractor(1000), (point, _) =>
        { if (point == BuildBoundary.BeforeBatch) cancellation.Cancel(); }).BuildAsync(request, cancellation.Token);
        Assert.Equal(StorageStatus.Cancelled, result.Status); Assert.Empty(Directory.GetFiles(f.DirectoryPath));
    }

    [Theory]
    [InlineData("PRAGMA user_version=30000", StorageStatus.UnsupportedVersion)]
    [InlineData("DROP TRIGGER asset_memo_changed", StorageStatus.InvalidPackage)]
    [InlineData("CREATE TABLE sqliteevil(payload TEXT)", StorageStatus.InvalidPackage)]
    [InlineData("UPDATE asset SET file_id=999999 WHERE asset_id=1", StorageStatus.InvalidPackage)]
    [InlineData("UPDATE asset SET class_id=999 WHERE asset_id=1", StorageStatus.InvalidPackage)]
    [InlineData("UPDATE asset SET identity_status='addressable' WHERE asset_id=4", StorageStatus.InvalidPackage)]
    [InlineData("UPDATE logical_file SET declared_asset_count=999", StorageStatus.InvalidPackage)]
    [InlineData("UPDATE source SET locator_key='wrong'", StorageStatus.InvalidPackage)]
    [InlineData("UPDATE asset SET name_ascii_key='wrong' WHERE asset_id=2", StorageStatus.InvalidPackage)]
    public async Task Admission_RejectsVersionSchemaForeignKeyAndSemanticCorruption(string mutation, StorageStatus expected)
    {
        using Fixture f = new(); var request = f.Request(); Assert.True((await new PackageBuilder(new SyntheticExtractor()).BuildAsync(request)).Succeeded);
        Edit(request.Destination, mutation);
        AdmissionResult admission = await PackageReader.OpenAsync(request.Destination);
        Assert.Equal(expected, admission.Status); Assert.Null(admission.Reader);
    }

    [Fact]
    public async Task UnsupportedDerivedProfile_ExposesOnlyRawValuesAndPreservesNullEmptyMemos()
    {
        using Fixture f = new(); var request = f.Request(); Assert.True((await new PackageBuilder(new SyntheticExtractor()).BuildAsync(request)).Succeeded);
        Edit(request.Destination, "UPDATE asset SET memo='' WHERE asset_id=2; UPDATE package SET derived_text_profile='unsupported-profile'; UPDATE asset SET name_ascii_key=CASE WHEN name IS NULL THEN NULL ELSE 'untrusted' END,memo_nonblank=1;");
        byte[] before = SHA256.HashData(File.ReadAllBytes(request.Destination));
        using (PackageReader reader = await Open(request.Destination))
        {
            Assert.False(reader.Info.DerivedFieldsCompatible); var rows = await reader.ReadAssetsAsync();
            Assert.Null(rows[0].Memo); Assert.Equal("", rows[1].Memo); Assert.Equal(1, rows[1].MemoRevision);
        }
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(request.Destination)));
        Assert.False(File.Exists(request.Destination + "-journal"));
    }

    [Fact]
    public async Task PagingIsBoundedAndNeedsNoOriginalSourceFile()
    {
        using Fixture f = new(); var request = f.Request(); Assert.False(File.Exists(request.Sources[0].Request.Path));
        var result = await new PackageBuilder(new SyntheticExtractor(1000, files: 1, duplicates: false)).BuildAsync(request);
        Assert.True(result.Succeeded, result.Message); Assert.InRange(result.Metrics.MaxBatch, 1, 128);
        using PackageReader reader = await Open(request.Destination);
        long after = 0; int count = 0;
        while (true)
        {
            var page = await reader.ReadAssetsAsync(after, 31); if (page.Count == 0) break;
            Assert.InRange(page.Count, 1, 31); Assert.True(page[0].RowId > after); after = page[^1].RowId; count += page.Count;
        }
        Assert.Equal(1000, count);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => reader.ReadAssetsAsync(pageSize: 501));
    }

    [Theory]
    [InlineData("../escape")] [InlineData("/absolute")] [InlineData("C:\\absolute")] [InlineData("a/./b")]
    public async Task UnsafeRelativePath_IsRejected(string relative)
    {
        using Fixture f = new(); var request = f.Request(); request = request with { Sources = [request.Sources[0] with { RelativePath = relative }] };
        Assert.False((await new PackageBuilder(new SyntheticExtractor()).BuildAsync(request)).Succeeded); Assert.Empty(Directory.GetFiles(f.DirectoryPath));
    }

    [Theory]
    [InlineData("external_file", "resources/unity_builtin_extra", "archive:/different/CAB-virtual")]
    [InlineData("external_file", "library/unity default resources", null)]
    [InlineData("external_file", "Mixed/Original\\CAB-Case", "")]
    [InlineData("external_file", "资源/原始路径", "虚拟:/目录/资源")]
    [InlineData("external_file", "", "virtual:/nonempty")]
    [InlineData("external_file", null, null)]
    [InlineData("assetbundle_dependency", "verbatim/Bundle依赖", "")]
    public async Task DependencyPaths_RoundTripIndependentRawEvidence(string kind, string? original, string? virtualPath)
    {
        using Fixture f = new(); var request = f.Request();
        var extractor = new SyntheticExtractor(dependencyMap: d => d with {
            Kind = kind, Path = kind == "assetbundle_dependency" ? original : original is null ? null : "Resources/normalized",
            OriginalPath = kind == "external_file" ? original : null, VirtualPath = virtualPath });
        var result = await new PackageBuilder(extractor).BuildAsync(request);
        Assert.True(result.Succeeded, result.Message);
        using PackageReader reader = await Open(request.Destination);
        var dependencies = await reader.ReadMetadataAsync(MetadataTable.Dependencies);
        Assert.Equal(kind == "external_file" ? new long[] { 0, 1, 0, 1 } : new long[] { 0, 0, 0, 0 },
            dependencies.Select(d => (long)d.Values["declaration_ordinal"]!));
        foreach (var dependency in dependencies.Where(d => d.Values["raw_cab"] is not null))
        {
            Assert.Equal(original, dependency.Values["raw_path"]);
            Assert.Equal(virtualPath, dependency.Values["raw_virtual_path"]);
            Assert.Equal("CAB-duplicate", dependency.Values["raw_cab"]);
            Assert.Equal("010203048090A0B0FFFFFFFF11223344", Convert.ToHexString((byte[])dependency.Values["guid_bytes"]!));
            Assert.Equal(1L, dependency.Values["external_type"]);
        }
        Assert.All(dependencies.Where(d => d.Values["raw_cab"] is null), d => {
            Assert.Equal("", d.Values["raw_path"]); Assert.Equal("", d.Values["raw_virtual_path"]); });
    }

    [Fact]
    public async Task MissingOriginalEvidence_DoesNotSubstituteNormalizedPath()
    {
        using Fixture f = new(); var request = f.Request();
        var result = await new PackageBuilder(new SyntheticExtractor(dependencyMap: d => d with { OriginalPath = null })).BuildAsync(request);
        Assert.Equal(StorageStatus.ValidationFailure, result.Status); Assert.Contains("original serialized path evidence", result.Message);
        Assert.False(File.Exists(request.Destination));
    }

    [Fact]
    public async Task PreAmendmentSchema_MissingVirtualPath_IsRejectedWithoutModification()
    {
        using Fixture f = new(); var request = f.Request();
        Assert.True((await new PackageBuilder(new SyntheticExtractor()).BuildAsync(request)).Succeeded);
        Edit(request.Destination, "ALTER TABLE dependency DROP COLUMN raw_virtual_path");
        byte[] before = SHA256.HashData(File.ReadAllBytes(request.Destination));
        var admission = await PackageReader.OpenAsync(request.Destination);
        Assert.Equal(StorageStatus.InvalidPackage, admission.Status); Assert.Null(admission.Reader);
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(request.Destination)));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MissingEvidenceOrIncorrectFinalCounts_PreventPublication(bool wrongCount)
    {
        using Fixture f = new(); var request = f.Request();
        var extractor = new MappedExtractor(new SyntheticExtractor(), r => wrongCount
            ? r with { CapturedAssets = r.CapturedAssets + 1 } : r with { Source = r.Source with { Sha256 = null } });
        var result = await new PackageBuilder(extractor).BuildAsync(request);
        Assert.Equal(StorageStatus.ValidationFailure, result.Status); Assert.Empty(Directory.GetFiles(f.DirectoryPath));
    }

    [Fact]
    public async Task EmptyReadableSource_IsNotTheSameAsUnreadableSource()
    {
        using Fixture f = new(); var request = f.Request();
        var result = await new PackageBuilder(new SyntheticExtractor(assets: 0, files: 1)).BuildAsync(request);
        Assert.True(result.Succeeded, result.Message);
        using PackageReader reader = await Open(request.Destination);
        Assert.Equal(0, reader.Info.Assets); Assert.Equal("complete", reader.Info.IndexStatus);
        var file = Assert.Single(await reader.ReadMetadataAsync(MetadataTable.LogicalFiles));
        Assert.Equal(0L, file.Values["declared_asset_count"]);
    }

    [Fact]
    public async Task ExistingValidPackage_SurvivesRejectedCreationByteForByte()
    {
        using Fixture f = new(); var request = f.Request(); Assert.True((await new PackageBuilder(new SyntheticExtractor()).BuildAsync(request)).Succeeded);
        byte[] before = SHA256.HashData(File.ReadAllBytes(request.Destination));
        var result = await new PackageBuilder(new SyntheticExtractor(), (_, _) => throw new IOException("must not reach injection")).BuildAsync(request);
        Assert.Equal(StorageStatus.DestinationExists, result.Status);
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(request.Destination)));
    }

    [Fact]
    public async Task InvalidHeaderAdmission_DoesNotCreateOrRepairAnything()
    {
        using Fixture f = new(); string path = Path.Combine(f.DirectoryPath, "bad.uabs.sqlite"); File.WriteAllText(path, "not SQLite content");
        byte[] before = File.ReadAllBytes(path);
        Assert.Equal(StorageStatus.InvalidPackage, (await PackageReader.OpenAsync(path)).Status);
        Assert.Equal(before, File.ReadAllBytes(path));
        string missing = Path.Combine(f.DirectoryPath, "missing.uabs.sqlite"); Assert.Equal(StorageStatus.InvalidPackage, (await PackageReader.OpenAsync(missing)).Status);
        Assert.False(File.Exists(missing));
    }

    [Fact]
    public async Task ReaderRevisionChanges_RequireExplicitReadmission()
    {
        using Fixture f = new(); var request = f.Request(); Assert.True((await new PackageBuilder(new SyntheticExtractor()).BuildAsync(request)).Succeeded);
        using PackageReader reader = await Open(request.Destination);
        Edit(request.Destination, "UPDATE package SET catalog_revision=catalog_revision+1");
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadAssetsAsync());
    }

    [Fact]
    public async Task CancellationBeforePublication_PreservesDestinationAndCleansStaging()
    {
        using Fixture f = new(); using CancellationTokenSource cancellation = new(); var request = f.Request();
        var result = await new PackageBuilder(new SyntheticExtractor(), (point, _) =>
        { if (point == BuildBoundary.BeforePublication) cancellation.Cancel(); }).BuildAsync(request, cancellation.Token);
        Assert.Equal(StorageStatus.Cancelled, result.Status); Assert.Empty(Directory.GetFiles(f.DirectoryPath));
    }

    [Fact]
    public void StorageDoesNotIntroduceUnityOrAnApplicationReference()
    {
        var references = typeof(PackageBuilder).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "").ToArray();
        Assert.DoesNotContain(references, name => name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) || name.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("UABS", references);
    }

    [Fact]
    [Trait("Category", "V2StorageBenchmark")]
    public async Task OptInStorageMeasurements()
    {
        if (Environment.GetEnvironmentVariable("UABS_V2_STORAGE_BENCHMARK") != "1") return;
        int[] scales = Environment.GetEnvironmentVariable("UABS_V2_STORAGE_MILLION") == "1" ? [1000000] : [10000, 100000];
        foreach (int size in scales)
        {
            List<(PackageBuildResult Build, double Admission, double First, double Next, double Read)> samples = [];
            for (int repeat = 0; repeat < 3; repeat++)
            {
                using Fixture fixture = new(); var request = fixture.Request();
                var built = await new PackageBuilder(new SyntheticExtractor(size, files: 1, duplicates: false)).BuildAsync(request);
                Assert.True(built.Succeeded, built.Message); Stopwatch time = Stopwatch.StartNew();
                using PackageReader reader = await Open(request.Destination); double admission = time.Elapsed.TotalMilliseconds;
                time.Restart(); var first = await reader.ReadAssetsAsync(); double firstMs = time.Elapsed.TotalMilliseconds;
                time.Restart(); var next = await reader.ReadAssetsAsync(first[^1].RowId); double nextMs = time.Elapsed.TotalMilliseconds;
                time.Restart(); long after = 0, count = 0;
                while (true) { var page = await reader.ReadAssetsAsync(after, 500); if (page.Count == 0) break; after = page[^1].RowId; count += page.Count; }
                Assert.Equal(size, count); samples.Add((built, admission, firstMs, nextMs, time.Elapsed.TotalMilliseconds));
            }
            double Median(Func<(PackageBuildResult Build, double Admission, double First, double Next, double Read), double> get) => samples.Select(get).Order().ElementAt(1);
            output.WriteLine($"assets={size} totalMs={Median(s => s.Build.Metrics.Total.TotalMilliseconds):F2} schemaMs={Median(s => s.Build.Metrics.Schema.TotalMilliseconds):F2} ingestMs={Median(s => s.Build.Metrics.Ingestion.TotalMilliseconds):F2} validateMs={Median(s => s.Build.Metrics.Validation.TotalMilliseconds):F2} publishMs={Median(s => s.Build.Metrics.Publication.TotalMilliseconds):F2} bytes={Median(s => s.Build.Metrics.FileBytes):F0} allocatedBytes={Median(s => s.Build.Metrics.ManagedAllocatedBytes):F0} processPeakBytes={Median(s => s.Build.Metrics.ProcessPeakWorkingSetBytes):F0} admitMs={Median(s => s.Admission):F2} first100Ms={Median(s => s.First):F2} next100Ms={Median(s => s.Next):F2} readAllMs={Median(s => s.Read):F2}");
        }
    }
}
