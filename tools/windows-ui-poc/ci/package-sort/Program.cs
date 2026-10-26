using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using UABS.PackageV2;
using UABS.UnityAssets.Metadata;

// No Search API/comparer is called. Real extraction into two independent DBs.
if (args.Length != 3) throw new ArgumentException("PreparePackageSort <repository> <new-output> <exact-commit>");
string repo = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]), commit = args[2];
if (!System.Text.RegularExpressions.Regex.IsMatch(commit, @"\A[0-9a-f]{40}\z") || Directory.Exists(output)) throw new InvalidDataException("New output/full SHA required.");
Directory.CreateDirectory(output);
string collection = Path.GetFullPath(Path.Combine(repo, "test-fixtures/unity/collections/smoke"));
string bundleRoot = Path.Combine(collection, "bundles");
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
void Unlinked(string path) { if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked fixture rejected: " + path); }
Unlinked(collection); Unlinked(bundleRoot);
string manifestPath = Path.Combine(collection, "fixture-manifest.json"); Unlinked(manifestPath);
using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath)); var root = manifest.RootElement;
if (root.GetProperty("profile").GetString() != "smoke" || root.GetProperty("seed").GetInt32() != 20211031
    || root.GetProperty("configuration").GetProperty("compression").GetString() != "lz4") throw new InvalidDataException("Unexpected published profile.");
string[] names = ["uabs-smoke-0000.bundle", "uabs-smoke-0001.bundle", "uabs-smoke-0002.bundle"];
var entries = root.GetProperty("bundles").EnumerateArray().ToArray();
if (!entries.Select(e => e.GetProperty("name").GetString()).Order(StringComparer.Ordinal).SequenceEqual(names)) throw new InvalidDataException("Bundle allowlist mismatch.");
var hashes = new Dictionary<string, string>();
foreach (var e in entries)
{
    string name = e.GetProperty("name").GetString()!, path = Path.Combine(bundleRoot, name);
    if (e.GetProperty("file").GetString() != "bundles/" + name) throw new InvalidDataException("Unexpected manifest path.");
    Unlinked(path); string hash = Hash(path);
    if (!hash.Equals(e.GetProperty("sha256").GetString(), StringComparison.OrdinalIgnoreCase)
        || new FileInfo(path).Length != e.GetProperty("sizeBytes").GetInt64()) throw new InvalidDataException("Published input hash/size differs.");
    hashes.Add(name, hash);
}
var fixtures = new List<object>(); var allIds = new HashSet<Guid>();
void Unique(Guid id) { if (id == Guid.Empty || !allIds.Add(id)) throw new InvalidDataException("Empty/reused package/source identity."); }
foreach (string label in new[] { "Alpha", "Zeta" })
{
    string[] assigned = label == "Alpha" ? [names[2], names[1]] : [names[0]];
    string database = Path.Combine(output, label + ".uabs.sqlite");
    var sources = assigned.Select(name => new BuildSource(0, name, "", new(Path.Combine(bundleRoot, name), Guid.NewGuid(), Guid.NewGuid(), hashes[name]))).ToArray();
    var built = await new PackageBuilder(new AssetsToolsV2MetadataExtractor()).BuildAsync(new(database, Guid.NewGuid(), Guid.NewGuid(), label,
        [new("Published Smoke bundles", bundleRoot, OperatingSystem.IsWindows() ? "windows-lexical-v1" : "posix-lexical-v1")], sources));
    if (!built.Succeeded) throw new InvalidDataException(built.Message);
    Guid packageId, generationId; PackageInfo info; var indexed = new List<object>();
    using (var package = (await PackageReader.OpenAsync(database)).Reader ?? throw new InvalidDataException("Admission failed."))
    {
        if (package.Info.Name != label || package.Info.IndexStatus != "complete" || package.Info.Sources != assigned.Length
            || package.Info.Assets < 1 || package.Info.Issues != 0) throw new InvalidDataException("Package labels/coverage/counts invalid.");
        info = package.Info; packageId = info.PackageId; generationId = info.GenerationId; Unique(packageId); Unique(generationId);
    }
    using (var sql = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
    {
        sql.Open(); using var command = sql.CreateCommand();
        command.CommandText = "SELECT source_id,original_name,original_path,kind,index_status,hex(content_sha256),source_uuid,source_revision_uuid,index_directory FROM source ORDER BY original_name";
        using var rows = command.ExecuteReader(); var seen = new HashSet<string>();
        while (rows.Read())
        {
            long id = rows.GetInt64(0); string name = rows.GetString(1), path = rows.GetString(2);
            if (!assigned.Contains(name) || !seen.Add(name) || path != Path.Combine(bundleRoot, name) || Path.GetFullPath(path) != path
                || rows.GetString(3) != "bundle" || rows.GetString(4) != "complete" || rows.GetString(8) != ""
                || !rows.GetString(5).Equals(hashes[name], StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Raw source evidence differs.");
            Guid sourceId = new((byte[])rows.GetValue(6), bigEndian: true), revision = new((byte[])rows.GetValue(7), bigEndian: true); Unique(sourceId); Unique(revision);
            var requested = sources.Single(source => Path.GetFileName(source.Request.Path) == name).Request;
            if (sourceId != requested.SourceId || revision != requested.RevisionId) throw new InvalidDataException("Stored source identities differ from the independently built input request.");
            using var witnessCommand = sql.CreateCommand();
            witnessCommand.CommandText = "SELECT a.name FROM asset a JOIN logical_file f ON f.file_id=a.file_id WHERE f.source_id=$id AND a.name IS NOT NULL";
            witnessCommand.Parameters.AddWithValue("$id", id); using var witnessReader = witnessCommand.ExecuteReader();
            var witnesses = new List<string>(); while (witnessReader.Read()) if (witnessReader.GetString(0).Contains("asset", StringComparison.OrdinalIgnoreCase)) witnesses.Add(witnessReader.GetString(0));
            if (witnesses.Count == 0) throw new InvalidDataException("No independent raw indexed asset witness for " + name);
            indexed.Add(new { name, originalPath = path, sha256 = hashes[name], sourceId, sourceRevision = revision, witnesses });
        }
        if (seen.Count != assigned.Length) throw new InvalidDataException("Missing indexed sources.");
    }
    if (File.Exists(database + "-wal") || File.Exists(database + "-shm")) throw new InvalidDataException("Package sidecars must be closed before hashing.");
    fixtures.Add(new { package = label, path = database, sha256 = Hash(database), packageId, generationId, sources = indexed, info });
}
foreach (string name in names) if (Hash(Path.Combine(bundleRoot, name)) != hashes[name]) throw new InvalidDataException("Original bundle changed.");
object[] ascending = [new { name = names[1], package = "Alpha" }, new { name = names[2], package = "Alpha" }, new { name = names[0], package = "Zeta" }];
object[] descending = [new { name = names[0], package = "Zeta" }, new { name = names[1], package = "Alpha" }, new { name = names[2], package = "Alpha" }];
void Write(string name, object value) => File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
Write("preparation.json", new { commit, manifestSha256 = Hash(manifestPath), sourceHashes = hashes, fixtures, ascending, descending, keyword = "asset" });
Write("scenario.local.json", new { repository = repo, expectedCommit = commit, package = Path.Combine(output, "Alpha.uabs.sqlite"),
    packageSha256 = Hash(Path.Combine(output, "Alpha.uabs.sqlite")), keyword = "asset", expectedRows = ascending,
    acceptance = "checkpoint21-package-sort", packageSortFixtures = fixtures });
Console.WriteLine("Prepared independent Alpha/Zeta databases and raw-verified three-row Package oracle; no Unity or Search API used.");
