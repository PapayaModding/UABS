using System.Diagnostics;

namespace UABS.UnityAssets.Packages.Memos;

/// <summary>Opt-in phase timing, without asset paths, identities or memo contents. No listener means no activity allocation.</summary>
public static class MemoTrace
{
    public static readonly ActivitySource Source = new("UABS.Memos");
    public static Activity? Start(string phase) => Source.StartActivity(phase, ActivityKind.Internal);
}
