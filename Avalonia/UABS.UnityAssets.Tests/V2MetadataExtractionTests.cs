using System.Security.Cryptography;
using AssetsTools.NET;
using UABS.UnityAssets.Metadata;

namespace UABS.UnityAssets.Tests;

public sealed class V2MetadataExtractionTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Theory]
    [InlineData("UWVT")]
    [InlineData("U")]
    public async Task ResourceFalsePositive_NegativeSignedVersionNeverReachesMetadataAllocation(string apparentVersion)
    {
        byte[] resource = new byte[64];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(resource.AsSpan(8), 0xE06E6010);
        System.Text.Encoding.ASCII.GetBytes(apparentVersion).CopyTo(resource, 20);
        using (AssetsFileReader oldProbe = new(new MemoryStream(resource)))
            Assert.True(AssetsFile.IsAssetsFile(oldProbe, 0, resource.Length)); // Probe only; never invoke unsafe metadata parse.
        using V2MetadataFixture fixture = new();
        var (result, rows) = await Run(Request(fixture.Save(V2MetadataFixture.Bundle("CAB-valid", leadingResource: resource), "false-positive.bundle")));
        Assert.Equal(V2ExtractionStatus.Complete, result.Status); Assert.True(result.Source.Verified);
        Assert.Equal(12, result.CapturedAssets); Assert.Equal(new[] { 1, 3 }, rows.OfType<V2LogicalFile>().Select(f => f.DirectoryOrdinal));
        Assert.DoesNotContain(rows.OfType<V2Diagnostic>(), d => d.Code == "table-unreadable");
    }

    [Fact]
    public async Task ValidUnflaggedSerializedFiles_AreRecognizedByHeaderNotFilenameSuffix()
    {
        using V2MetadataFixture fixture = new();
        var (result, rows) = await Run(Request(fixture.Save(V2MetadataFixture.Bundle("valid.resS", unflaggedLogical: true), "unflagged.bundle")));
        Assert.Equal(V2ExtractionStatus.Complete, result.Status); Assert.Equal(12, result.CapturedAssets);
        Assert.Equal(new[] { 1, 3 }, rows.OfType<V2LogicalFile>().Select(f => f.DirectoryOrdinal));
    }

    [Fact]
    public async Task FlaggedFutureVersion_RemainsFailedCoverageWithoutMetadataParsing()
    {
        byte[] bytes = V2MetadataFixture.Serialized(); System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8), 23);
        using V2MetadataFixture fixture = new();
        var (result, rows) = await Run(Request(fixture.Save(V2MetadataFixture.Bundle("future", serializedOverride: bytes), "future.bundle")));
        Assert.Equal(V2ExtractionStatus.Partial, result.Status); Assert.Equal(0, result.CapturedAssets);
        Assert.All(rows.OfType<V2LogicalFile>(), f => { Assert.Equal(V2ExtractionStatus.Failed, f.Status); Assert.Equal(23u, f.SerializedVersion); Assert.Null(f.DeclaredAssetCount); });
        Assert.All(rows.OfType<V2Diagnostic>().Where(d => d.Code == "table-unreadable"), d => Assert.Contains("stage=header", d.Message));
    }

    private static int TypeCountPosition(byte[] serialized)
    {
        using AssetsFileReader reader = new(new MemoryStream(serialized)); var header = new AssetsFileHeader(); header.Read(reader);
        reader.ReadNullTerminated(); reader.ReadUInt32(); if (header.Version >= 13) reader.ReadBoolean(); return (int)reader.Position;
    }

    [Fact]
    public async Task MalformedTypeCount_ReportsSafeHeaderParserAndScalarEvidenceWithoutClaimingCompleteCoverage()
    {
        byte[] bytes = V2MetadataFixture.Serialized(0); int countOffset = TypeCountPosition(bytes);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(countOffset), -1);
        using V2MetadataFixture fixture = new(); string path = fixture.Save(bytes);
        var (result, rows) = await Run(Request(path)); var diagnostic = Assert.Single(rows.OfType<V2Diagnostic>(), d => d.Code == "table-unreadable");
        Assert.Equal(V2ExtractionStatus.Partial, result.Status); Assert.Equal(0, result.CapturedAssets);
        Assert.Contains("ArgumentOutOfRangeException", diagnostic.Message); Assert.Contains("stage=metadata/table", diagnostic.Message);
        Assert.Contains("version=22", diagnostic.Message); Assert.Contains("AssetsFileMetadata.Read", diagnostic.Message);
        Assert.Contains("LE=-1", diagnostic.Message); Assert.Contains("metadataSize=", diagnostic.Message); Assert.Contains(path, diagnostic.Message);
        Assert.Null(Assert.Single(rows.OfType<V2LogicalFile>()).DeclaredAssetCount);
    }

    [Fact]
    public async Task TruncatedObjectTable_ReportsEndOfStreamWithoutInventingRows()
    {
        byte[] original = V2MetadataFixture.Serialized(0); int count = TypeCountPosition(original);
        int end = (count + 8 + 3) & ~3; byte[] bytes = original[..end];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(count), 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(count + 4), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20), (uint)(end - 48));
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(24), end);
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(32), end);
        using V2MetadataFixture fixture = new(); var (result, rows) = await Run(Request(fixture.Save(bytes)));
        Assert.Equal(V2ExtractionStatus.Partial, result.Status); Assert.Empty(rows.OfType<V2Asset>());
        Assert.Contains(rows.OfType<V2Diagnostic>(), d => d.Code == "table-unreadable" && d.Message.Contains("EndOfStreamException") && d.Message.Contains("stage=metadata/table"));
        Assert.Null(Assert.Single(rows.OfType<V2LogicalFile>()).DeclaredAssetCount);
    }

    [Fact]
    public void BudgetDiagnostics_ReportActualLimitAndOffsetInsteadOfGenericIoError()
    {
        using var payload = new MemoryStream(); using (var writer = new BinaryWriter(payload, System.Text.Encoding.UTF8, true)) writer.Write(500);
        payload.Position = 0; using var reader = new AssetsFileReader(payload);
        var error = Assert.Throws<V2MetadataEnrichment.MetadataBudgetException>(() => V2MetadataEnrichment.ReadString(reader, 32));
        Assert.Contains("string-bytes", error.Message); Assert.Contains("observed=500", error.Message); Assert.Contains("limit=32", error.Message);
        Assert.Contains("offset=0", error.Message); Assert.DoesNotContain("I/O error occurred", error.Message);
    }
    private static V2ExtractionRequest Request(string path) => new(path, Guid.NewGuid(), Guid.NewGuid());
    private static async Task<(V2ExtractionResult Result, List<V2MetadataRecord> Rows)> Run(
        V2ExtractionRequest request, V2ExtractionOptions? options = null)
    {
        List<V2MetadataRecord> rows = [];
        V2ExtractionResult result = await new AssetsToolsV2MetadataExtractor(TestPaths.ClassData)
            .ExtractAsync(request, (batch, _) => { rows.AddRange(batch); return ValueTask.CompletedTask; }, options);
        return (result, rows);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealFixtures_RetainEveryClassAndVerifiedProvenance(bool bundle)
    {
        string path = bundle ? TestPaths.BundleFixture : TestPaths.SerializedFixture;
        byte[] before = SHA256.HashData(await File.ReadAllBytesAsync(path));
        V2ExtractionRequest request = Request(path);
        var (result, rows) = await Run(request, new(BatchSize: 7));
        Assert.True(result.CommitEligible, string.Join(";", rows.OfType<V2Diagnostic>().Select(d => d.Message)));
        Assert.Equal(V2ExtractionStatus.Complete, result.Status);
        Assert.Equal(request.SourceId, result.Source.SourceId);
        Assert.Equal(request.RevisionId, result.Source.RevisionId);
        Assert.Equal(request.Path, result.Source.OriginalPath);
        Assert.Equal(Path.GetFullPath(request.Path), result.Source.FullPath);
        Assert.Equal(Convert.ToHexString(before), result.Source.Sha256);
        Assert.Equal(before, SHA256.HashData(await File.ReadAllBytesAsync(path)));
        V2Asset[] assets = rows.OfType<V2Asset>().ToArray();
        Assert.Equal(43, assets.Length);
        Assert.Equal(34, assets.Count(a => a.ClassId == 213));
        Assert.Equal(2, assets.Count(a => a.ClassId == 28));
        Assert.Equal(5, assets.Count(a => a.ClassId == 74));
        Assert.Contains(assets, a => a.ClassId == 91);
        Assert.Contains(assets, a => a.ClassId == 142);
        Assert.Equal(Enumerable.Range(0, 43), assets.Select(a => a.TableOrdinal));
        Assert.All(assets, a => { Assert.Equal(V2IdentityStatus.Addressable, a.IdentityStatus); Assert.True(a.ByteSize > 0); });
        Assert.Single(rows.OfType<V2LogicalFile>());
        Assert.All(assets, a => Assert.Equal(0, a.DirectoryOrdinal));
        Assert.InRange(result.Metrics.MaxEmittedBatch, 1, 7);
        Assert.DoesNotContain(assets, a => a.Name == "Unnamed asset");
        using FileStream exclusive = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    [Fact]
    public async Task StructuralOnly_NeverReadsAssetNamesOrBundlePayloads()
    {
        var (_, rows) = await Run(Request(TestPaths.SerializedFixture), new(ReadNames: false, ReadDependencies: false));
        Assert.Equal(43, rows.OfType<V2Asset>().Count());
        Assert.All(rows.OfType<V2Asset>(), a => { Assert.Null(a.Name); Assert.Equal(V2NameStatus.Unattempted, a.NameStatus); });
        Assert.Empty(rows.OfType<V2Dependency>());
        Assert.Equal(V2DependencyStatus.Unavailable, Assert.Single(rows.OfType<V2LogicalFile>()).DependencyStatus);
    }

    [Fact]
    public async Task WrongExpectedRevision_RejectsBeforeAnyAssetEmission()
    {
        var (result, rows) = await Run(Request(TestPaths.SerializedFixture) with { ExpectedSha256 = new string('0', 64) });
        Assert.False(result.CommitEligible);
        Assert.Equal(V2ExtractionStatus.RevisionMismatch, result.Status);
        Assert.Empty(rows.OfType<V2Asset>());
        Assert.Contains(rows.OfType<V2Diagnostic>(), d => d.Code == "revision-mismatch");
    }

    [Fact]
    public async Task SameBytesNeverMergeCallerAssignedOccurrences()
    {
        var (one, _) = await Run(Request(TestPaths.SerializedFixture));
        var (two, _) = await Run(Request(TestPaths.SerializedFixture));
        Assert.Equal(one.Source.Sha256, two.Source.Sha256);
        Assert.NotEqual(one.Source.SourceId, two.Source.SourceId);
        Assert.NotEqual(one.Source.RevisionId, two.Source.RevisionId);
    }

    [Fact]
    public async Task CancellationAtBatchBoundary_AbortsWithoutFinalVerifiedVerdict()
    {
        using CancellationTokenSource cancellation = new();
        int batches = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsToolsV2MetadataExtractor(TestPaths.ClassData)
            .ExtractAsync(Request(TestPaths.SerializedFixture), (batch, _) =>
            {
                batches++; cancellation.Cancel(); return ValueTask.CompletedTask;
            }, new(BatchSize: 1), cancellationToken: cancellation.Token));
        Assert.Equal(1, batches);
        using FileStream exclusive = new(TestPaths.SerializedFixture, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    [Fact]
    public async Task MissingSource_IsFailedNotSuccessfulEmpty()
    {
        var (result, rows) = await Run(Request(Path.Combine(Path.GetTempPath(), $"absent-{Guid.NewGuid():N}.assets")));
        Assert.False(result.CommitEligible);
        Assert.Equal(V2ExtractionStatus.Failed, result.Status);
        Assert.Contains(rows.OfType<V2Diagnostic>(), d => d.Code == "source-unreadable");
    }

    [Fact]
    public async Task ReceiverFailure_IsNotMisreportedAsSuccessfulSource()
    {
        Exception error = await Assert.ThrowsAnyAsync<Exception>(() => new AssetsToolsV2MetadataExtractor(TestPaths.ClassData)
            .ExtractAsync(Request(TestPaths.SerializedFixture), (_, _) => throw new InvalidOperationException("sink failed"), new(BatchSize: 1)));
        Assert.Equal("sink failed", error.InnerException?.Message);
    }

    [Fact]
    public async Task NameBudgetFailure_RetainsAllAssetRows()
    {
        var (result, rows) = await Run(Request(TestPaths.SerializedFixture), new(MaxNameBytes: 0));
        Assert.True(result.CommitEligible);
        Assert.Equal(43, rows.OfType<V2Asset>().Count());
        Assert.Contains(rows.OfType<V2Asset>(), a => a.NameStatus == V2NameStatus.Budgeted);
    }

    [Fact]
    public async Task SyntheticTable_PreservesNamesUnknownClassesDuplicatesSignedIdsAndGuidFields()
    {
        using V2MetadataFixture fixture = new();
        var (result, rows) = await Run(Request(fixture.Save(V2MetadataFixture.Serialized())));
        Assert.True(result.CommitEligible, string.Join(";", rows.OfType<V2Diagnostic>().Select(d => d.Message)));
        V2Asset[] assets = rows.OfType<V2Asset>().ToArray();
        Assert.Equal(6, assets.Length);
        Assert.Equal("", assets[0].Name);
        Assert.Equal(V2NameStatus.Present, assets[0].NameStatus);
        Assert.Equal(-77, assets[0].PathId);
        Assert.Contains(assets, a => a.PathId == long.MaxValue);
        V2Asset unknown = Assert.Single(assets, a => a.PathId == 2);
        Assert.Equal(1234567, unknown.ClassId);
        Assert.Null(unknown.Name);
        Assert.Equal(V2NameStatus.Failed, Assert.Single(assets, a => a.PathId == 3).NameStatus);
        V2Asset[] duplicates = assets.Where(a => a.PathId == 900).ToArray();
        Assert.Equal(2, duplicates.Length);
        Assert.All(duplicates, a => { Assert.Equal("duplicate", a.Name); Assert.Equal(V2IdentityStatus.Ambiguous, a.IdentityStatus); });
        Assert.Equal(Enumerable.Range(0, 6), assets.Select(a => a.TableOrdinal));
        // The fixture writer sorts before serialization; this is the actual on-disk table order.
        Assert.Equal(new long?[] { -77, 2, 3, 900, 900, long.MaxValue }, assets.Select(a => a.PathId));
        Assert.All(assets, a => { Assert.NotNull(a.LocalTypeIndex); Assert.NotNull(a.ByteOffset); Assert.True(a.ByteSize > 0); });
        V2Dependency[] deps = rows.OfType<V2Dependency>().ToArray();
        Assert.Equal(new[] { 0, 1 }, deps.Select(d => d.DeclarationOrdinal));
        Assert.Equal("010203048090A0B0FFFFFFFF11223344", deps[0].GuidHex);
        // OriginalPathName is the parser's original serialized PathName, not an extra on-disk field.
        Assert.Equal("CAB-duplicate", deps[0].OriginalPath);
        Assert.Equal("CAB-duplicate", deps[0].CabEvidence);
        Assert.Equal("", deps[1].Path);
        Assert.Equal(new string('0', 32), deps[1].GuidHex);
    }

    [Theory]
    [InlineData("CAB-duplicate")]
    [InlineData("")]
    public async Task SyntheticBundle_OriginalOrdinalsSurviveGapsAndDuplicateLogicalNames(string name)
    {
        using V2MetadataFixture fixture = new();
        var (result, rows) = await Run(Request(fixture.Save(V2MetadataFixture.Bundle(name), "test.bundle")));
        Assert.True(result.CommitEligible, string.Join(";", rows.OfType<V2Diagnostic>().Select(d => d.Message)));
        Assert.Equal(new[] { 1, 3 }, rows.OfType<V2LogicalFile>().Select(f => f.DirectoryOrdinal));
        Assert.All(rows.OfType<V2LogicalFile>(), f => Assert.Equal(name, f.Name));
        Assert.Equal(12, rows.OfType<V2Asset>().Count());
        Assert.Equal(6, rows.OfType<V2Asset>().Count(a => a.DirectoryOrdinal == 1));
        Assert.Equal(6, rows.OfType<V2Asset>().Count(a => a.DirectoryOrdinal == 3));
    }

    [Fact]
    public async Task SyntheticCorruptSibling_PreservesReadableTableAndExplicitPartialStatus()
    {
        using V2MetadataFixture fixture = new();
        var (result, rows) = await Run(Request(fixture.Save(V2MetadataFixture.Bundle("CAB-duplicate", true), "test.bundle")));
        Assert.True(result.CommitEligible);
        Assert.Equal(V2ExtractionStatus.Partial, result.Status);
        Assert.Equal(6, rows.OfType<V2Asset>().Count());
        V2LogicalFile failed = Assert.Single(rows.OfType<V2LogicalFile>(), f => f.Status == V2ExtractionStatus.Failed);
        Assert.Null(failed.DeclaredAssetCount);
        Assert.Equal(3, failed.DirectoryOrdinal);
        Assert.Contains(rows.OfType<V2Diagnostic>(), d => d.Code == "table-unreadable");
    }

    [Fact]
    public async Task LargeSyntheticTable_EmitsBoundedBackpressuredBatches()
    {
        using V2MetadataFixture fixture = new();
        int count = 0, maximum = 0;
        V2ExtractionResult result = await new AssetsToolsV2MetadataExtractor(TestPaths.ClassData).ExtractAsync(
            Request(fixture.Save(V2MetadataFixture.Serialized(10000))), async (batch, _) =>
            {
                maximum = Math.Max(maximum, batch.Count);
                count += batch.OfType<V2Asset>().Count();
                await Task.Yield();
            }, new(BatchSize: 31, ReadNames: false, ReadDependencies: false));
        Assert.True(result.CommitEligible);
        Assert.Equal(10000, count);
        Assert.Equal(31, maximum);
        Assert.Equal(10000, result.CapturedAssets);
    }

    [Fact]
    public async Task ChangedFinalTimestamp_RejectsAllProvisionalRows()
    {
        int checks = 0, assets = 0;
        DateTime original = File.GetLastWriteTimeUtc(TestPaths.SerializedFixture);
        // Controlled filesystem-evidence seam; no genuine fixture mutation.
        AssetsToolsV2MetadataExtractor extractor = new(TestPaths.ClassData,
            _ => Interlocked.Increment(ref checks) == 1 ? original : original.AddSeconds(1));
        V2ExtractionResult result = await extractor.ExtractAsync(Request(TestPaths.SerializedFixture), (batch, _) =>
        { assets += batch.OfType<V2Asset>().Count(); return ValueTask.CompletedTask; });
        Assert.Equal(43, assets);
        Assert.Equal(V2ExtractionStatus.RevisionMismatch, result.Status);
        Assert.False(result.Source.Verified);
        Assert.False(result.CommitEligible);
    }

    [Fact]
    public async Task PrecancelledExtraction_EmitsNothing()
    {
        int batches = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsToolsV2MetadataExtractor().ExtractAsync(
            Request(TestPaths.SerializedFixture), (_, _) => { batches++; return ValueTask.CompletedTask; },
            cancellationToken: new CancellationToken(true)));
        Assert.Equal(0, batches);
    }

    [Fact]
    public async Task HashPhaseCancellation_ReleasesSource()
    {
        using CancellationTokenSource cancellation = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsToolsV2MetadataExtractor().ExtractAsync(
            Request(TestPaths.SerializedFixture), (_, _) => ValueTask.CompletedTask,
            progress: new InlineProgress(p => { if (p.Phase == "fingerprint") cancellation.Cancel(); }),
            cancellationToken: cancellation.Token));
        using FileStream exclusive = new(TestPaths.SerializedFixture, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    [Fact]
    public async Task UnsupportedSource_DoesNotPretendToContainEmptyTable()
    {
        using V2MetadataFixture fixture = new();
        var (result, rows) = await Run(Request(fixture.Save(new byte[64])));
        Assert.Equal(V2ExtractionStatus.Unsupported, result.Status);
        Assert.False(result.CommitEligible);
        Assert.Empty(rows.OfType<V2LogicalFile>());
        Assert.Contains(rows.OfType<V2Diagnostic>(), d => d.Code == "unsupported-source");
    }

    [Fact]
    public async Task InvalidOptionsAndUnassignedIdentity_AreRejected()
    {
        AssetsToolsV2MetadataExtractor extractor = new();
        await Assert.ThrowsAsync<ArgumentException>(() => extractor.ExtractAsync(Request(TestPaths.SerializedFixture) with { SourceId = Guid.Empty }, (_, _) => ValueTask.CompletedTask));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => extractor.ExtractAsync(Request(TestPaths.SerializedFixture), (_, _) => ValueTask.CompletedTask, new(BatchSize: 0)));
    }

    [Fact]
    public async Task ExtractionCostProbe_ReportsSmallFixtureCostsNotProductionBudgets()
    {
        foreach (string path in new[] { TestPaths.SerializedFixture, TestPaths.BundleFixture })
        foreach (string mode in new[] { "structure", "names", "all" })
        {
            List<V2ExtractionResult> samples = [];
            int named = 0, dependencies = 0;
            for (int i = 0; i < 3; i++)
            {
                var (result, rows) = await Run(Request(path), new(ReadNames: mode != "structure", ReadDependencies: mode == "all"));
                Assert.True(result.CommitEligible);
                samples.Add(result);
                named = rows.OfType<V2Asset>().Count(a => a.NameStatus == V2NameStatus.Present);
                dependencies = rows.OfType<V2Dependency>().Count();
            }
            double Median(Func<V2ExtractionResult, double> value) => samples.Select(value).Order().ElementAt(1);
            output.WriteLine($"{Path.GetFileName(path)} {mode}: total={Median(r => r.Metrics.Total.TotalMilliseconds):F3}ms " +
                $"hash={Median(r => r.Metrics.Fingerprinting.TotalMilliseconds):F3}ms structure={Median(r => r.Metrics.Structure.TotalMilliseconds):F3}ms " +
                $"names={Median(r => r.Metrics.Names.TotalMilliseconds):F3}ms deps={Median(r => r.Metrics.Dependencies.TotalMilliseconds):F3}ms " +
                $"allocated={Median(r => r.Metrics.ManagedAllocatedBytes):F0}B processPeak={Median(r => r.Metrics.ProcessPeakWorkingSetBytes):F0}B " +
                $"presentNames={named} declarations={dependencies}");
        }
        using CancellationTokenSource cancellation = new();
        System.Diagnostics.Stopwatch elapsed = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new AssetsToolsV2MetadataExtractor(TestPaths.ClassData)
            .ExtractAsync(Request(TestPaths.BundleFixture), (_, _) =>
            { elapsed.Start(); cancellation.Cancel(); return ValueTask.CompletedTask; }, new(BatchSize: 1), cancellationToken: cancellation.Token));
        output.WriteLine($"Cancellation from emitted batch to observed exception: {elapsed.Elapsed.TotalMilliseconds:F3}ms (single observation).");
    }

    private sealed class InlineProgress(Action<V2ExtractionProgress> report) : IProgress<V2ExtractionProgress>
    { public void Report(V2ExtractionProgress value) => report(value); }

    [Fact]
    public void BoundedBundleDependencyWalker_PreservesEmptyDuplicateDeclarationsAndHonorsBudget()
    {
        using MemoryStream payload = new();
        using BinaryWriter writer = new(payload, System.Text.Encoding.UTF8, leaveOpen: true);
        void String(string value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length); writer.Write(bytes);
            while (payload.Position % 4 != 0) writer.Write((byte)0);
        }
        String("bundle"); writer.Write(3); String(""); String("CAB-repeat"); String("CAB-repeat");
        V2MetadataEnrichment.Field text = new("data", "string", -1, false, []);
        V2MetadataEnrichment.Field array = new("Array", "Array", -1, false,
            [new("size", "int", 4, false, []), text]);
        V2MetadataEnrichment.Field shape = new("Base", "AssetBundle", -1, false,
            [new("m_Name", "string", -1, false, []), new("m_Dependencies", "vector", -1, false, [array])]);
        AssetsFile file = new() { Header = new() { DataOffset = 0, Endianness = false }, Reader = new(payload) };
        AssetFileInfo info = new() { ByteOffset = 0, ByteSize = (uint)payload.Length };
        Assert.Equal(new[] { "", "CAB-repeat", "CAB-repeat" }, V2MetadataEnrichment.BundleDependencies(file, info, shape, new(), default));
        var budget = Assert.Throws<V2MetadataEnrichment.MetadataBudgetException>(() =>
            V2MetadataEnrichment.BundleDependencies(file, info, shape, new(MaxDependencyItems: 2), default));
        Assert.Contains("dependency-items", budget.Message); Assert.Contains("observed=3", budget.Message); Assert.Contains("limit=2", budget.Message);
    }

    [Fact]
    public async Task MissingClassDatabase_DoesNotDropStructuralRows()
    {
        List<V2MetadataRecord> rows = [];
        V2ExtractionResult result = await new AssetsToolsV2MetadataExtractor(TestPaths.MissingClassData)
            .ExtractAsync(Request(TestPaths.SerializedFixture), (batch, _) => { rows.AddRange(batch); return ValueTask.CompletedTask; });
        Assert.True(result.CommitEligible);
        Assert.Equal(43, rows.OfType<V2Asset>().Count());
    }
}
