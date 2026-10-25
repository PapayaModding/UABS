internal static class EvidenceValue
{
    public static object? ReadHandle(Func<IntPtr> read) => Read(() => read().ToInt64());

    public static object CreateDialogSnapshot(int processId, string expectedTitle, Task? invocation,
        Func<object?> readWindows) => new
    {
        utc = DateTime.UtcNow,
        expectedProcessId = processId,
        expectedTitle,
        invocation = new
        {
            state = invocation is null ? "not-started" : invocation.IsFaulted ? "faulted"
                : invocation.IsCanceled ? "canceled" : invocation.IsCompletedSuccessfully ? "completed" : "pending",
            taskStatus = invocation?.Status.ToString(),
            error = invocation?.Exception?.ToString()
        },
        // Preserve invocation/identity expectations even if enumeration itself fails.
        windows = Read(readWindows)
    };

    // Diagnostic reads only: scenario assertions must continue to use strict UIA reads.
    public static object? Read(Func<object?> read)
    {
        try { return read(); }
        catch (Exception error)
        {
            // Providers may reject a property or lose an element during enumeration.
            // Keep the remaining fields/elements and preserve the reason for this gap.
            return new { available = false, reason = error.GetType().Name + ": " + error.Message };
        }
    }
}
