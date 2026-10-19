namespace UABS.UnityAssets.Packages;

public interface IPackageEditorSessionGuard
{
    bool IsPackageOpen(string packagePath);
}

public interface IPackageEditorSessionController
{
    void SetActivePackage(string? packagePath);
}

public sealed class PackageEditorSessionGuard : IPackageEditorSessionGuard, IPackageEditorSessionController
{
    private readonly object _sync = new();
    private string? _activePackageIdentity;

    public bool IsPackageOpen(string packagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        string identity = PackagePathComparer.GetIdentity(packagePath);
        lock (_sync)
            return string.Equals(_activePackageIdentity, identity, StringComparison.OrdinalIgnoreCase);
    }

    public void SetActivePackage(string? packagePath)
    {
        string? identity = string.IsNullOrWhiteSpace(packagePath)
            ? null
            : PackagePathComparer.GetIdentity(packagePath);
        lock (_sync)
            _activePackageIdentity = identity;
    }
}
