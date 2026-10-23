using System.Collections.Concurrent;
using System.Diagnostics;
using UABS.PackageV2;
using UABS.PackageV2.Memos;
using UABS.UnityAssets.Packages;

namespace UABS.UnityAssets.Tests;

public sealed class V2MemoWriteAdmissionTests
{
    private sealed class Traces : IDisposable
    {
        private readonly Activity _root = new Activity("write-admission-test").Start();
        private readonly ActivityListener _listener;
        internal ConcurrentQueue<string> Names { get; } = new();
        internal int Full => Names.Count(n => n == "full-validation");
        internal Traces()
        {
            _listener = new() { ShouldListenTo = s => s.Name == "UABS.Memos",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = a => { if (a.TraceId == _root.TraceId) Names.Enqueue(a.OperationName); } };
            ActivitySource.AddActivityListener(_listener);
        }
        public void Dispose() { _listener.Dispose(); _root.Dispose(); }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        internal V2SearchTests.Corpus Corpus = new();
        internal PackageWriteCoordinator Writes = new();
        internal V2PackageEditorGuard Editors = new();
        internal V2MemoPersistenceService Writer = null!;
        internal V2MemoRegistration Registration = null!;
        internal StoredAsset[] Rows = [];
        internal string Path => Corpus.Packages[0].Path;
        internal static async Task<Fixture> Open(Action<MemoBoundary>? hook = null, Func<VerifiedMemoChange, Task>? publish = null)
        {
            var f = new Fixture();
            try
            {
                await f.Corpus.Add(20); f.Writer = new(f.Writes, f.Editors, publish, hook, reuseAdmission: true);
                f.Registration = (await f.Writer.RegisterAsync(f.Path)).Registration!;
                using var reader = (await PackageReader.OpenAsync(f.Path)).Reader!;
                f.Rows = (await reader.ReadAssetsAsync()).ToArray(); return f;
            }
            catch { await f.DisposeAsync(); throw; }
        }
        internal MemoWriteRequest Request(int row = 0, string? memo = "saved") => new(Registration, MemoTarget.From(Rows[row])!, memo, Rows[row].Memo, Rows[row].MemoRevision);
        public async ValueTask DisposeAsync() { if (Registration is not null) await Writer.UnloadAsync(Registration); Corpus.Dispose(); }
    }
    private static void Edit(string path, string text)
    {
        using var c = V2SearchTests.Connect(path); c.CreateFunction<string?, int>("uabs_has_text_v1", s => string.IsNullOrWhiteSpace(s) ? 0 : 1);
        using var cmd = c.CreateCommand(); cmd.CommandText = text; cmd.ExecuteNonQuery();
    }
    [Fact]
    public async Task FirstAndRepeatedSaves_ReuseAdmission_ButAlwaysVerifyFreshly()
    {
        await using var f = await Fixture.Open(); using var trace = new Traces();
        var first = await f.Writer.PersistAsync(f.Request()); Assert.True(first.Succeeded, first.Message);
        Assert.True((await f.Writer.PersistAsync(f.Request() with { DesiredMemo = "second", ExpectedOriginalMemo = "saved", ExpectedMemoRevision = 1 })).Succeeded);
        Assert.True((await f.Writer.PersistAsync(f.Request(1, "different asset"))).Succeeded);
        Assert.Equal(OperatingSystem.IsWindows() ? 0 : 6, trace.Full);
        Assert.Equal(3, trace.Names.Count(n => n == "fresh-verification"));
        Assert.Equal(MemoStatus.RevisionConflict, (await f.Writer.PersistAsync(f.Request())).Status);
    }
    [Theory]
    [InlineData("memo")]
    [InlineData("catalog")]
    [InlineData("derived")]
    [InlineData("schema")]
    [InlineData("generation")]
    public async Task ExternalCommit_InvalidatesWriterAdmission_AndCannotAuthorizeCorruptOrStaleWrites(string kind)
    {
        await using var f = await Fixture.Open();
        Edit(f.Path, kind switch { "memo" => "UPDATE asset SET memo='external' WHERE path_id=-77",
            "catalog" => "UPDATE asset SET memo='external other' WHERE path_id=5",
            "derived" => "UPDATE asset SET name_ascii_key='wrong' WHERE path_id=5",
            "schema" => "CREATE INDEX unexpected ON asset(memo)", _ => "UPDATE package SET generation_uuid=randomblob(16)" });
        using var trace = new Traces(); var result = await f.Writer.PersistAsync(f.Request());
        Assert.True(trace.Full > 0 || kind == "schema");
        Assert.Equal(kind switch { "memo" => MemoStatus.RevisionConflict, "catalog" => MemoStatus.Saved,
            "generation" => MemoStatus.GenerationChanged, _ => MemoStatus.InvalidPackage }, result.Status);
        if (kind is "derived" or "schema") Assert.False((await f.Writer.PersistAsync(f.Request())).Succeeded);
        if (kind == "catalog")
        {
            trace.Names.Clear(); Assert.True((await f.Writer.PersistAsync(f.Request(2))).Succeeded);
            Assert.Equal(OperatingSystem.IsWindows() ? 0 : 2, trace.Full);
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndependentWriterConnections_CompeteUsingFreshMemoPreconditions(bool sameAsset)
    {
        await using var f = await Fixture.Open();
        var other = new V2MemoPersistenceService(f.Writes, f.Editors, reuseAdmission: true);
        var registration = (await other.RegisterAsync(f.Path)).Registration!;
        try
        {
            var results = await Task.WhenAll(f.Writer.PersistAsync(f.Request()),
                other.PersistAsync(f.Request(sameAsset ? 0 : 1, "other") with { Registration = registration }));
            Assert.Equal(sameAsset ? 1 : 2, results.Count(r => r.Succeeded));
            if (sameAsset) Assert.Contains(results, r => r.Status == MemoStatus.RevisionConflict);
        }
        finally { await other.UnloadAsync(registration); }
    }
    [Theory]
    [InlineData(MemoBoundary.BeforeCommit)]
    [InlineData(MemoBoundary.AfterCommit)]
    public async Task CachedWriter_PreservesCancellationBoundary(MemoBoundary boundary)
    {
        using var cancel = new CancellationTokenSource();
        await using var f = await Fixture.Open(hook: b => { if (b == boundary) cancel.Cancel(); });
        var result = await f.Writer.PersistAsync(f.Request(), cancel.Token);
        Assert.Equal(boundary == MemoBoundary.AfterCommit ? MemoStatus.Saved : MemoStatus.CancelledBeforeCommit, result.Status);
        Assert.Equal(boundary == MemoBoundary.AfterCommit ? MemoCommitState.Committed : MemoCommitState.NotCommitted, result.CommitState);
    }
    [Fact]
    public async Task UncertainTransaction_DiscardsCachedConnection_AndDoesNotPublishSuccess()
    {
        bool inject = true;
        await using var f = await Fixture.Open(hook: b => { if (inject && b is MemoBoundary.CommitStarting or MemoBoundary.BeforeRollback) throw new IOException("modeled commit/rollback transport failure"); });
        var failed = await f.Writer.PersistAsync(f.Request()); Assert.False(failed.Succeeded);
        Assert.Equal(MemoCommitState.Uncertain, failed.CommitState); Assert.Null(failed.Change);
        inject = false;
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(MemoStatus.RegistrationChanged, (await f.Writer.PersistAsync(f.Request())).Status);
            var reopened = (await f.Writer.RegisterAsync(f.Path)).Registration!;
            try { Assert.True((await f.Writer.PersistAsync(f.Request() with { Registration = reopened })).Succeeded); }
            finally { await f.Writer.UnloadAsync(reopened); }
        }
        else Assert.True((await f.Writer.PersistAsync(f.Request())).Succeeded);
    }
    [Fact]
    public async Task PublicationCapability_Expires_AndExternalCorruptionCannotReuseIt()
    {
        VerifiedMemoChange? published = null; string path = "";
        await using var f = await Fixture.Open(publish: async change => {
            published = change; using var reader = (await PackageReader.OpenMemoPublicationAsync(path, change)).Reader!;
            Assert.Equal(change.Memo, (await reader.ReadMemoAsync(change.Target)).Memo);
        }); path = f.Path;
        Assert.True((await f.Writer.PersistAsync(f.Request())).Succeeded);
        Edit(path, "UPDATE asset SET name_ascii_key='wrong' WHERE path_id=5");
        Assert.Null((await PackageReader.OpenMemoPublicationAsync(path, published!)).Reader);
        Assert.Null((await PackageReader.OpenMemoPublicationAsync(path, published! with { Verified = true })).Reader);
    }
    [Fact]
    public async Task ExternalCommitBetweenVerificationAndPublication_ForcesFullAdmission()
    {
        string path = "";
        await using var f = await Fixture.Open(publish: async change => {
            Edit(path, "UPDATE asset SET name_ascii_key='bad' WHERE path_id=5");
            var opened = await PackageReader.OpenMemoPublicationAsync(path, change);
            opened.Reader?.Dispose(); if (opened.Reader is null) throw new InvalidDataException(opened.Message);
        }); path = f.Path;
        var result = await f.Writer.PersistAsync(f.Request());
        Assert.Equal(MemoStatus.PublicationFailure, result.Status); Assert.Equal(MemoCommitState.Committed, result.CommitState);
    }
    [Fact]
    public async Task CleanEditorExclusion_AndExplicitUnload_ReleaseAdmissionResources()
    {
        await using var f = await Fixture.Open();
        using (var editor = (await f.Writer.OpenEditorAsync(f.Registration, new(f.Registration.Info.PackageId, f.Registration.Info.GenerationId, f.Registration.Info.CatalogRevision))).Session!)
            Assert.Equal(MemoStatus.EditorActive, (await f.Writer.PersistAsync(f.Request())).Status);
        Assert.True((await f.Writer.PersistAsync(f.Request())).Succeeded);
        if (OperatingSystem.IsWindows()) Assert.ThrowsAny<IOException>(() => File.Move(f.Path, f.Path + ".moved"));
        await f.Writer.UnloadAsync(f.Registration); File.Move(f.Path, f.Path + ".moved");
        Assert.Equal(MemoStatus.RegistrationChanged, (await f.Writer.PersistAsync(f.Request())).Status);
        File.Move(f.Path + ".moved", f.Path);
        var reopened = (await f.Writer.RegisterAsync(f.Path)).Registration!;
        try { Assert.NotEqual(f.Registration.Id, reopened.Id); Assert.Equal(MemoStatus.RegistrationChanged, (await f.Writer.PersistAsync(f.Request())).Status); }
        finally { await f.Writer.UnloadAsync(reopened); }
    }
    [Theory]
    [InlineData("missing")]
    [InlineData("ambiguous")]
    [InlineData("original")]
    [InlineData("revision")]
    public async Task CachedAdmission_NeverReplacesExactTargetAuthorization(string kind)
    {
        await using var f = await Fixture.Open(); var request = f.Request();
        if (kind == "ambiguous") Edit(f.Path, "UPDATE asset SET identity_status='ambiguous' WHERE path_id=-77");
        request = kind switch { "missing" => request with { Target = request.Target with { SourceRevision = Guid.NewGuid() } },
            "original" => request with { ExpectedOriginalMemo = "" }, "revision" => request with { ExpectedMemoRevision = 1 }, _ => request };
        var result = await f.Writer.PersistAsync(request);
        Assert.Equal(kind switch { "missing" => MemoStatus.MissingAsset, "ambiguous" => MemoStatus.AmbiguousIdentity, _ => MemoStatus.RevisionConflict }, result.Status);
        Assert.Equal(MemoCommitState.NotCommitted, result.CommitState);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unicode 钩爪 😀\u0000")]
    public async Task WarmWriter_PreservesNullableAndUnicodeValues(string? text)
    {
        await using var f = await Fixture.Open(); var saved = await f.Writer.PersistAsync(f.Request(memo: text));
        Assert.True(saved.Succeeded, saved.Message); Assert.Equal(text, saved.Change!.Memo);
        using var reader = (await PackageReader.OpenAsync(f.Path)).Reader!;
        Assert.Equal(text, (await reader.ReadMemoAsync(f.Request().Target)).Memo);
        Assert.Equal(MemoStatus.Unchanged, (await f.Writer.PersistAsync(f.Request(memo: text) with {
            ExpectedOriginalMemo = text, ExpectedMemoRevision = saved.Change.MemoRevision })).Status);
    }
    [Fact]
    public async Task PublicationReader_BlocksStalePagingEvenAfterMemoRefresh()
    {
        PackageReader? publishedReader = null; string path = "";
        await using var f = await Fixture.Open(publish: async change => {
            publishedReader = (await PackageReader.OpenMemoPublicationAsync(path, change)).Reader!;
        }); path = f.Path;
        try
        {
            Assert.True((await f.Writer.PersistAsync(f.Request())).Succeeded);
            await publishedReader!.CheckAdmissionAsync();
            Edit(path, "UPDATE source SET original_name='externally changed' WHERE source_id=1"); // no catalog trigger
            await publishedReader.ReadMemoAsync(f.Request().Target, refreshAdmission: true);
            await Assert.ThrowsAsync<InvalidDataException>(() => publishedReader.CheckAdmissionAsync());
        }
        finally { publishedReader?.Dispose(); }
    }
    [Fact]
    public async Task QueuedUnload_DoesNotRetargetAnOldWriterRequest()
    {
        await using var f = await Fixture.Open();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = f.Writes.ExecutePackageWriteAsync(f.Path, async _ => { reached.SetResult(); await release.Task; });
        await reached.Task;
        var unload = f.Writer.UnloadAsync(f.Registration); var save = f.Writer.PersistAsync(f.Request());
        release.SetResult(); await held; await unload;
        Assert.Equal(MemoStatus.RegistrationChanged, (await save).Status);
    }
    [Fact]
    public async Task RetainedWriter_StillHonorsReadOnlyFileFlag()
    {
        await using var f = await Fixture.Open(); var attributes = File.GetAttributes(f.Path);
        try { File.SetAttributes(f.Path, attributes | FileAttributes.ReadOnly); Assert.Equal(MemoStatus.ReadOnly, (await f.Writer.PersistAsync(f.Request())).Status); }
        finally { File.SetAttributes(f.Path, attributes); }
    }
    [Fact]
    public async Task RetainedWriter_RejectsExternallyChangedJournalMode()
    {
        await using var f = await Fixture.Open(); Edit(f.Path, "PRAGMA journal_mode=WAL");
        var result = await f.Writer.PersistAsync(f.Request()); Assert.False(result.Succeeded);
        Assert.Equal(MemoCommitState.NotCommitted, result.CommitState);
    }
    [Fact]
    public async Task ConcurrentPublicationReaders_ShareNoConnectionOperations_AndExpiredReceiptsReadmit()
    {
        string path = ""; VerifiedMemoChange? receipt = null;
        await using var f = await Fixture.Open(publish: async change => {
            receipt = change;
            var admitted = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => PackageReader.OpenMemoPublicationAsync(path, change)));
            try
            {
                foreach (var result in admitted)
                {
                    Assert.Equal(StorageStatus.Success, result.Status);
                    Assert.Equal(change.Memo, (await result.Reader!.ReadMemoAsync(change.Target)).Memo);
                }
            }
            finally { foreach (var result in admitted) result.Reader?.Dispose(); }
        }); path = f.Path;
        Assert.True((await f.Writer.PersistAsync(f.Request())).Succeeded);
        // The same published value outside its attempt is not authority to reuse admission.
        using var trace = new Traces(); using var later = (await PackageReader.OpenMemoPublicationAsync(path, receipt!)).Reader!;
        Assert.Equal(1, trace.Full); Assert.Equal("saved", (await later.ReadMemoAsync(receipt!.Target)).Memo);
    }
}
