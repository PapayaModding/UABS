using System.Text.Json;
using Microsoft.Data.Sqlite;
using Uabs.UiPoc.Fixtures;

if (args.Length != 3 || !System.Text.RegularExpressions.Regex.IsMatch(args[2], @"\A[0-9a-f]{40}\z")) throw new ArgumentException("<repository> <new-output> <full-commit>");
string repo = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
var fixtures = await IndeterminateFixtureBuilder.BuildAsync(output);
var complete = fixtures.Single(f => !f.Partial);
var coverageEvidence = fixtures.Select(f =>
{
    using var sql = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = f.Path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
    sql.Open(); using var command = sql.CreateCommand();
    command.CommandText = "SELECT s.original_path,s.original_name,s.kind,s.index_status,hex(s.content_sha256) AS source_sha256,s.byte_size,f.declared_asset_count,(SELECT count(*) FROM asset WHERE file_id=f.file_id) AS captured_asset_count,a.name,a.name_status FROM source s JOIN logical_file f ON f.source_id=s.source_id JOIN asset a ON a.file_id=f.file_id";
    using var reader = command.ExecuteReader(); var rows = new List<Dictionary<string, object>>();
    while (reader.Read()) rows.Add(Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue));
    using var input = JsonDocument.Parse(File.ReadAllText(f.SourcePath));
    return new { f.Package, sourceContract = input.RootElement.Clone(), rawRows = rows };
}).ToArray();
foreach (var f in fixtures) if (IndeterminateFixtureBuilder.Hash(f.Path) != f.Sha256) throw new InvalidDataException("Read-only evidence capture changed database.");
object Row(CoverageFixture f) => new { name = Path.GetFileName(f.SourcePath), package = f.Package };
void Write(string name, object value) => File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
Write("preparation.json", new { commit = args[2], provenance = IndeterminateFixtureBuilder.Provenance, fixtures, coverageEvidence,
    keyword = IndeterminateFixtureBuilder.Keyword, excluded = "g5_absent_exclusion", expectedBackend = IndeterminateFixtureBuilder.Backend,
    baseline = new[] { Row(complete) }, allIndexed = fixtures.Select(Row) });
Write("scenario.local.json", new { repository = repo, expectedCommit = args[2], package = complete.Path, packageSha256 = complete.Sha256,
    keyword = IndeterminateFixtureBuilder.Keyword, expectedRows = new[] { Row(complete) }, acceptance = "checkpoint21-indeterminate", indeterminateFixtures = fixtures });
Console.WriteLine("Prepared explicitly synthetic complete/eligible-partial contract packages, with acknowledgement and raw coverage proof. Not Unity-extracted bundles.");
