using System.Collections.Concurrent;
using System.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using UABS.PackageV2;
using UABS.PackageV2.Memos;
using UABS.Services;
using UABS.ViewModels;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Memos;

namespace UABS.UnityAssets.Tests;

public sealed class V2MemoPerformanceTests
{
    private sealed class Traces : IDisposable
    {
        private readonly Activity _root = new Activity("test").Start();
        private readonly ActivityListener _listener;
        public ConcurrentQueue<string> Names { get; } = new();
        public Action<string>? Starting;
        public Traces()
        {
            _listener = new() { ShouldListenTo = s => s.Name == "UABS.Memos",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = a => { if (a.TraceId == _root.TraceId) Names.Enqueue(a.OperationName); },
                ActivityStarted = a => { if (a.TraceId == _root.TraceId) Starting?.Invoke(a.OperationName); } };
            ActivitySource.AddActivityListener(_listener);
        }
        public int Validations => Names.Count(n => n == "full-validation");
        public void Dispose() { _listener.Dispose(); _root.Dispose(); }
    }
    private static void Edit(string path, string sql)
    {
        using var c = V2SearchTests.Connect(path); c.CreateFunction<string?, int>("uabs_has_text_v1", s => string.IsNullOrWhiteSpace(s) ? 0 : 1);
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery();
    }
    [Fact]
    public async Task PinnedAdmission_ReusesExactReads_AndInvalidatesOnExternalCommits()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        using var reader = (await PackageReader.OpenAsync(path, memoReuse: true)).Reader!;
        var row = (await reader.ReadAssetsAsync())[0]; var target = MemoTarget.From(row)!;
        using var trace = new Traces();
        Assert.Null((await reader.ReadMemoAsync(target, refreshAdmission: true)).Memo);
        Assert.Equal(OperatingSystem.IsWindows() ? 0 : 1, trace.Validations);
        Edit(path, "UPDATE asset SET memo='新 memo' WHERE path_id=-77"); trace.Names.Clear();
        var external = await reader.ReadMemoAsync(target, refreshAdmission: true);
        Assert.Equal("新 memo", external.Memo); Assert.Equal(1, external.MemoRevision); Assert.Equal(1, trace.Validations);
        trace.Names.Clear(); Assert.Equal(external, await reader.ReadMemoAsync(target, refreshAdmission: true));
        Assert.Equal(OperatingSystem.IsWindows() ? 0 : 1, trace.Validations);
        Edit(path, "UPDATE asset SET memo='' WHERE path_id=-77"); Assert.Equal("", (await reader.ReadMemoAsync(target, refreshAdmission: true)).Memo);
        Edit(path, "UPDATE asset SET memo=NULL WHERE path_id=-77"); Assert.Null((await reader.ReadMemoAsync(target, refreshAdmission: true)).Memo);
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadMemoAsync(target with { SourceId = Guid.NewGuid() }, refreshAdmission: true));
    }
    [Theory]
    [InlineData("schema")]
    [InlineData("derived")]
    [InlineData("generation")]
    public async Task ChangedSchemaSemanticsOrGeneration_NeverUseStaleAdmission(string kind)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        using var reader = (await PackageReader.OpenAsync(path, memoReuse: true)).Reader!;
        var target = MemoTarget.From((await reader.ReadAssetsAsync())[0])!;
        await reader.ReadMemoAsync(target, refreshAdmission: true);
        Edit(path, kind == "schema" ? "CREATE INDEX unexpected ON asset(memo)" : kind == "derived"
            ? "UPDATE asset SET name_ascii_key='wrong' WHERE path_id=-77" : "UPDATE package SET generation_uuid=randomblob(16)");
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadMemoAsync(target, refreshAdmission: true));
        await Assert.ThrowsAsync<InvalidDataException>(() => reader.ReadMemoAsync(target, refreshAdmission: true));
    }
    [Fact]
    public async Task PinnedWindowsAdmissionBlocksReplacement_AndReleasesOnDispose()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        var reader = (await PackageReader.OpenAsync(path, memoReuse: true)).Reader!;
        try { if (OperatingSystem.IsWindows()) Assert.ThrowsAny<IOException>(() => File.Move(path, path + ".moved")); }
        finally { reader.Dispose(); }
        File.Move(path, path + ".moved"); Assert.True(File.Exists(path + ".moved"));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AttemptLocalReuse_StillFreshlyVerifies_AndExternalCommitsForceFullValidation(bool external)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        var writer = new V2MemoPersistenceService(new PackageWriteCoordinator(), new V2PackageEditorGuard(), boundary: b => {
            if (external && b == MemoBoundary.BeforeVerification) Edit(path, "UPDATE asset SET memo='other asset' WHERE path_id=5");
        });
        var registration = (await writer.RegisterAsync(path)).Registration!;
        using var reader = (await PackageReader.OpenAsync(path)).Reader!; var row = (await reader.ReadAssetsAsync())[0];
        using var trace = new Traces();
        var result = await writer.PersistAsync(new(registration, MemoTarget.From(row)!, "verified", row.Memo, row.MemoRevision));
        Assert.Equal(MemoStatus.Saved, result.Status); Assert.True(result.Change!.Verified);
        Assert.Contains("fresh-verification", trace.Names);
        Assert.Equal(!external && OperatingSystem.IsWindows() ? 1 : 2, trace.Validations);
        using var fresh = (await PackageReader.OpenAsync(path)).Reader!;
        Assert.Equal("verified", (await fresh.ReadMemoAsync(MemoTarget.From(row)!)).Memo);
    }
    [Fact]
    public async Task ExternalSemanticCorruptionAfterCommit_FailsVerificationRatherThanUsingReceipt()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); string path = corpus.Packages[0].Path;
        var writer = new V2MemoPersistenceService(new PackageWriteCoordinator(), new V2PackageEditorGuard(), boundary: b => {
            if (b == MemoBoundary.BeforeVerification) Edit(path, "UPDATE asset SET name_ascii_key='wrong' WHERE path_id=5");
        });
        var registration = (await writer.RegisterAsync(path)).Registration!;
        using var reader = (await PackageReader.OpenAsync(path)).Reader!; var row = (await reader.ReadAssetsAsync())[0];
        var result = await writer.PersistAsync(new(registration, MemoTarget.From(row)!, "committed", row.Memo, row.MemoRevision));
        Assert.Equal(MemoStatus.VerificationFailure, result.Status); Assert.Equal(MemoCommitState.Committed, result.CommitState); Assert.Null(result.Change);
    }
    private sealed class DelayedBasis : IPackageMemoPersistenceService
    {
        public List<(TaskCompletionSource<MemoPersistenceBasis> Ready, CancellationToken Token)> Loads { get; } = [];
        public Task<string?> CaptureRevisionAsync(string path, CancellationToken token = default) => throw new NotSupportedException();
        public Task<MemoPersistenceBasis> CaptureBasisAsync(PackageMemoIdentity identity, string? memo, CancellationToken token = default)
        { var ready = new TaskCompletionSource<MemoPersistenceBasis>(TaskCreationOptions.RunContinuationsAsynchronously); Loads.Add((ready, token)); return ready.Task.WaitAsync(token); }
        public Task<PackageMemoPersistenceResult> PersistAsync(PackageMemoPersistenceRequest request, CancellationToken token = default) => throw new NotSupportedException();
    }
    private sealed class NoDebounce : IMemoDraftDelay
    { public Task DelayAsync(TimeSpan delay, CancellationToken token) => Task.Delay(Timeout.InfiniteTimeSpan, token); }
    private static PackageMemoIdentity Identity() => new("fixture.uabs.sqlite", "", "", "", -77,
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, -77));
    private static PackageMemoResolution Resolution(PackageMemoIdentity identity, string? memo) => new(PackageMemoResolutionStatus.Available,
        identity, "Fixture", identity.PackagePath, memo, PackageMemoStorageState.Value, "exact");
    [AvaloniaFact]
    public async Task PresentationIsImmediate_ButRevisionAuthorizesEditing_AndStaleLoadIsCancelled()
    {
        var persistence = new DelayedBasis(); using var drafts = new MemoDraftCoordinator(persistence, new NoDebounce());
        using var inspector = new EditableMemoInspectorViewModel(drafts, new PackageEditorSessionGuard());
        var view = new UABS.Page.EditableMemoInspectorView { DataContext = inspector };
        var window = new Avalonia.Controls.Window { Content = view }; window.Show();
        try
        {
        var one = Identity(); var two = Identity();
        var first = inspector.ShowAsync(Resolution(one, "first visible")); Assert.Equal("first visible", inspector.MemoText); Assert.False(inspector.IsEditable);
        view.UpdateLayout();
        var input = view.FindControl<Avalonia.Controls.TextBox>("MemoEditor")!;
        Assert.Equal("first visible", input.Text); Assert.False(input.IsEffectivelyEnabled);
        var second = inspector.ShowAsync(Resolution(two, "second visible")); Assert.Equal("second visible", inspector.MemoText);
        Assert.True(persistence.Loads[0].Token.IsCancellationRequested);
        persistence.Loads[1].Ready.SetResult(new("authoritative second", "v2:0:0")); await second;
        persistence.Loads[0].Ready.SetResult(new("stale first", "v2:0:0")); await first;
        Assert.True(inspector.IsEditable); Assert.True(PackageMemoIdentityComparer.Same(two, inspector.Identity)); Assert.Equal("authoritative second", inspector.MemoText);
        inspector.MemoText = "retained draft";
        var refresh = inspector.ShowAsync(Resolution(two, "older catalog")); Assert.Equal("retained draft", inspector.MemoText); Assert.False(inspector.IsEditable);
        persistence.Loads[2].Ready.SetResult(new("authoritative second", "v2:0:0")); await refresh;
        Assert.Equal("retained draft", inspector.MemoText); Assert.Equal(MemoDraftSaveState.Unsaved, inspector.SaveState);
        view.UpdateLayout(); Assert.Equal("retained draft", input.Text); Assert.True(input.IsEffectivelyEnabled);
        }
        finally { window.Close(); }
    }
    [Fact]
    public async Task UnloadDuringTargetLookupCancelsPublication()
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(); using var catalog = new V2BrowseCatalog();
        var entry = await catalog.RegisterAsync(corpus.Packages[0].Path);
        using var reader = (await PackageReader.OpenAsync(entry.Path)).Reader!; var row = (await reader.ReadAssetsAsync())[0];
        var service = new InspectorMemoService(new DelayedBasis(), new PackageMemoResolver(), new PackageWriteCoordinator(), new PackageEditorSessionGuard(), () => [entry]);
        var resolution = service.ResolveIndexed(entry, row.Key.PackageId, row.Key.GenerationId, row.Key.SourceId, row.Key.SourceRevision, row.Key.DirectoryOrdinal, row.Key.PathId, row.IdentityStatus, row.Memo);
        using var trace = new Traces(); using var release = new ManualResetEventSlim();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        trace.Starting = name => { if (name == "memo-query") { reached.TrySetResult(); if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException(); } };
        var load = service.CaptureBasisAsync(resolution.Identity!, null);
        try
        {
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(10)); var unload = catalog.UnloadAsync(entry); release.Set();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => load); await unload;
        }
        finally { release.Set(); }
    }
}
