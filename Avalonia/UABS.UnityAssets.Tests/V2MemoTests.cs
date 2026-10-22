using Microsoft.Data.Sqlite;
using UABS.PackageV2;
using UABS.PackageV2.Memos;
using UABS.PackageV2.Search;
using UABS.UnityAssets.Packages;
using UABS.UnityAssets.Packages.Search;

namespace UABS.UnityAssets.Tests;

public sealed class V2MemoTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly V2SearchTests.Corpus Corpus = new();
        internal readonly PackageWriteCoordinator Writes = new();
        internal readonly V2PackageEditorGuard Guard = new();
        internal V2MemoPersistenceService Service = null!;
        internal V2MemoRegistration Registration = null!;
        internal StoredAsset[] Assets = [];
        internal string Path => Corpus.Packages[0].Path;
        internal static async Task<Fixture> Create(Action<MemoBoundary>? hook = null, Func<VerifiedMemoChange, Task>? publish = null)
        {
            var f = new Fixture();
            try
            {
                await f.Corpus.Add(6);
                f.Service = new(f.Writes, f.Guard, publish, hook, busyTimeoutSeconds: 1);
                var admitted = await f.Service.RegisterAsync(f.Path); Assert.Null(admitted.Failure);
                f.Registration = admitted.Registration!; f.Assets = await Read(f.Path); return f;
            }
            catch { f.Dispose(); throw; }
        }
        internal MemoWriteRequest Request(int asset = 0, string? text = "new memo") =>
            new(Registration, MemoTarget.From(Assets[asset])!, text, Assets[asset].Memo, Assets[asset].MemoRevision);
        internal static async Task<StoredAsset[]> Read(string path)
        {
            var result = await PackageReader.OpenAsync(path); Assert.Equal(StorageStatus.Success, result.Status);
            using var reader = result.Reader!; return (await reader.ReadAssetsAsync()).ToArray();
        }
        public void Dispose() => Corpus.Dispose();
    }
    private static void Edit(string path, string sql)
    {
        using var c = V2SearchTests.Connect(path);
        c.CreateFunction<string?, int>("uabs_has_text_v1", text => string.IsNullOrWhiteSpace(text) ? 0 : 1);
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery();
    }
    private static async Task AssertMemo(string path, long id, string? memo, long revision)
    {
        var asset = Assert.Single(await Fixture.Read(path), a => a.Key.PathId == id);
        Assert.Equal(memo, asset.Memo); Assert.Equal(revision, asset.MemoRevision);
    }

    [Theory]
    [InlineData(0, "hello 中文 😀\u0000\r\n")]
    [InlineData(3, "")]
    [InlineData(1, null)]
    public async Task NullableUnicodeTransitions_OnlyOneTargetChanges(int index, string? memo)
    {
        using var f = await Fixture.Create(); var request = f.Request(index, memo);
        long size = new FileInfo(f.Path).Length;
        var saved = await f.Service.PersistAsync(request);
        Assert.Equal(MemoStatus.Saved, saved.Status); Assert.Equal(MemoCommitState.Committed, saved.CommitState);
        Assert.True(saved.Change!.Verified); Assert.Equal(request.ExpectedMemoRevision + 1, saved.Change.MemoRevision);
        Assert.Equal(f.Registration.Info.CatalogRevision + 1, saved.Change.CatalogRevision);
        var after = await Fixture.Read(f.Path);
        foreach (var original in f.Assets)
        {
            var current = Assert.Single(after, a => a.Key == original.Key);
            Assert.Equal(original == f.Assets[index] ? memo : original.Memo, current.Memo);
            Assert.Equal(original == f.Assets[index] ? original.MemoRevision + 1 : original.MemoRevision, current.MemoRevision);
            Assert.Equal(original.Name, current.Name); Assert.Equal(original.ClassId, current.ClassId);
        }
        Assert.Equal(size, new FileInfo(f.Path).Length); // small update; not a generation/file rewrite
        Assert.Equal(1234567, after[index].ClassId); // unknown classes and absent original source are writable
        Assert.False(File.Exists(System.IO.Path.Combine(f.Corpus.DirectoryPath, "source-0.bundle")));
    }

    [Fact]
    public async Task Unchanged_VerifiesAndPublishes_WithoutRevisionIncrement()
    {
        VerifiedMemoChange? delta = null;
        using var f = await Fixture.Create(publish: d => { delta = d; return Task.CompletedTask; });
        var request = f.Request(text: null); var result = await f.Service.PersistAsync(request);
        Assert.Equal(MemoStatus.Unchanged, result.Status); Assert.Equal(MemoCommitState.NotCommitted, result.CommitState);
        Assert.Equal(request.ExpectedMemoRevision, delta!.MemoRevision);
        Assert.Equal(f.Registration.Info.CatalogRevision, delta.CatalogRevision); Assert.Null(delta.Memo);
    }

    [Fact]
    public async Task InvalidUtf16_AndIncompleteTargets_NeverWrite()
    {
        using var f = await Fixture.Create(); var request = f.Request(text: "\ud800");
        Assert.Equal(MemoStatus.InvalidText, (await f.Service.PersistAsync(request)).Status);
        Assert.Equal(MemoStatus.AmbiguousIdentity, (await f.Service.PersistAsync(f.Request() with {
            Target = request.Target with { SourceId = Guid.Empty } })).Status);
        await AssertMemo(f.Path, -77, null, 0);
    }

    [Theory]
    [InlineData("memo-revision")]
    [InlineData("original")]
    [InlineData("catalog")]
    [InlineData("generation")]
    [InlineData("source-revision")]
    [InlineData("directory")]
    [InlineData("package")]
    public async Task ExactPreconditions_RejectStaleOrMissingTargets(string conflict)
    {
        using var f = await Fixture.Create(); var q = f.Request();
        q = conflict switch {
            "memo-revision" => q with { ExpectedMemoRevision = 99 },
            "original" => q with { ExpectedOriginalMemo = "" },
            "catalog" => q with { ExpectedCatalogRevision = 99 },
            "generation" => q with { Target = q.Target with { GenerationId = Guid.NewGuid() } },
            "package" => q with { Target = q.Target with { PackageId = Guid.NewGuid() } },
            "directory" => q with { Target = q.Target with { DirectoryOrdinal = 99 } },
            _ => q with { Target = q.Target with { SourceRevision = Guid.NewGuid() } }
        };
        var result = await f.Service.PersistAsync(q);
        Assert.Equal(conflict is "generation" or "package" ? MemoStatus.GenerationChanged
            : conflict is "source-revision" or "directory" ? MemoStatus.MissingAsset : MemoStatus.RevisionConflict, result.Status);
        Assert.Equal(MemoCommitState.NotCommitted, result.CommitState); await AssertMemo(f.Path, -77, null, 0);
    }

    [Fact]
    public async Task DuplicatePathsAcrossAllStatuses_AreRejected_NotChosenByRowId()
    {
        using var f = await Fixture.Create();
        Edit(f.Path, "UPDATE asset SET identity_status='ambiguous' WHERE table_ordinal IN (0,1); UPDATE asset SET path_id=-77 WHERE table_ordinal=1;");
        Assert.Equal(MemoStatus.AmbiguousIdentity, (await f.Service.PersistAsync(f.Request())).Status);
        Assert.Null(MemoTarget.From((await Fixture.Read(f.Path))[0]));
    }

    [Fact]
    public async Task IncompleteStatusWithPath_IsNotWritable()
    {
        using var f = await Fixture.Create(); Edit(f.Path, "UPDATE asset SET identity_status='incomplete' WHERE table_ordinal=0");
        Assert.Equal(MemoStatus.AmbiguousIdentity, (await f.Service.PersistAsync(f.Request())).Status);
    }

    [Fact]
    public async Task DuplicateNamesAndIdsAcrossPackages_UseImmutablePackageAndSourceIdentity()
    {
        using var f = await Fixture.Create(); await f.Corpus.Add(6);
        var other = (await f.Service.RegisterAsync(f.Corpus.Packages[1].Path)).Registration!;
        var row = (await Fixture.Read(other.Path))[0];
        Assert.Equal(MemoStatus.Saved, (await f.Service.PersistAsync(new(other, MemoTarget.From(row)!, "other", row.Memo, row.MemoRevision))).Status);
        await AssertMemo(f.Path, -77, null, 0); await AssertMemo(other.Path, -77, "other", 1);
        Assert.Equal(MemoStatus.Saved, (await f.Service.PersistAsync(f.Request())).Status);
    }

    [Fact]
    public async Task EditorLease_ExcludesCleanAndDirtyUntilExplicitRelease_AndBaseIsStrict()
    {
        using var f = await Fixture.Create(); var info = f.Registration.Info;
        var baseline = new EditorBaseRevision(info.PackageId, info.GenerationId, info.CatalogRevision);
        var opened = await f.Service.OpenEditorAsync(f.Registration, baseline); Assert.Null(opened.Failure);
        using var lease = opened.Session!;
        Assert.Equal(MemoStatus.EditorActive, (await f.Service.PersistAsync(f.Request())).Status);
        Assert.Equal(MemoStatus.EditorActive, (await f.Service.OpenEditorAsync(f.Registration, baseline)).Failure);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Equal(MemoStatus.CancelledBeforeCommit, (await f.Service.OpenEditorAsync(f.Registration, baseline, cancelled.Token)).Failure);
        Assert.True(f.Guard.IsOpen(info.PackageId)); // failed/cancelled operations don't release active editor
        // The lease deliberately has no clean/dirty switch or editor-clone reference.
        Assert.Equal(MemoStatus.EditorActive, (await f.Service.PersistAsync(f.Request())).Status);
        lease.Dispose(); lease.Dispose(); Assert.False(f.Guard.IsOpen(info.PackageId));
        Assert.Equal(MemoStatus.Saved, (await f.Service.PersistAsync(f.Request())).Status);
        Assert.Equal(MemoStatus.RevisionConflict, (await f.Service.OpenEditorAsync(f.Registration, baseline)).Failure);
        Assert.Equal(MemoStatus.GenerationChanged, baseline.Check(info with { GenerationId = Guid.NewGuid() }));
        Assert.Equal(MemoStatus.RevisionConflict, baseline.Check(info with { CatalogRevision = info.CatalogRevision + 1 }));
        Assert.Null(baseline.Check(info));
    }

    [Fact]
    public async Task SamePackageSerialized_ThroughPublication_OtherPackageRemainsIndependent()
    {
        var publishing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        using var f = await Fixture.Create(publish: async _ => { if (Interlocked.Increment(ref calls) == 1) { publishing.SetResult(); await release.Task; } });
        await f.Corpus.Add(6); var other = (await f.Service.RegisterAsync(f.Corpus.Packages[1].Path)).Registration!;
        var row = (await Fixture.Read(other.Path))[0];
        Task<MemoWriteResult> first = f.Service.PersistAsync(f.Request()); await publishing.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            Task<MemoWriteResult> stale = f.Service.PersistAsync(f.Request(text: "stale"));
            Assert.False(stale.IsCompleted);
            using var cancel = new CancellationTokenSource();
            var waiting = f.Service.PersistAsync(f.Request(1), cancel.Token); cancel.Cancel();
            Assert.Equal(MemoStatus.CancelledBeforeCommit, (await waiting).Status);
            var independent = await f.Service.PersistAsync(new(other, MemoTarget.From(row)!, "independent", row.Memo, row.MemoRevision))
                .WaitAsync(TimeSpan.FromSeconds(10)); Assert.Equal(MemoStatus.Saved, independent.Status);
            release.SetResult(); Assert.Equal(MemoStatus.Saved, (await first).Status);
            Assert.Equal(MemoStatus.RevisionConflict, (await stale).Status);
        }
        finally { release.TrySetResult(); await first; }
    }

    [Theory]
    [InlineData(MemoBoundary.BeforeTransaction)]
    [InlineData(MemoBoundary.BeforeCommit)]
    [InlineData(MemoBoundary.AfterCommit)]
    public async Task CancellationHonorsCommitBoundary(MemoBoundary boundary)
    {
        using var cancel = new CancellationTokenSource();
        using var f = await Fixture.Create(b => { if (b == boundary) cancel.Cancel(); });
        var result = await f.Service.PersistAsync(f.Request(), cancel.Token);
        bool committed = boundary == MemoBoundary.AfterCommit;
        Assert.Equal(committed ? MemoStatus.Saved : MemoStatus.CancelledBeforeCommit, result.Status);
        Assert.Equal(committed ? MemoCommitState.Committed : MemoCommitState.NotCommitted, result.CommitState);
        await AssertMemo(f.Path, -77, committed ? "new memo" : null, committed ? 1 : 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompetingExternalWriter_DifferentAssetAllowed_SameAssetConflicts(bool sameAsset)
    {
        Fixture? current = null;
        using var f = await Fixture.Create(b => { if (b == MemoBoundary.BeforeTransaction) Edit(current!.Path,
            $"UPDATE asset SET memo='external' WHERE table_ordinal={(sameAsset ? 0 : 1)}"); }); current = f;
        var saved = await f.Service.PersistAsync(f.Request());
        Assert.Equal(sameAsset ? MemoStatus.RevisionConflict : MemoStatus.Saved, saved.Status);
        await AssertMemo(f.Path, -77, sameAsset ? "external" : "new memo", 1);
        if (!sameAsset) Assert.Equal(f.Registration.Info.CatalogRevision + 2, saved.Change!.CatalogRevision);
    }

    [Fact]
    public async Task BusyExternalTransaction_IsStructured_AndLeavesMemoUnchanged()
    {
        using var f = await Fixture.Create(); using var external = V2SearchTests.Connect(f.Path);
        using var locked = external.BeginTransaction(deferred: false);
        var result = await f.Service.PersistAsync(f.Request()); Assert.Equal(MemoStatus.Busy, result.Status);
        Assert.Equal(MemoCommitState.NotCommitted, result.CommitState); locked.Rollback();
        await AssertMemo(f.Path, -77, null, 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExternalWriteAfterCommit_SupersessionIsNotRollback(bool sameAsset)
    {
        Fixture? current = null;
        using var f = await Fixture.Create(b => { if (b == MemoBoundary.BeforeVerification) Edit(current!.Path,
            $"UPDATE asset SET memo='external' WHERE table_ordinal={(sameAsset ? 0 : 1)}"); }); current = f;
        var result = await f.Service.PersistAsync(f.Request());
        Assert.Equal(sameAsset ? MemoStatus.CommittedButSuperseded : MemoStatus.Saved, result.Status);
        Assert.Equal(MemoCommitState.Committed, result.CommitState);
        if (sameAsset) Assert.Null(result.Change); else Assert.Equal(f.Registration.Info.CatalogRevision + 2, result.Change!.CatalogRevision);
    }

    [Theory]
    [InlineData(MemoBoundary.BeforeCommit, MemoStatus.WriteFailure, MemoCommitState.NotCommitted)]
    [InlineData(MemoBoundary.BeforeVerification, MemoStatus.VerificationFailure, MemoCommitState.Committed)]
    public async Task InjectedFailure_RollbackVersusPostCommitFailure(MemoBoundary point, MemoStatus status, MemoCommitState state)
    {
        using var f = await Fixture.Create(b => { if (b == point) throw new IOException("injected failure"); });
        var result = await f.Service.PersistAsync(f.Request()); Assert.Equal(status, result.Status); Assert.Equal(state, result.CommitState);
        await AssertMemo(f.Path, -77, state == MemoCommitState.Committed ? "new memo" : null, state == MemoCommitState.Committed ? 1 : 0);
    }

    [Fact]
    public async Task PublicationFailure_RetainsVerifiedDiskChangeAndDraft()
    {
        using var f = await Fixture.Create(publish: _ => throw new IOException("catalog unavailable"));
        var attempt = new MemoSaveAttempt(4, f.Request()); var result = await new V2MemoDraftAdapter(f.Service).SaveAsync(attempt);
        Assert.Equal(MemoStatus.PublicationFailure, result.Status); Assert.Equal(MemoCommitState.Committed, result.CommitState);
        Assert.True(result.Change!.Verified);
        var draft = V2MemoDraftAdapter.Complete(attempt, 4, "new memo", result);
        Assert.False(draft.MayMarkSaved); Assert.Equal("new memo", draft.RetainedDraft);
        await AssertMemo(f.Path, -77, "new memo", 1);
    }

    [Fact]
    public async Task DraftCompletion_DoesNotClearNewerEditsOrDifferentTargets()
    {
        using var f = await Fixture.Create(); var attempt = new MemoSaveAttempt(10, f.Request());
        var result = await new V2MemoDraftAdapter(f.Service).SaveAsync(attempt);
        Assert.True(V2MemoDraftAdapter.Complete(attempt, 10, "new memo", result).MayMarkSaved);
        var newer = V2MemoDraftAdapter.Complete(attempt, 11, "later draft", result);
        Assert.False(newer.MayMarkSaved); Assert.Equal("later draft", newer.RetainedDraft);
        Assert.False(V2MemoDraftAdapter.Complete(attempt with { Request = f.Request(1) }, 10, "new memo", result).MayMarkSaved);
    }

    [Fact]
    public async Task VerifiedPublication_ExactIdentity_AndSearchCursorInvalidation()
    {
        VerifiedMemoChange? published = null;
        using var f = await Fixture.Create(publish: d => { published = d; return Task.CompletedTask; });
        var search = new V2SearchService(); var registration = (await search.RegisterAsync(f.Path)).Registration!;
        var created = search.CreateSession(new(PathId: -77, Fields: PackageSearchFields.None, ResultKinds: PackageSearchResultKinds.Asset), [registration.RegistrationId]);
        using var session = created.Session!;
        Assert.Equal(SearchStatus.Success, (await search.ReadPageAsync(session)).Status);
        var result = await f.Service.PersistAsync(f.Request()); Assert.Equal(result.Change, published);
        Assert.Equal(f.Request().Target, published!.Target); Assert.True(published.Verified);
        Assert.Equal(SearchStatus.StaleCursor, (await search.ReadPageAsync(session)).Status);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("generation")]
    [InlineData("schema")]
    [InlineData("version")]
    [InlineData("profile")]
    [InlineData("source")]
    [InlineData("journal")]
    public async Task MissingReplacementAndInvalidPackage_AreNeverSilentlyRetargeted(string change)
    {
        using var f = await Fixture.Create();
        switch (change)
        {
            case "missing": File.Move(f.Path, f.Path + ".moved"); break;
            case "generation": Edit(f.Path, "UPDATE package SET generation_uuid=randomblob(16)"); break;
            case "schema": Edit(f.Path, "DROP TRIGGER asset_memo_changed"); break;
            case "version": Edit(f.Path, "PRAGMA user_version=99999"); break;
            case "profile": Edit(f.Path, "UPDATE package SET derived_text_profile='unknown'"); break;
            case "source": Edit(f.Path, "UPDATE source SET source_revision_uuid=randomblob(16)"); break;
            case "journal": Edit(f.Path, "PRAGMA journal_mode=WAL"); break;
        }
        var result = await f.Service.PersistAsync(f.Request());
        Assert.Equal(change == "missing" ? MemoStatus.PackageMissing : change == "generation" ? MemoStatus.GenerationChanged
            : change == "source" ? MemoStatus.MissingAsset : MemoStatus.InvalidPackage, result.Status);
        Assert.Equal(MemoCommitState.NotCommitted, result.CommitState);
    }

    [Fact]
    public async Task UnloadAndMove_NeverRetargetOldRequests_AndAliasAdmissionIsRejected()
    {
        using var f = await Fixture.Create(); string alias = f.Path + ".copy"; File.Copy(f.Path, alias);
        Assert.Equal(MemoStatus.RegistrationChanged, (await f.Service.RegisterAsync(alias)).Failure);
        await f.Service.UnloadAsync(f.Registration);
        Assert.Equal(MemoStatus.RegistrationChanged, (await f.Service.PersistAsync(f.Request())).Status);
        var moved = (await f.Service.RegisterAsync(alias)).Registration!;
        Assert.NotEqual(moved.Id, f.Registration.Id);
        Assert.Equal(MemoStatus.MissingAsset, (await f.Service.PersistAsync(f.Request() with {
            Registration = moved, Target = f.Request().Target with { SourceRevision = Guid.NewGuid() } })).Status);
        Assert.Equal(MemoStatus.RegistrationChanged, (await f.Service.PersistAsync(f.Request())).Status);
    }

    [Fact]
    public async Task ReadOnlyFile_ProducesNoSuccessOrMutation()
    {
        using var f = await Fixture.Create(); File.SetAttributes(f.Path, File.GetAttributes(f.Path) | FileAttributes.ReadOnly);
        try
        {
            var result = await f.Service.PersistAsync(f.Request());
            Assert.Equal(MemoStatus.ReadOnly, result.Status); Assert.Equal(MemoCommitState.NotCommitted, result.CommitState);
        }
        finally { File.SetAttributes(f.Path, FileAttributes.Normal); }
        await AssertMemo(f.Path, -77, null, 0);
    }

    [Fact]
    public async Task EditorOnOnePackage_DoesNotBlockAnother_AndUnloadingDoesNotReleaseLease()
    {
        using var f = await Fixture.Create(); await f.Corpus.Add(6);
        var other = (await f.Service.RegisterAsync(f.Corpus.Packages[1].Path)).Registration!;
        var info = f.Registration.Info;
        using var editor = (await f.Service.OpenEditorAsync(f.Registration, new(info.PackageId, info.GenerationId, info.CatalogRevision))).Session!;
        var row = (await Fixture.Read(other.Path))[0];
        Assert.Equal(MemoStatus.Saved, (await f.Service.PersistAsync(new(other, MemoTarget.From(row)!, "other", row.Memo, row.MemoRevision))).Status);
        await f.Service.UnloadAsync(f.Registration);
        Assert.True(f.Guard.IsOpen(info.PackageId)); // do not discard an editor on unload
        var reopened = (await f.Service.RegisterAsync(f.Path)).Registration!;
        Assert.Equal(MemoStatus.EditorActive, (await f.Service.PersistAsync(f.Request() with { Registration = reopened })).Status);
        editor.Dispose();
        Assert.Equal(MemoStatus.Saved, (await f.Service.PersistAsync(f.Request() with { Registration = reopened })).Status);
    }

    [Fact]
    public async Task UnloadQueuedBeforeWrite_RefusesCapturedRegistrationAndKeepsDraft()
    {
        using var f = await Fixture.Create(); var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task holder = f.Writes.ExecutePackageWriteAsync(f.Path, async _ => { held.SetResult(); await release.Task; }); await held.Task;
        try
        {
            var unload = f.Service.UnloadAsync(f.Registration);
            Assert.False(unload.IsCompleted); release.SetResult(); await holder; await unload;
            var attempt = new MemoSaveAttempt(2, f.Request());
            var result = await new V2MemoDraftAdapter(f.Service).SaveAsync(attempt);
            Assert.Equal(MemoStatus.RegistrationChanged, result.Status);
            var completion = V2MemoDraftAdapter.Complete(attempt, 2, "new memo", result);
            Assert.False(completion.MayMarkSaved); Assert.Equal("new memo", completion.RetainedDraft);
        }
        finally { release.TrySetResult(); await holder; }
    }

    [Fact]
    public async Task RemovedAssetAndConflictedDraft_KeepLastConfirmedDiskValue()
    {
        using var f = await Fixture.Create();
        var attempt = new MemoSaveAttempt(1, f.Request());
        Edit(f.Path, "UPDATE asset SET memo='newer external memo' WHERE table_ordinal=0");
        var result = await new V2MemoDraftAdapter(f.Service).SaveAsync(attempt);
        Assert.Equal(MemoStatus.RevisionConflict, result.Status);
        var completion = V2MemoDraftAdapter.Complete(attempt, 1, "new memo", result);
        Assert.False(completion.MayMarkSaved); Assert.Equal("new memo", completion.RetainedDraft);
        Edit(f.Path, "DELETE FROM asset WHERE table_ordinal=5; UPDATE logical_file SET declared_asset_count=5;");
        Assert.Equal(MemoStatus.MissingAsset, (await f.Service.PersistAsync(f.Request(5))).Status);
        await AssertMemo(f.Path, -77, "newer external memo", 1);
    }

    [Fact]
    public async Task TwoIndependentServicesShareCoordinator_PerAssetPreconditionsAllowUnrelatedWrites()
    {
        using var f = await Fixture.Create(); var otherService = new V2MemoPersistenceService(f.Writes, f.Guard);
        var other = (await otherService.RegisterAsync(f.Path)).Registration!;
        Assert.Equal(MemoStatus.RegistrationChanged, (await otherService.PersistAsync(f.Request())).Status); // ownership is checked
        var first = f.Service.PersistAsync(f.Request());
        var second = otherService.PersistAsync(f.Request(1) with { Registration = other });
        var results = await Task.WhenAll(first, second); Assert.All(results, r => Assert.Equal(MemoStatus.Saved, r.Status));
        var latest = await Fixture.Read(f.Path); Assert.Equal("new memo", latest[0].Memo); Assert.Equal("new memo", latest[1].Memo);
    }

    [Fact]
    public async Task GenerationChangesAfterCommit_AreNotReportedAsRollbackOrPublished()
    {
        Fixture? current = null; bool published = false;
        using var f = await Fixture.Create(b => { if (b == MemoBoundary.BeforeVerification)
            Edit(current!.Path, "UPDATE package SET generation_uuid=randomblob(16)"); },
            _ => { published = true; return Task.CompletedTask; }); current = f;
        var result = await f.Service.PersistAsync(f.Request());
        Assert.Equal(MemoStatus.GenerationChanged, result.Status); Assert.Equal(MemoCommitState.Committed, result.CommitState);
        Assert.False(published); Assert.Null(result.Change);
    }

    [Fact]
    public async Task ReplacedLocationRequiresExplicitUnloadBeforeNewAdmission()
    {
        using var f = await Fixture.Create(); Edit(f.Path, "UPDATE package SET package_uuid=randomblob(16),generation_uuid=randomblob(16)");
        Assert.Equal(MemoStatus.RegistrationChanged, (await f.Service.RegisterAsync(f.Path)).Failure);
        Assert.Equal(MemoStatus.GenerationChanged, (await f.Service.PersistAsync(f.Request())).Status);
        await f.Service.UnloadAsync(f.Registration);
        Assert.Null((await f.Service.RegisterAsync(f.Path)).Failure);
    }

}

// Opt-in diagnostics, not tests or performance acceptance assertions. Owned synthetic packages only.
public static class V2MemoMeasurements
{
    public static async Task MeasureAsync(int assets)
    {
        using var corpus = new V2SearchTests.Corpus(); await corpus.Add(assets);
        var service = new V2MemoPersistenceService(new PackageWriteCoordinator(), new V2PackageEditorGuard());
        var registration = (await service.RegisterAsync(corpus.Packages[0].Path)).Registration!;
        var admitted = await PackageReader.OpenAsync(registration.Path);
        StoredAsset asset;
        using (var reader = admitted.Reader!) asset = (await reader.ReadAssetsAsync())[0];
        var target = MemoTarget.From(asset)!;
        byte[] before = File.ReadAllBytes(registration.Path); List<double> elapsed = [];
        for (int i = 0; i < 5; i++)
        {
            var request = new MemoWriteRequest(registration, target, "measured memo " + i, asset.Memo, asset.MemoRevision);
            var timer = System.Diagnostics.Stopwatch.StartNew(); var result = await service.PersistAsync(request); elapsed.Add(timer.Elapsed.TotalMilliseconds);
            Assert.Equal(MemoStatus.Saved, result.Status);
            asset = asset with { Memo = result.Change!.Memo, MemoRevision = result.Change.MemoRevision };
        }
        byte[] after = File.ReadAllBytes(registration.Path);
        int changedBlocks = Enumerable.Range(0, (after.Length + 4095) / 4096).Count(i =>
            i * 4096 >= before.Length || !before.AsSpan(i * 4096, Math.Min(4096, before.Length - i * 4096))
                .SequenceEqual(after.AsSpan(i * 4096, Math.Min(4096, after.Length - i * 4096))));
        elapsed.Sort();
        Console.WriteLine($"MEMO_MEASURE assets={assets} five_save_median_ms={elapsed[2]:F2} observed_min_ms={elapsed[0]:F2} observed_max_ms={elapsed[^1]:F2} before_bytes={before.Length} after_bytes={after.Length} changed_4k_blocks={changedBlocks} (logical_file_diff_not_physical_IO)");
    }
}
