using System.Reflection;

namespace UABS.UnityAssets.Tests;

public sealed class UnityDependencyAuditTests
{
    [Fact]
    public void ApplicationAssemblies_DoNotReferenceOrShipUnityRuntimeAssemblies()
    {
        string[] applicationAssemblies =
        [
            Path.Combine(AppContext.BaseDirectory, "UABS.dll"),
            Path.Combine(AppContext.BaseDirectory, "UABS.UnityAssets.dll")
        ];

        string[] references = applicationAssemblies
            .Select(AssemblyName.GetAssemblyName)
            .SelectMany(name => Assembly.Load(name).GetReferencedAssemblies())
            .Select(name => name.Name ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] shipped = Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.DoesNotContain(references, IsUnityAssembly);
        Assert.DoesNotContain(shipped, IsUnityAssembly);
    }

    private static bool IsUnityAssembly(string name) =>
        name.Equals("UnityEngine", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("UnityEngine.", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UnityEditor", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("UnityEditor.", StringComparison.OrdinalIgnoreCase);
}
