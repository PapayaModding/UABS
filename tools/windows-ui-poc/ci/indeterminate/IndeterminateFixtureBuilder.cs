using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using UABS.PackageV2;
using UABS.UnityAssets;
using UABS.UnityAssets.Metadata;

namespace Uabs.UiPoc.Fixtures;

// Explicit synthetic metadata contract evidence, NOT a Unity bundle/extractor.
public sealed record CoverageInput(string Provenance, bool Partial, string Asset, int Declared, int Captured);
public sealed record CoverageFixture(string Package, string Path, string Sha256, Guid PackageId, Guid GenerationId,
    string SourcePath, string SourceSha256, Guid SourceId, Guid SourceRevision, bool Partial);
public static class IndeterminateFixtureBuilder
{
    public const string Keyword = "g5_known_asset";
    public const string Provenance = "synthetic IV2MetadataExtractor contract; not Unity-extracted data";
    public const string Backend = "Bundle exclusion is indeterminate: an eligible bundle has incomplete asset coverage or unavailable requested name evidence. No sources were skipped or queried; an unknown-coverage policy is required for this selected scope.";
    public static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    public static async Task<CoverageFixture[]> BuildAsync(string output, bool acknowledge = true)
    {
        if (Directory.Exists(output)) throw new InvalidDataException("Fixture output must be new.");
        output = System.IO.Path.GetFullPath(output); Directory.CreateDirectory(System.IO.Path.Combine(output, "inputs"));
        var fixtures = new List<CoverageFixture>();
        foreach (bool partial in new[] { false, true })
        {
            string prefix = partial ? "partial" : "complete", label = "G5 " + prefix + " synthetic metadata";
            string input = System.IO.Path.Combine(output, "inputs", "g5-" + prefix + ".metadata.json");
            await File.WriteAllTextAsync(input, JsonSerializer.Serialize(new CoverageInput(Provenance, partial, Keyword, partial ? 2 : 1, 1)));
            string hash = Hash(input), database = System.IO.Path.Combine(output, "G5-" + prefix + ".uabs.sqlite");
            Guid source = Guid.NewGuid(), revision = Guid.NewGuid(), package = Guid.NewGuid(), generation = Guid.NewGuid();
            int acknowledgements = 0;
            var result = await new PackageBuilder(new ContractExtractor()).BuildAsync(new(database, package, generation, label,
                [new("Explicit synthetic metadata", System.IO.Path.GetDirectoryName(input)!, OperatingSystem.IsWindows() ? "windows-lexical-v1" : "posix-lexical-v1")],
                [new(0, System.IO.Path.GetFileName(input), "", new(input, source, revision, hash))]),
                acknowledgePartial: (summary, _) => {
                    if (!partial || summary.Assets != 1 || summary.Issues != 1 || !summary.Details.Any(d => d.Contains("g5-deliberate-incomplete")))
                        throw new InvalidDataException("Unexpected acknowledgement summary.");
                    acknowledgements++; return Task.FromResult(acknowledge);
                });
            if (!result.Succeeded)
            {
                if (!acknowledge && partial && result.Status == StorageStatus.PartialAcknowledgementRequired && !File.Exists(database))
                    throw new InvalidDataException("Expected partial acknowledgement refusal; no partial package published.");
                throw new InvalidDataException(result.Message);
            }
            if (acknowledgements != (partial ? 1 : 0)) throw new InvalidDataException("Partial acknowledgement lifecycle differs.");
            using (var reader = (await PackageReader.OpenAsync(database)).Reader ?? throw new InvalidDataException("Admission failed."))
                if (reader.Info.Name != label || reader.Info.PackageId != package || reader.Info.GenerationId != generation
                    || reader.Info.IndexStatus != prefix || reader.Info.Sources != 1 || reader.Info.Assets != 1 || reader.Info.Issues != (partial ? 1 : 0))
                    throw new InvalidDataException("Admitted package evidence differs.");
            using (var sql = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            {
                sql.Open(); using var command = sql.CreateCommand();
                command.CommandText = "SELECT s.kind,s.index_status,s.original_path,hex(s.content_sha256),s.byte_size,s.source_uuid,s.source_revision_uuid,f.declared_asset_count,(SELECT count(*) FROM asset WHERE file_id=f.file_id),f.index_status,a.name,a.name_status FROM source s JOIN logical_file f ON f.source_id=s.source_id JOIN asset a ON a.file_id=f.file_id";
                using var row = command.ExecuteReader();
                if (!row.Read() || row.GetString(0) != "bundle" || row.GetString(1) != prefix || row.GetString(2) != input
                    || !row.GetString(3).Equals(hash, StringComparison.OrdinalIgnoreCase) || row.GetInt64(4) != new FileInfo(input).Length
                    || new Guid((byte[])row[5], bigEndian: true) != source || new Guid((byte[])row[6], bigEndian: true) != revision
                    || row.GetInt64(7) != (partial ? 2 : 1) || row.GetInt64(8) != 1 || row.GetString(9) != prefix
                    || row.GetString(10) != Keyword || row.GetString(11) != "present" || row.Read())
                    throw new InvalidDataException("Raw eligible coverage/witness evidence differs.");
            }
            if (Hash(input) != hash || File.Exists(database + "-wal") || File.Exists(database + "-shm")) throw new InvalidDataException("Source changed or readers not closed.");
            fixtures.Add(new(label, database, Hash(database), package, generation, input, hash, source, revision, partial));
        }
        return fixtures.ToArray();
    }
    private sealed class ContractExtractor : IV2MetadataExtractor
    {
        public async Task<V2ExtractionResult> ExtractAsync(V2ExtractionRequest request,
            Func<IReadOnlyList<V2MetadataRecord>, CancellationToken, ValueTask> receive,
            V2ExtractionOptions? options = null, IProgress<V2ExtractionProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            string hash = Hash(request.Path); var input = JsonSerializer.Deserialize<CoverageInput>(await File.ReadAllTextAsync(request.Path, cancellationToken))!;
            if (hash != request.ExpectedSha256 || input.Provenance != Provenance || input.Asset != Keyword || input.Captured != 1 || input.Declared != (input.Partial ? 2 : 1))
                throw new InvalidDataException("Synthetic input contract/hash mismatch.");
            var status = input.Partial ? V2ExtractionStatus.Partial : V2ExtractionStatus.Complete;
            await receive([new V2AssetType(0, 0, 49, null, "TextAsset"),
                new V2Asset(0, 0, 1, 49, 0, null, null, null, V2IdentityStatus.Addressable, input.Asset, V2NameStatus.Present, "TextAsset"),
                new V2LogicalFile(0, "synthetic contract", "CAB-g5-synthetic", null, null, true, input.Declared, 1, status, V2DependencyStatus.Complete)], cancellationToken);
            if (input.Partial) await receive([new V2Diagnostic(0, null, "table", "g5-deliberate-incomplete", "Synthetic contract intentionally captures one of two declared assets; remaining coverage is unknown.")], cancellationToken);
            return new(new(request.SourceId, request.RevisionId, request.Path, System.IO.Path.GetFullPath(request.Path), UnityFileKind.Bundle,
                new FileInfo(request.Path).Length, File.GetLastWriteTimeUtc(request.Path), hash, true), status, 1, 1, new(default, default, default, default, default, 0, 0, 3));
        }
    }
}
