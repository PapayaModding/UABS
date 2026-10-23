using System.Diagnostics;

namespace UABS.PackageV2.Search;

/// <summary>Opt-in paging phase evidence; no criteria, paths or memo contents are recorded.</summary>
public static class SearchTrace
{
    public static readonly ActivitySource Source = new("UABS.Search");
    public static Activity? Start(string phase) => Source.StartActivity(phase, ActivityKind.Internal);
}
