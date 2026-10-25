internal static class PackageAdmission
{
    internal const string PickerTitle = "Open read-only UABS V2 package";

    internal static bool IsPicker(int processId, string title, int expectedProcessId) =>
        processId == expectedProcessId && title == PickerTitle;

    // IDs and display names alone cannot establish which file the app registered.
    internal static bool IsVerifiedCopy(string? reportedPath, string copy, string expectedHash,
        Func<string, string> hash)
    {
        if (string.IsNullOrWhiteSpace(reportedPath)) return false;
        try
        {
            return Path.IsPathFullyQualified(reportedPath)
                && Path.GetFullPath(reportedPath).Equals(Path.GetFullPath(copy), StringComparison.OrdinalIgnoreCase)
                && hash(copy).Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}
