internal static class EvidenceValue
{
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
