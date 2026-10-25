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
