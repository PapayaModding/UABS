using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using UABS.PackageV2;
using UABS.UnityAssets.Metadata;
using UABS.UnityAssets.Packages;

// Preparation only: real AssetsTools metadata extraction, no synthetic records,
// no Search backend calls, and no fixture publication.
if (args.Length != 3) throw new ArgumentException("Usage: PrepareSmoke <repository> <new-output> <exact-commit>");
string repository = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]), commit = args[2];
if (!System.Text.RegularExpressions.Regex.IsMatch(commit, "\\A[0-9a-f]{40}\\z")) throw new ArgumentException("Full commit SHA required.");
if (Directory.Exists(output)) throw new IOException("Preparation output must be new.");
Directory.CreateDirectory(output);
string collection = Path.Combine(repository, "test-fixtures/unity/collections/smoke");
string manifestPath = Path.Combine(collection, "fixture-manifest.json");
using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
var root = manifest.RootElement;
if (root.GetProperty("profile").GetString() != "smoke" || root.GetProperty("seed").GetInt32() != 20211031
    || root.GetProperty("configuration").GetProperty("compression").GetString() != "lz4")
    throw new InvalidDataException("Unexpected Smoke fixture profile/seed/compression.");
var sourceHashes = new Dictionary<string, string>(StringComparer.Ordinal);
var bundles = root.GetProperty("bundles").EnumerateArray().ToArray();
if (bundles.Length != 3) throw new InvalidDataException("Expected three allowlisted Smoke bundles.");
string[] sourcePaths = bundles.Select(bundle =>
{
    string name = bundle.GetProperty("name").GetString()!;
    string file = bundle.GetProperty("file").GetString()!;
    if (file != "bundles/" + name || name != Path.GetFileName(name)
        || name.Contains('\\') || !name.StartsWith("uabs-smoke-", StringComparison.Ordinal))
        throw new InvalidDataException("Unexpected fixture path.");
    string path = Path.Combine(collection, "bundles", name);
    if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked fixture rejected.");
    string expected = bundle.GetProperty("sha256").GetString()!;
    if (!Hash(path).Equals(expected, StringComparison.OrdinalIgnoreCase)
        || new FileInfo(path).Length != bundle.GetProperty("sizeBytes").GetInt64())
        throw new InvalidDataException("Fixture hash/size mismatch: " + name);
    sourceHashes.Add(name, expected);
    return path;
}).ToArray();
const string keyword = "テスト";
// Fixed expectation for this seed, cross-checked against the published manifest
// and, independently, every raw indexed asset name below. Never learn expected
// rows from Search output (the system under test).
string[] expectedNames = ["uabs-smoke-0000.bundle"];
var manifestNames = bundles.Where(bundle => bundle.GetProperty("sourceAssignments").EnumerateArray()
    .Any(asset => asset.GetProperty("assetName").GetString()!.Contains(keyword, StringComparison.Ordinal)))
    .Select(bundle => bundle.GetProperty("name").GetString()!).Order().ToArray();
if (!manifestNames.SequenceEqual(expectedNames)) throw new InvalidDataException("Published manifest Unicode expectation changed.");

string database = Path.Combine(output, "Smoke.uabs.sqlite");
var sources = sourcePaths.Select(path => new BuildSource(0, Path.GetFileName(path), "",
    new(path, Guid.NewGuid(), Guid.NewGuid()))).ToArray();
var built = await new PackageBuilder(new AssetsToolsV2MetadataExtractor()).BuildAsync(new(database,
    Guid.NewGuid(), Guid.NewGuid(), "Smoke",
    [new("Published Smoke bundles", Path.GetDirectoryName(sourcePaths[0])!,
        OperatingSystem.IsWindows() ? "windows-lexical-v1" : "posix-lexical-v1")], sources));
if (!built.Succeeded) throw new InvalidDataException("Smoke indexing failed: " + built.Message);
var admission = await PackageReader.OpenAsync(database);
using var package = admission.Reader ?? throw new InvalidDataException("Package admission failed: " + admission.Message);
if (package.Info.IndexStatus != "complete" || package.Info.Sources != 3 || package.Info.Assets < 1 || package.Info.Issues != 0)
    throw new InvalidDataException("Smoke package must be fully indexed with three sources and no issues.");

var witnesses = new List<object>();
var observedNames = new HashSet<string>(StringComparer.Ordinal);
var sortNames = new HashSet<string>(StringComparer.Ordinal);
using (var sql = new SqliteConnection(new SqliteConnectionStringBuilder {
    DataSource = database, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
{
    sql.Open();
    using (var command = sql.CreateCommand())
    {
        command.CommandText = "SELECT original_name, kind, index_status, hex(content_sha256) FROM source ORDER BY original_name";
        using var rows = command.ExecuteReader();
        int count = 0;
        while (rows.Read())
        {
            if (!sourceHashes.TryGetValue(rows.GetString(0), out string? hash) || rows.GetString(1) != "bundle"
                || rows.GetString(2) != "complete" || !rows.GetString(3).Equals(hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Indexed source identity/state/hash mismatch.");
            count++;
        }
        if (count != 3) throw new InvalidDataException("Unexpected indexed source count.");
    }
    using (var command = sql.CreateCommand())
    {
        command.CommandText = "SELECT s.original_name, a.name FROM asset a JOIN logical_file f ON f.file_id=a.file_id JOIN source s ON s.source_id=f.source_id WHERE a.name IS NOT NULL";
        using var rows = command.ExecuteReader();
        while (rows.Read())
        {
            if (rows.GetString(1).Contains("asset", StringComparison.OrdinalIgnoreCase)) sortNames.Add(rows.GetString(0));
            if (rows.GetString(1).Contains(keyword, StringComparison.OrdinalIgnoreCase))
            { observedNames.Add(rows.GetString(0)); witnesses.Add(new { bundle = rows.GetString(0), assetName = rows.GetString(1) }); }
        }
    }
}
if (!observedNames.Order().SequenceEqual(expectedNames))
    throw new InvalidDataException("Raw indexed Unicode witnesses disagree with independently fixed expected bundle results.");
string[] expectedSortNames = ["uabs-smoke-0000.bundle", "uabs-smoke-0001.bundle", "uabs-smoke-0002.bundle"];
if (!sortNames.Order().SequenceEqual(expectedSortNames)) throw new InvalidDataException("Independent multirow sort oracle differs.");
foreach (string path in sourcePaths)
    if (!Hash(path).Equals(sourceHashes[Path.GetFileName(path)], StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException("Published fixture changed during preparation.");
var expectedRows = expectedNames.Select(name => new { name, package = "Smoke" }).ToArray();
var expectedSortRows = expectedSortNames.Select(name => new { name, package = "Smoke" }).ToArray();
string legacyRoot = Path.Combine(repository, "Avalonia/UABS.UnityAssets.Tests/Fixtures/LegacyPackage");
var legacy = await new PackageService().LoadAsync(legacyRoot);
var legacyMatches = legacy.Bundles.SelectMany(bundle => bundle.AssetInfos.Select(asset => new { bundle.Name, asset }))
    .Where(row => row.asset.Name == "portrait").ToArray();
if (legacyMatches.Length != 1 || legacyMatches[0].Name != "character.ab"
    || legacyMatches[0].asset.PathId != 1001 || legacyMatches[0].asset.Memo != "legacy memo")
    throw new InvalidDataException("Independent legacy fixture expectation changed.");
var legacyHashes = new[] { "Validation.txt", "index.json", "nested/index.json" }.ToDictionary(file => file, file => Hash(Path.Combine(legacyRoot, file)));
Write("preparation.json", new { commit, keyword, manifestSha256 = Hash(manifestPath), sourceHashes, package.Info, witnesses, expectedRows,
    sortKeyword = "asset", expectedSortRows, legacyHashes, legacyMatches });
Write("scenario.local.json", new { repository, expectedCommit = commit, package = database,
    packageSha256 = Hash(database), keyword, expectedRows, expectedSortRows });
Console.WriteLine($"Prepared fully indexed Smoke package: {package.Info.Assets} assets; Unicode query expects {expectedRows.Length} bundle.");

static string Hash(string path)
{
    using var stream = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
}
void Write(string name, object value) => File.WriteAllText(Path.Combine(output, name),
    JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
