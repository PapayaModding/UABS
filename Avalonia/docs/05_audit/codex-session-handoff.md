# Codex Session Handoff

**Updated:** 2026-10-20

**Current task:** Package V2 architecture approval and implementation-roadmap finalization (documentation only)

### V2 approved design checkpoint

- Preserved the existing two unstaged docs and untracked V2 specification at HEAD `3159778`; index was empty and remains untouched. Only those same three documentation files changed. No production/spike code, fixture, legacy Unity, original source or real package changes.
- **D1–D8 approved by the user, 2026-10-20.** The [V2 contract](../03_design/package-format-v2-design.md) now supersedes provisional audit/spike recommendations; approved DDL and identity keys are unchanged.
- D3: intentional same-package reindex preserves package UUID but creates a generation; committed memos transfer only across the same verified source identity/bytes/revision, original logical ordinal and unique Path ID. No changed/unverified-source transfer or name/CAB matching; protect pending/failed drafts and explicitly back up the old generation.
- D5: invalid revision vectors stop paging/count jobs; retain displayed rows/selection/scroll and synchronize all exact verified memo deltas, mark stale, and offer explicit **Refresh Search**. Never silently rerun, remove/reorder rows or append newer-revision pages.
- Section 6 defines correctness-equivalent Search/performance gates: 10k/100k/1M, short Chinese/mixed-language, many-package, first/late-page, broad-result/memory and build/size measurements. Repetition methods are specified; numeric budgets remain **pending calibration/approval**, not claimed measurements. Historical spike/audit results remain qualified baselines.
- Six separately reviewable gates cover extractor fixtures → standalone storage → exact bounded Search → memo/editor adapters → separately approved production integration → separately approved final retirement. **Next:** obtain an explicit request for gate 1, neutral extraction contracts and real multi-logical-file/duplicate-CAB fixture validation; do not start it automatically.
- V2 production is **not started**; V1 remains working and must be preserved until separately authorized/verified replacement. Global editor leases are not approved; Memo Stage 7 shutdown/crash-recovery safeguards remain deferred. Remaining risks: real identity/version/GUID fixtures, Unicode/global-sort and many-package budgets, native/RID/filesystem faults, concurrency/publication integration.
- Final checks pass: **3 documents, 48 local links/anchors**, balanced fences and 2 syntax-reviewed Mermaid blocks (no renderer); design SQL SHA-256 unchanged. Staged/unstaged and untracked-spec whitespace checks pass; only informational LF/CRLF notices. No conflict markers or non-documentation changes; index remains empty.
- Design SQL retains its prior 27-check validation; app builds/full suites were not rerun for this docs-only change. Production buildability is not newly verified. No desktop control, staging or commit.

### V2 design specification checkpoint (historical; approval superseded above)

- Started clean at `3159778`; prior spike/audit work is committed and preserved. Only docs are being edited; no production or spike code/schema, real package, Unity or fixture changes are authorized.
- Completed [package-format-v2-design.md](../03_design/package-format-v2-design.md): immutable package/generation/source/revision/directory-ordinal/Path-ID keys; provisional normalized STRICT DDL; ambiguous/incomplete row retention; exact .NET text semantics; optional derived caches; bounded keyset federation, revision invalidation and memo/editor/portability contracts.
- A disposable external Debug validator executes the actual fenced SQL with the existing spike provider/Store helper in memory. **27 checks pass** on native SQLite 3.53.3/.NET 9.0.5, including corrected pre-AFTER-trigger memo-clearing checks and RETURNING timing, FK/version constraints, ambiguity and nullable Unicode keysets. Scratch: `C:\Users\kolynlin\Documents\Codex\2026-10-16\files-pasted-by-the-user-i\package-v2-contract-validation`. No repository code/project or package schema was changed.
- D1–D8 remain provisional pending user review; V2 production remains unimplemented. Next action is review/approval, particularly revision-scoped logical ordinals, Unicode scan/cache performance, reindex memo carry-forward, stale pagination/count UX and row-versus-package conflict scope. Do not start production replacement or Memo Stage 7 automatically.
- Updated only the specification, docs index and this handoff. Documentation/whitespace/scope checks finalize the task; existing spike build/tests below remain historical evidence, not renewed production buildability verification.

### .NET SQLite spike checkpoint

- Started from clean `d0a6834`; previous V2 audit is preserved. Current work is under `experiments/PackageV2.SqliteSpike` and docs only; no solution membership, production dependencies/services, legacy Unity or real package changes.
- Microsoft.Data.Sqlite 10.0.12 / SQLitePCLRaw bundle 2.1.12 restored from the local cache. Normal directory creation needed a parent-folder write grant, which was obtained; normal editing/build now succeeds.
- Final Debug spike + infrastructure-reference build passes with zero warnings/errors; **60 executable assertions pass**, including all-class real fixtures, Unicode/null memo semantics, exact transactions, competing writers, rollback/FTS consistency, cancellation, selection scope, pagination, sidecars and unload/move. These are not 60 new production xUnit tests. Startup app/production suite were not rerun because their code, solution and dependencies are unchanged.
- Sequential 10k/100k/1M benchmarks and three fresh-process probes per size completed. All 36 first/plain/FTS full-record comparisons per size plus federation/attachment/save assertions pass. Raw ignored results live in `experiments/PackageV2.SqliteSpike/artifacts/`; final IDs and all measurements are in the report.
- At 1M, eight DBs with FTS total 161.84 MiB; selective substring ~22.7 ms, verified memo update ~11.7 ms. Broad materialized queries remain slow/memory-heavy (~2.8 GiB aggregate process peak); a direct 100-row page is ~1.4 ms versus ~141 ms when forcing broad FTS candidates. Bounded federation 4 improves this synthetic workload but is not a universal setting.
- Report and index complete: [package-format-v2-dotnet-sqlite-spike.md](package-format-v2-dotnet-sqlite-spike.md). Remaining product risks: logical-file ordinal/raw-name API gaps, Unicode/collation versioning and mixed-language performance, selectivity-aware planning/global paging, package revisions/editor/catalog integration, RID testing and crash recovery. Stage 7 remains deferred.
- Final doc/whitespace/scope checks pass: four documents, 42 local links, balanced fences/one reviewed Mermaid block. Local experiment ignore rules explicitly expose its `.csproj` despite the root Unity ignore, while keeping `bin/obj/artifacts` ignored. Git index remains unchanged; no commit/stage/reset/clean occurred.
- Next: approve a separate V2 schema/identity/extraction-contract milestone; do not replace persistence, migrate real packages or implement UI automatically. Spike is verified buildable in Debug; historical full app tests below are not renewed evidence of production buildability.

### Package V2 audit checkpoint

- Started from clean `593fd10`; staged/unstaged/untracked work was absent. Prior Kind-column work is committed; historical checkpoints below are retained.
- No application changes, migration, new production dependency, real-package writes or desktop interaction are authorized. V2 has no backward-compatibility requirement, but existing data must remain untouched.
- Confirmed current writer limits new references to Texture2D/Sprite and discards class/type/logical-file metadata; the asset-session enumeration already carries these without media decoding. Legacy package indexing instead calls `GetBaseField` for each indexed asset.
- Working report: [package-format-v2-architecture-audit.md](package-format-v2-architecture-audit.md). Disposable harness/results live outside Git at `C:\Users\kolynlin\Documents\Codex\2026-10-16\files-pasted-by-the-user-i\package-v2-benchmark`.
- Final isolated 10k/100k/1M prototypes pass 13-query full-result hash equivalence across nested JSON, compact JSON, SQLite B-tree and SQLite trigram variants; separate post-load secondary-index/header-read measurements also completed. Main repeated timings are medians of three, with fresh-process/OS-cache-warm observations explicitly distinguished from physical cold I/O.
- Report complete: proposed all-class metadata-first indexing, package/generation/source/logical-file/Path-ID identity, bounded federation, Stage 2–6 memo adaptation, risks and incremental approval gates. At 1M total assets, compact JSON is 116.42 MiB versus SQLite 176.91 MiB / trigram 209.54 MiB; SQLite selective lookups and memo updates win, broad materialized searches do not. Recommended SQLite is conditional on product decisions and a .NET/provider/real-metadata validation spike, not an approved implementation.
- Changed only this handoff, the new report, and the documentation index. The old architecture/system docs contain historical pre-auto-save statements; current source was inspected instead of treating those as current behavior. No production source, project/dependency, fixture, legacy Unity or genuine package changes occurred.
- Production source is unchanged; no application build/regression suite was run for this analysis-only task. Earlier build results below remain historical, not new buildability evidence. Documentation links/fences and Git whitespace/scope checks are the relevant final checks. Exact next step: obtain the report's storage/Unicode/generation/location/paging decisions, then authorize a separate V2 contract/provider spike. Do not implement V2, migrate packages, or begin Memo Stage 7 automatically.

### Kind full-hide checkpoint

- Preserved all unstaged/untracked work from the prior Kind-column task; HEAD remains `94532b8` and the index is untouched. The checkpoint below is historical and its 20 px minimum is superseded here.
- Explicit `MinWidth="0"` on the five existing Kind columns overrides Avalonia's inherited grid minimum without changing other columns. Defaults remain 52 px, maxima 80 px, and native resizing remains enabled. No custom drag handlers or new controls were added.
- Headless pointer tests verify zero actual column width, zero-width clipped or invisible virtualized Kind cells, and restoration to 52 px by dragging the next header's left edge (normally Name). This works in Assets, Search, Navigator, Package Contents, and Package Editor.
- Final Debug startup build passes with **0 warnings/errors**; the Debug solution build also passes. The four sizing tests and broader affected UI/memo focused suite pass **69/69**. A new full-suite run is unnecessary for this five-column minimum override; the prior shared-style/converter task's full **340/340** result remains historical evidence, not a new run.
- Changed only the four grid XAML files (five Kind columns), shared sizing test helper, policy/design minimum/restoration wording, and this handoff. Documentation checks pass for the three changed documents (21 local links/anchors and balanced fences); normal worktree/index whitespace checks and new-helper checks pass. Existing unstaged icon/style/test work remains intact, with no staging or commits.
- Manual acceptance: launch the current Debug executable, drag each Kind column's right edge fully left until no icon/column width remains, then restore it by dragging the left edge of the next header (normally Name) to the right. Verify ordinary resizing and selection/hover remain comfortable. No desktop control or real package writes occurred.

**Current verification state:** Full hiding/restoration is implemented using native Avalonia sizing and covered by headless tests; visible desktop acceptance remains pending. Do not begin another task automatically.

### Kind-column final checkpoint

- Started from clean HEAD `94532b8`; all prior Stage 6 work was already committed. Search used an 85 px text column, so its XAML had no icon template or converter binding.
- Search now reuses `AssetTypeIconConvertor` and existing folder/file/neutral-asset images; no Unity type is inferred or metadata changed. Kind remains the first blank-header sortable column.
- Assets, Search, Navigator, Package Contents, and Package Editor Kind columns now default to 52 px, allow user resizing, and cap at 80 px without a custom minimum. Dependency and candidate grids have no Kind column and are unchanged. Shared `KindIconCellTheme` derives from Fluent and clips narrow cell contents.
- Real headless header-drag checks pass across all five grids: effective minimum 20 px (Avalonia's untouched grid default), maximum 80 px, and reopening by the same native header edge. No complete collapse or new restoration UI is introduced. Four focused grid tests pass, including shared Search icons, accessibility, centering, sorting, selection, and parity.
- Changed areas: `AssetTypeIconConvertor.cs`, `AssetListStyles.axaml`, Assets/Search/Navigator/Packages grid XAML, four existing UI test files plus new `KindColumnAssertions.cs`, `ai-agent.md`, UI workflow design, and this handoff. No memo, persistence, package-model, indexing, editor-lifecycle, or grid-parity logic changed.
- Mandatory Debug startup and solution builds pass with **0 warnings/errors**. Focused affected UI/memo tests pass **69/69**. Since the converter and cell theme are shared infrastructure, the full Debug suite was run and passes **340/340** with 0 skipped. Git whitespace checks pass with informational LF-to-CRLF warnings only; all changes are unstaged and the Git index remains unchanged. No legacy Unity, genuine package, fixture, desktop, or screenshot operations occurred.
- Documentation validation passes: **33 Markdown files, 98 local links/anchors, 42 Mermaid blocks**, and balanced fences. The policy/design documents contain no remaining operative fixed-width Kind rule.
- Policies now supersede the old fixed-width rule and document the measured minimum. Manual visual acceptance remains pending: launch `E:\Projects\UABS\Avalonia\UABS\bin\Debug\net9.0\UABS.exe`, check Search Package/SourceFile/Asset icons, drag each Kind header to its smallest and largest widths and widen it again, and check ordinary-width centering, clipping, selection, and hover in light/dark themes. No new restoration interface is needed at the native 20 px limit.

**Current verification state:** Complete, buildable in Debug, regression-tested, and ready for user visual acceptance. Review and commit only on explicit authorization; do not begin another UX or memo stage automatically.

### Stage 6 final checkpoint

- Started from clean HEAD `331a3d1`; no staged or unstaged changes were present. Earlier checkpoints below describe historical work and must not be treated as current Git status.
- Assets now uses the same `EditableMemoInspectorViewModel` / View and application coordinator as Search. Its existing Stage 1 resolver still requires a unique indexed identity, and asset/session transitions flush the old identity without blocking the next presentation.
- Shared presenter reads the current coordinator snapshot when applying delayed notifications. Editor lifetime changes recheck both Inspectors; replacing an already-open clean editor also announces eligibility changes.
- Package Editor ordinary memo presentation is read-only and Clear is removed from the View. Backend mutation APIs and Apply Memo Copy remain intact; explicit Save/Close behavior is unchanged. The UI explains Save/close/catalog requirements and ambiguous-target limitations.
- Revision capture observes the coordinator entry before awaiting I/O and refuses to reset it from old catalog data if another edit/save/presentation advanced it. Queued Inspector notifications and verified Search row updates read the current identity snapshot when applied on the UI thread.
- Files changed: `AssetListViewModel.cs`, `MainWindowViewModel.cs`, `EditableMemoInspectorViewModel.cs`, `SearchWorkspaceViewModel.cs`, `PackageEditorViewModel.cs`, `AssetInspectorView.axaml`, `PackageManagementView.axaml`, `MemoDraftCoordinator.cs`, new shared `UnavailableMemoDraftCoordinator.cs`, new `AssetsMemoIntegrationTests.cs`, UI workflow/system documentation, and this handoff. No grid columns, indexing, schema, transaction writer, legacy Unity files, real packages, or shutdown flow changed.
- Sixteen new headless/integration cases cover exact identity and non-Texture indexed types, missing/empty/value semantics, loose/ambiguous/missing/removed targets, unload/reload, shared debounce, Save Now/Retry, cross-workspace failed drafts, inactive Search rows, duplicate names across packages, catalog refresh, asset/session/workspace navigation, stale notifications, delayed revision capture, editor exclusion/clean replacement/close, reusable editable UI, and read-only Package Editor controls with retained Apply Memo Copy. Existing editor dirty Save/Discard/Cancel, failure, and memo-copy regression tests remain passing.
- Mandatory Debug startup build (`dotnet build UABS/UABS.csproj -c Debug --no-restore`) and Debug solution build pass with **0 warnings/errors**. Focused Assets/Search/draft/editor tests pass **125/125**; full Debug regression passes **339/339**, with 0 skipped; zero-Unity-runtime audit passes **1/1**. Runs were sequential. The verified user launch target is `E:\Projects\UABS\Avalonia\UABS\bin\Debug\net9.0\UABS.exe` with its current companion DLL.
- Git staged/unstaged whitespace checks and separate new-file checks pass (informational LF-to-CRLF warnings only); current changes are entirely unstaged, including the two new untracked files. Repository-root Git inspection works with authorized command escalation; the earlier environmental limitations below are historical. Documentation validation passes: **33 Markdown files, 98 local links/anchors, 42 Mermaid blocks**, and balanced fences. No desktop control, screenshots, or visual-acceptance claim was made.
- Known limits: individual memo editing requires a unique persisted, loaded index record and a closed Package Editor. New/imported references must first be explicitly saved; ambiguous/incomplete targets stay unavailable, with the existing safe bulk-copy path retained. Package indexing still creates Texture2D/Sprite records only. Stage 7 shutdown draining/confirmation remains deferred: pending drafts are not claimed safe at process exit.

**Current verification state:** Stage 6 implementation is complete, buildable in Debug, and fully regression-tested. Manual Inspector acceptance remains user-driven. No commits or staging were performed.

**Next exact step:** Launch the verified Debug executable; edit one uniquely indexed memo in Assets, check the same draft/status in Search and verified row text, test Save Now/Retry and selection switching, then check Package Editor exclusion, read-only display, Apply Memo Copy, and eligibility after Close Editor. Review and commit only on user authorization. Do not begin Stage 7 automatically.

**Historical repository state (Packages checkpoint):** Branch `main` was at `06b1c9b` (`feat(memos): add debounced draft coordination`). The original 13-file Stage 5 implementation remained staged, and the Search memo-row synchronization follow-up remained unstaged. That Packages follow-up added only unstaged Avalonia source/test changes plus this handoff. Worktree-aware Git commands were rejected by the environment at the repository root in that session. The Stage 6 checkpoint above supersedes this historical status.

### Packages workspace UX follow-up checkpoint

- Package Contents, Direct Dependencies, dependency candidate records, and Package Editor asset rows now opt into the existing theme-aware `assetList` style. One Packages view loading-row handler reapplies `assetRowEven` / `assetRowOdd` from the current visible row index, preserving Fluent hover, selected, selected-hover, foreground, sorting, refresh, and virtualized-row behavior without changing models or columns.
- Added a clearly labeled **Close Editor** header action. Clean sessions close immediately. Dirty sessions use the existing confirmation service with an explicit Save / Discard / Cancel result: Save reuses the coordinated transactional editor Save and closes only after verified current-version success; failures and newer edits keep the editor open; Discard abandons only the working clone; Cancel changes nothing.
- Every successful close uses the existing `CloseEditor` lifecycle path, which clears the working editor and releases `PackageEditorSessionGuard`. Existing Package Editor memo controls, explicit Save semantics, package schema, indexing, and persistence implementation remain unchanged.
- Added focused headless/ViewModel coverage for themed row parity across all four grids, sort and catalog refresh reapplication, Close Editor binding, clean close, verified Save-and-close with a disposable package, failed Save retention, Discard, Cancel, and guard release. Focused Packages/editor tests pass **58/58**; the full suite passes **323/323**; and the zero-Unity-runtime audit passes **1/1**.
- Release solution build passes with **0 warnings and 0 errors**. A normal Debug build was blocked only because the user-running `UABS` process (PID 17432) held `bin/Debug/net9.0/UABS.exe`; the process was not terminated or controlled. A scoped whitespace/conflict-marker scan of all seven implementation/test files passes. Repository-root `git diff --check` remains unavailable because the environment denies Git changing to the root.
- No desktop behavior is claimed. Manual acceptance should verify light/dark zebra visibility and hover/selection precedence in the three requested grids, then exercise clean Close, dirty Save, dirty Discard, and Cancel.

**Current verification state:** The implementation is buildable and fully regression-tested in Release. Source behavior is safe to review; final repository-wide Git diff/whitespace review remains a user-side check because of the environment restriction.

**Next exact step:** Manually accept the Packages visuals and confirmation dialog, inspect the combined staged/unstaged Git diff, then stage/commit only with explicit authorization. Do not begin Memo Stage 6 or remove Package Editor memo editing as part of this checkpoint.

### Memo Integration Stage 5 implementation checkpoint

- Follow-up: `SearchResultItemViewModel` now owns an observable memo presentation initialized from the immutable search snapshot. Verified coordinator completions update all existing rows matching the complete asset identity on the UI thread. Failed/conflicted outcomes do not update rows, duplicate names in other packages remain independent, and the result collection, order, criteria, filters, selection, and scroll ownership are unchanged.
- Package Editor audit only: the direct memo mutation UI is the two-way `Editor.MemoText` text box plus `ClearMemoCommand`; package-to-package Apply Memo Copy is a separate bulk mutation path. Both update the isolated working clone, mark it dirty, and depend on explicit Save. The editor grid Memo column and Packages Contents grid are read-only. No Package Editor control or behavior changed in this follow-up.

- Replaced only the Search memo presentation with reusable `EditableMemoInspectorViewModel` / `EditableMemoInspectorView`. Eligible indexed asset results show a wrapped, scrolling multiline editor, package context, Saved/Unsaved/Saving/Failed state, Save Now, and Retry. Assets Inspector remains read-only.
- Eligibility requires a complete exact Stage 1 identity revalidated against the current catalog. Package/source results and missing, stale, ambiguous, unloaded, or incomplete identities remain disabled with an explanation. The active Package Editor guard blocks the same package whether clean or dirty; closing the editor re-resolves and restores any coordinator-owned draft.
- `MemoDraftCoordinator.TrackAsync` captures the whole-package revision before editing. Search uses the one application-lifetime coordinator, flushes the old immutable identity on result change and the active identity when leaving Search, and filters completion events by identity so stale saves cannot overwrite the newly selected result.
- The Stage 3 catalog callback now marshals verified package publication to the UI thread and replaces only the matching catalog item. Search results, criteria, scope, and selection are not rerun or discarded, while the Inspector revalidates against the fresh package. Package Editor working clones and dirty state are untouched.
- Added focused ViewModel/headless coverage for editable rendering, exact Save Now targeting, failed-draft Retry, result-switch flushing, stale completion isolation, active editor exclusion/recovery, catalog refresh with a pending draft, and workspace-exit flushing. Focused Search/Stage 2-4 memo/persistence/shell checks pass **55/55**.
- Remaining boundary: Stage 7 must integrate `DrainForShutdownAsync` with application close confirmation. Closing with pending drafts is not yet claimed safe. Assets Inspector editing, media/source inspection, schema/index expansion, and automatic Search reruns remain out of scope.

**Current verification state:** Safe and buildable. `dotnet build Avalonia.sln --no-restore` passes with 0 warnings/errors; focused Search/draft tests pass **33/33**; and the full suite passes **317/317**. The four follow-up files have no trailing whitespace or conflict markers. No desktop behavior is claimed.

**Next exact step:** User manual acceptance of the Search memo editor, then review and commit Stage 5 if accepted. Do not begin Assets memo editing or Stage 7 shutdown integration without a separate task.

### Memo Integration Stage 4 implementation checkpoint

- Added application-lifetime `MemoDraftCoordinator` infrastructure keyed by the complete normalized Stage 1 identity. It retains draft text, confirmed text/package revision, monotonic draft revision, structured persistence outcome/error, and observable Unavailable/Saved/Unsaved/Saving/Failed snapshots independently of selection or workspace state.
- The internal 1.5-second debounce uses injectable `IMemoDraftDelay`; every edit cancels and replaces the pending delay. Save Now, explicit Retry, navigation flush, and shutdown drain reuse only `IPackageMemoPersistenceService`. Per-asset save gates deduplicate overlapping equivalent requests, while Stage 3 continues serializing same-package disk transactions.
- A newer edit cannot be cleared by an older completion. Failures retain the complete draft and do not automatically retry; editing a failure reschedules, explicit Retry is available, and navigation flush preserves an existing Failed draft. Shutdown drain cancels timers, waits for in-flight work, attempts remaining drafts once, and reports any failures.
- `MainWindowViewModel` owns one coordinator for the application lifetime, but no Inspector binding or editable UI exists. Stage 5 must connect target tracking/navigation flush and a UI-thread catalog publication callback. Stage 7 must call shutdown drain from the close-confirmation workflow; synchronous disposal currently cancels timers but intentionally does not claim saved shutdown state.
- Twelve deterministic tests cover debounce timing/reset/coalescing, no-op edits, Save Now, overlap deduplication, Retry/conflicts, navigation identity, newer edits during save, multiple package/asset drafts, editor-blocked outcomes, shutdown drain, timer cleanup, and a disposable-package round trip through the real Stage 3 writer. Focused Stage 2-4 persistence/orchestration and composition tests pass **40/40**.
- Final automated verification: `dotnet build Avalonia.sln --no-restore` passes with 0 warnings/errors; the full suite passes **310/310**; and the zero-Unity-runtime audit passes **1/1**. Documentation scan: 33 Markdown files, 41 Mermaid blocks, 0 missing relative links, and 0 unbalanced fenced files.
- The staged diff whitespace check passes. The pre-interruption unstaged diff check also passed; after resume, the sandbox permits index-only Git inspection but refuses worktree-aware Git operations even after approved repository permissions. Direct staged-patch and filesystem review found no View/XAML, package-schema, fixture, real-package, or legacy Unity changes.

**Current verification state:** Safe, buildable, and fully regression-tested. Stage 4 is complete for its infrastructure-only scope; no desktop behavior is claimed.

**Next exact step:** Review the staged Stage 4 patch and this unstaged final handoff update, then stage/commit only with user authorization. Do not begin Stage 5 Inspector editing or Stage 7 shutdown integration.

### Memo Integration Stage 3 checkpoint

- Added `PackageMemoPersistenceService`, whose immutable request carries the exact Stage 1 identity, desired draft, expected original memo, and optional package revision. Structured results retain the draft and distinguish saved/unchanged, missing or ambiguous targets, revision conflicts, active editor exclusion, write/verification/publication failures, and pre-commit cancellation.
- The service acquires the shared Stage 2 package coordinator, fingerprints the complete package directory, loads the latest document, resolves the exact asset, edits one memo on a clone, rechecks freshness, saves through transactional `PackageService`, reloads without cancellation, and verifies the persisted value and revision. An optional catalog callback publishes only the fresh verified document while coordination is still held.
- Added `PackageEditorSessionGuard` and connected it to the existing editor lifecycle. Any active editor session blocks memo-only writes to the same package even when clean; other packages remain independent. The service never mutates, saves, refreshes, or discards an editor working clone.
- Fingerprints cover normalized directory/file paths, file lengths, and all file content while excluding reparse points. This detects changes beyond one index file. It is not a cross-process lock, so an external writer can still race after the last fingerprint and before replacement; conflicts are not merged or retried.
- All tests use disposable package directories. No Inspector editing, debounce, auto-save, package schema/indexing, memo-copy, or Package Editor explicit-Save behavior changed.
- Eleven Stage 3 tests use disposable fixtures and cover successful exact writes, unrelated memo/unknown-field preservation, empty versus missing values, duplicate/missing/ambiguous identities, expected revision and memo conflicts, mid-load external changes, same/different-package concurrency, cancellation, writer/verification/publication failures, active-editor exclusion, retained dirty editor state, and fresh publication under the held lock.
- Final verification: solution build passes with 0 warnings/errors; focused memo/coordinator/editor tests pass **63/63**; full regression passes **297/297**; and the zero-Unity-runtime audit passes **1/1**. `git diff --check` and cached diff check pass with one informational line-ending warning. Documentation scan: 33 Markdown files, 40 Mermaid blocks, 0 missing relative links, and 0 unbalanced fenced files. Final status contains only the eight scoped Avalonia Stage 3 files, the index is empty, and the legacy Unity tree is unchanged.

**Current verification state:** Safe, buildable, and fully regression-tested. Stage 3 is complete for its backend-only transactional persistence scope.

**Next exact step:** Review and commit Stage 3 if accepted. Do not begin Stage 4 Inspector editing, debounce, or auto-save without explicit approval.

### Memo Integration Stage 2 checkpoint

- Added injectable `IPackageWriteCoordinator`/`PackageWriteCoordinator` in neutral package infrastructure. It uses `PackagePathComparer` identities, one asynchronous semaphore per active normalized path, reference-counted waiter/holder cleanup, cancellable waiting, and no global/disposable coordinator lock.
- `MainWindowViewModel` owns one shared coordinator and injects it through `PackageManagementViewModel` into `PackageEditorViewModel`. Directly composed editors default to a private coordinator for compatibility.
- Explicit editor Save is the only trigger. The lock covers staged save, uncancelled fresh reload verification, editor-generation publication, and catalog callback. Existing isolated snapshots, overwrite confirmation, rollback writer, dirty state, failed-save retention, and newer-edit protection remain intact.
- Coordination provides in-process mutual exclusion only. It does not merge stale documents, lock external processes, edit Inspector memos, debounce, auto-save, or change the rule that future Inspector editing must be disabled while the same package is open in Package Editor.
- Baseline before changes: build passed with 0 warnings/errors and full suite passed **276/276**. Final focused coordinator/editor/package-service tests pass **69/69**. Final solution build passes with 0 warnings/errors, the full regression suite passes **285/285**, and the zero-Unity-runtime audit passes **1/1**.
- `git diff --check` and `git diff --cached --check` pass with one informational line-ending warning. Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unbalanced fenced files. The index remains unchanged, and no legacy Unity files changed.
- No desktop application, automation, screenshots, real package data, or source fixtures were used or modified.

**Current verification state:** Safe, buildable, and fully regression-tested. Stage 2 is complete for its infrastructure-only scope.

**Next exact step:** Review and commit Stage 2 if accepted. Do not begin Stage 3 memo persistence or Stage 4 debounce/auto-save without explicit approval.

### Memo Integration Stage 1 checkpoint

- Added immutable, UI-neutral `PackageMemoResolution` results with `Available`, `NoMatch`, `Ambiguous`, `Unsupported`, and `MissingOrStale` outcomes. `PackageMemoResolver` performs synchronous read-only lookup over the loaded catalog and reuses `PackageMemoService` for strict complete-identity reads.
- Assets memo lookup is type-agnostic and requires exactly one normalized source-path + exact name + Path-ID match. Duplicate matches and duplicate identities across logical serialized files are refused rather than guessed. Existing package creation/import still emits only Texture2D/Sprite records; broadening indexing is explicitly deferred.
- Search memo lookup revalidates package path, index directory, source path, asset name, and Path ID against the current catalog. Package/source results are unsupported, and unloaded or changed results become stale.
- Added a reusable read-only memo Inspector presenter to Assets and Search. It shows the full memo, package context, and distinct missing, explicit-empty, no-match, ambiguous, unsupported, and stale states. Assets and Search selection state remains independent.
- No save, auto-save, writer, editor transaction, navigation, search execution, package persistence, or splitter behavior changed. Focused resolver/ViewModel/headless tests pass **32/32**, including no-write behavior, dirty-editor preservation, catalog unload/reload, selection changes, exact provenance, full untruncated text, and logical-file ambiguity.
- `dotnet build Avalonia.sln --no-restore` passes with 0 warnings and 0 errors. The full regression suite passes **273/273** with 0 failures and 0 skips, and the focused zero-Unity-runtime dependency audit passes **1/1**.
- `git diff --check` and `git diff --cached --check` pass with informational line-ending warnings only. Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unbalanced fenced files.
- No desktop application, automation, or screenshots were used; Inspector appearance remains pending user visual acceptance.
- Follow-up product decision: removed the resolver's Texture2D/Sprite class-ID gate because the existing `AssetInfos` identity is type-agnostic. Focused resolver/Assets tests pass **19/19**, including TextAsset, AudioClip, and MonoBehaviour records, and the solution build passes with 0 warnings and 0 errors. Package creation/import and Add Selected remain intentionally limited to Texture2D/Sprite and were not changed.

**Current verification state:** Safe and buildable. The generalized resolver adjustment is focused-tested; the prior complete Stage 1 suite remains recorded above.

**Next exact step:** Decide and scope the separate package-index expansion if newly created packages must include every Unity asset type. Do not begin Stage 2 editing/auto-save without explicit approval.

### Packages loaded-packages sidebar checkpoint

- Added Packages-owned sidebar layout state without changing Assets or Search layout behavior. The loaded-packages list retains its 330 px default, has a 260 px normal minimum, and requires the established 36 px deliberate overshoot to snap closed.
- Added a centered 28×28 header chevron and a 26×56 keyboard-accessible edge restore handle. The previous comfortable width is retained, while the contents/editor region keeps a 520 px minimum.
- Sidebar visibility is independent from package selection and editor lifetime. Focused tests verify that collapse/restore and workspace switching preserve the selected package, loaded catalog, selected editor item, dirty flag, renamed package, pending edits, and unsaved memo.
- Focused package/workspace tests pass 20/20, including actual splitter gestures, exact-minimum behavior, overshoot collapse, centered controls, keyboard restoration, width memory, and independent Assets/Search layout state.
- `dotnet build Avalonia.sln --no-restore` passes with 0 warnings and 0 errors. `git diff --check` and `git diff --cached --check` pass with informational line-ending warnings only; `git status --short -- UABS` is empty.
- No package persistence, Recent Packages, legacy Unity files, or real package data were changed. No desktop application, automation, or screenshots were used.

**Current verification state:** Safe and buildable. Focused tests cover the Packages layout and dirty-editor risks; a full suite was not required because shared Assets/Search layout logic was not changed.

**Next exact step:** The user manually verifies the Packages splitter, collapse/restore controls, and editor usability. Do not begin another UX task automatically.

### UX-3A Search panel collapse/restore checkpoint

- Added Search-owned panel layout state without changing the Assets layout implementation. Search Controls and Search Inspector resize and collapse independently, use 240 px and 260 px minimums, and require the established 36 px deliberate splitter overshoot to snap closed.
- Compact centered 28×28 collapse buttons and 26×56 keyboard-accessible edge restore handles retain each panel's last comfortable width (defaults 290 px and 310 px). The center Results region retains a 280 px minimum.
- The long-lived Search ViewModel owns visibility and widths, so workspace switching preserves layout independently from Assets. Hiding/restoring panels retains criteria, package selections, results, selected result, and the controls scroller offset.
- Preserved the corrected Search scroller configuration: Fluent `AllowAutoHide="True"` with one 20 px inner gutter for the 16 px overlay track plus 4 px separation.
- Focused Search/workspace headless tests pass 14/14, covering real splitter gestures, exact-minimum behavior, 36 px overshoot, independent collapse, centered controls, keyboard restoration, width memory, Search state and scroll retention, and Assets/Search layout independence.
- `dotnet build Avalonia.sln --no-restore` passes with 0 warnings and 0 errors. `git diff --check` and `git diff --cached --check` pass with informational line-ending warnings only; no legacy Unity changes are reported.
- No desktop application, automation, or screenshots were used; visual acceptance remains user-driven.

**Current verification state:** Safe and buildable. Focused tests cover the changed Search layout behavior; a full suite was not required because no shared Assets layout behavior changed.

**Next exact step:** The user manually verifies splitter feel, chevron placement, restoration, center usability, and the corrected Search scrollbar. Do not begin UX-3B automatically.

### UX-3A scrollbar and toolbar polish checkpoint

- Follow-up correction restores Fluent `AllowAutoHide="True"` behavior. Runtime inspection confirmed the expanded Fluent overlay track is 16 px wide, so one inner content host reserves that track plus a 4 px gutter with `Padding="0,0,20,0"`; individual controls are unchanged.
- Focused Search workspace tests pass 10/10, including actual-template checks for auto-hide, vertical overflow, and at least 4 px between the outer Search scrollbar and the input edge. The Enter-key test now awaits command completion deterministically rather than relying on one scheduler yield.
- The toolbar cleanup, workspace navigation, and all Search behavior remain unchanged. No desktop automation was performed; visual acceptance remains user-driven.

- Removed the obsolete top `Toolbar` control and its private style. Its useful actions were already available in workspace-local controls: Open File in Assets, folder/history/refresh in Navigator, and Assets/Search/Packages navigation in the visible tabs. Disabled Export/Filter/Memo decoration was not relocated; Depend had only opened Packages and the Direct Dependencies tab remains there.
- Simplified `MainWindow` to host the named three-workspace `TabControl` directly. Window title bar and standard window controls are unchanged, and the existing navigation commands remain available.
- Added headless layout/shell tests verifying reserved scrollbar viewport space and all three workspace navigation routes. Focused Search/shell/MainWindow tests pass 21/21.
- `dotnet build Avalonia.sln --no-restore` passes with 0 warnings and 0 errors. Because the shell changed, the full regression suite was run and passes 255/255 with 0 failures and 0 skips.
- `git diff --check` and `git diff --cached --check` pass with informational line-ending warnings only. No live `Toolbar`/style references remain, and the legacy Unity tree is unchanged.
- No desktop application, automation, or screenshots were used; scrollbar appearance and compact tab navigation remain pending user visual acceptance.

**Polish changed files:** `MainWindow.axaml`, `SearchWorkspaceView.axaml`, `SearchWorkspaceViewModelTests.cs`, new `MainWindowShellTests.cs`, current UX/audit documentation, and removal of `Toolbar.axaml`, its code-behind, and `ToolbarStyle.axaml`.

**Next exact step:** The user manually verifies that the Search scrollbar no longer covers controls and that the three workspace tabs remain sufficient navigation. If accepted, the user may stage the polish additions/deletions with the existing UX-3A index and commit. Do not begin UX-3B automatically.

### UX-3A safe checkpoint

- Added `PackageSearchRequest.RequireNonEmptyMemo`; `PackageSearchService` applies it only to asset candidates and independently from memo text matching.
- Added a long-lived `SearchWorkspaceViewModel` and dedicated top-level Search tab. The toolbar opens Search directly, and the old embedded Packages/Search tab was removed.
- Search owns independent package scope, criteria, result selection, loading/error/empty state, cancellation, generation guards, and executed-criteria tracking. Initial catalog packages are selected; later additions remain unselected until explicit user action.
- The Search view provides package filtering and checkbox scope, Select All/Clear, include/exclude/Path-ID criteria, exact matching, Search memos, Memo only, result-kind filters, an explicit Search button, Enter execution, merged results using the existing green DataGrid style, and a metadata-only inspector.
- Focused service, Search ViewModel, headless interaction/grid, and MainWindow workspace tests pass 39/39. These cover memo-filter independence, multi-package provenance, selection reconciliation, preserved results plus criteria-changed state, validation, stale-result suppression, long-lived state across workspace switching, Enter execution, sorting availability, Search-local selection, row styling, and virtualized parity.
- The loading-state edge case where an invalid request supersedes an active valid request is fixed and regression-tested; cancellation can no longer leave Search appearing busy.
- Sequential `dotnet build Avalonia.sln --no-restore` passes with 0 warnings and 0 errors. The first interrupted-session verification attempt ran build and tests concurrently and produced a temporary `CS2012` output-file lock; it was an orchestration artifact, not a source defect.
- Full `dotnet test Avalonia.sln --no-build` passes 253/253 with 0 failures and 0 skips. The focused zero-Unity-runtime dependency audit passes 1/1.
- Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unclosed fenced blocks. UX foundation, implemented workflow, architecture, and package-search system documents distinguish UX-3A from deferred UX-3B/3C/3D work.
- `git diff --check` and `git diff --cached --check` pass; the index remains empty. Final status contains only the expected Avalonia UX-3A source, tests, and documentation. `git status --short -- UABS` is empty, confirming no tracked legacy Unity changes.
- No desktop application launch, computer control, screenshots, source-file inspection, or manual UI claim was made. No new Unity-exclusive dependency was introduced or discovered.

**Changed areas:** package-search request/service and tests; dedicated Search View/ViewModel and tests; MainWindow/toolbar workspace composition; removal of the embedded Packages Search tab; architecture/design/system documentation and this handoff.

**Current verification state:** Safe, buildable, and fully regression-tested. UX-3A is complete for its explicit metadata-only scope; desktop appearance and interaction remain pending user acceptance.

**Next exact step:** The user manually verifies the dedicated Search workspace checklist below the final report. If accepted, review/stage the UX-3A files for a user-authorized commit. Do not begin UX-3B/3C/3D automatically.

### UX-2C fixed Asset List Kind-column checkpoint

- Confirmed the center Assets grid has no textual `Kind` column. Its first blank-header 42 px template column is the kind indicator because it renders the asset-type icon; the separate textual `Type` column remains unchanged.
- Added Avalonia's per-column `CanUserResize="False"` only to the 42 px icon column. The grid-level resize setting and every Name/Type/Class ID/Path ID/Size/File column remain user-resizable.
- Preserved the column's first position, blank header, 24 px bitmap content, width, selection/focus behavior, zebra styling, and existing sort configuration.
- Added one headless regression test confirming the semantic Kind/icon column is fixed at 42 px, adjacent Name resizing does not change it, all other columns remain resizable, and the existing 24 px icon still renders.
- Focused Asset List row-style and ViewModel tests pass 9/9.

### Fixed Asset Kind-column changed files

- `UABS/Script/View/Pages/FolderView.axaml`
- `UABS.UnityAssets.Tests/AssetListRowStyleTests.cs`
- `docs/05_audit/codex-session-handoff.md`

### Fixed Asset Kind-column final verification

- Focused Asset List row-style and ViewModel tests: passed, 9/9.
- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 242/242, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check` and `git diff --cached --check`: passed; only informational LF-to-CRLF conversion warnings were emitted.
- Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unclosed fenced blocks.
- Final scoped review confirmed the preserved eleven-file index is unchanged and this task adds only the three expected unstaged files. The legacy Unity tree, real package data, and asset fixtures are unchanged.
- No desktop automation, application launch, screenshot, or manual visual interaction was performed.

**Next exact step:** the user manually verifies that the 42 px Assets Kind/icon column cannot be resized while adjacent columns can. If accepted, review and stage the three-file adjustment with the preserved UX-2C/UX-2D checkpoint for a user-authorized commit. Do not begin Search, preview, or unrelated UX work.

### UX-2D final Kind-column polish checkpoint

- Moved the existing Kind icon template from the second column to the first. Name, Size, and Modified retain their relative order.
- Kept the Kind width at 52 px and set Avalonia's supported per-column `CanUserResize="False"`; the grid and every other column remain user-resizable.
- Replaced visible `Kind` header text with an empty `TextBlock` carrying `AutomationProperties.Name="Kind"`, preserving the normal header cell layout.
- Set per-column `CanUserSort="False"` so the blank header is not an ambiguous sorting target. `SortMemberPath="Kind"` is retained and programmatic collection-view Kind sorting remains verified.
- Folder/document geometry, theme colors, selected-white contrast, tooltips, entry accessibility names, activation, selection, zebra parity, virtualization, and browser logic are unchanged.
- Updated the existing headless tests to verify first display position, blank accessible header, fixed 52 px width after adjacent-column resize, Kind non-resize/non-click-sort configuration, other-column resizing, programmatic Kind sorting, icons, row parity, and activation. Focused tests pass 14/14.

### Final Kind-column polish changed files

- `UABS/Script/View/Pages/BrowserView.axaml`
- `UABS.UnityAssets.Tests/NavigatorRowStyleTests.cs`
- `docs/03_design/ux-redesign-foundation.md`
- `docs/03_design/ui-and-workflow-design.md`
- `docs/05_audit/codex-session-handoff.md`

### Final Kind-column polish verification

- Focused Navigator style/icon and existing activation tests: passed, 14/14.
- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 241/241, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check` and `git diff --cached --check`: passed; only informational LF-to-CRLF conversion warnings were emitted.
- Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unclosed fenced blocks.
- Final Git review confirmed the eleven-file index is unchanged and the cumulative polish remains limited to the six expected unstaged files. The legacy Unity tree, real package data, and asset fixtures are unchanged.
- No desktop automation, application launch, screenshot, or manual visual interaction was performed.

**Next exact step:** the user performs the final Kind-column manual checklist. If accepted, review and stage the six-file cumulative polish with the preserved UX-2C/UX-2D checkpoint for a user-authorized commit. Do not begin Search, preview, or unrelated UX work.

### UX-2D Kind-icon polish checkpoint

- Confirmed the Navigator `Kind` cell was a 75 px `DataGridTextColumn` rendering the existing `BrowserEntryKind` enum as `Directory` / `File`.
- Read-only legacy inspection confirmed `EntryFolderInfoView` selects a sprite through `FolderViewType2IconData`: folders use the yellow `folder.png`, while bundles use a generic asset icon; a separate blue `file.png` is also present. Avalonia uses original vector geometry rather than importing Unity sprites.
- Replaced the text cell with a 52 px `DataGridTemplateColumn` that retains `SortMemberPath="Kind"`. A centered 20 px host contains an 18 px folder silhouette or folded-corner document `PathIcon` selected from the existing `IsDirectory` classification.
- Light icon brushes are folder `#A66B00` and file `#2563A8`; dark brushes are folder `#F1B84B` and file `#8CB4E8`. Selected icons resolve to the existing white selected foreground.
- The host tooltip and automation name bind to `BrowserEntry.Kind`, and each visible icon also has an explicit `Directory` or `File` automation name.
- No model, classification, directory service, recognition, navigation, activation, selection, splitter, or row-style behavior changed.
- Added three headless tests covering icon type/geometry, exact alignment and column width, light/dark-aware selected foreground, tooltips, automation names, Kind sorting, deep virtualized scrolling/parity, and activation by clicking the icon. The focused Navigator icon/style plus existing activation set passes 14/14.

### Kind-icon polish changed files

- `UABS/Script/Styles/AssetListStyles.axaml`
- `UABS/Script/View/Pages/BrowserView.axaml`
- `UABS.UnityAssets.Tests/NavigatorRowStyleTests.cs`
- `docs/03_design/ux-redesign-foundation.md`
- `docs/03_design/ui-and-workflow-design.md`
- `docs/05_audit/codex-session-handoff.md`

### Kind-icon polish final verification

- Focused Navigator icon/style and existing activation tests: passed, 14/14.
- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 241/241, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check` and `git diff --cached --check`: passed; only informational LF-to-CRLF conversion warnings were emitted.
- Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unclosed fenced blocks.
- Combined status/diff review confirmed the eleven-file UX-2C/UX-2D index is preserved and only the six scoped Kind-icon files are unstaged. The legacy Unity tree, genuine package data, and asset fixtures are unchanged.
- No desktop automation, application launch, screenshot, or manual visual interaction was performed.

**Next exact step:** the user performs the Kind-icon manual visual checklist. If accepted, review and stage the six-file polish with the preserved UX-2C/UX-2D checkpoint for a user-authorized commit. Do not begin Search, preview, or unrelated UX work.

## UX-2D implementation checkpoint

- Confirmed the Navigator is the existing read-only Fluent `DataGrid`, not a separate list-template system. It remains bound to `BrowserViewModel.Entries`, and its tap/Enter activation path is unchanged.
- Extended the UX-2C style resource with an opt-in `navigatorList` treatment. Navigator light even/odd are `#F5F9F5` / `#EEF5EE`; dark even/odd are `#1A271E` / `#203024`.
- Renamed only the shared hover/selection resource keys in the unstaged worktree layer. Both Assets and Navigator still use light/dark hover `#CCE3D0` / `#2C4633`, selection `#39724D` with white text, and selected-hover `#2F613F`; UX-2C rendered colors are unchanged.
- `BrowserView` clears and reapplies parity classes from each current `DataGridRow.Index` during `LoadingRow`, preventing stale presentation after column sorting, navigation, refresh, or virtualized reuse without changing `BrowserEntry` data.
- State precedence is selected-hover, selected, hover, then alternating base. The Fluent cell-focus template, file/folder content, column sizing, empty-directory state, single-click activation, and Enter behavior remain intact.
- Added three headless style tests for light/dark alternation, column sorting, navigation, refresh, deep virtualized scrolling/reuse, hover, selection, white selected text, and keyboard-focus template retention. Existing activation tests remain part of the focused regression set.
- Baseline before UX-2D: build passed with 0 warnings/errors and full tests passed 235/235. Current focused Navigator style, browser interaction, and browser-service tests pass 20/20.

### UX-2D changed files

- `UABS/Script/Styles/AssetListStyles.axaml` (shared style resource; UX-2C staged content remains preserved in the index)
- `UABS/Script/View/Pages/BrowserView.axaml`
- `UABS/Script/View/Pages/BrowserView.axaml.cs`
- `UABS.UnityAssets.Tests/NavigatorRowStyleTests.cs`
- `docs/03_design/ux-redesign-foundation.md`
- `docs/03_design/ui-and-workflow-design.md`
- `docs/05_audit/codex-session-handoff.md`

### UX-2D final verification

- Focused Navigator style, browser interaction, and directory-browser tests: passed, 20/20.
- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 238/238, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check` and `git diff --cached --check`: passed; only informational LF-to-CRLF conversion warnings were emitted.
- Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unclosed fenced blocks.
- Combined status/diff review found the original eight UX-2C files still staged, with only the seven scoped UX-2D files unstaged. The legacy `UABS` tree, genuine package data, and asset fixtures are unchanged.
- No desktop automation, application launch, screenshot, or manual visual interaction was performed.

**Next exact step:** the user performs the UX-2D manual visual checklist. If accepted, review and stage UX-2D together with the preserved UX-2C checkpoint for a user-authorized commit. Do not begin Search, preview, or unrelated UX work.

## UX-2C implementation checkpoint

- Confirmed the checkout was clean at `564f68d`; the previously documented UX-2A/UX-2B work had been committed before this task began.
- Confirmed the Assets list is a read-only Avalonia `DataGrid` bound to the sorted/filtered immutable `AssetListViewModel.Assets` snapshot. It uses the Fluent DataGrid theme, default extended selection, virtualized rows, and the standard cell-focus rectangles.
- Added opt-in `AssetListStyles.axaml` resources and selectors. Packages is unaffected; a future asset list can reuse the style explicitly with the `assetList` class.
- Added light even/odd `#F3F8F3` / `#E8F2E9` and dark even/odd `#18231B` / `#1E2C22`; light/dark hover is `#CCE3D0` / `#2C4633`; selected is `#39724D` with white text; selected-hover is `#2F613F`.
- `FolderView` assigns parity classes from `DataGridRow.Index` during `LoadingRow`, keeping row appearance outside asset data and refreshing it when sorted, filtered, or virtualized containers are reused.
- Styles target the Fluent row template's `BackgroundRectangle` rather than competing with ignored row-state backgrounds. Precedence is selected-hover, selected, hover, then alternating base. All rows in extended selection receive the selected style; the existing cell focus template remains intact.
- Added three headless tests covering light/dark resource resolution and alternation, sorting/filtering plus virtualized scroll/reuse, hover/selection/selected-hover, extended selection, selected foreground, and keyboard-focused cell template retention.
- Implementation checkpoint: build passed with 0 warnings/errors and the focused style/`AssetListViewModel` tests passed 8/8 before the full verification run.

### UX-2C changed files

- `UABS/App.axaml`
- `UABS/Script/Styles/AssetListStyles.axaml`
- `UABS/Script/View/Pages/FolderView.axaml`
- `UABS/Script/View/Pages/FolderView.axaml.cs`
- `UABS.UnityAssets.Tests/AssetListRowStyleTests.cs`
- `docs/03_design/ux-redesign-foundation.md`
- `docs/03_design/ui-and-workflow-design.md`
- `docs/05_audit/codex-session-handoff.md`

### UX-2C final verification

- Focused Assets style and `AssetListViewModel` tests: passed, 8/8.
- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 235/235, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check`: passed; only informational LF-to-CRLF conversion warnings were emitted.
- Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unclosed Mermaid blocks.
- Final status review found only the eight scoped UX-2C files listed above. No legacy Unity file, genuine package data, or asset fixture changed.
- No desktop automation, application launch, screenshot, or manual visual interaction was performed.

**Next exact step:** the user performs the UX-2C manual visual checklist. If accepted, the eight-file checkpoint is ready for a user-authorized commit. Do not begin UX-3, preview work, or restyle Packages.

## UX-2B implementation checkpoint

- Confirmed UX-2A used normal `GridSplitter` minimum constraints and large text toggles; it had no gesture that could distinguish reaching the minimum from deliberately dragging beyond it.
- Retained the 220 px Navigator minimum, 260 px Inspector minimum, 640 px maximum, independent in-memory visibility, remembered widths, single selection, and single session ownership.
- Added one shared per-view drag tracker used by both splitters. Reaching the minimum remains expanded; a further 36 px of pointer movement during the same drag snaps the corresponding panel closed.
- A snap collapse does not record the constrained minimum as the saved width. Restoration therefore returns to the last comfortable expanded width.
- Replaced the large text buttons with compact, accessible chevrons in the expanded panel headers. Collapsed panels expose slim edge restore handles with tooltips, automation names, default focus visuals, and Enter/Space button semantics.
- Added viewport-aware maximum-width allocation. Narrowing the window temporarily caps rendered side widths to preserve the 280 px center minimum without changing visibility preferences or remembered widths; widening can restore the requested widths.
- Collapse/restore remains independent and does not affect browser history, asset selection, lazy metadata, the loaded session, packages, or memo ownership.
- Focused headless coverage passes 11/11 for real splitter resize/collapse gestures, exact-minimum behavior, independent panels, accessible keyboard restoration, repeated operations, narrow supported widths, and retained selection/session state.
- No desktop automation, real package writes, Unity changes, preview decoding, Search/Dependencies redesign, or cross-launch settings were introduced.

### UX-2B final verification

- Focused Assets workspace layout and headless interaction tests: passed, 11/11.
- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 231/231, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check` and `git diff --cached --check`: passed; only informational LF-to-CRLF conversion warnings were emitted.
- Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, and 0 unclosed Mermaid blocks.
- Combined index/worktree review confirmed the staged UX-2A foundation remains preserved and UX-2B is the unstaged interaction refinement. No legacy Unity file, genuine package data, or asset fixture changed.
- No desktop automation, application launch, screenshot, or manual UI interaction was performed. User desktop acceptance remains pending.

### UX-2B icon-alignment polish

- The Avalonia 11.3.3 Fluent `Button` template forwards content alignment directly to its `ContentPresenter`; the four icon buttons previously left those properties at their control defaults and used font-baseline-dependent Unicode triangles.
- Added one shared local button style that sets zero padding and centers content horizontally and vertically. All four buttons now use symmetric, DPI-independent `PathIcon` triangle geometry with one shared icon style; their established 28×28 collapse and 26×56 restore hit targets are unchanged.
- Added a headless rendered-layout regression test that verifies the icon center matches the button center for both expanded collapse buttons and collapsed restore handles.
- Focused interaction tests: 8/8 passed. Full build: 0 warnings/errors. Full suite: 232/232 passed. Desktop visual acceptance remains pending.

**Next exact step:** the user performs the UX-2B manual acceptance checklist. If accepted, review and stage UX-2B together with the preserved UX-2A checkpoint for a user-authorized commit. Do not begin UX-3, media previews, Search/Dependencies rehosting, or cross-launch layout persistence without approval.

## UX-2A baseline checkpoint

- Verified the Assets tab currently uses a fixed `460,4,*` grid: `BrowserView` is the left navigator and `FolderView` occupies the rest.
- Verified `FolderView` combines the asset filter/grid and read-only metadata panel; there is no dedicated Inspector component.
- Verified `MainWindowViewModel` owns the active session, `AssetListViewModel` owns the single asset selection and cancellable details request, and switching workspaces retains both long-lived ViewModels.
- Verified there is one existing Navigator splitter but no Inspector splitter, panel collapse state, width memory, or cross-workspace layout state.
- UX-2A will preserve those ownership boundaries, move the existing details presentation into a read-only Inspector view, and introduce only in-memory layout state. It will not add previews, memo editing, new workspaces, or disk settings.
- No source implementation has been changed at this checkpoint. Only this handoff records the verified baseline.

## UX-2A implementation checkpoint

- Added `AssetsWorkspaceLayoutViewModel` as the in-memory owner of Navigator/Inspector visibility and remembered widths. It is separate from browser, asset-session, selection, package, and memo state.
- Added `AssetsWorkspaceView` with persistent top-level restore controls and the implemented `Navigator | Assets | Inspector` regions.
- Both boundaries use real `GridSplitter` controls. Drag completion records clamped widths (Navigator minimum 220, Inspector minimum 260, maximum 640); the center remains star-sized.
- Collapsing a panel sets both its content and splitter column to zero while retaining the expanded width. Both panels can be collapsed and restored independently.
- Moved the existing read-only details presentation out of `FolderView` into reusable `AssetInspectorView`, bound directly to the existing `AssetListViewModel`. No second selection or asset session exists.
- Inspector shows name, type, class ID, Path ID, byte size, logical file, source path, detected file kind, Unity version, logical-file count, loading/error state, a no-selection state, and an honest future-preview placeholder.
- The package memo editor remains unchanged and remains the sole owner of editable memo state and explicit Save.
- Focused layout/workspace tests pass 11/11, including actual headless splitter drags, independent resizing, minimums, collapse/restore, both collapsed, width memory, workspace switching, selection/details retention, session retention, and existing package-state navigation tests.

### UX-2A changed files

- `UABS/Script/MainWindow.axaml`
- `UABS/Script/View/Pages/AssetsWorkspaceView.axaml`
- `UABS/Script/View/Pages/AssetsWorkspaceView.axaml.cs`
- `UABS/Script/View/Pages/AssetInspectorView.axaml`
- `UABS/Script/View/Pages/AssetInspectorView.axaml.cs`
- `UABS/Script/View/Pages/FolderView.axaml`
- `UABS/Script/ViewModels/AssetsWorkspaceLayoutViewModel.cs`
- `UABS/Script/ViewModels/AssetListViewModel.cs`
- `UABS/Script/ViewModels/MainWindowViewModel.cs`
- `UABS.UnityAssets.Tests/AssetsWorkspaceLayoutViewModelTests.cs`
- `UABS.UnityAssets.Tests/AssetsWorkspaceViewInteractionTests.cs`
- `UABS.UnityAssets.Tests/MainWindowViewModelTests.cs`
- `docs/02_architecture/avalonia-architecture.md`
- `docs/03_design/ui-and-workflow-design.md`
- `docs/03_design/ux-redesign-foundation.md`
- `docs/04_systems/filesystem-browser.md`
- `docs/05_audit/codex-session-handoff.md`

### UX-2A final verification

- Focused layout/workspace/headless tests: passed, 11/11.
- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 228/228, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check`: passed; only informational LF-to-CRLF conversion warnings were emitted.
- Documentation scan: 33 Markdown files, 39 Mermaid blocks, 0 missing relative links, 0 unclosed Mermaid blocks. Mermaid CLI is unavailable, so validation was structural plus source review rather than rendered compilation.
- Final status/diff review found only the seventeen scoped UX-2A files listed above. No legacy Unity file, real package, or genuine asset fixture was modified.
- No desktop automation or manual UI verification was performed.

**Next exact step:** the user performs the UX-2A manual acceptance checklist. If accepted, the worktree is ready for a user-authorized commit. Do not implement previews, cross-launch panel persistence, new workspaces, or another UX milestone without approval.

## UX-1B baseline checkpoint

- Verified legacy `ReadFolderContent` shows all immediate directories but filters files only by `.bundle`, `.ab`, and `.assets`; it does not use the legacy header detector for browser visibility.
- Verified Avalonia `DirectoryBrowserService` currently shows every immediate file without classification.
- Verified `UnityFileDetector` already performs bounded header inspection for `UnityFS`/`UnityRaw`/`UnityWeb` bundles and serialized asset files, restores stream position, and disposes path-opened streams.
- UX-1B will reuse that detector behind an explicit recognition result, run bounded inspection off the UI thread, preserve all directories, hide invalid/unreadable/unsupported files, and retain the existing navigation generation guards.
- No source implementation has been changed at this checkpoint. Only this handoff records the verified baseline.

## UX-1B implementation checkpoint

- Added `IUnityFileRecognitionService`, `UnityFileRecognitionService`, and explicit Supported / RecognizedUnsupported / Invalid / Unreadable results around the existing bounded `UnityFileDetector`.
- `DirectoryBrowserService` now enumerates every immediate directory without recursion and includes only header-recognized Bundle or SerializedFile rows.
- File inspection runs inside the existing worker operation with a maximum of four parallel inspections by default. Every file is inspected at most once per scan; cancellation stops the bounded loop, and refresh deliberately rescans current filesystem state without adding a persistent cache or watcher.
- Unreadable/deleted files are hidden and logged with their exception instead of being silently treated as invalid content. Invalid content and recognized-but-unsupported content are hidden without presenting a navigation error.
- The browser exposes a non-error empty state: “No supported Unity asset files found in this folder.”
- UX-1A activation code remains unchanged. Its view and ViewModel regression tests pass alongside new filtering coverage.
- Current checkpoint: build passes with 0 warnings/errors; full suite passes 219/219.

### UX-1B changed files

- `UABS.UnityAssets/IUnityFileRecognitionService.cs`
- `UABS.UnityAssets/UnityFileRecognition.cs`
- `UABS.UnityAssets/UnityFileRecognitionService.cs`
- `UABS/Script/Services/DirectoryBrowserService.cs`
- `UABS/Script/ViewModels/BrowserViewModel.cs`
- `UABS/Script/View/Pages/BrowserView.axaml`
- `UABS.UnityAssets.Tests/UnityFileRecognitionServiceTests.cs`
- `UABS.UnityAssets.Tests/DirectoryBrowserServiceTests.cs`
- `UABS.UnityAssets.Tests/BrowserViewModelTests.cs`
- `UABS.UnityAssets.Tests/BrowserViewInteractionTests.cs`
- `docs/03_design/ux-redesign-foundation.md`
- `docs/04_systems/filesystem-browser.md`
- `docs/05_audit/codex-session-handoff.md`

### UX-1B final verification

- Focused recognition/browser/navigation/interaction tests: passed, 29/29.
- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 219/219, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check`: passed; only informational LF-to-CRLF conversion warnings were emitted.
- Documentation scan: 33 Markdown files, 38 Mermaid blocks, 0 missing relative links, 0 unclosed Mermaid blocks. Mermaid CLI is unavailable, so validation was structural plus source review rather than rendered compilation.
- Final status/diff review found only the thirteen scoped UX-1B files listed above. No legacy Unity file, real package, or genuine asset fixture was modified.
- No desktop automation or manual UI verification was performed.

**Next exact step:** the user performs the UX-1B manual acceptance checklist. If accepted, the worktree is ready for a user-authorized commit. Do not begin another UX milestone without approval.

## UX-1A safe checkpoint

- Confirmed the original defect before editing: DataGrid selection changed `SelectedEntry`, but no row gesture invoked the existing open path.
- `BrowserView` now activates the tapped row. Enter activates the selected row through a handled-event-aware tunnel handler; Space remains unhandled and reserved.
- `BrowserViewModel.ActivateEntryAsync` is the shared route for the view and existing **Open Selected** command. It reuses an in-flight same-item task and cancels a superseded different-item activation.
- Directory activation reuses cancellable, generation-protected navigation and commits history only for the current successful result.
- File activation invalidates pending navigation and delegates to the existing safe `MainWindowViewModel` load path. Existing session-generation protection and failure retention remain unchanged.
- A second pointer tap inside the platform double-tap interval is ignored, preventing a double-click from launching a duplicate operation.
- Added official Avalonia headless xUnit event-level coverage for row taps and routed keyboard input. No desktop application, picker, automation, mouse control, dialog interaction, or screenshot was used.
- The broader four-workspace redesign and header-based file filtering remain unimplemented and unapproved.

### UX-1A modified files

- `UABS/Script/View/Pages/BrowserView.axaml`
- `UABS/Script/View/Pages/BrowserView.axaml.cs`
- `UABS/Script/ViewModels/BrowserViewModel.cs`
- `UABS.UnityAssets.Tests/AvaloniaTestApplication.cs`
- `UABS.UnityAssets.Tests/BrowserViewInteractionTests.cs`
- `UABS.UnityAssets.Tests/BrowserViewModelTests.cs`
- `UABS.UnityAssets.Tests/UABS.UnityAssets.Tests.csproj`
- `docs/03_design/ux-redesign-foundation.md`
- `docs/04_systems/filesystem-browser.md`
- `docs/05_audit/codex-session-handoff.md`

### UX-1A verification

- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- Focused browser tests: passed, 13/13.
- `dotnet test Avalonia.sln --no-build`: passed, 205/205, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- `git diff --check`: passed; Git emitted only informational LF-to-CRLF conversion warnings for two Markdown files.
- Documentation scan: 33 Markdown files, 38 Mermaid blocks, 0 missing relative links, and 0 unclosed Mermaid blocks. Mermaid CLI is unavailable, so validation was structural plus source review rather than rendered compilation.
- Final status/diff review found only the ten scoped UX-1A files listed above; no package data or legacy Unity file changed.

**Next exact step:** the user performs the manual UX-1A acceptance checklist. If accepted, this checkpoint is ready for a user-authorized commit. Do not begin content filtering or the broader redesign without explicit approval.

## Prior UX redesign foundation checkpoint

- Added `docs/03_design/ux-redesign-foundation.md` with the source-verified current/desired matrix, four-workspace proposal, text wireframes, shared inspector boundary, legacy search inventory, incremental sequence, risks, and approval decisions.
- Confirmed the browser activation defect: `BrowserView` binds selection but no row gesture invokes `OpenSelectedCommand`; only the separate **Open Selected** button activates a directory or file.
- Confirmed `DirectoryBrowserService` lists every file without calling the header-based `UnityFileDetector`; content detection currently happens during open/package import.
- Confirmed Search and Direct Dependencies are presentation tabs inside Packages even though their neutral services are reusable. Toolbar Search and Depend both only select the Packages workspace.
- Confirmed dependency candidates retain original `PackageBundleReference.Path` provenance and Avalonia performs no cache copying. Legacy Search/Dependency display copied source files into system cache; that behavior should not be ported.
- Confirmed the current asset grid and metadata block are embedded in `FolderView`. `AssetListViewModel` and session contracts are useful foundations, but there is no reusable inspector or media preview pipeline.
- Confirmed the package memo editor remains inside `PackageEditorViewModel`'s explicit-Save transaction. A shared inspector must not invent standalone memo persistence.
- No application source, package schema, legacy Unity file, or real package was modified. No desktop automation was used.

The previously recommended browser activation slice is now implemented above. Broader workspace, media-decoding, and redesign decisions remain unapproved.

## Completed Milestone 8 work

- Verified the legacy Unity memo readers, writers, inheritance code, models, tests, and genuine legacy test-package representation without modifying the Unity repository.
- Confirmed that only `AssetInfos[].Memo` persists. Missing (`null`/omitted) and explicit empty (`""`) values are distinct.
- Confirmed that legacy “inheritance” is cross-package copy through matching relative `index.json` paths, not directory-parent inheritance.
- Added UI-neutral `IPackageMemoService`/`PackageMemoService` with rich identity, missing/empty/value state, local edits, transactional Safe/Overwrite/Force copy, cancellation, and ambiguity reporting.
- Integrated asset memo editing into the existing package editor. Edits remain in the isolated working document, participate in existing dirty/discard behavior, and reach disk only through explicit staged Save and fresh reopen verification.
- Added package-copy source/mode controls and status reporting. Package, directory, and source rows remain non-editable because the legacy schema has no memo field for them.
- Added focused service/ViewModel tests and a minimized fixture representation derived from genuine read-only legacy data (`hook`, Path ID `-9195836073069537342`, memo `钩爪`).
- Updated architecture, workflow, system, compatibility, risk, roadmap, feature-status, Unity-boundary, project overview, documentation index, and milestone report documents.
- Added the permanent **User-Driven UI Verification and Computer-Control Restrictions** policy to `docs/01_core/ai-agent.md`.

## Compatibility decisions

- Direct editing identifies package path, relative index, normalized source path, asset name, and Path ID without adding serialized fields.
- Duplicate complete identities and ambiguous copy keys are reported rather than resolved by legacy first-match order.
- Source copy matching retains the verified legacy filename-stem/suffix heuristic; underscore-free names use the full stem instead of throwing.
- Safe copies non-whitespace source values only into missing/whitespace targets. Overwrite copies non-whitespace source values regardless of target. Force copies source values including empty or missing.
- Copy output has no provenance because the unversioned schema stores only the resulting asset memo.
- UE-008 uses MVVM in-memory editing plus explicit package Save. No debounce timer, focus-loss save, background queue, autosave, or per-keystroke disk write was introduced.

## Verification

Final verification on 2026-10-17:

- `dotnet build Avalonia.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test Avalonia.sln --no-build`: passed, 196/196, 0 failed, 0 skipped.
- Focused memo/editor/workspace tests: passed, 51/51, 0 failed, 0 skipped.
- Focused zero-Unity-runtime dependency audit: passed, 1/1.
- Release build/test checkpoint: passed, 0 warnings/errors and 196/196 tests.
- The full suite includes the zero-Unity-runtime dependency audit and memo compatibility/persistence coverage.
- `git diff --check -- Avalonia`: passed; Git emitted only an informational CRLF conversion warning for this handoff before its final rewrite.
- Current documentation scan: 33 Markdown files, 37 Mermaid blocks, no missing relative Markdown links, and no unclosed Mermaid blocks. Mermaid CLI is unavailable, so diagram verification was structural plus source review rather than rendered compilation.

An earlier Debug build was blocked by a user-running UABS process holding a Debug DLL. No process was terminated; the lock later cleared and the final Debug build/test succeeded.

## Desktop verification status

No Milestone 8 desktop interaction is claimed. The final continuation explicitly prohibited computer control, screenshots, pickers, and UABS-window interaction. The user will perform the checklist in `docs/05_audit/milestone-08-report.md`.

UE-008 remains `PARTIALLY_REPLACED` solely pending user-driven desktop acceptance of the memo editor and package-copy controls. The implementation and automated persistence behavior are complete for Milestone 8 scope.

## Milestone 8 implementation inventory

The following implementation is committed in `e0878a8`; it is listed here to preserve continuation context, not as an uncommitted audit change.

Implementation and tests:

- `UABS.UnityAssets/Packages/Memos/PackageMemoService.cs`
- `UABS/Script/ViewModels/PackageEditorViewModel.cs`
- `UABS/Script/ViewModels/PackageManagementViewModel.cs`
- `UABS/Script/View/Pages/PackageManagementView.axaml`
- `UABS.UnityAssets.Tests/PackageMemoServiceTests.cs`
- `UABS.UnityAssets.Tests/PackageEditorViewModelTests.cs`
- `UABS.UnityAssets.Tests/MainWindowViewModelTests.cs`
- `UABS.UnityAssets.Tests/TestPaths.cs`
- `UABS.UnityAssets.Tests/UABS.UnityAssets.Tests.csproj`
- `UABS.UnityAssets.Tests/Fixtures/LegacyMemoPackage/`

The earlier UX foundation audit changed documentation only: `docs/03_design/ux-redesign-foundation.md`, the documentation index, project overview, implemented-workflow cross-link, and this handoff. UX-1A now adds the scoped source and test changes listed above.

## Preserved work and safety

- Committed Milestones 7.4, 7.5, and 8 behavior remains intact. UX-1A changes only browser activation source plus its tests and documentation.
- No real package or Unity asset file was a write target.
- `E:\Projects\UABS\UABS` remains read-only and has no working-tree changes from this milestone.
- No branch operation, reset, clean, revert, or commit was performed during this audit.
- No incomplete code is enabled. The current Avalonia worktree is safe to run and safe to commit after the user reviews the diff.

## Remaining broader work and exact next step

1. Complete the user-driven UX-1A desktop checklist; automated verification is complete but is not a claim of observed desktop behavior.
2. Review and approve or revise the remaining decisions in `docs/03_design/ux-redesign-foundation.md`, especially Search scope, reverse dependencies, common-inspector memo behavior, panel-state persistence, and preview technology gates.
3. Keep content classification as a separate task; do not begin it without explicit authorization.
4. The independent Milestone 8 desktop checklist remains pending. If it passes, update UE-008 from `PARTIALLY_REPLACED` to `REPLACED` in its documentation and annotation.
5. Do not begin the broad four-workspace redesign, media decoding, or Milestone 9 without explicit authorization.

Suggested UX-1A commit message after review: `fix(browser): activate filesystem rows safely`
