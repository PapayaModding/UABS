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
