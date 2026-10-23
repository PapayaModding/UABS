using System.Reflection;
using UABS.PackageV2.Search;
using UABS.UnityAssets.Packages.Search;
using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class V2SearchWindowTests
{
    // Controlled opaque cursor construction only in this controller-level fixture;
    // production cursors always originate in the unchanged Gate3 engine.
    private sealed class Pages(int count)
    {
        private readonly Guid _session = Guid.NewGuid();
        private readonly SearchRevision _revision = new(Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), 0);
        internal int? FailOnce;
        internal bool UnknownCount;
        internal int Active, Peak, Calls;
        internal Func<long, CancellationToken, Task>? Before;
        private static readonly ConstructorInfo Constructor = typeof(SearchCursor).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
        private static readonly PropertyInfo Loaded = typeof(SearchCursor).GetProperty("Loaded", BindingFlags.NonPublic | BindingFlags.Instance)!;
        internal async Task<SearchPage> Fetch(SearchCursor? cursor, CancellationToken token)
        {
            long start = cursor is null ? 0 : (long)Loaded.GetValue(cursor)!; long page = start / 100;
            Calls++; Peak = Math.Max(Peak, Interlocked.Increment(ref Active));
            try
            {
                if (Before is not null) await Before(page, token); token.ThrowIfCancellationRequested();
                if (FailOnce == page) { FailOnce = null; return new(SearchStatus.Failed, [], null, false, new(SearchCountKind.Unknown, 0), [new("Injected", "Retry fixture")], [_revision]); }
                var rows = Enumerable.Range(0, (int)Math.Min(100, count - start)).Select(i => {
                    long number = start + i;
                    return new SearchResult(new(_revision.PackageId, _revision.GenerationId, PackageSearchResultKind.Asset,
                        _session, _session, 3, (int)number), PackageSearchMatchCategory.AssetName, "Package", "fixture.uabs.sqlite", "index",
                        "Source", "missing.bundle", "相同 name " + number, number, 1234567, "UnknownType", null, 0, "addressable");
                }).ToArray();
                bool more = start + rows.Length < count;
                var next = more ? (SearchCursor)Constructor.Invoke([_session, rows[^1], start + rows.Length]) : null;
                return new(SearchStatus.Success, rows, next, more, new(UnknownCount ? SearchCountKind.Unknown : more ? SearchCountKind.LoadedAtLeast : SearchCountKind.Exact,
                    UnknownCount ? 0 : start + rows.Length), [], [_revision]);
            }
            finally { Interlocked.Decrement(ref Active); }
        }
    }
    [Fact]
    public async Task DeepForwardAndBackwardReplay_BoundsRowsAndCheckpoints_WithoutLostPositions()
    {
        var pages = new Pages(35000); var window = new V2SearchWindow(pages.Fetch);
        for (int page = 0; page < 350; page++)
        {
            window.Apply(page, await window.ReadAsync(page, default));
            Assert.InRange(window.Rows.Count, 1, V2SearchWindow.MaximumRows);
            Assert.InRange(window.CheckpointCount, 1, V2SearchWindow.MaximumCheckpoints);
            Assert.Equal(Enumerable.Range((int)window.StartIndex, window.Rows.Count).Select(i => (long?)i), window.Rows.Select(r => r.PathId));
        }
        Assert.False(window.CanForward); Assert.Equal(35000, window.ExactCount);
        while (window.CanBackward)
        {
            long previous = window.StartPage - 1; window.Apply(previous, await window.ReadAsync(previous, default));
            Assert.Equal(previous * 100, window.Rows[0].PathId); Assert.True(window.Rows.Count <= 600);
        }
        Assert.Equal(0, window.StartIndex); Assert.Equal(1, pages.Peak); Assert.True(window.FetchedPages > 350); // replay, not a new Search
    }
    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    [InlineData(100)]
    [InlineData(217)]
    public async Task UnknownTotalsAndFinalPages_DoNotInventExtentOrFalseExhaustion(int count)
    {
        var pages = new Pages(count) { UnknownCount = true }; var window = new V2SearchWindow(pages.Fetch);
        long index = 0;
        do { window.Apply(index, await window.ReadAsync(index, default)); index++; } while (window.CanForward);
        Assert.Equal(count, window.SeenRows); Assert.Equal(count, window.ExactCount);
        Assert.Equal(count, window.Rows.Count); Assert.False(window.CanForward);
    }
    [Fact]
    public async Task FailureAndCancellation_PreserveRowsAndCanRetryTheSameBoundary()
    {
        var pages = new Pages(350); var window = new V2SearchWindow(pages.Fetch);
        window.Apply(0, await window.ReadAsync(0, default)); var original = window.Rows.ToArray();
        pages.FailOnce = 1;
        Assert.Equal(SearchStatus.Failed, (await window.ReadAsync(1, default)).Status); Assert.Equal(original, window.Rows);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => window.ReadAsync(1, cancelled.Token)); Assert.Equal(original, window.Rows);
        window.Apply(1, await window.ReadAsync(1, default)); Assert.Equal(200, window.Rows.Count);
        Assert.Equal(200, window.Rows.Select(r => r.PathId).Distinct().Count());
    }
    [Fact]
    public async Task FetchesAreSerialized_AndWaitingRequestsAreCancellable()
    {
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pages = new Pages(200) { Before = async (_, ct) => { reached.TrySetResult(); await release.Task.WaitAsync(ct); } };
        var window = new V2SearchWindow(pages.Fetch); var first = window.ReadAsync(0, default); await reached.Task;
        using var cancel = new CancellationTokenSource(); var second = window.ReadAsync(0, cancel.Token); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second); release.SetResult(); await first;
        Assert.Equal(1, pages.Peak); Assert.Equal(1, pages.Calls);
    }
}
