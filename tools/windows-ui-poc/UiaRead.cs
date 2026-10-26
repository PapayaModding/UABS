using System.Runtime.InteropServices;

internal static class UiaRead
{
    // Read-only discovery only. Never pass an Invoke, SetValue, or input action.
    internal static T Fresh<T>(string phase, Func<T> query, Action<int, COMException>? onFailure = null,
        Action? pause = null)
    {
        for (int attempt = 1; ; attempt++)
        {
            try { return query(); }
            catch (COMException error)
            {
                onFailure?.Invoke(attempt, error);
                bool transient = error.HResult is unchecked((int)0x80004005) // E_FAIL: observed FindAll failure
                    or unchecked((int)0x80040201) // UIA_E_ELEMENTNOTAVAILABLE
                    or unchecked((int)0x80010108); // RPC_E_DISCONNECTED
                if (!transient || attempt == 3)
                    throw new AccessibilityUnavailable($"UIA provider inaccessible during {phase}; HRESULT 0x{error.HResult:X8}; {attempt} read attempt(s). {error}");
                if (pause is null) Thread.Sleep(100); else pause();
            }
        }
    }
}
