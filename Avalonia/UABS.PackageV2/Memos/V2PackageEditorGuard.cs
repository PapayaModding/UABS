namespace UABS.PackageV2.Memos;

/// <summary>Application-lifetime local guard; share with the writer. Not a cross-process lease.</summary>
public sealed class V2PackageEditorGuard
{
    private readonly object _gate = new();
    private readonly HashSet<Guid> _packages = [];
    public bool IsOpen(Guid packageId) { lock (_gate) return _packages.Contains(packageId); }
    internal V2EditorSession? Enter(EditorBaseRevision revision)
    {
        lock (_gate) return _packages.Add(revision.PackageId) ? new(this, revision) : null;
    }
    internal void Release(Guid packageId) { lock (_gate) _packages.Remove(packageId); }
}

/// <summary>Only explicit disposal closes the editor. Failed Save/Cancel never disposes this lease.</summary>
public sealed class V2EditorSession : IDisposable
{
    private V2PackageEditorGuard? _guard;
    internal V2EditorSession(V2PackageEditorGuard guard, EditorBaseRevision revision) { _guard = guard; Base = revision; }
    public EditorBaseRevision Base { get; }
    public void Dispose() => Interlocked.Exchange(ref _guard, null)?.Release(Base.PackageId);
}
