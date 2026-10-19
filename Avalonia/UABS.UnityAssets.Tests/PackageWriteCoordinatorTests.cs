using UABS.UnityAssets.Packages;

namespace UABS.UnityAssets.Tests;

public sealed class PackageWriteCoordinatorTests
{
    [Fact]
    public async Task SameNormalizedPackagePathSerializesOperations()
    {
        PackageWriteCoordinator coordinator = new();
        TaskCompletionSource firstEntered = Completion();
        TaskCompletionSource releaseFirst = Completion();
        TaskCompletionSource secondEntered = Completion();

        Task first = coordinator.ExecutePackageWriteAsync(
            @"C:\packages\one\",
            async _ =>
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
            });
        await firstEntered.Task;
        Task second = coordinator.ExecutePackageWriteAsync(
            "c:/PACKAGES/one",
            _ =>
            {
                secondEntered.SetResult();
                return Task.CompletedTask;
            });

        Assert.False(secondEntered.Task.IsCompleted);
        releaseFirst.SetResult();
        await Task.WhenAll(first, second);

        Assert.True(secondEntered.Task.IsCompletedSuccessfully);
        Assert.Equal(0, coordinator.TrackedPathCount);
    }

    [Fact]
    public async Task DifferentPackagesCanExecuteConcurrently()
    {
        PackageWriteCoordinator coordinator = new();
        TaskCompletionSource firstEntered = Completion();
        TaskCompletionSource secondEntered = Completion();
        TaskCompletionSource release = Completion();

        Task first = coordinator.ExecutePackageWriteAsync(
            @"C:\packages\one",
            async _ =>
            {
                firstEntered.SetResult();
                await release.Task;
            });
        Task second = coordinator.ExecutePackageWriteAsync(
            @"C:\packages\two",
            async _ =>
            {
                secondEntered.SetResult();
                await release.Task;
            });

        await Task.WhenAll(firstEntered.Task, secondEntered.Task);
        Assert.Equal(2, coordinator.TrackedPathCount);
        release.SetResult();
        await Task.WhenAll(first, second);
        Assert.Equal(0, coordinator.TrackedPathCount);
    }

    [Fact]
    public async Task CancellationWhileWaitingDoesNotRunOrBlockLaterOperation()
    {
        PackageWriteCoordinator coordinator = new();
        TaskCompletionSource firstEntered = Completion();
        TaskCompletionSource releaseFirst = Completion();
        bool cancelledOperationRan = false;
        using CancellationTokenSource cancellation = new();

        Task first = coordinator.ExecutePackageWriteAsync(
            @"C:\packages\one",
            async _ =>
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
            });
        await firstEntered.Task;
        Task waiting = coordinator.ExecutePackageWriteAsync(
            @"C:\packages\one",
            _ =>
            {
                cancelledOperationRan = true;
                return Task.CompletedTask;
            },
            cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.False(cancelledOperationRan);
        releaseFirst.SetResult();
        await first;

        bool laterRan = false;
        await coordinator.ExecutePackageWriteAsync(
            @"C:\packages\one",
            _ =>
            {
                laterRan = true;
                return Task.CompletedTask;
            });
        Assert.True(laterRan);
        Assert.Equal(0, coordinator.TrackedPathCount);
    }

    [Fact]
    public async Task OperationFailureReleasesPathForNextWriter()
    {
        PackageWriteCoordinator coordinator = new();

        await Assert.ThrowsAsync<IOException>(() => coordinator.ExecutePackageWriteAsync(
            @"C:\packages\one",
            _ => Task.FromException(new IOException("write failed"))));

        int result = await coordinator.ExecutePackageWriteAsync(
            @"C:\packages\one",
            _ => Task.FromResult(42));
        Assert.Equal(42, result);
        Assert.Equal(0, coordinator.TrackedPathCount);
    }

    [Fact]
    public async Task RepeatedOperationsDoNotRetainPerPathLocks()
    {
        PackageWriteCoordinator coordinator = new();

        for (int index = 0; index < 100; index++)
        {
            await coordinator.ExecutePackageWriteAsync(
                $@"C:\packages\package-{index}",
                _ => Task.CompletedTask);
            Assert.Equal(0, coordinator.TrackedPathCount);
        }
    }

    [Fact]
    public async Task PreCancelledRequestDoesNotCreateLockOrRunOperation()
    {
        PackageWriteCoordinator coordinator = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        bool ran = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.ExecutePackageWriteAsync(
            @"C:\packages\one",
            _ =>
            {
                ran = true;
                return Task.CompletedTask;
            },
            cancellation.Token));

        Assert.False(ran);
        Assert.Equal(0, coordinator.TrackedPathCount);
    }

    private static TaskCompletionSource Completion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
