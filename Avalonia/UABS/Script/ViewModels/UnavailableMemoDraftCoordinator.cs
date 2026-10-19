using System;
using System.Threading;
using System.Threading.Tasks;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.ViewModels;

// Standalone presenters without the application coordinator must never write.
internal sealed class UnavailableMemoDraftCoordinator : IMemoDraftCoordinator
{
    public event EventHandler<MemoDraftChangedEventArgs>? DraftChanged { add { } remove { } }
    public MemoDraftSnapshot Track(PackageMemoIdentity identity, string? persistedMemo, string? packageRevision) => MemoDraftSnapshot.Unavailable;
    public MemoDraftSnapshot Edit(PackageMemoIdentity identity, string? memo) => MemoDraftSnapshot.Unavailable;
    public MemoDraftSnapshot GetSnapshot(PackageMemoIdentity? identity) => MemoDraftSnapshot.Unavailable;
    public Task<MemoDraftSnapshot> SaveNowAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) => Task.FromResult(MemoDraftSnapshot.Unavailable);
    public Task<MemoDraftSnapshot> RetryAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) => Task.FromResult(MemoDraftSnapshot.Unavailable);
    public Task<MemoDraftSnapshot> FlushForNavigationAsync(PackageMemoIdentity identity, CancellationToken cancellationToken = default) => Task.FromResult(MemoDraftSnapshot.Unavailable);
    public Task<MemoDraftDrainResult> DrainForShutdownAsync(CancellationToken cancellationToken = default) => Task.FromResult(new MemoDraftDrainResult([]));
}
