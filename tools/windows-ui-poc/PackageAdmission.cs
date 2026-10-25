internal static class PackageAdmission
{
    internal static T WaitForUnique<T>(Func<T[]> query, TimeSpan timeout, Action? pause = null,
        string description = "Filename Edit AutomationId 1148")
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            var matches = query();
            if (matches.Length == 1) return matches[0];
            if (matches.Length > 1)
                throw new InvalidOperationException($"Ambiguous {description}: found {matches.Length}.");
            if (watch.Elapsed >= timeout)
                throw new TimeoutException($"{description} did not become available (found 0).");
            if (pause is null) System.Threading.Thread.Sleep(100);
            else pause();
        }
    }

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
