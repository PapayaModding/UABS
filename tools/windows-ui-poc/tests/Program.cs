using System.Runtime.InteropServices;
using System.Text.Json;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

foreach (object? value in new object?[] { "SearchButton", "", false, true, null })
    Check(Equals(value, EvidenceValue.Read(() => value)), "Supported value was changed.");

foreach (Exception error in new Exception[] {
    new NotSupportedException("AutomationId unsupported"),
    new COMException("Element no longer available") })
{
    int subsequentReads = 0;
    var row = new {
        id = EvidenceValue.Read(() => throw error),
        name = EvidenceValue.Read(() => { subsequentReads++; return "Search"; }),
        enabled = EvidenceValue.Read(() => false)
    };
    using var json = JsonDocument.Parse(JsonSerializer.Serialize(row));
    var root = json.RootElement;
    Check(!root.GetProperty("id").GetProperty("available").GetBoolean(), "Missing unavailable marker.");
    Check(root.GetProperty("id").GetProperty("reason").GetString() == error.GetType().Name + ": " + error.Message,
        "Missing diagnostic reason.");
    Check(subsequentReads == 1 && root.GetProperty("name").GetString() == "Search", "Later fields were not collected.");
    Check(!root.GetProperty("enabled").GetBoolean(), "Supported false was treated as unavailable.");
}
Console.WriteLine("PASS: supported values preserved; unsupported/provider failures recorded; later fields serialized.");

Check(PackageAdmission.IsPicker(123, PackageAdmission.PickerTitle, 123), "Expected picker rejected.");
Check(!PackageAdmission.IsPicker(456, PackageAdmission.PickerTitle, 123), "Other process picker accepted.");
Check(!PackageAdmission.IsPicker(123, "Open", 123), "Wrong picker title accepted.");
string copy = Path.GetFullPath("disposable/Smoke.uabs.sqlite");
Check(PackageAdmission.IsVerifiedCopy(copy, copy, "abc", _ => "ABC"), "Verified copy rejected.");
Check(!PackageAdmission.IsVerifiedCopy("Smoke", copy, "abc", _ => "abc"), "Display name accepted as identity.");
Check(!PackageAdmission.IsVerifiedCopy(Path.GetFullPath("other/Smoke.uabs.sqlite"), copy, "abc", _ => "abc"), "Different same-name package accepted.");
Check(!PackageAdmission.IsVerifiedCopy(copy, copy, "abc", _ => "wrong"), "Wrong hash accepted.");
Check(!PackageAdmission.IsVerifiedCopy(null, copy, "abc", _ => "abc"), "Missing path accepted.");
Check(!PackageAdmission.IsVerifiedCopy(copy, copy, "abc", _ => throw new IOException()), "Unreadable copy accepted.");
Console.WriteLine("PASS: picker process/title and exact package-copy path/hash admission checks.");

try
{
    PackageAdmission.WaitForUnique(() => Array.Empty<string>(), TimeSpan.Zero);
    throw new Exception("Zero matches were accepted.");
}
catch (TimeoutException error) { Check(error.Message.Contains("found 0"), "Zero-match diagnostic missing."); }
try
{
    PackageAdmission.WaitForUnique(() => new[] { "edit1", "edit2" }, TimeSpan.FromSeconds(1));
    throw new Exception("Duplicate matches were accepted.");
}
catch (InvalidOperationException error) { Check(error.Message.Contains("found 2"), "Duplicate-match diagnostic missing."); }
int probes = 0, pauses = 0;
string delayed = PackageAdmission.WaitForUnique(
    () => ++probes < 3 ? Array.Empty<string>() : new[] { "filename Edit" },
    TimeSpan.FromSeconds(1), () => pauses++);
Check(delayed == "filename Edit" && probes == 3 && pauses == 2, "Delayed unique field was not awaited.");
Console.WriteLine("PASS: zero matches time out; duplicate matches reject; delayed unique matches succeed.");

foreach (long handle in new long[] { 0, 12345, -1 })
{
    // Serialize the same nested window shape used by DialogSnapshot, not just the reader result.
    var snapshot = EvidenceValue.CreateDialogSnapshot(123, PackageAdmission.PickerTitle, Task.CompletedTask,
        () => new[] { new { handle = EvidenceValue.ReadHandle(() => new IntPtr(handle)) } });
    using var json = JsonDocument.Parse(JsonSerializer.Serialize(snapshot));
    Check(json.RootElement.GetProperty("windows")[0].GetProperty("handle").GetInt64() == handle,
        "Native handle did not serialize as an integer.");
}
using (var unavailable = JsonDocument.Parse(JsonSerializer.Serialize(
    EvidenceValue.ReadHandle(() => throw new NotSupportedException("NativeWindowHandle unsupported")))))
{
    Check(!unavailable.RootElement.GetProperty("available").GetBoolean(), "Unsupported handle missing unavailable marker.");
    Check(unavailable.RootElement.GetProperty("reason").GetString()!.Contains("NativeWindowHandle unsupported"),
        "Unsupported handle lost diagnostic reason.");
}
var pending = new TaskCompletionSource();
foreach (var (task, expected) in new (Task?, string)[] {
    (null, "not-started"), (pending.Task, "pending"), (Task.CompletedTask, "completed"),
    (Task.FromException(new Exception("Invoke failed")), "faulted"),
    (Task.FromCanceled(new CancellationToken(true)), "canceled") })
{
    using var json = JsonDocument.Parse(JsonSerializer.Serialize(
        EvidenceValue.CreateDialogSnapshot(123, PackageAdmission.PickerTitle, task, () => Array.Empty<object>())));
    Check(json.RootElement.GetProperty("invocation").GetProperty("state").GetString() == expected, "Incorrect Invoke state.");
    if (expected == "faulted") Check(json.RootElement.GetProperty("invocation").GetProperty("error").GetString()!
        .Contains("Invoke failed"), "Invoke fault details missing.");
}
using (var failed = JsonDocument.Parse(JsonSerializer.Serialize(EvidenceValue.CreateDialogSnapshot(
    123, PackageAdmission.PickerTitle, pending.Task, () => throw new COMException("Enumeration failed")))))
{
    Check(!failed.RootElement.GetProperty("windows").GetProperty("available").GetBoolean(), "Enumeration gap missing.");
    Check(failed.RootElement.GetProperty("invocation").GetProperty("state").GetString() == "pending", "Enumeration failure lost Invoke state.");
}
using (var subsequent = JsonDocument.Parse(JsonSerializer.Serialize(EvidenceValue.CreateDialogSnapshot(
    123, PackageAdmission.PickerTitle, Task.CompletedTask, () => new[] { "later attempt" }))))
    Check(subsequent.RootElement.GetProperty("windows")[0].GetString() == "later attempt", "Subsequent diagnostic attempt failed.");
Console.WriteLine("PASS: native handles serialize; unavailable handles and enumeration failures retain diagnostics; Invoke states and later snapshots preserved.");

var paired = SearchAccessibility.CompleteRows(["Name", "Package"],
    [["テスト.bundle", "Smoke"], ["other.bundle", "Other"]], "1–2 displayed / 2 exact indexed matches");
Check(paired.SequenceEqual(new[] { new ExpectedRow("テスト.bundle", "Smoke"), new ExpectedRow("other.bundle", "Other") }),
    "Name/Package association or Unicode was lost.");
void Inaccessible(string[] headers, string[][] cells, string summary)
{
    try { SearchAccessibility.CompleteRows(headers, cells, summary); }
    catch (AccessibilityUnavailable) { return; }
    throw new Exception("Ambiguous/incomplete accessibility was accepted.");
}
Inaccessible(["Package", "Name"], [["bundle", "Smoke"]], "1–1 displayed / 1 exact indexed matches");
Inaccessible(["Name", "Package"], [["bundle"]], "1–1 displayed / 1 exact indexed matches");
Inaccessible(["Name", "Package"], [["bundle", "Smoke", "extra"]], "1–1 displayed / 1 exact indexed matches");
Inaccessible(["Name", "Package"], [["bundle", "Smoke"]], "1–2 displayed / 2 exact indexed matches");
Inaccessible(["Name", "Package"], [["bundle", "Smoke"]], "1–1 displayed / at least 1 indexed matches");
Inaccessible(["Name", "Package"], [["bundle", "Smoke"]], "2–2 displayed / 2 exact indexed matches");
Console.WriteLine("PASS: structural rows preserve Unicode/column association; ambiguous headers/cells and partial/virtualized counts block.");

void Field(string expected, string? value, string?[]? selection, string[]? text)
    => Check(SearchAccessibility.VerifyField(value, selection, text).Status == expected,
        $"Incorrect field verdict: {expected}, Value={value}.");
Field("PASS", "Assets", [null], null); // Unrealized item has no usable Name.
Field("PASS", "Assets", ["ComboBoxItem"], null); // Unusable item identity cannot override a supported Value.
Field("PASS", "Assets", [], ["Assets"]);
Field("PASS", null, ["Assets"], null);
Field("PASS", null, [null], ["Assets"]);
Field("FAIL", "Names", [null], null);
Field("FAIL", null, ["Paths"], ["Paths"]);
Field("FAIL", null, null, ["Dependencies"]);
Field("BLOCKED", null, [null], null);
Field("BLOCKED", null, [], ["Assets", "Names"]);
Field("BLOCKED", "Assets", ["Names"], null);
Field("BLOCKED", "Assets", ["Assets"], ["Names"]);
Field("BLOCKED", "Assets", ["Assets", "Assets"], null);
Field("BLOCKED", "ComboBoxItem", ["Unknown"], null);
Field("BLOCKED", "Unknown", ["Assets"], ["Assets"]);
Console.WriteLine("PASS: field Value/selection/collapsed text verification distinguishes Assets, wrong values, missing and conflicting evidence.");
Check(SearchAccessibility.VerifyField("Names", [null], null, "Names").Status == "PASS", "Explicit Names intent cannot be verified.");
Check(SearchAccessibility.VerifyField("Assets", [null], null, "Names").Status == "FAIL", "Unexpected explicit selector reset was accepted.");
Check(SearchAccessibility.AggregateAcceptance(["PASS", "BLOCKED"]) == "BLOCKED", "Blocked acceptance was promoted to PASS.");
Check(SearchAccessibility.AggregateAcceptance(["BLOCKED", "FAIL"]) == "FAIL", "Failure was hidden by blocked coverage.");
Check(SearchAccessibility.AggregateAcceptance(["PASS", "PASS"]) == "PASS", "Passing coverage aggregation failed.");
Check(SearchAccessibility.AggregateAcceptance([]) == "BLOCKED", "Unexercised acceptance was promoted to PASS.");
Check(SearchAccessibility.AggregateAcceptance(["UNKNOWN"]) == "BLOCKED", "Unknown acceptance was promoted to PASS.");
Console.WriteLine("PASS: explicit selector intent and acceptance PASS/FAIL/BLOCKED aggregation.");

var actions = new List<string>();
SearchAccessibility.GuardedSelectionKeys(() => actions.Add("focus"), () => {
    actions.Add("verify owned focus"); return true;
}, () => actions.Add("keys"));
Check(actions.SequenceEqual(new[] { "focus", "verify owned focus", "keys" }), "Keyboard selection order/gate changed.");
bool sentKeys = false;
try
{
    SearchAccessibility.GuardedSelectionKeys(() => { }, () => false, () => sentKeys = true);
    throw new Exception("Unverified keyboard focus was accepted.");
}
catch (AccessibilityUnavailable) { Check(!sentKeys, "Keys sent without verified owned focus."); }
try
{
    SearchAccessibility.GuardedSelectionKeys(() => throw new NotSupportedException(), () => true, () => sentKeys = true);
    throw new Exception("Unsupported UIA focus was accepted.");
}
catch (NotSupportedException) { Check(!sentKeys, "Keys sent after focus failed."); }
SearchAccessibility.RequireNameSortTarget(true, true, true, true, true);
for (int unavailable = 0; unavailable < 5; unavailable++)
{
    bool[] capability = [true, true, true, true, true]; capability[unavailable] = false;
    try { SearchAccessibility.RequireNameSortTarget(capability[0], capability[1], capability[2], capability[3], capability[4]);
        throw new Exception("Unsupported or unowned sorting target was accepted."); }
    catch (AccessibilityUnavailable) { }
}
string selectionCopy = Path.GetFullPath("selection/Smoke.uabs.sqlite");
var selectedRow = new ExpectedRow("uabs-smoke-0000.bundle", "Smoke");
string inspector = $"Name: {selectedRow.Name}\nPackage: Smoke\nPackage path: {selectionCopy}\nOriginal indexed source: {Path.GetFullPath(selectedRow.Name)}\n"
    + $"Package UUID: {Guid.NewGuid()}\nGeneration: {Guid.NewGuid()}\nSource UUID: {Guid.NewGuid()}\nSource revision: {Guid.NewGuid()}";
Check(SearchAccessibility.SelectedInspectorMatches(inspector, selectedRow, selectionCopy), "Valid selected source identity rejected.");
Check(!SearchAccessibility.SelectedInspectorMatches(inspector, new("wrong.bundle", "Smoke"), selectionCopy), "Wrong selected bundle accepted.");
Check(!SearchAccessibility.SelectedInspectorMatches(inspector, new(selectedRow.Name, "Other"), selectionCopy), "Wrong package accepted.");
Check(!SearchAccessibility.SelectedInspectorMatches(inspector, selectedRow, Path.GetFullPath("other/Smoke.uabs.sqlite")), "Unrelated same-name package accepted.");
Check(!SearchAccessibility.SelectedInspectorMatches(inspector + "\nName: duplicate", selectedRow, selectionCopy), "Duplicate Inspector identity accepted.");
Check(!SearchAccessibility.SelectedInspectorMatches("Name: " + selectedRow.Name, selectedRow, selectionCopy), "Incomplete identity accepted.");
Check(!SearchAccessibility.SelectedInspectorMatches(null, selectedRow, selectionCopy), "Unavailable Inspector accepted.");
Console.WriteLine("PASS: keyboard ownership/focus gating, selected Inspector identity validation, and unsupported sorting stays blocked.");

var matchReport = SearchAccessibility.ExplainInspector(inspector, selectedRow, selectionCopy);
Check(matchReport.Matches && matchReport.Checks.Values.All(value => value), "Valid identity diagnostics disagree with strict matching.");
var wrongFirst = SearchAccessibility.ExplainInspector(inspector, new("uabs-smoke-0002.bundle", "Smoke"), selectionCopy);
Check(!wrongFirst.Matches && !wrongFirst.Checks["name"] && wrongFirst.ActualFields["Name"] == selectedRow.Name,
    "Expected/actual row-order mismatch not explicitly recorded.");
var wrongPath = SearchAccessibility.ExplainInspector(inspector, selectedRow, Path.GetFullPath("unrelated/Smoke.uabs.sqlite"));
Check(!wrongPath.Matches && !wrongPath.Checks["packagePathEqualsCopy"], "Copy-path mismatch diagnostic lost.");
var unavailableReport = SearchAccessibility.ExplainInspector(null, selectedRow, selectionCopy);
Check(!unavailableReport.Matches && !unavailableReport.Checks["hasDetails"], "Unavailable Inspector diagnostics silently passed.");
using (var reportJson = JsonDocument.Parse(JsonSerializer.Serialize(wrongFirst)))
    Check(reportJson.RootElement.GetProperty("Expected").GetProperty("Name").GetString() == "uabs-smoke-0002.bundle"
        && reportJson.RootElement.GetProperty("ActualFields").GetProperty("Name").GetString() == selectedRow.Name,
        "Serialized comparison lacks exact expected/actual identities.");
Console.WriteLine("PASS: Inspector comparison diagnostics retain strict verdicts and report exact row/path/unavailable mismatches.");

var viewport = new UiBounds(100, 50, 400, 300);
PositionedRow Positioned(string name, double top, bool hidden = false) =>
    new(new(name, "Smoke"), new(100, top, 400, 25), hidden);
var reversedEnumeration = new[] { Positioned("0002", 100), Positioned("0001", 75), Positioned("0000", 50) };
Check(SearchAccessibility.VisualRowOrder(reversedEnumeration, viewport).Select(row => row.Name)
    .SequenceEqual(new[] { "0000", "0001", "0002" }), "Reversed enumeration became keyboard boundary order.");
var scrambled = new[] { Positioned("0001", 75), Positioned("0000", 100), Positioned("0002", 50) };
Check(SearchAccessibility.VisualRowOrder(scrambled, viewport).Select(row => row.Name)
    .SequenceEqual(new[] { "0002", "0001", "0000" }), "Order was guessed by reversing enumeration or sorting names.");
Check(SearchAccessibility.VisualRowOrder(reversedEnumeration.Reverse().ToArray(), viewport)
    .SequenceEqual(SearchAccessibility.VisualRowOrder(reversedEnumeration, viewport)), "Visual order depends on enumeration order.");
var reportedEnumeration = new[] {
    Positioned("uabs-smoke-0002.bundle", 100), Positioned("uabs-smoke-0001.bundle", 75),
    Positioned("uabs-smoke-0000.bundle", 50) };
var keyboardOrder = SearchAccessibility.VisualRowOrder(reportedEnumeration, viewport);
Check(!SearchAccessibility.SelectedInspectorMatches(inspector, reportedEnumeration[0].Identity, selectionCopy)
    && SearchAccessibility.SelectedInspectorMatches(inspector, keyboardOrder[0], selectionCopy),
    "Reported Ctrl+Home identity must match visual first row, not reversed enumeration first row.");
void AmbiguousLayout(PositionedRow[] rows, UiBounds? bounds = null)
{
    try { SearchAccessibility.VisualRowOrder(rows, bounds ?? viewport); }
    catch (AccessibilityUnavailable) { return; }
    throw new Exception("Unreliable visual boundary order accepted.");
}
AmbiguousLayout([Positioned("a", 50), Positioned("b", 50)]);
AmbiguousLayout([Positioned("a", 50), Positioned("b", 60)]);
AmbiguousLayout([Positioned("a", 40), Positioned("b", 75)]);
AmbiguousLayout([Positioned("a", 50), Positioned("b", 340)]);
AmbiguousLayout([Positioned("a", 50), Positioned("b", 75, true)]);
AmbiguousLayout([Positioned("a", 50), new(new("b", "Smoke"), new(100, 75, 0, 25), false)]);
AmbiguousLayout([Positioned("a", 50), Positioned("b", double.NaN)]);
AmbiguousLayout([Positioned("a", 50), Positioned("a", 75)]);
AmbiguousLayout([Positioned("a", 50), Positioned("b", 75)], new(0, 0, 0, 0));
Console.WriteLine("PASS: visual bounds determine Home/End boundaries independently of UIA enumeration; ambiguous/clipped/offscreen layouts block.");

bool maximizedAction = false;
SearchAccessibility.PrepareMaximizedWindow(true, false, true, () => maximizedAction = true);
Check(maximizedAction, "Supported deterministic window maximization not requested.");
maximizedAction = false;
SearchAccessibility.PrepareMaximizedWindow(true, true, true, () => maximizedAction = true);
Check(!maximizedAction, "Already maximized window was unnecessarily changed.");
foreach (var capability in new[] { (false, true), (true, false) })
{
    try { SearchAccessibility.PrepareMaximizedWindow(capability.Item1, false, capability.Item2, () => maximizedAction = true); throw new Exception("Unsupported maximize accepted."); }
    catch (AccessibilityUnavailable) { Check(!maximizedAction, "Unsupported window action attempted."); }
}
AmbiguousLayout(reversedEnumeration, new UiBounds(394, 626, 264, -5)); // Actual 27px grid minus 32px header.
int queries = 0, pausesRead = 0, recordedFailures = 0;
string recovered = UiaRead.Fresh("fresh picker", () => {
    if (++queries < 3) throw new COMException("Provider transition", unchecked((int)0x80004005));
    return "fresh window";
}, (_, _) => recordedFailures++, () => pausesRead++);
Check(recovered == "fresh window" && queries == 3 && pausesRead == 2 && recordedFailures == 2, "Bounded fresh-query recovery failed.");
queries = 0;
try {
    UiaRead.Fresh<string>("catalog completion", () => { queries++; throw new COMException("Persistent provider", unchecked((int)0x80004005)); }, pause: () => { });
    throw new Exception("Persistent provider error was swallowed.");
}
catch (AccessibilityUnavailable error) { Check(queries == 3 && error.Message.Contains("80004005") && error.Message.Contains("catalog completion"), "Persistent COM failure lacks phase/HRESULT/bound."); }
queries = 0;
try {
    UiaRead.Fresh<string>("denied", () => { queries++; throw new COMException("Denied", unchecked((int)0x80070005)); }, pause: () => { });
    throw new Exception("Access denied was swallowed.");
}
catch (AccessibilityUnavailable) { Check(queries == 1, "Non-transient provider failure was retried."); }
foreach (int code in new[] { unchecked((int)0x80040201), unchecked((int)0x80010108) })
{
    queries = 0;
    Check(UiaRead.Fresh("stale provider", () => {
        if (++queries == 1) throw new COMException("Disconnected element", code);
        return "rediscovered";
    }, pause: () => { }) == "rediscovered" && queries == 2, "Known stale-element read did not rediscover safely.");
}
queries = 0;
try {
    UiaRead.Fresh<string>("application assertion", () => { queries++; throw new InvalidOperationException("Wrong result"); }, pause: () => { });
    throw new Exception("Assertion failure was swallowed.");
}
catch (InvalidOperationException error) { Check(queries == 1 && error.Message == "Wrong result", "Application assertion retried or reclassified."); }
Console.WriteLine("PASS: supported maximization, reported negative viewport rejection, bounded fresh COM read recovery, persistent provider blocking, and strict assertion preservation.");

foreach (var sample in new[] {
    ("Current Name order: not active (PackageName Ascending). Next Name order: Ascending.", "not active (PackageName Ascending)", "Ascending", false),
    ("Current Name order: Ascending. Next Name order: Descending.", "Ascending", "Descending", false),
    ("Current Name order: Descending. Next Name order: Ascending.", "Descending", "Ascending", false),
    ("Sorting in progress. Current Name order: Ascending. Next Name order: Ascending.", "Ascending", "Ascending", true) })
{
    var state = SearchAccessibility.ReadNameSortState(sample.Item1);
    Check(state == new NameSortState(sample.Item2, sample.Item3, sample.Item4), "Name sort direction/busy evidence changed.");
}
foreach (string? unavailable in new[] { null, "", "Ascending", "Current Name order: Ascending. Next Name order: Ascending.",
    "Current Name order: Descending. Next Name order: Descending.",
    "Current Name order: not active (PackageName Ascending). Next Name order: Descending.",
    "Current Name order: Ascending. Next Name order: Descending. extra" })
{
    try { SearchAccessibility.ReadNameSortState(unavailable); throw new Exception("Missing/conflicting sort evidence accepted."); }
    catch (AccessibilityUnavailable) { }
}
ExpectedRow[] sortOracle = [new("a", "Smoke"), new("b", "Smoke"), new("c", "Smoke")];
PositionedRow[] sortEnumeration = [Positioned("c", 100), Positioned("b", 75), Positioned("a", 50)];
var visualAscending = SearchAccessibility.VisualRowOrder(sortEnumeration, viewport);
SearchAccessibility.VerifyNameOrder(visualAscending, sortOracle, "Ascending");
SearchAccessibility.VerifyNameOrder(SearchAccessibility.VisualRowOrder(
    [Positioned("a", 100), Positioned("c", 50), Positioned("b", 75)], viewport), sortOracle, "Descending");
foreach (var wrong in new[] { sortOracle.Reverse().ToArray(), new[] { sortOracle[0], sortOracle[2], sortOracle[1] },
    new[] { sortOracle[0], sortOracle[1] }, new[] { sortOracle[0], sortOracle[1], sortOracle[1] },
    new[] { sortOracle[0], sortOracle[1], new ExpectedRow("c", "Other") } })
{
    try { SearchAccessibility.VerifyNameOrder(wrong, sortOracle, "Ascending"); throw new Exception("Incorrect Name order or set accepted."); }
    catch (InvalidOperationException error) { Check(error.Message.Contains("independent"), "Wrong order did not remain an assertion failure."); }
}
foreach (var ambiguous in new[] { Array.Empty<ExpectedRow>(), new[] { sortOracle[0] },
    new[] { sortOracle[0], sortOracle[0] }, new[] { sortOracle[0], new ExpectedRow("a", "Other") } })
{
    try { SearchAccessibility.NameOrderOracle(ambiguous, "Ascending"); throw new Exception("Ambiguous sort oracle accepted."); }
    catch (AccessibilityUnavailable) { }
}
try { SearchAccessibility.NameOrderOracle(sortOracle, "unknown"); throw new Exception("Unknown direction accepted."); }
catch (AccessibilityUnavailable) { }
Console.WriteLine("PASS: exact independent ascending/descending visual Name order, wrong sets/orders fail, and ambiguous direction/target/oracle evidence blocks.");

CriteriaEdit[] emptyCriteria = CriteriaContract.Fields.Select((pair, index) => new CriteriaEdit("runtime-" + index, pair.Value.AutomationId, pair.Value.AccessibleName, true, "")).ToArray();
var boundCriteria = CriteriaContract.Bind(emptyCriteria.Reverse().ToArray());
var sentinels = CriteriaContract.Sentinels("テスト");
var populatedCriteria = emptyCriteria.Select(edit => edit with { Value = sentinels[boundCriteria.Single(pair => pair.Value == edit.RuntimeId).Key] }).ToArray();
CriteriaContract.Equal(sentinels, CriteriaContract.Read(boundCriteria, populatedCriteria.Reverse().ToArray()));
foreach (var changed in new[] {
    populatedCriteria.Select(edit => edit with { Value = "" }).ToArray(),
    populatedCriteria.Select(edit => edit.RuntimeId == boundCriteria["classId"] ? edit with { Value = sentinels["pathId"] } : edit).ToArray(),
    populatedCriteria.Select(edit => edit.RuntimeId == boundCriteria["include"] ? edit with { Value = "wrong Unicode テスト" } : edit).ToArray() })
{
    try { CriteriaContract.Equal(sentinels, CriteriaContract.Read(boundCriteria, changed)); throw new Exception("Changed/default/swapped criteria accepted."); }
    catch (InvalidOperationException error) { Check(error.Message.Contains("semantic criteria"), "Observed value mismatch did not remain FAIL."); }
}
foreach (var unsupported in new[] {
    emptyCriteria.Skip(1).ToArray(), emptyCriteria.Append(emptyCriteria[0] with { RuntimeId = "duplicate-label" }).ToArray(),
    emptyCriteria.Select(edit => edit with { RuntimeId = "same-id" }).ToArray(),
    emptyCriteria.Select(edit => edit with { Writable = false }).ToArray(),
    emptyCriteria.Select(edit => edit with { Value = "persisted" }).ToArray(),
    emptyCriteria.Select(edit => edit with { AutomationId = null }).ToArray(),
    emptyCriteria.Select(edit => edit with { AccessibleName = null }).ToArray(),
    emptyCriteria.Select(edit => edit with { AutomationId = "wrong-id" }).ToArray(),
    emptyCriteria.Select(edit => edit with { AccessibleName = "wrong-name" }).ToArray(),
    emptyCriteria.Append(emptyCriteria[0] with { RuntimeId = "duplicate-id", AccessibleName = "Other" }).ToArray(),
    emptyCriteria.Append(emptyCriteria[0] with { RuntimeId = "duplicate-name", AutomationId = "Other" }).ToArray(),
    emptyCriteria.Select(edit => edit with { RuntimeId = "" }).ToArray() })
{
    try { CriteriaContract.Bind(unsupported); throw new Exception("Ambiguous/nonempty/unsupported criterion binding accepted."); }
    catch (AccessibilityUnavailable) { }
}
foreach (var unsupported in new[] {
    populatedCriteria.Skip(1).ToArray(), populatedCriteria.Append(populatedCriteria[0]).ToArray(),
    populatedCriteria.Select(edit => edit with { RuntimeId = "replacement-" + edit.RuntimeId }).ToArray(),
    populatedCriteria.Select(edit => edit with { Writable = false }).ToArray(),
    populatedCriteria.Select(edit => edit with { AccessibleName = "changed-name" }).ToArray(),
    populatedCriteria.Select(edit => edit with { AutomationId = "changed-id" }).ToArray(),
    populatedCriteria.Append(populatedCriteria[0] with { RuntimeId = "duplicate-name", AutomationId = "Other" }).ToArray(),
    populatedCriteria.Append(populatedCriteria[0] with { RuntimeId = "duplicate-id", AccessibleName = "Other" }).ToArray() })
{
    try { CriteriaContract.Read(boundCriteria, unsupported); throw new Exception("Lost/replaced/ambiguous runtime identity accepted."); }
    catch (AccessibilityUnavailable) { }
}
Check(CriteriaContract.Sentinels("テスト")["pathId"].StartsWith('-') && sentinels.Values.All(value => value.Length > 0), "Non-default signed/numeric/type sentinels missing.");
CriteriaContract.Equal(sentinels, CriteriaContract.Read(boundCriteria,
    populatedCriteria.Append(new CriteriaEdit("unrelated", null, null, true, "")).Reverse().ToArray()));
Check(CriteriaContract.Bind(emptyCriteria.Append(new CriteriaEdit("unrelated", null, null, true, "")).ToArray()).Count == 5,
    "An unrelated unnamed Edit was used as a role or blocked verified fields.");
Console.WriteLine("PASS: unique paired AutomationId/Name binding, stable runtime identities, reordered discovery, exact Unicode/non-default readback; changed values FAIL and missing/duplicate/conflicting/replaced identities BLOCK.");

var retainedBaseline = new RetainedResultsBaseline([new("テスト.bundle", "Smoke")], "Include: テスト; Path ID: (none); packages: 1.");
var retainedState = new RetainedResultsState(["1 displayed · stale count"], [RetainedResultsContract.StaleIndicator],
    [RetainedResultsContract.ChangedIndicator], [retainedBaseline.LastExecutedCriteria], [], 0);
string[][] retainedCells = [["テスト.bundle", "Smoke"]];
RetainedResultsContract.Verify(["Name", "Package"], retainedCells, retainedState, retainedBaseline, true);
var freshRetainedState = retainedState with { Summaries = ["1–1 displayed / 1 exact indexed matches"], StaleIndicators = [] };
RetainedResultsContract.Verify(["Name", "Package"], retainedCells, freshRetainedState, retainedBaseline, false);
Inaccessible(["Name", "Package"], retainedCells, "1 displayed · stale count"); // Fresh readers remain strictly fresh.
foreach (var unavailable in new[] {
    retainedState with { Summaries = [] }, retainedState with { Summaries = ["1 displayed · stale count", "1 displayed · stale count"] },
    retainedState with { Summaries = ["unknown result state"] }, retainedState with { Summaries = ["one displayed · stale count"] },
    retainedState with { StaleIndicators = [] }, retainedState with { StaleIndicators = [RetainedResultsContract.StaleIndicator, RetainedResultsContract.StaleIndicator] },
    retainedState with { ChangedIndicators = [] }, retainedState with { ChangedIndicators = [RetainedResultsContract.ChangedIndicator, RetainedResultsContract.ChangedIndicator] },
    retainedState with { LastExecutedCriteria = [] }, retainedState with { LastExecutedCriteria = [retainedBaseline.LastExecutedCriteria, retainedBaseline.LastExecutedCriteria] },
    freshRetainedState with { StaleIndicators = [RetainedResultsContract.StaleIndicator] } })
{
    try { RetainedResultsContract.Verify(["Name", "Package"], retainedCells, unavailable, retainedBaseline, false); throw new Exception("Ambiguous/unavailable retained evidence accepted."); }
    catch (AccessibilityUnavailable) { }
}
foreach (var wrong in new[] {
    retainedState with { Summaries = ["2 displayed · stale count"] }, retainedState with { Summaries = ["0 displayed · stale count"] },
    retainedState with { Summaries = ["01 displayed · stale count"] }, retainedState with { LastExecutedCriteria = ["Include: changed; Path ID: -42; packages: 1."] },
    retainedState with { Errors = ["Unexpected automatic Search rejection"] }, retainedState with { ActiveSearches = 1 }, freshRetainedState })
{
    try { RetainedResultsContract.Verify(["Name", "Package"], retainedCells, wrong, retainedBaseline, true); throw new Exception("Wrong count or implicit Search evidence accepted."); }
    catch (InvalidOperationException) { }
}
foreach (var wrong in new[] { new[] { new[] { "wrong.bundle", "Smoke" } }, new[] { new[] { "テスト.bundle", "Other" } } })
{
    try { RetainedResultsContract.Verify(["Name", "Package"], wrong, retainedState, retainedBaseline, true); throw new Exception("Wrong retained identity accepted."); }
    catch (InvalidOperationException) { }
}
try { RetainedResultsContract.Verify(["Name", "Package"], [["wrong.bundle", "Smoke"]], retainedState with { StaleIndicators = [] }, retainedBaseline, true); throw new Exception("Known incorrect complete rows were hidden by missing status evidence."); }
catch (InvalidOperationException) { }
foreach (string[][] incomplete in new string[][][] { [], [["テスト.bundle"]], [["テスト.bundle", "Smoke", "extra"]], [ ["", "Smoke"] ] })
{
    try { RetainedResultsContract.Verify(["Name", "Package"], incomplete, retainedState, retainedBaseline, true); throw new Exception("Incomplete retained UIA evidence accepted."); }
    catch (AccessibilityUnavailable) { }
}
var twoRetained = new RetainedResultsBaseline([new("a", "Smoke"), new("b", "Smoke")], retainedBaseline.LastExecutedCriteria);
var twoState = retainedState with { Summaries = ["2 displayed · stale count"] };
RetainedResultsContract.Verify(["Name", "Package"], [["b", "Smoke"], ["a", "Smoke"]], twoState, twoRetained, true);
try { RetainedResultsContract.Verify(["Name", "Package"], [["a", "Smoke"], ["a", "Smoke"]], twoState, twoRetained, true); throw new Exception("Wrong duplicate retained multiset accepted."); }
catch (InvalidOperationException) { }
Console.WriteLine("PASS: exact retained stale/fresh phase distinction; complete row multisets and unchanged executed criteria; wrong counts/identities/implicit execution FAIL; missing, virtualized or ambiguous evidence BLOCKS; fresh reader unchanged.");

var packageAsc = PackageSortContract.Oracle("Ascending"); var packageDesc = PackageSortContract.Oracle("Descending");
PackageSortContract.VerifyOrder(packageAsc, "Ascending"); PackageSortContract.VerifyOrder(packageDesc, "Descending");
Check(!packageDesc.SequenceEqual(packageAsc.Reverse()), "Descending must not reverse ascending ties.");
foreach (var wrong in new[] { packageAsc.Reverse().ToArray(), new[] { packageDesc[0], packageDesc[2], packageDesc[1] },
    packageAsc, packageDesc.Take(2).ToArray(), new[] { packageDesc[0], packageDesc[1], new ExpectedRow(packageDesc[2].Name, "Wrong") } })
{
    try { PackageSortContract.VerifyOrder(wrong, "Descending"); throw new Exception("Wrong Package primary/tie/complete set accepted."); }
    catch (InvalidOperationException) { }
}
var packageGeometry = new[] { new PositionedRow(packageAsc[2], new(100, 100, 400, 25), false),
    new PositionedRow(packageAsc[0], new(100, 50, 400, 25), false), new PositionedRow(packageAsc[1], new(100, 75, 400, 25), false) };
PackageSortContract.VerifyOrder(SearchAccessibility.VisualRowOrder(packageGeometry, viewport), "Ascending");
foreach (string state in new[] { "Current Package order: Ascending. Next Package order: Descending.",
    "Current Package order: Descending. Next Package order: Ascending.",
    "Current Package order: not active (SourceName Ascending). Next Package order: Ascending.",
    "Sorting in progress. Current Package order: Ascending. Next Package order: Ascending." })
    PackageSortContract.ReadState(state);
foreach (string? state in new[] { null, "", "Current Name order: Ascending. Next Name order: Descending.",
    "Current Package order: Ascending. Next Package order: Ascending.", "Sort all matching V2 results; Descending" })
{
    try { PackageSortContract.ReadState(state); throw new Exception("Unsupported/wrong-field/ambiguous Package state accepted."); }
    catch (AccessibilityUnavailable) { }
}
// Structural unit vectors only; these are not databases or fixture-generation claims.
PackageSortSource SourceVector(int i) => new(PackageSortContract.Names[i], Path.GetFullPath(Path.Combine("fixture-unit", "bundles", PackageSortContract.Names[i])), new string('a', 64), Guid.NewGuid(), Guid.NewGuid(), ["asset witness"]);
PackageSortFixture[] fixtureVectors = [new("Alpha", Path.GetFullPath("fixture-unit/Alpha.uabs.sqlite"), new string('b', 64), Guid.NewGuid(), Guid.NewGuid(), [SourceVector(2), SourceVector(1)]),
    new("Zeta", Path.GetFullPath("fixture-unit/Zeta.uabs.sqlite"), new string('c', 64), Guid.NewGuid(), Guid.NewGuid(), [SourceVector(0)])];
PackageSortContract.Validate(fixtureVectors);
foreach (var invalid in new[] { fixtureVectors.Take(1).ToArray(), new[] { fixtureVectors[0], fixtureVectors[1] with { Package = "Alpha" } },
    new[] { fixtureVectors[0], fixtureVectors[1] with { PackageId = fixtureVectors[0].PackageId } },
    new[] { fixtureVectors[0] with { Path = "relative.sqlite" }, fixtureVectors[1] },
    new[] { fixtureVectors[0] with { Sha256 = "wrong" }, fixtureVectors[1] },
    new[] { fixtureVectors[0] with { Sources = [fixtureVectors[0].Sources[0]] }, fixtureVectors[1] },
    new[] { fixtureVectors[0] with { Sources = fixtureVectors[0].Sources.Select(s => s with { Witnesses = [] }).ToArray() }, fixtureVectors[1] },
    new[] { fixtureVectors[0] with { Sources = fixtureVectors[0].Sources.Select(s => s with { SourceId = Guid.Empty }).ToArray() }, fixtureVectors[1] } })
{
    try { PackageSortContract.Validate(invalid); throw new Exception("Invalid Package allocation/identity/path/hash/witness accepted."); }
    catch (AccessibilityUnavailable) { }
}
if (args.Length != 0)
{
    Check(args.Length == 2 && args[0] == "--package-fixture", "Expected --package-fixture <prepared scenario>.");
    using var fixtureJson = JsonDocument.Parse(File.ReadAllText(args[1]));
    var metadata = JsonSerializer.Deserialize<PackageSortFixture[]>(fixtureJson.RootElement.GetProperty("packageSortFixtures").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    PackageSortContract.Validate(metadata);
    Console.WriteLine("PASS: real prepared Alpha/Zeta metadata passes the runner contract (UUID JSON and canonical path round trip).");
}
Console.WriteLine("PASS: Package primary/tie oracle, geometry-derived order, active-field state and invalid fixture metadata; independent of Search/comparer/UI enumeration.");
Check(PackageSortContract.Completed(new("Ascending", "Descending", false), new("Descending", "Ascending", false), false, "Descending",
    "Sort all matching V2 results; Descending", "Sort all matching V2 results; not the active order"), "Genuine active Package completion rejected.");
Check(!PackageSortContract.Completed(new("not active (SourceName Ascending)", "Ascending", false), new("not active (SourceName Ascending)", "Ascending", false), false, "Ascending", "", ""), "Old inactive state inferred completion before the action began.");
try { PackageSortContract.Completed(new("Ascending", "Descending", false), new("not active (SourceName Ascending)", "Ascending", false), false, "Descending",
    "Sort all matching V2 results; not the active order", "Sort all matching V2 results; Ascending"); throw new Exception("Wrong active primary field accepted."); }
catch (InvalidOperationException) { }
Check(!PackageSortContract.Completed(new("Ascending", "Descending", false), new("not active (SourceName Ascending)", "Ascending", false), false, "Descending",
    "Sort all matching V2 results; Descending", "Sort all matching V2 results; not the active order"), "Conflicting Package state/header evidence inferred completion.");
PackageSortContract.RequireAction(true, true, true, true, true);
for (int missing = 0; missing < 5; missing++)
{
    bool[] capability = [true, true, true, true, true]; capability[missing] = false;
    try { PackageSortContract.RequireAction(capability[0], capability[1], capability[2], capability[3], capability[4]); throw new Exception("Unsupported Package action accepted."); }
    catch (AccessibilityUnavailable) { }
}
