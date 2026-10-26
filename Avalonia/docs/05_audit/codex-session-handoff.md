# Codex Session Handoff

**Updated:** 2026-10-26

**Current task:** Recovery Checkpoint2.1 — final acceptance gap audit; implemented Windows scenarios pass, remaining evidence/signoff pending. No navigation or issue closure.

### Checkpoint2.1 — next test-only G2/G3/G4 slice (Windows pending)

- Added optional `checkpoint21-gaps` hosted job without changing the passing Smoke/V1/V2 paths. [Slice guide](../../../tools/windows-ui-poc/ACCEPTANCE-GAP-SLICE.md) describes real pre-admission input/defaulting, non-default numeric/type/exclude/flag retention across scope/workspace/catalog changes and metadata-exclusion rejection with exact state/result/Inspector preservation.
- Controls bind by unique actual template watermark evidence while empty, then supported runtime identity, never Edit order or expected values. A headless control-peer regression verifies the existing template/Value provider; actual Windows runtime identity/native picker verification remains pending. No production files, fixtures or result expectations changed.
- G1 Package ordering, G5 indeterminate-coverage feedback, G6 tooltip-hover/HelpText equivalence and full Linux regression disposition remain open. No Checkpoint2.1 closure, additional accessibility implementation or Checkpoint3 authorization. The final audit remains the acceptance authority.

### Checkpoint2.1 — final acceptance gap audit (2026-10-26)

- User confirms [Windows run 40259657455](https://github.com/PapayaModding/UABS/actions/runs/40259657455) at `26f38e68511cd6eaf0d0c23b7ce9fac9228aab70` passed Smoke, expanded V2 and fresh V1, including accessible Name sorting. These are real user-confirmed Windows outcomes; this audit's API read returned Forbidden and no artifacts were independently inspected.
- [Final acceptance audit](v2-recovery-checkpoint-02-1-final-acceptance-gap-audit.md) maps the checklist to passing cases. Mandatory remaining evidence: nontrivial Package order; non-default numeric/type/exclude/flag retention; text before admission/defaulting; metadata-exclusion error feedback; indeterminate-coverage feedback; literal tooltip hover or explicit acceptance of equivalent accessible diagnostic evidence. Existing passing cases are not implementation work to repeat.
- Full Linux VSTest remains failed/aborted, with an unchanged-baseline reproduction and 184 clean focused passes from the implementation task. Not all first-run failures were proven identical to baseline. Hosted UI jobs do not execute full application xUnit. Record regression-risk disposition rather than claiming the full suite is green or automatically waiving failures.
- **Next proposed implementation slice:** test-only G2/G3/G4 retention, pre-admission text/defaulting and existing metadata-exclusion feedback using the same complete Smoke fixtures. Separate partial-evidence preparation for G5; manual Package-header and tooltip evidence or separately reviewed accessibility/equivalence decisions for G1/G6. No implementation is performed/authorized by this audit. Checkpoint2.1 and all issue statuses remain open; no Checkpoint3.

The implementation/build records below are historical, not a claim that the
remaining audit gaps have been exercised.

### Checkpoint2.1 — final verified checkpoint

- Entry baseline `f3a350a`, clean worktree/index. User reports Checkpoint2 works with Sources only / Assets fields / known asset keyword; Mixed/Names fails as expected. This is limited user-reported acceptance, not agent visual observation or full issue closure.
- Root cause: production bundle Search retained V1's initial mixed kinds/Names selectors. Added unambiguous selected-scope defaults only while the four kind/field selectors remain untouched: V2 Sources/Assets; V1 original mixed/Names. Once any selector actually changes, all future scope/workspace/catalog transitions preserve it. Empty/mixed-format scope is not guessed; text and other criteria are never reset.
- SearchWorkspaceViewModel formats backend InvalidRequest into corrective messages with original details retained in ErrorDetails; existing Search error TextBlock exposes a tooltip. Backend SQL/mapping/paging/schema/memos unchanged. Changed only these2 production files, V2BundleSearchWorkspaceTests and3 checkpoint/roadmap/handoff documents. No existing work discarded; index remains empty.
- Focused bundle-workspace18/18 passed(3.58s), including4 new cases and prior sorting/paging/selection/D5 coverage. Existing focused ViewModel22/integration18/headers15/infinite-scroll7/bundle SQL49/raw Search54 passed. Full in-process xUnit792 total:791 passed,0 failed,1 existing hard-link permission skip(66.93s); not VSTest. Final Debug solution including startup0 warnings/errors(1.21s); runner0 warnings/errors(2.46s). Missing import/test-only lifecycle naming and a V1 fixture-order mistake corrected before successful verification; no outstanding build/test failures.
- Schema/design/embedded-DDL equality, zero-Unity project/output scan,7 baseline fixture hashes, protected backend/Assets/Packages/Unity/persistence paths,52 documentation references/fences, whitespace/conflicts and unchanged17 Reported statuses passed. Removed only session-created diagnostic test log. Current worktree verified buildable and ready for scoped review/commit; no desktop observation, original source/real-package writes, staging or commit.
- **Next exact action:** user launches `E:/Projects/UABS/Avalonia/UABS/bin/Debug/net9.0/UABS.exe` and follows [Checkpoint2.1 manual checklist](v2-recovery-checkpoint-02-1-search-defaults.md#limits-and-manual-acceptance): untouched V2 scope defaults Sources/Assets; explicit Names/mixed/error tooltip and retained criteria; V1 defaults; sorting/selection/Refresh unchanged. Issue #1 stays open pending acceptance. Unsupported package-field/kind/exclusion/coverage semantics remain explicit errors. Do not start Checkpoint3/navigation or any other recovery work automatically.

### Checkpoint2 — final verified checkpoint

- Entry baseline `2b345c7`, clean worktree/index. Search application composition now explicitly opts into validated bundle projection; existing programmatic asset-search consumers retain their existing constructor/API route. No user-facing mode switch or default-control changes. Added engine-native source-session adapter to reuse existing cursors/window, no query semantics broadened.
- Modified BundleSearchContracts, V2SearchOperation, SearchWorkspaceViewModel, SearchWorkspaceView.axaml/code-behind and only the Search construction in MainWindowViewModel. Bundle mode hides extra grid columns, maps Name/Package headers globally, shows selectable source metadata and hides non-owned memo UI. V1 and legacy asset-search programmatic route retained. No Assets/Packages/layout/schema/writer changes.
- Unsupported existing field/kind/exclusion/coverage combinations still error; defaults are not silently altered. New production-mode/headless tests14/14 passed(3.31s). Final in-process xUnit788 total,787 passed,0 failed,1 existing hard-link permission skip(66.54s); not VSTest. Final Debug solution/startup build0 warnings/errors(1.16s), initial integration build14.50s, final runner build2.35s. Corrected one test-only nonexistent diagnostic-property reference before successful builds; no production defect found. [Checkpoint2 report](v2-recovery-checkpoint-02-search-ui.md) records valid/unsupported manual recipes and exact coverage.
- P1-D1–P1-D15 and17 unresolved issues remain; no stage/commit or desktop control. Shared draft coordinator and memo persistence untouched; selected bundle has no owned memo target. Go to Assets deferred.
- Existing focused Search regressions passed: integration18, headers15, infinite scrolling7, bundle SQL49 and raw Search54. Initial full run found2 historical production-composition assumptions expecting individual Search assets/editable asset memos. Adapted V2MemoIntegrationTests to retain all asset-memo assertions through the unchanged programmatic asset route plus assert production bundle isolation/shared-draft preservation; adapted V2BrowseIntegrationTests to assert source identity and retained real asset Inspector editability. Focused memo23 and browse26 passed; final full rerun passed above. No persistence/draft code changed.
- Final schema/design/embedded-DDL equality, zero-Unity project/output audit, seven baseline fixture hashes, protected-path/scope review,49 changed-document references,209 existing-document references, balanced fences, whitespace/conflicts and unchanged17 Reported statuses passed. No new Mermaid; existing20 blocks preserved, not rendered. Index empty;6 production files,3 test files,3 documentation files changed/added. Removed only session-created test diagnostic log. Worktree verified buildable and ready for user review/commit; no real package/source/Unity edits or desktop observation.
- **Next exact action:** manually launch `E:/Projects/UABS/Avalonia/UABS/bin/Debug/net9.0/UABS.exe` and follow the Checkpoint2 report checklist using disposable V2 packages. For a supported query use Sources only, Assets fields and a contained asset-name keyword; defaults/Names/package fields/mixed kinds and unresolved metadata exclusions intentionally error. Confirm Name/Package/global paging/exact Inspector plus V1 behavior before closing issue #1. Further mapping decisions, Go to Assets/navigation and other recovery checkpoints require separate authorization. Do not automatically begin them.

### Checkpoint1.1 — verified compatibility scope; explicit unsupported cases retained

- Entry baseline `9094328`, clean worktree/index. Reviewed CP1 code/tests, V1/V2 candidate filters, source completeness publication and UI field groups. [Pre-implementation matrix](v2-recovery-checkpoint-01-1-search-compatibility.md) records supported plans versus blocked ambiguities.
- Implemented positive source path/index-directory/CAB/dependency matching, asset-only exclusion-field inference without mutating caller request, and per-registration eligible-bundle asset/name coverage captured under the admitted revision. Complete bundles in a package partial for unrelated ineligible sources can proceed; uncertain eligible bundles/requested names still reject whole scope explicitly. Existing V1/asset paths and publication/schema unchanged.
- Modified BundleSearchContracts/BundleSearchQuery/SearchSession/V2SearchService and extended disposable V2BundleSearchTests with29 added cases, no old cases removed. Focused bundle SQL49/49 passed(2.83s); existing Search54/54 freshly passed on recovery(18.52s; pre-interruption54/54 also passed18.04s). Recovered final full in-process xUnit completion:774 total,773 passed,0 failed,1 existing CreateHardLink permission skip(67.98s), **not VSTest**. Library/test DLL hashes in the recovered runner match current repository Debug outputs; no source changes after that run, so full suite not needlessly repeated.
- Final recovered Debug solution build includes actual UABS startup:0 warnings/errors(1.34s); pre-interruption final Debug solution build also passed0 warnings/errors(1.55s), runner/test build0 warnings/errors(16.10s), library0 warnings/errors(4.38s). A sandbox-only recovery build failed on output-directory/project-reference access; authorized normal build passed without source correction. Prior final safety command was not executed because Auto-review hit a usage limit; recovered authorized checks completed without security-setting changes or bypasses.
- Final diff review found no scope violations or source defects requiring recovery edits. Protected UI/navigation/V1/Unity/fixture/schema paths unchanged; embedded canonical DDL equality and zero-Unity-runtime project/output audit passed. Documentation links/anchors/fences, whitespace/conflicts and17 unchanged Reported issue rows checked. Index empty;4 backend files,1 test file,2 tracked docs modified plus new compatibility report. Removed only session-created `Avalonia/uabs_log.txt` from existing regression failure-path tests; no user data or pre-existing source removed. No desktop control/process termination/staging/commit.
- **Next exact action:** user reviews [remaining compatibility decisions](v2-recovery-checkpoint-01-1-search-compatibility.md#remaining-user-visible-decisions) before any unsupported mapping expansion or UI integration: package-field/result-kind membership, source/mixed exclusion fields and presentation of indeterminate coverage. Current bounded backend is verified buildable and existing app safe to run; those unsupported cases remain intentional errors, not unfinished enabled code. Do not start Checkpoint2, navigation, Packages changes, schema work or V1 retirement automatically. Issue #1 remains unresolved pending authorized UI integration and manual acceptance.

### Checkpoint1 — verified bounded backend; contract clarification required

- Entry baseline `551e1de`, clean worktree/index. Authorized backend only, not Search UI/navigation/Packages/schema/V1/memos. New BundleSearchContracts/BundleSearchQuery reuse existing search sessions, federation, budgets and keysets through format-specific methods; existing public asset SearchRequest/CreateSession untouched in meaning. Edits also in SearchSession, SearchQuery and V2SearchService. No stage/commit.
- Baseline Debug solution/startup build passed0 warnings/errors(37.21s). Final Debug solution/startup build passed0 warnings/errors(1.56s); final external verification runner build passed0 warnings/errors(10.91s). Focused new bundle SQL tests20/20 passed(1.82s); existing V2 Search regressions54/54 passed(18.09s). One final full in-process xUnit regression:745 total,744 passed,0 failed,1 existing permission-related skip(66.05s); **not VSTest**. Initial sandboxed fixture run failed on temp-file permissions; authorized synthetic-fixture rerun passed. Transient new test params-construction compile errors corrected before successful verification. Current worktree is verified buildable; backend safe for isolated callers, not yet integrated UI.
- Explicit bounded scope: positive SourceName/AssetName/Memo fields, only SourceFile projection; unresolved other field/result-kind mappings return InvalidRequest. Exclusions require caller-specified AssetName/Memo scope; source metadata exclusion policy not invented. Exclusions on partial packages rejected until unknown-coverage policy approved; partial positives retain existing diagnostic evidence. No UI control translation implemented.
- Separate bundle result has immutable package/generation/source/revision identity and source details, no asset memo/type/Path-ID properties. New original-file navigation remains out of scope. P1-D1–P1-D15 and17 unresolved issue statuses preserved.
- Added [checkpoint implementation report](v2-recovery-checkpoint-01-bundle-search.md), disposable V2BundleSearchTests and roadmap progress only. Final Git/whitespace/conflict, protected-path, zero-Unity-runtime project/output, embedded canonical DDL and documentation checks passed;48 local links/anchors and17 Reported rows checked. No schema/V1/UI/memo/real-package/Unity edits; no desktop control/process termination/staging/commit. Removed only session-created `Avalonia/uabs_log.txt` emitted by existing regression failure-path tests; no pre-existing file removed.
- **Next exact action:** user resolves unsupported field/result-kind compatibility, UI exclusion-field mapping and incomplete-package exclusion policy before expanding/integrating this backend. Keep new API SourceFile-only and positive SourceName/AssetName/Memo subset explicit; do not silently translate current controls. Review/commit is user-owned. Do not start navigation, Packages changes or V1 retirement. This is a bounded partial checkpoint, not full recovery issue #1 completion.

### Phase 1D Approved Decisions P1-D10–P1-D15

- Baseline `c14362c`; clean index/worktree at entry. Updated only roadmap, Phase1D audit and this handoff. [Authoritative approval record](v2-ui-workflow-recovery-roadmap.md#phase-1d-approved-decisions-p1-d10p1-d15) completes current P1-D1–P1-D15 design approval set; six S1/S2/S3/N1/N2/N3 questions decided. All17 issues remain Reported/unresolved; no implementation authorization.
- **P1-D10 / S1:** any contained asset matching active exclusion vetoes entire bundle before emission/global order/pages, regardless of positive matches. Candidate-only exclusion recommendation superseded; term parsing/comparison preserved, exact exclusion-field scope not invented.
- **P1-D11 / S2:** separate children can satisfy separate mandatory conditions; AND mandatory filters, preserve keyword include OR, no same-asset-only/unconditional-OR shortcut. P1-D10 remains independent.
- **P1-D12 / S3:** preserve supported Search fields, keyword/exact/substring/OR, Memo-only/type/Class ID/signed Path ID/result-kind/package-scope controls; asset fields are child predicates, not bundle values. Any result-kind incompatible with eligible bundle output is an explicit compatibility conflict, not permission for redesign/evidence-toggle remapping.
- **P1-D13 / N1:** exact original Unity source in filesystem browsing, directory Back/Forward/Up intact. Indexed metadata primary/fallback recommendation superseded. Missing/invalid/ambiguous originals fail clearly with existing state retained; no same-name retargeting or arbitrary unrelated asset selection.
- **P1-D14 / N2:** stale result may navigate after exact registration/package/source/generation/revision/original-file revalidation; invalid targets preserve Assets, no blanket stale ban, no implicit Search rerun/D5 change.
- **P1-D15 / N3:** nonblocking navigation preserves pending/failed asset drafts and errors; no save-success gate/modal workflow/discard/false Saved state. Any retention gap is an implementation dependency, not claimed support.
- **Remaining technical dependencies:** exclusion-field/source-metadata scope; result-kind compatibility; incomplete-index negative evidence; exact original locator/byte verification; transactional directory/native-load publication, cancellation and owner revalidation; native child memo provenance and draft retention per transition. P1-D4 independent indexed browsing remains supported where available, but never as Go to Assets fallback. Genuine bundle memos remain separate; approved V2 D1–D8/schema/D5 unchanged.
- **Next exact action:** obtain a bounded Phase1 implementation request; encode approvals in CP1 contracts while explicitly resolving technical conflicts, then CP2/3 Search and CP4/5 original-source filesystem navigation. No automatic implementation, schema changes or V1 retirement. Historical recommendations below are not current authority where superseded.
- Validation passed: all15 decision IDs in each changed document,15 roadmap approval headings, approved veto/original-source routing,61 local references/anchors, balanced fences, whitespace/conflicts, unchanged17 Reported rows and only three intended documentation paths. Index empty. No builds/tests/desktop/package/source/Unity changes, staging or commits; no fresh executable verification claim.

### Phase1D — historical focused audit; recommendations superseded by approvals above

- Baseline `f6183cf`; clean index/worktree at entry. Created [Phase1D audit](v2-recovery-phase-01d-search-navigation-semantics.md); updated roadmap and this handoff only. P1-D1–P1-D9, approved V2 D1–D8/schema/D5 and all17 Reported statuses unchanged.
- Confirmed current V1/V2 comma OR, literal OrdinalIgnoreCase exact/substring, candidate exclusions and asset-only gates. One include box cannot express independently required name AND memo groups. Package filter affects visibility only; Select All/Clear affect all scope entries. Current result-kind boxes emit different candidates and need explicit bundle-evidence mapping, not silent removal.
- Historical predicate recommended candidate exclusions/no bundle veto; **superseded by P1-D10**. Independent mandatory child witnesses now approved conceptually under P1-D11. Original code evidence unchanged; exact identity lookup stays indivisible.
- Historical navigation recommendation used indexed destination; **superseded by P1-D13 original Unity filesystem navigation, no metadata fallback**. Findings still apply: action absent, directory history separate, workspace switch doesn't await flush success or auto-retry failed drafts. Exact revalidation and retained nonblocking drafts now approved under P1-D14/P1-D15, not implemented by the record.
- Historical next action requested six approvals; all are now decided as above. Technical compatibility details and separate implementation request remain. CP4/5 must follow original-source filesystem route, not old indexed proposal; bundle memos/other inventory policies remain separately scoped.
- Verification passed: three changed documentation paths only;58 local references/anchors, balanced Markdown fences, whitespace/conflicts and17 Reported rows. Approved decision sections remain unchanged; index empty. No builds/tests/package access/desktop automation, source/schema/Unity edits, staging or commits. Existing executable buildability is not newly verified by this documentation task.

### Phase 1C Approved Decisions P1-D6–P1-D9

- Baseline `dc75c9e`; clean index/worktree at entry. Updated roadmap, Phase1B audit and this handoff only. [Authoritative approvals](v2-ui-workflow-recovery-roadmap.md#phase-1c-approved-decisions-p1-d6p1-d9) apply with P1-D1–P1-D5; all nine are binding design direction, not implementation authorization. All17 issues remain Reported/unresolved.
- **P1-D6 / B1 decided:** bundle-level Search accepts confirmed actual Unity bundles only, based on reliable existing source-kind/indexing evidence. No filename/extension/CAB/name inference or silent standalone/resource inclusion. Insufficient evidence is a dependency, not permission to invent classification/storage.
- **P1-D7 / B2 principle decided:** different children can collectively satisfy conditions (name `hero`, type `Texture2D`, memo `Attack` may come from separate assets). Phase1B same-asset-only recommendation and its single-child SQL sketch are explicitly superseded, not active requirements. Preserve bundle-name matching and aggregation before global sort/pages; no unconditional OR or control removal.
- **P1-D8 / B3 decided:** exact indexed occurrence navigation; directory Back/Forward/Up remain unchanged. Only necessary additional source context, no replacement history or command redefinition.
- **P1-D9 / B4 decided:** bundle Search has exactly Name and Package, meaningful supported global sorts; no Source/Path ID/Class ID/Kind/Memo columns. Source path/technical identity remain in Inspector and immutable internal context. Other workspaces are not redesigned.
- **Remaining technical dependencies:** include OR groups; candidate excludes/negation scope; Memo-only; exact/substring; type/Class ID/Path ID; bundle-name/filter composition; empty/incomplete sets; identity-bound predicates; source selection/missing files/unload/replacement/stale results/navigation context and draft policies; reliable classification and display/sort/cursor mapping. Non-bundle Packages inventory is not separately decided. Genuine bundle memos remain a separate design milestone; approved V2 D1–D8/schema/D5 are unchanged.
- **Next exact action:** obtain the remaining explicit Boolean/control and navigation implementation contracts, then a bounded implementation request. Do not implement from this approval record, start bundle memo storage or retire V1. Historical checkpoints below preserve prior evidence/proposals; conflicting recommendations and old approval requests are superseded by P1-D6–P1-D9.
- Validation passed: all nine decision IDs in each updated document; exact two-header contract and superseded same-asset proposals reviewed;57 local links/anchors, balanced fences, all17 Reported statuses, Git whitespace/conflicts and three-file-only scope checked. Index empty. No builds/tests/desktop control, application/schema/package/Unity changes, staging or commits; no new executable verification claim.

### Phase1B — historical contract audit; proposals superseded where noted

- Baseline `efe97e0`, clean worktree/index at entry. Created [Phase1B implementation contract](v2-recovery-phase-01b-implementation-contract.md); updated roadmap and this handoff only. Existing P1-D1–P1-D5 remain binding; all17 issues remain Reported. No source/XAML/tests/schema/package/legacy changes, builds, regression runs or desktop control.
- Confirmed: source identity/storage supports schema-free source projection; current SourceFile Search does not aggregate asset hits, and indexed Assets pages all package assets without exact selected-source scope. Existing directory Back/Forward is not source/workspace history. Asset-only filters/result-kind controls cannot silently become source semantics.
- Historical query proposal used one outer source row with correlated same-child predicates. **Same-child requirement superseded by P1-D7**; cross-asset Boolean details remain to specify. Retain600/128 bounds, lifecycle checks, Unicode semantics and D5. Existing asset fast-path timings do not establish source-query performance; no borrowed bundle memo values.
- Historical next action requested B1–B4 approvals; their product directions are now decided by P1-D6–P1-D9 above. Detailed contracts and separate implementation authorization remain required. Six checkpoints are still proposals, not completed implementation.
- Verification scope: local documentation links/anchors, Markdown/Mermaid fence structure and source references; unchanged17 Reported rows; staged/unstaged whitespace/conflict and protected-path review. No new buildability/test claim: executable source is unchanged from baseline, not rebuilt in this documentation task. Index remains untouched; three documentation paths are the only intended changes.

### Phase 1 Approved Decisions

**Historical P1-D1–P1-D5 recording checkpoint:** those five approvals remain binding and are now supplemented by P1-D6–P1-D9 above. Earlier open questions below are historical where the new approvals decide them.

- Recorded explicit user approval in the [roadmap's authoritative decision section](v2-ui-workflow-recovery-roadmap.md#phase-1-approved-decisions) and reconciled the [Phase1A audit](v2-recovery-phase-01a-bundle-audit.md#phase-1-approved-decisions). **Decided:** P1-D1 one immutable indexed source occurrence/no label/hash merging; P1-D2 source-name OR child-name/memo matching with approved V2 asset semantics, aggregated before global sort/pages; P1-D3 genuine bundle-memo design in a separate milestone, existing asset memos unchanged/no fabricated values; P1-D4 exact Search → Assets navigation preserving history/Back/Forward and supported offline metadata; P1-D5 deterministic global Name/existing meaningful source fields, bundle Memo sorting deferred.
- This approval record authorizes **no implementation phase**, source/UI/schema/trigger/package/memo/navigation/V1/test/build changes. All17 recovery issue statuses remain Reported; #1/#2/#3/#7 are not implemented or resolved. Existing V2 D1–D8 and D5 Refresh are not amended by the distinct P1 IDs.
- Remaining dependencies: standalone/failed-source inclusion, mixed-result/Path ID/Class/type/Memo-only control mapping, exact source-query predicates/coverage, typed source/cursor/tie mappings, native ordinal/package provenance and action/return-context/missing-source UX. Independent bundle-memo identity/storage/transactions/conflicts/UI/publication requires a separate approved design/milestone; no schema/triggers/storage work now.
- Worktree/index clean at entry; only roadmap/audit/handoff documentation updated. Verified three decision sections with all five IDs,97 local links/anchors, balanced fences, unchanged17 Reported issue rows, whitespace/conflicts and Git staged/unstaged checks. Index remains empty; protected code/UI/schema/tests/package/Unity paths unchanged. No builds/tests or desktop control. Historical source findings/checkpoints below remain evidence, not current open product questions where superseded by P1-D1–P1-D5.
- **Next exact action:** obtain a separately scoped implementation request or detailed design task for the remaining dependencies. Do not start Phase1 automatically, implement bundle memo storage, or retire V1.

### Phase1A — audit checkpoint; implementation approval required

- Baseline `f815443`; worktree/index clean at entry. Created [bundle identity/memo audit](v2-recovery-phase-01a-bundle-audit.md), referenced it in the recovery roadmap, and updated this handoff only. No source, tests, XAML, schema, packages or legacy Unity edits; no builds/tests/desktop control/Git mutations.
- Legacy Search accepts bundle index items via bundle/asset-name/asset-memo predicates and opens copied bundle entries into Assets. Its memo ownership is individual assets. Filename cache collisions, field-local excludes/exact-mode quirks and per-package view replacement are verified source limitations, not recommended behavior to copy blindly. No distinct literal Go to Assets command or equivalent Unity Indexed Assets DataGrid established by inspected paths.
- Current V2 independently returns Package/SourceFile/Asset candidates; source results do not aggregate asset hits. Packages Indexed Assets and indexed Assets share an individual-asset grid; technical columns/no Memo are confirmed. Source metadata supports a schema-free typed source list/query, but grouping must precede global paging/federation. Search → Assets is still absent by design. The live Assets entry lacks original ordinals/package binding; exact source navigation alone does not guarantee unambiguous native memo selection.
- Pending decisions: source-as-bundle identity including standalone/failed sources and cross-package duplicates; match/exclusion/exact/Memo-only aggregation and mixed-result controls; bundle-owned memo storage (currently absent); action/history/native provenance; source-level sorting/pagination and scoped Assets Memo mapping. Preserve D1–D8/schema/D5, all-asset index and exact asset memos. All17 issues remain Reported/unresolved.
- Documentation validation: three documents/local source-reference targets, balanced Markdown fences, static syntax review of the single new existing-schema Mermaid diagram (not rendered), whitespace/conflicts and all17 unchanged Reported statuses checked. Protected application/test/schema/Unity paths unchanged; index empty, only this handoff/roadmap modified and the new audit untracked. Historical Gate5G.1 build/test evidence below is not rerun or claimed as Phase1A runtime verification. No automatic Phase1 implementation, V1 retirement or bundle-memo schema amendment.
- **Next exact action:** user reviews the eight-row decision matrix and approves a bounded recovery slice/contracts before source edits. No implementation has been started.

### V2 recovery roadmap — documentation checkpoint

- Baseline `4bb4bc8` (committed Gate5G.1); worktree/index clean at entry. Created [v2-ui-workflow-recovery-roadmap.md](v2-ui-workflow-recovery-roadmap.md) with all17 reported issues, intended behavior, scope, proposed priority/dependencies/acceptance criteria, status table, five proposed phases and open decisions. All17 remain **Reported / Not yet resolved under this roadmap**; no fresh desktop observations or fixes claimed.
- Added concise protected-behavior guidance/reference to `docs/01_core/ai-agent.md` and a documentation-index entry. Only these four documentation files are in scope. Preserved historical Gate5G.1 evidence below; its manual Debug acceptance remains pending.
- Bundle/source identity and aggregation, bundle memo storage, appropriate global Name/Memo semantics, Search → Assets interaction, V1 parity decisions and system-icon mode require further scoped decisions/inspection. Proposed V2-only/no-migration direction is recorded, not immediate V1 deletion permission. Existing schema/D5/drafts and useful workflows stay protected.
- Verified four documentation files/93 local links and anchors, balanced fences, whitespace/conflicts, all17 issue headings and structured fields, and all17 Reported status rows. Git whitespace checks pass; three modified tracked docs and one new untracked roadmap, empty index. No source/XAML/package/Unity changes, build/test runs, desktop control, staging, commits or other Git mutations. Historical buildability/tests below were not rerun for this docs-only task.
- **Next exact action:** review the roadmap/decisions, then explicitly authorize a bounded issue investigation or implementation slice. Do not automatically start Phase1, shared-grid refactoring or V1 retirement. Update issue evidence and separate automated/manual acceptance as work proceeds.

### Gate5G.1 — final verified checkpoint

- Continued existing backend edits, not restarted. Search-only View/VM/operation and tests changed; Assets columns, schema, writer and package workflows untouched. Supported Package/Source/Path ID and Assets-only raw Name headers route global sessions; Kind/Memo/mixed Name disabled with explanations. V1 retains native header sorting. Native Fluent pseudo-classes show only published global order, including pointer transitions; no new templates, columns or layouts.
- Latest-request-wins sort queue cancels old fetches, skips intermediate requests and compares against the original frozen query/revision vector. Exact selected assets are re-probed, metadata selected from the same query retained, shared draft/Inspector preserved. Windows remain600 rows/128 checkpoints; catalog changes require explicit Refresh.
- Fixed actual-grid selection coercion found by the new tests: DataGrid defers its reset to measure, so drain that layout under a scoped header guard and restore the retained row highlight without flushing/replacing its draft. Native V1 sort descriptions cannot leak into V2. A newly executed mixed-result query resets an ineligible raw AssetName order to the unchanged PackageName ascending default; no invented mixed Name/Kind/Memo semantics.
- **Verified buildable:** final Debug solution including startup0 warnings/errors (53.26s); runner0 warnings/errors (4.41s). New headless sorting15/15 (9.72s), existing integration18/18, viewport7/7, SearchVM22/22: **62 focused tests passed**. Final standard in-process xUnit **724 passed,1 existing physical-hardlink permission skip,0 failed** (725 cases,74.45s), run once on final source. Not VSTest. No current known source/test failure.
- Canonical DDL matches approved design exactly; targeted Unity project/dependency/output audit, genuine fixture hashes, changed/new source whitespace/conflict checks and Git staged/unstaged whitespace checks pass. Documentation16 files/196 local links/anchors/balanced fences;20 unchanged Mermaid blocks counted, no rendering or new diagrams. Protected legacy Unity, V1 core, V2 schema/writers/engine, fixtures and Assets/Packages views unchanged. Ten scoped files (four production Search files, three tests, three docs); index empty, new test remains untracked and must be included in any user-authorized commit.
- **Limitations:** general/nondefault global orders can be slower; no new OWL/large-scale sorting latency claim. Cancel after retiring the old session can require explicit Search to resume paging while retaining rows/selection/draft. Desktop visual behavior remains user acceptance, not an agent observation. No new Unity-exclusive decisions.
- **Next exact action:** user-operated Debug checklist in the [Gate5G sorting follow-up](package-format-v2-gate-05g-search-paging-performance.md#gate5g1--supported-global-header-sorting-2026-10-23), then review the entire unstaged diff/new test and decide whether to commit `fix(search): restore supported V2 global header sorting`. No new milestone, Assets-column correction, schema/persistence change, V1 retirement, desktop automation or Git mutations. Worktree is buildable and ready for scoped review/commit, with manual acceptance pending.

### Historical Gate5G.1 — pre-wiring checkpoint (superseded above)

- Baseline `1015b57` (committed Gate5G). Recovered the two existing unstaged source edits, with empty index and no untracked repository files. Preserved them unchanged during this recovery. No Assets-page column edits, desktop control, real-package/source writes or Git mutations.
- Confirmed cause: Gate5F intentionally binds Search `CanUserSortColumns` to `CanSortResultsLocally`, disabling V2 native sorting to protect the partial 600-row window. V1 native sorting remains enabled. Header routing is **not yet implemented**, so the reported interaction remains unfixed.
- Partial files: `UABS/Script/Services/V2SearchOperation.cs` adds an exact admitted-identity membership probe and common page reader; `UABS/Script/ViewModels/SearchWorkspaceViewModel.cs` adds dormant global-sort execution, frozen criteria, revision-vector comparison and displayed-order properties. No View, test, schema, writer or Assets-page changes exist yet.
- Supported engine mappings: Package → PackageName, Source → SourceName, Path ID → PathId; Name → raw AssetName only for executed Assets-only criteria. Kind/Memo and mixed-result Name lack equivalent approved global semantics. Do not substitute local-window ordering or alter columns/defaults. Other sorts use the general engine path and may be slower than Gate5G's default optimized shape.
- **Verified buildable:** current Debug solution including actual startup application passed with zero warnings/errors (1m41.53s); external in-process runner rebuilt Debug with zero warnings/errors (4.34s). Existing focused in-process xUnit tests: V2SearchIntegrationTests18/18, V2SearchInfiniteScrollTests7/7, SearchWorkspaceViewModelTests22/22. These are regression checks, **not new header-sort acceptance tests**; VSTest transport and manual UI behavior not verified.
- Known unfinished risks: `CanChangeV2Sort` currently rejects rapid changes during admission/initial fetch; metadata-row selection is currently cleared by the partial sort path rather than retained/proven. Resolve these before enabling header clicks. The asset probe and new sort path have no dedicated tests yet. Do not advertise the dormant API as complete.
- Final checkpoint regression: standard **in-process xUnit709 passed,1 existing permission-related skip,0 failed** (710 cases,69.45s); not VSTest. No source edits were made during this recovery. Documentation checker passed16 documents/194 local links/anchors and balanced fences;20 pre-existing Mermaid blocks counted, not rendered. No new diagrams. Targeted project/output Unity-reference scan and source conflict-marker scan found no matches. Git whitespace checks passed; only the two recovered source files and these two audit documents differ from baseline, with index empty. Existing regression success does not close the missing header-sort acceptance coverage.
- **Next exact steps:** review/fix the above partial lifecycle/selection behavior; wire only supported Search headers to global sessions with handled V2 sorting (never local SortDescriptions), preserve V1 native events, and synchronize accurate native direction indicators across pointer/template updates. Keep unsupported headers explicit. Add actual headless clicks/toggles, >600/multi-package/NULL/Unicode order, paging, rapid sort/cancellation, selection/draft, D5 and V1 tests. Then rebuild Debug, run focused/full regressions and safety checks, and finish the Gate5G report. Do not begin the separate Assets-page audit or retire V1.

Historical Gate5G results below apply to the committed baseline; current Gate5G.1 results are recorded at the top.

### Gate5G — final verified optimization checkpoint

- Baseline clean committed `b08a8f6` (Gate5F); index empty. Normal scoped Avalonia permission granted. No prior changes, real package/source/Unity writes, desktop control or Git mutations.
- Confirmed costs: production whole-file hashes twice per page and repeated managed Unicode conversion/query/sort work, not UI publication. Implemented only safe existing-reader proof and existing-index plan changes; Gate5F600-row/128-checkpoint/selection/draft/D5 behavior stays intact.
- Instrumented baseline Debug solution/startup/runner built0 warnings/errors. Three-sample10k/100k/1M production-model profiles all completed in external gate5g-baseline-{count}.jsonl; do not repeat. Warm medians35.87/249.44/2534.25ms;1M allocation~505MB. At100k one page SQL207ms, hashes33ms, publication0.29ms;100k predicates/200k comparisons imply conversion/query work, not collection work. [Gate5G report](package-format-v2-gate-05g-search-paging-performance.md) records exact methodology/limits. Median uses floor(count/2) for odd sample counts, not rounded integer conversion.
- Implemented pinned existing-reader immutable admission tokens: same-connection data-version inside manifest-pinned snapshot, fresh identity/catalog/DELETE journal and physical/epoch/lifetime checks before/after pages. New capture after external commit requires full validation; old proof never renews. No extra persistent connection or idle transaction. Non-Windows/unpinned fallback hashes remain. Schema/memo-writer behavior unchanged.
- Indexed ASCII/uncovered partitions use only the canonical name B-tree with authoritative matching and complete keyset/global merge. Only asset-only default PackageName sort with exact constant source path/index directory and compatible profile qualifies. Selective Path ID/identity/class/type/Memo-only and other sorts/mixed-source profiles retain general plans. No NOCASE/LIKE/FTS substitution, schema, full-match cache or hidden omission. Unicode-heavy/many-source/duplicate-heavy shapes can remain slow; do not extrapolate single-source gains to OWL.
- **Verified buildable:** final sequential Debug solution/startup0 warnings/errors(23.68s), runner0 warnings/errors(23.70s). New focused20/20(6.03s); existing engine54/54, Search18/18, headless viewport7/7 passed focused earlier. Final standard in-process xUnit **709 passed,1 existing physical-hardlink permission skip,0 failed** (710 cases,65.63s). Not VSTest; no transport/permission bypass. Earlier705-case pass predates final selective-plan safeguard; final rerun covers current source. No known unfinished/failing source.
- Completed equivalent three-sample10k/100k/1M baseline/optimized logs once each: warm35.87→11.27 /249.44→28.86 /2534.25→188.27ms; first74.29→51.57 /612.71→413.33 /5601.17→3360.32ms. Warm allocation~5.7/51/505MB→0.54MB.1M backward2316.54→178.82ms. External rejection7.11/21.77/178.99→6.40/6.38/6.08ms; first post-external Refresh can still pay two full validations (~6.79s at1M). Final journal/eligibility guards preserve broad SQL; current-source10k sanity warm13.82/deep11.75/backward11.80ms. Raw gate5g-{baseline|optimized}-{size}.jsonl and final-safety/final-verified logs retained externally; do not repeat blindly. All owned fixture scopes completed/disposed, no active test/benchmark process remains.
- Canonical DDL, genuine fixture hashes, zero-Unity, protected legacy/V1/writer/schema/builder/extractor/design/XAML/fixtures and Git/new-file integrity checks pass.16 docs/193 local links/anchors/fences;20 Mermaid blocks syntax-reviewed, not rendered.13 scoped files only; index empty. Report/index complete. No real data, desktop control/screenshots, Git mutations or user-process termination.
- **Next exact action:** user-operated current Debug checklist in the Gate5G report, then review/commit this optimization checkpoint. Launch E:/Projects/UABS/Avalonia/UABS/bin/Debug/net9.0/UABS.exe with disposable packages. Proposed warm medians20/50/300ms are reference recommendations, not approved p95/real-corpus guarantees. Broader many-source/Unicode-heavy sort acceleration requires separately measured/proven work; do not silently change D5 or schema. No V1 retirement, structural editor, Gate6, memo-save changes or deferred shutdown/crash recovery. No new Unity-exclusive decisions.

### Gate5F — final verified functional checkpoint; acceptance limitations

- Baseline clean committed `7558935`; Gate5E is committed. Index empty; no previous work discarded. Scoped Avalonia access authorized; no real package/source/Unity writes, desktop automation or Git mutations.
- Implemented native DataGrid over a bounded sliding window:100-row internal pages,600 published/700 temporary VM rows,128 compacted sparse/local keyset checkpoints and20-row automatic edge prefetch. Backward replay stays in the same immutable session; no fake exact count, unbounded materialized prefix or Search restart. One selected identity/Inspector/draft remains separately retained outside eviction. Deep missing checkpoints can require multiple serial queries; scrollbar represents current window, not global random access.
- Approved D5 explicitly forbids silent revision renewal/reordering after memo commits. Preserve exact row deltas, stale windows and explicit Refresh; document automatic-refresh proposal for separate approval, do not change D5/schema.
- ViewModel coalesces/serializes fetches, cancels obsolete direction/new Search/unload/navigation work, rejects late completions and retains failed boundaries for Retry. Stable collection/selection survive window shifts; exact verified memo deltas include an off-window selection. Next UI removed; V2 local fragment sorting disabled, V1 unchanged. Current-source late-session anchor invalidation and O(1) format-sort eligibility are verified.
- Actual bidirectional viewport tests pass, including partial-row pixel anchors within1DIP, selection leaving/returning, light/dark and two window heights. Native DataGrid keeps hidden recycled row visuals; lookup must use visible geometry. RangeBase.Value moves only the thumb; fallback uses installed11.3.3 rows-presenter DIP scroll gestures after selection/publication layout. Coalesced no-ops must not overwrite the active ViewportLoadTask. Temporary traces removed; do not restart this completed diagnosis or weaken assertions.
- **Verified buildable:** final sequential Debug solution/startup/tests/runner0 warnings/errors (solution32.96s,6.72s after test-only correction). Controller7/7, headless viewport7/7(2.90s), Search18/18(2.87s), memo integration23/23 pass. Final standard in-process xUnit **689 passed,1 existing physical-hardlink permission skip,0 failed** (690 cases,63.29s). Not VSTest; transport not retried/bypassed. Initial full run exposed an old test manually fetching while a live viewport stayed at top and correctly canceled; same page/order assertions now run before attaching its view, actual viewport tests remain. No known failing/incomplete source.
- Disposable10k/100k/1M calibration completed once per size; raw external gate5f-scroll-{10000|100000|1000000}.jsonl retained. First-result model124.52/640.60/5581.66ms; forward medians33.97/228.71/2269.73ms; backward29.69/225.46/2281.75ms. Peak published600/temporary700 rows, checkpoints82/128/14, engine worker peak1 with0 active after operations.1M sampled12 forward pages, not whole corpus. Process peak71/77/258MB includes fixture/runtime/native work, not isolated UI cache.1M page allocation~505MB and2.27s latency are significant: **do not claim smooth million-row acceptance**. Existing full-file before/after evidence hashes and managed query/collation remain package-scale; no engine validation/semantics changed. Details/limits: [Gate5F report](package-format-v2-gate-05f-search-ux.md).
- Canonical DDL, genuine fixture SHA256, zero-Unity references/output, protected legacy/V1 core/V2 engine/writer/schema/design/fixtures, Git staged/unstaged whitespace and12-file new/modified conflict/control/whitespace checks pass.15 docs/186 links/anchors/fences pass;19 Mermaid blocks reviewed for syntax, not rendered.12 task files only; index empty; report/UI/index complete. No real-data/desktop/Git mutations or active benchmark/test process remains.
- **Next exact action:** user-operated current Debug acceptance from the report, then separately scope profiling/optimization of per-page evidence/query costs before full large-corpus smoothness acceptance. Launch E:/Projects/UABS/Avalonia/UABS/bin/Debug/net9.0/UABS.exe with disposable packages. Functional checkpoint is safe to review/commit with performance caveat, not fully accepted Gate5F. Preserve all edits; do not restart or blindly rerun completed benchmarks. No Gate6, V1 retirement, structural editor, migration or shutdown/crash-recovery work; no new Unity-exclusive decisions.

### Gate5E — final verified checkpoint

- Baseline clean committed `3491933`;12 task files, index empty. No prior work discarded, staging/commits/reset/clean/branches, real package/source/legacy writes, process termination or desktop use. Full scope/files/manual checklist: [Gate5E save performance](package-format-v2-gate-05e-memo-save-performance.md).
- Profiled production Save Now on equivalent disposable10k/100k/1M fixtures before optimization. Repeated global validation dominated:100k809–819ms;1M~8.7s, versus~3–5ms UPDATE/commit. Baseline and optimized raw external gate5e-{baseline|optimized}-{count}.jsonl retained; do not rerun million-row calibration unnecessarily.
- Implemented Windows opt-in writer registration with pinned retained connection, same-connection data_version + fresh canonical manifest/catalog under BEGIN IMMEDIATE; full validation after other-connection commits/failures. Exact immutable target/nullable original/revision/generation/editor checks, DELETE/FULL, trigger verification and fresh persisted verification remain mandatory. No V1/schema/D1–D8/draft/debounce/UI changes.
- Attempt-limited publication proof reuses only proven unchanged admission on fresh verification/publication connections. Expiry now drains concurrent proof readers before coordinator release/origin reuse. Published browse reader owns a separate immutable admission token; memo refresh cannot conceal external structural changes. Unload drains/disposes; Windows rename/replacement blocked while pinned; explicit reopen creates new epoch. Uncertain cached commit retires registration/connection, requiring explicit reconciliation/reopen; failures retain drafts.
- Measured warm medians before→after:110.94→11.77ms /859.93→10.17ms /9117.59→10.03ms (baseline2 vs optimized4 samples). First saves173.72→95.88 /1256.65→539.62 /12320.93→5365.24ms. External-write medians65.84/540.19/5293.68ms still pay full validation. Same-owner other-asset saves~10–12ms; other connections invalidate. Explicit replacement/re-admission measured3 times per size. Warm allocation~1.8–2.2MB independent of count; journal observed12,824B, absent afterward; file lengths unchanged. Synthetic inline dispatch/limited samples, not OWL/rendered UI/p95/hardware durability guarantees.
- **Verified buildable:** final Debug solution/startup0 warnings/errors(11.66s), runner0 warnings/errors(4.16s); new admission26/26(1.60s); prior performance10/10 and production integration23/23 passed. Final current-source standard in-process xUnit **675 passed,1 existing permission skip,0 failed** (676 cases,54.18s). Not VSTest; transport not retried. Final expiry synchronization justified a regression rerun; current-source10k calibration first94.89ms/second14.01ms/warm median12.52ms, no global warm scans. Full three-size table predates only final bounded journal/expiry safety guards, explicitly disclosed. Scoped grant expired at continuation and was normally reauthorized, no project-path workaround.
- Canonical DDL equality, zero-Unity3 assemblies/Debug output, both genuine fixture SHA256s and protected legacy/V1 writer/builder/extractor/schema/design/fixtures pass. Docs14/174 local links/anchors/fences pass;18 Mermaid blocks syntax-reviewed, not rendered. All12 scoped tracked/new files pass staged/unstaged/new-file whitespace/conflict/control-character checks; index empty. No unfinished source or known failing test. Report/index/handoff finalized.
- **Next exact action:** user manual Debug checklist in the report on disposable copies, then deliberate review/commit of the12-file Gate5E diff. Launch E:/Projects/UABS/Avalonia/UABS/bin/Debug/net9.0/UABS.exe. Ready for isolated checkpoint review/commit, not rendered/real-OWL/platform/hardware-fault acceptance. First/external saves retain package-wide validation latency; hostile raw tampering and namespace ABA remain unsupported. No Gate6/structural editing/migration/recovery safeguards; no new Unity-exclusive decisions.

### Gate5D — final verified checkpoint

- Preserved all29 pre-existing Gate5B/5C changed/new files at HEAD 01e2ac8; combined scope35 files, index empty. No Git mutation, legacy Unity/real package/source write, desktop control or process termination. Report/files/manual checklist: [Gate5D performance](package-format-v2-gate-05d-memo-performance.md).
- Confirmed bottleneck with repeated equivalent disposable10k/100k/1M baseline: per-selection full admission/hashes and three full validations per save. Already-loaded indexed memo/draft text was immediate; revision eligibility, not debounce, stalled. Source SHA remains initial-session evidence, never rehashed per selection. Native UI/legacy scan timing and real OWL were not separately calibrated.
- Implemented opt-in Windows-pinned read-only connection admission reuse: snapshot-local manifest/identity + same-connection data_version, full validation on external commits/errors, new admission on unload/reopen, physical file identity and no cross-connection/TTL tokens. Non-Windows/non-pinned reads fall back to full validation. Writer admission is lazy; location-only hashes become bounded physical checks. Superseded presentation loads cancel without losing drafts.
- Saves retain full BEGIN IMMEDIATE validation and exact original nullable value/revision/generation/editor checks. Attempt-local reuse only skips duplicate global post-commit scans if the original connection detects no external commit and a Windows pin prevents replacement. Fresh-connection schema/manifest/exact persisted target verification always runs; full publication admission, stale Search and verified exact-row synchronization remain. No long-lived writer cache, schema/D1–D8 change or deferred recovery feature. Cleanup failures preserve known commit state.
- **Verified buildable:** final Debug startup/tests/runner0 warnings/errors; new deterministic safety/performance10/10 (1.11s), existing integration23/23 (2.11s); final standard in-process xUnit **649 passed,1 existing permission skip,0 failed** (650 cases,51.93s). Not VSTest; no restricted-transport retries. Original Debug DLL lock resolved by user closing UABS normally. Resumed scoped access was granted without project-reference edits.
- Warm revision-ready medians before→after:45.58→0.92ms /393.85→0.78ms /3829.37→0.87ms. Save medians151.05→95.01ms /1310.55→897.68ms /12597.37→9340.29ms. First save still pays writer admission (~1.29s at100k/~12.33s at1M). Warm reads allocate~8KiB rather than~4/32/315MB; saves still allocate and scan at package scale.1M sample overlap with1.82s incremental build and model/inline-dispatch limits are explicit, not p95/desktop claims. Six raw external gate5d-{baseline|optimized}-{count}.jsonl logs retained; do not rerun blindly. Owned fixtures disposed.
- Scope: new MemoTrace/performance tests/lifecycle helper; reader/validator/writer, facade/catalog/presenter and coordinator tracing; report/UI/index/handoff. Full write-transaction and publication scans remain intentionally O(package size); broader cache needs separate design. No unfinished source or known failing test. Final Debug solution/startup0 warnings/errors; zero-Unity references/output, canonical DDL equality, both genuine SHA256 hashes, protected legacy/V1 writer/builder/extractor/design/fixtures and staged/unstaged/new-file whitespace/conflict/control-character checks pass.13 docs/164 local links/fences pass;17 Mermaid blocks syntax-reviewed, not rendered.35 combined files, index empty. Preserve all prior work; no commit was created.
- **Next exact action:** user manual Debug checklist in the report with disposable copies: native/indexed Assets/Search readiness, rapid switching/shared drafts, Save Now/debounce/conflicts, exact rows/stale Refresh and V1/editor guard regression. Current launch target: E:/Projects/UABS/Avalonia/UABS/bin/Debug/net9.0/UABS.exe. Automated scope complete; large-save/real-corpus/desktop/platform/fault acceptance remains limited. No structural saving, migration, reindexing, Gate6 or Stage7 safeguards; no new Unity-exclusive decisions.

### Gate5C — final verified checkpoint

- Preserved all11 pre-existing Gate5B files at HEAD `01e2ac8`; combined worktree29 tracked/new files, index empty. Scoped Avalonia authorization only; no Git mutations, real package/source or legacy Unity writes. Files/architecture/manual checklist: [Gate5C report](package-format-v2-gate-05c-memo-integration.md).
- User's **filesystem Navigator** error was V1-only lookup excluding SQLite documents. Implemented captured session SHA256 + original source path/logical-name/signed-ID unique V2 bridge; indexed Assets/Search keep full UUID/revision/ordinal identity. No name/CAB-only targeting, guessed ordinal or ambiguity bypass. Native loading adds two hashes, not parser changes.
- `InspectorMemoService` routes V1 unchanged and V2 to the existing Gate4 writer/guard. Domain-separated epoch-bound identities and fresh nullable memo/revision basis adapt the original single draft coordinator. Assets/Search share1.5-second debounce, Save Now/Retry/navigation flush, failure/conflict retention and stale-completion protection. Verified dispatcher publication updates exact rows in place without Search rerun; old cursors require Refresh. Package Editor exclusion, unload-before/after-commit and newer package selection are protected. No old-epoch retargeting or shutdown/crash recovery.
- **Verified buildable:** final sequential Debug solution/startup/tests/runner0 warnings/errors; memo23/23 (2.22s), browse26/26 (3.10s); final standard in-process xUnit **639 passed,1 existing permission skip,0 failed** (640 cases,51.40s). Not VSTest; historical transport restriction not retried/bypassed. Initial obsolete UI assertion/raw-versus-effective command state and fixture SQL corrected without weakening safety checks; final unload-await race fixed and covered. No unfinished source or known test failure.
- One disposable100k profile completed; do not rerun blindly: reader364.11ms, basis383.47ms, production save1314.71ms;4 changed4KiB logical blocks, unchanged22,945,792B. Gate4 validation twice plus production publication readmission (third pass), no cache/validation weakening. Raw external scratch log `gate5c-memo-100000.log`; owned fixtures disposed. This is synthetic/warm single-sample evidence, not OWL/p95/UI latency. Cache invalidation design deferred.
- Zero-Unity references/output, approved canonical DDL, genuine fixture hashes, protected schema/validator/Gate4 writer/V1 package transaction/legacy paths, Git whitespace/conflict/scope checks pass. Twelve relevant docs' local links/fences validate;15 Mermaid blocks syntax-reviewed, not rendered. Report/UI/index/handoff finalized. No desktop control/process termination; read-only CIM inventory denied and not retried.
- **Next exact action:** user manual checklist in Gate5C report using disposable package copies and current `E:\Projects\UABS\Avalonia\UABS\bin\Debug\net9.0\UABS.exe`; review combined Gate5B/5C diff before deciding to commit. Automated scope complete, worktree safe to review/commit; visual/real-corpus latency/platform fault acceptance remains pending. V2 structural editor guard is service-tested because structural UI/save is unavailable. No Gate6/reindex/migration/Stage7 safeguards or new Unity-exclusive decisions.

### Gate5B — final verified checkpoint

- Baseline clean `01e2ac8`; previous extraction/creation/browse work is committed. No Git mutations or real package/Unity writes. Scoped Avalonia write permission granted. Baseline actual Debug startup build passes0 warnings/errors.
- Chosen explicit one-format-per-query policy; never ignore a selected package or unboundedly concatenate V1/V2 results. Reuse Gate3 engine with a narrow production operation, per-Search admission and evidence checks, original registration lifetime/epoch, bounded pages and read-only V2 result presentation. No V2 memos/writer/schema changes.
- Phase1 verified: new `V2SearchOperation.cs` and disposable `V2SearchIntegrationTests.cs`; expose browse lifetime/monotonic location epoch. Debug startup/tests/runner0 warnings/errors;3/3 focused integration cases pass, including federation/page exhaustion, unchanged counts, readmission after memo revision and unload/cancel. Fixed an adapter catch filter (InvalidDataException is not IOException) found by the new test; no existing tests weakened.
- Phase2 verified: format-aware scope/row mapping, paged Search lifecycle, read-only V2 Inspector, Refresh/Next controls and workspace lifecycle hooks integrated into existing VM/view. Debug startup/tests/runner0 warnings/errors and17/17 in-process integration tests pass (3.04s), covering actual headless bindings/styles, V1/mixed policy, exact identity/nullable names, criteria, revision/Refresh, scope, cancel/unload and stale completions. V1 nonnullable Result API preserved with explicit V2 rejection; no fabricated V1 targets.
- **Final verified buildable state:** Debug solution/startup/tests/runner0 warnings/errors; production Search18/18 and browse26/26; full standard in-process xUnit **616 passed,1 existing permission skip,0 failed** (617 cases,49.14s). Includes final admission-scope guard and boundary test. Obsolete Gate5A no-V2-Search assertion replaced with stronger exact-scope/read-only/no-V1-provider/shell-retention guards; no existing test weakened. Measurement helper outside xUnit class; initial nullable/analyzer/test-access issues fixed without suppression. Not VSTest transport; historical restriction not retried/bypassed. No desktop control or user-process termination.
- Completed one disposable100k/two-package performance run; do not rerun blindly. Browse admission419.88ms; Search admission185–381ms; first pages40–178ms, broad next155ms; actualVM first including admission531.60ms/next177.77ms,100 retained rows. Triggered cancel3.44ms/unload1.65ms,0 rows. Raw log external scratch `gate5b-search-100000.log`; fixtures disposed. Single warm-process observations, not medians/p95/real OWL/desktop acceptance. Later scope guard does not change measured SQL/workers/data; see report for exact values/limits.
- Final zero-Unity/DDL/genuine hashes/protected paths and11-doc/134-link/fence checks pass;14 Mermaid blocks syntax-reviewed, not rendered. Staged/unstaged/new-file whitespace/conflict review passes; index empty. Eleven files: new operation and integration/measurement tests/report; existing browse regression, browse lifetime/epoch, Search VM/view, shell workspace hooks, UI design/index/handoff. No package/library/extractor/writer/legacy changes. All work unstaged/untracked, no Git mutations.
- **Next exact action:** user manual Debug checklist in [Gate5B report](package-format-v2-gate-05b-search-integration.md), then review/commit if accepted. Launch current `E:\Projects\UABS\Avalonia\UABS\bin\Debug\net9.0\UABS.exe`. Implementation complete for scoped automated acceptance; no unfinished source. Restrictions/limits: mixed formats require separate queries; current-page grid sorting; per-query full admission and per-page full evidence hashing; no idle watcher; real825k latency, visual/platform/physical-I/O/resource limits unverified. No Gate5C/migration/structural writes/shutdown work authorized. No new Unity-exclusive decisions.
- Keep existing599-case baseline evidence (598 pass/1 permission skip), but this task requires current-source verification. VSTest transport was historically restricted; use labeled in-process xUnit if still unavailable. No desktop control. Do not begin Gate5C.

### Real-corpus extractor investigation — final verified checkpoint

- User supplied `E:\Media\UABS_Packages\OWL_V2.uabs.sqlite`; read-only issue SQL maps table failures to unflagged ordinal1 `.resS` entries, while ordinal0 serialized siblings are complete Unity2022.3.62f2/version22. Original package/source bundles untouched. Read grants obtained for package and recorded Steam source tree; worktree/staged23-file checkpoint preserved.
- Before code changes, reproduced negative-capacity and EOF on two disposable bundle copies under `UABS.PackageV2/artifacts/uabs-v2-corpus-8028d5a208bf4d8ab1d84ca2ae253a4f` (0.bundle/1.bundle), log in external scratch `corpus-before.log`. The vendored `IsAssetsFile` returns true on these flags0 resource blobs. IL shows signed version only checked `>99` (negative passes) and permits extremely short ASCII version strings, with no full header/range sanity. Original resource payload is then fed to metadata list allocations.
- **Do not repeat the old EOF reproduction:** it allocated ~28GB cumulative / process-lifetime peak~16.6GB before failing; no source writes/process termination. Implemented bounded V2 header preflight/classification, detailed table/range/parser diagnostics and reasoned budget exceptions. Flagged bad/future tables still fail with unknown counts/partial coverage; filenames never decide admission. No V1/vendor DLL/schema/Search/memo changes.
- All46 issues classified:19 false resource tables+19 Unavailable summaries+4 budget failures+4 Partial summaries. Rechecked disposable copies of all23 affected sources (`artifacts/uabs-v2-corpus-8bbb2a0daa8243279f70c43a7ba570ec`), hashes equal recorded package fingerprints;100,242 captured assets/per-source counts unchanged; actual tables all complete. Only4 dependency diagnostics remain: `m_PreloadTable/Array` counts42335/17418/26807/21481 exceed16384 at offset44. Generic I/O text was our empty budget exception, not demonstrated disk I/O. Keep limits/Partial warnings, no optimization or schema change.
- **Verified buildable:** user closed locked Debug application normally; Debug startup/solution/runner build0 warnings/errors. Focused extractor29/29 (1.64s), creation40 passed/1 existing permission skip (12.47s); full standard in-process xUnit **598 passed,1 skipped,0 failed** (599 cases,55.81s). No VSTest transport retries or desktop control. Final recovery changed documentation only; no unnecessary test rerun.
- Final three-assembly/Debug-output zero-Unity, canonical DDL equality, original genuine fixture hashes, protected legacy/V1 core/schema/Search/memo paths and staged/unstaged whitespace checks pass. Ten documents/121 local links/anchors and balanced fences pass;13 existing Mermaid blocks syntax-reviewed, not rendered. Conflict-marker scan passes;all9 untracked files pass trailing-whitespace review. Existing23-file staged checkpoint retained. No real-package/source writes or stage/commit/reset/clean/branch operations.
- Seven new regression cases in existing extractor tests; modified extractor/enrichment/fixture helper and new `V2SerializedHeader.cs`; updated Gate1 audit, Gate5A.1 report and handoff (eight task files). Existing staged23-file index/all other unstaged/untracked work preserved. Copies/logs retained; real packages/Unity sources only read. No Git mutations.
- **Next exact action:** review/commit the intended combined work deliberately (pre-existing23-file staged Gate5A checkpoint plus other unstaged/untracked follow-ups remain separate). This targeted fix is ready for review/commit with disclosed limits. User may create a disposable **new-name** package with current Debug to confirm corrected issue/completeness metadata; preserve the existing real package. No full825,730-asset rebuild or automatic repair was done. Affected-source counts are proven unchanged;8 remaining builder issue records are predicted, not a verified whole-corpus output. No Gate5B/5C. No new Unity-exclusive decisions.

### V2 Add Folder — final checkpoint

- Authorized recursive folder selection, no V1/schema/writer/Search/memo changes. Existing staged23-file Gate5A index and all prior unstaged/untracked work preserved. Normal Avalonia write access granted.
- Investigated V1 picker/VM/PackageFolderScanner/service: current-folder default in actual V1 code, opt-in recursion, header filtering then legacy Texture2D/Sprite references, sorted breadth-first scan, skipped links and directory failures. V2 explicitly defaults recursion on per this task; V1 unchanged. Reuse picker and exact V2 header/physical checks, not V1 reference creation.
- Implemented V2FolderDiscoveryService and VM/view wiring: incremental one-directory enumerator/one header handle, no reparse traversal, lexical root containment, deterministic final sort, canonical/physical dedup against prior selection, bounded diagnostics/progress, atomic merge only after success. No source-count cap. Any inaccessible/read failure preserves prior selection; unsupported/link skips are explicit nonfatal diagnostics. Retains only selected paths plus traversal/dedup metadata; not a constant total-memory or hostile namespace snapshot guarantee.
- **Verified buildable:** Debug startup/storage/tests/runner and final solution0 warnings/errors. Focused folder **14/14 passed** (7.39s), existing creation **40 passed,1 skipped** (11.96s); full standard in-process xUnit **591 passed,1 skipped,0 failures** (592 cases,55.75s). Not VSTest; known transport restriction not retried. Existing physical-hardlink fixture permission skip unchanged; no new native permission attempts.
- Actual257/1200 disposable-header discovery,1000 unsupported diagnostics, overlapping/manual merging, removal, simulated ACL/link/cycle/escape, cancellation and headless bindings pass. Initial fixture separator expectations corrected without weakening tests. Actual parsing remains existing genuine fixture coverage, not1200 game bundles. V1 folder/creation and V2 safe-publication regressions pass. No native junction/ACL/desktop acceptance claimed.
- Seven-file follow-up: new discovery service/tests; creation service read-only inspection helper, creation VM/view, report and handoff. Canonical DDL/genuine hashes/zero-Unity/protected paths/Git whitespace/conflicts and9-doc/108-link/fence checks pass; no new Mermaid. Owned disposable fixtures removed by owners, older ignored files preserved. Existing staged23-file Gate5A index untouched; all prior changes retained, no Git mutations.
- **Next exact action:** user manual current Debug Add Folder checklist in the creation report: default recursive tree, current-only toggle, overlapping/manual inputs, counts/removal, cancellation and disposable create/browse. Live native junction/ACL faults, picker/layout and large-game throughput remain acceptance limits. Implementation complete for this scope; no Gate5B/5C, source-count ceiling, schema/V1/Gate1/writer/Search/memo changes.

### Gate5A.1 source-selection follow-up — final checkpoint

- User requested removing the arbitrary256-file ceiling, accurate counts, removal and bounded resources. Original limit was V2-only: XAML label, VM validation, service validation; picker/builder/V1 had no source-count cap. Existing23-file staged Gate5A index and all other work preserved.
- Implemented no total-source cap; cancellable off-thread selection merge with one replacement notification, count/removal controls and standard ListBox virtualization. Sequential preparation/extraction/final identity+hash recheck replaces O(source-count) open source pins; only lightweight source evidence retained. Root grouping uses a dictionary; final hashes share one64KiB buffer. Final recheck is not a group filesystem snapshot: earlier rechecked files can change afterward; indexed revisions remain historical evidence. Use quiesced inputs when that stronger guarantee matters.
- **Verified buildable:** Debug startup/storage/tests/runner and solution0 warnings/errors. Focused standard in-process xUnit **40 passed,1 skipped** (41 cases,12.31s); full **577 passed,1 skipped,0 failures** (578 cases,49.13s). Not VSTest; prior transport restriction not retried. Existing physical-hardlink fixture skip remains (Win32 access denied); no bypass or new native attempts.
- Selection-only coverage256/257/1000/5000 paths; accumulated1000 paths, exact removal/count/duplicate behavior and cancellation preserving500 prior paths. Headless actual ListBox verifies virtualization and count/removal binding. Actual257/1000-source staged SQLite builds use tiny disposable supported-header inputs and a deterministic extractor, not1000 real game bundles. Tests prove sequential extraction and that noncurrent handles are released; cancellation and post-extraction hash-change rejection pass. Prior real86-asset and V1/safe-publication tests still pass.
- Six files changed in this follow-up: creation service/VM/view/tests/report and handoff. Canonical schema, real-fixture hashes, zero-Unity references/output, protected paths, Git whitespace/conflicts and9-doc/104-link/fence checks pass; updated Mermaid syntax reviewed, not rendered. Owned creation fixtures disposed; older ignored files preserved. No desktop automation, staging/commits, schema/V1/Search/memo changes.
- **Next exact action:** user manual current Debug acceptance with >256 real selected files, repeated Add Sources, count/removal/cancel and disposable creation/browse. Native dialog/platform limits, actual large-corpus throughput and physical-hardlink acceptance remain unverified. Source-limit implementation is complete; no replacement cap or next gate. Prior10k timing is historical; do not rerun blindly.

### Gate5A.1 final checkpoint

- Baseline `d42da6d`; all 23 Gate5A files are already staged by the user. Preserve that index exactly. New work remains unstaged/untracked; no Git mutation or protected data writes.
- Read task, policy, relevant creation/admission/extraction contract and production V1/V2 code. Normal scoped Avalonia write authorization obtained.
- Implemented optional builder progress/incomplete-coverage approval callback and narrow production orchestration service. Same-directory staged/no-overwrite writer and canonical schema unchanged. Source pins/header checks/physical duplicate rejection; fixed local Windows NTFS output only. No V1 intermediate. Complete report: [Gate5A.1 creation](package-format-v2-gate-05a1-creation.md).
- Creation service/form integrated into Packages, independently retained beside the V1 editor; explicit New Legacy V1 retained. Off-thread builder, coalesced progress, partial approval, post-publication verification and Gate5A registration/open wired. Original selected path spelling preserved separately from canonical physical comparison. Fixed local NTFS only; reject aliases/reparse/unsupported headers and existing output/sidecars.
- **Final buildable state:** sequential Debug startup/storage/test/runner and solution builds0 warnings/errors. Focused standard in-process xUnit **29 passed,1 skipped** (3.06s); full **566 passed,1 skipped,0 failures** (567 cases,38.89s). Not VSTest; prior transport restriction not retried. No desktop interaction/screenshots.
- One new physical-hardlink fixture is explicitly skipped: native creation denied (Win32 error5). Do not bypass/retry on this host; production physical-ID rejection exists but actual hardlink acceptance remains pending an authorized environment. Canonical duplicates pass. Fixture expectations corrected without weakening old tests:43 assets per genuine file/86 for two; serialized path evidence follows actual writer bytes.
- Disposable10k creation measurement completed once:1.736s total,1.681s builder,142.72ms validation,2.10ms publication,1,818,624B, batch256; registered/browsed100 rows. Peak70.73MB is process lifetime, not isolated UI/retained memory. Log in external scratch `gate5a1-creation-10000.log`; do not rerun blindly. Owned fixture files disposed; older ignored artifacts/logs preserved.
- Canonical DDL, three-assembly/shipped zero-Unity, genuine fixture hashes, protected-path scope, documentation links/fences and Git whitespace/conflict checks pass. New Mermaid syntax reviewed, not rendered.16 Gate5A.1 files changed/new; existing23-file staged Gate5A index preserved; new work unstaged/untracked. No commits/staging or Git config/history changes.
- **Next exact action:** user manual current Debug checklist in the creation report; verify the skipped physical-alias case only on an authorized host. Supported-path implementation is complete and safe for review/commit with these disclosed limits. No Gate5B/5C/structural saves, migration, shutdown/crash-recovery work authorized.

### Gate5A final checkpoint

- **Final verified state:** source and read-only UI integration complete. Debug solution/startup/library/tests/runner **0 warnings/errors**; current-source focused **26 passed**, full standard in-process xUnit **537 passed,0 failed/skipped,38.69s**. Not VSTest; known transport limitation not retried/bypassed. Headless visibility/bindings/styles are automated evidence; no desktop application/automation/screenshots or manual visual acceptance.
- Seven-document/90-link/anchor/fence checks pass; six Mermaid blocks syntax-reviewed, not rendered. Canonical DDL equality, genuine fixture hashes, three-assembly/shipped zero-Unity, native Win-x64 SQLite packaging, protected legacy/V1 core/V2 Search/writer/schema scope and Git/new-file whitespace/conflicts pass. Debug startup now intentionally ships V2/SQLite; earlier isolation assertions are historical. No unfinished SQLite/build sidecars or owned test/measurement fixture directories; older Gate3B ignored studies retained.
- **Scope/Git:**23 files (11 tracked modifications,12 new). Project reference, picker/filesystem recognition, package catalog union and main Assets routing; new presentation DTO/catalog/VM and three read-only view components; one bounded reader-evidence query; two test/measurement helpers; report/index/handoff. Index empty, all work preserved unstaged/untracked, no commits/staging/reset/clean/config/branch changes. V1 core/schema/V2 Search/memo writer unchanged; no real packages or Unity sources modified.
- **Exact next action:** user manual Debug checklist in [Gate5A report](package-format-v2-gate-05a-integration.md), then review/commit read-only integration. Launch `E:\Projects\UABS\Avalonia\UABS\bin\Debug\net9.0\UABS.exe`, not stale Release. Remaining limits: full admission/file-hash cost, no payload-byte/whole-process hard cap, physical aliases/non-Windows/fault/RID acceptance and no automatic file watcher (recheck on explicit operations/workspace return). Gate5B Search and5C editable memo adapters require separate authorization; no structural saves/migration/Stage7 work. No new Unity-exclusive decisions.

- Baseline clean `d42da6d`; Gate4 committed. Normal Avalonia write grant obtained. No legacy/real package/schema/writer changes authorized. No stage/commit/reset/clean/branch/config or desktop control.
- Inspected production shell, retained workspaces, V1 catalog/editor/AssetList and V2 reader. V1 `AssetEntry` cannot represent nullable IDs/names or exact V2 provenance; do not coerce V2 into V1 documents/sessions. Use stable presentation DTOs, shared bounded V2 browse state in existing Assets/Packages regions and reader-only registration adapter. Gate5B Search/5C editable memos stay off.
- Baseline Debug solution passed0 warnings/errors. Reader adapter/DTO/bounded VM and production startup integration compile0 warnings/errors. Added read-only package/asset/inspector surfaces in existing Assets/Packages regions; V1 catalog objects remain separate from V2 and V2 does not enter V1 Search/editor/memo paths. New shared green grids use approved Kind52/0–80/native resizing/clipping.
- Admission uses existing `PackageReader`, once per registration. Pages verify physical Windows file identity + full main-file SHA256 before/after plus existing transactional catalog/generation checks. Windows pinned read handle denies rename/delete until unload; no source Unity files opened. No unsound validation cache or memo-writer/schema changes. File hashing is conservative O(database bytes); measure costs separately.
- Earlier checkpoint: Debug reader/startup/test/runner0 warnings/errors; focused25/25 and full536/536 passed before the final late-cancellation ownership regression. Initial test exception/name/issue-fixture assumptions corrected without weakening validation. Actual headless workspace containers show V2 and disable V1 editor/Inspector paths; final26/537 results above supersede these counts.
- **All three disposable diagnostics completed:**10k/100k/1M admission48.93/430.56/3945.44ms; next5.77/37.59/357.07ms;100 visible asset rows and1 evidence row at every size.1M single-file237,584,384B/setup133.72s. Churn4.80/32.90/315.49MB; negative live-heap deltas are baseline-temporary retirement, not isolated UI memory/peak proof. Owned fixtures disposed; logs `gate5a-browse-10000/100000/1000000.log` in external scratch. Do not rerun completed measurements.
- Review found a late-cancellation ownership edge after successful admission. Moved visible registration bookkeeping before selection cancellation, added a deterministic commit-boundary test; no late selection/rows and no invisible reader/pin. Final26/537 regression verification and audits completed; measured reader/admission/page plans unchanged. No next gate or real data writes.

### Gate 4 final checkpoint

- **Recovery completed 2026-10-22:** no source/test changes or test reruns. Portable-PDB SHA-256 hashes match the current three memo source files and `V2MemoTests.cs`; prior Debug0-warning/45+49 focused/511 full in-process results remain valid evidence. Finished normal authorized Git safety checks after prior usage-limit reviewer interruption; index empty, expected seven-file scope preserved. DDL/zero-Unity/startup isolation/fixture hashes and six-doc/82-link/fence checks pass; five Mermaid blocks syntax-reviewed, not rendered. No automatic staging/commit or desktop control.
- Read-only performance diagnosis recorded in the Gate4 report: `StorageSchema.Validate(...,false,...)` runs twice on successful/unchanged saves (write transaction and fresh verification), repeating schema signatures, quick_check/FK, global semantic/count/locator checks and all-asset derived-text reconstruction. `integrity_check` is not used on this path; the reference canonical signature is cached once, actual DB signature is not. Registration validation is separate. No per-phase timing attribution or validation weakening; admission caching/targeted validation require a separate reviewed invalidation/trust policy. Gate4 verification complete; review/commit isolated checkpoint, do not begin Gate5.

- Started clean at `e656b00` (Gate3B committed); normal Avalonia write permission granted. V1, production UI, schema, legacy and real packages are protected. No stage/commit/reset/clean/branch operation authorized.
- Read approved D6, canonical trigger behavior and existing V1 coordination/draft contracts. Reuse injected `IPackageWriteCoordinator`; isolated immutable registration/addressable target, transactional writer, editor lease/base-revision guard and verified delta contracts only. Draft coordinator is V1-key-specific: no duplicate coordinator; defer identity/revision integration to Gate5 and provide stateless completion contract.
- Baseline Debug solution/startup build passed0 warnings/errors. Implemented `UABS.PackageV2/Memos/{MemoContracts,V2PackageEditorGuard,V2MemoPersistenceService}.cs`, disposable `V2MemoTests.cs` and draft Gate4 report/index link. Reuses shared coordinator, local editor lease/base checks, immediate transaction, exact nullable/revision preconditions, trigger SELECT, fresh verification and awaited exact delta publication. No production services/XAML/schema changes.
- **Final verification:** Debug solution (including actual startup), library/tests and scratch runner build0 warnings/errors; **45/45 memo +49/49 storage** focused in-process checks pass. Full standard in-process xUnit **511 passed,0 failed/skipped,32.56s**. These are not VSTest results; known transport restriction not retried/bypassed. Initial test-only compile/analyzer issues fixed. Final source verified buildable; no active builds/tests/measurements.
- Disposable five-save diagnostic (not acceptance/p95):10k median99.60ms,100k854.06ms; same file lengths2,224,128/22,945,792 bytes,4 changed4KiB blocks each. Full validation remains O(metadata), not an O(1) save claim. All measurement processes finished and owned fixtures disposed; never repeat against real packages.
- Three-assembly zero-Unity and startup-isolation audits pass; approved DDL exact normalized equality, genuine fixture hashes and protected production/V1/legacy/schema paths preserved. Documentation local links/fences and five Mermaid blocks checked (syntax only, not rendered); Git index empty, whitespace/conflict checks pass. No unfinished build/SQLite sidecars or owned disposable test directories. Previous ignored Gate3B calibration studies deliberately retained. No real packages/Unity bundles modified and no desktop control.
- **Files/Git:** three new `UABS.PackageV2/Memos` source files, new `V2MemoTests.cs`, new [Gate4 report](package-format-v2-gate-04-memos.md), index and this handoff (seven files). Unstaged/untracked, no commits/staging/reset/clean/branch changes. Read the report for transaction boundaries, structured commit states and deferred draft integration.
- **Exact next action:** review/accept isolated Gate4 foundation. No unfinished Gate4 source. Gate5 requires separate explicit production approval and existing draft-coordinator key/revision adaptation (no second coordinator), dispatcher/monotonic exact-identity publication, authoritative registration lifecycle and strict editor checks inside future transactions. V2 debounce/Inspector/editor UI integration remains intentionally deferred; full validation still O(metadata), sustained reader contention/physical I/O, independent-process/RID/fault and uncertain-COMMIT I/O injection remain unvalidated. No new Unity-exclusive decisions. Gate3B budget approval and Stage7 shutdown/crash recovery remain separate; do not start Gate5 automatically.

### Gate 3B final checkpoint (historical)

**Final status:** implementation/calibration complete and verified buildable; proposed performance budgets need user approval. No Gate4/5 work authorized. The following bullets retain incremental evidence; this final status supersedes their pending-check notes.

- **Latest verification:** Debug solution/startup build0 warnings/errors; current-source focused54/54 entries (53 functional plus inactive benchmark); full standard in-process xUnit466 passed/0 failed/0 skipped,29.05s. Not VSTest; known transport restriction not retried/bypassed.
- Final source: proven-constant sort/keyset folding, redundant predicate elimination, configurable1MiB SQLite value/record and16MiB retained-text budget, global cancellation on any selected registration unload. Strict Unicode/identity/order/schema/V1 semantics preserved. No production integration or persistence changes.
- Final-source100k three-process20-repeat medians (ms): exact54.03,path16.75,ASCII48.75,CJK68.02,mixed94.14,excludes157.65,memo63.17,broad92.41,text94.90,class70.09. Broad baseline137.71ms/303.10MB pair →92.41ms/80.41MB.1M broad~767ms;excludes~1456ms,2.21GB pair churn remains a risk. Final128 broad202.53/204.15ms first/next; all scope workload p95≤248.31ms. Cancellation~4–7ms; final1M unload-and-page1.33ms/128 packages2.08ms,0workers/no partial rows.
- Final raw logs: scratch `gate3b-finalsteady100k-p1..p3.jsonl`, `gate3b-final128-p1..p3.jsonl`, `gate3b-final-resources.jsonl`; earlier size/deep/profile logs remain valid, only lifetime linking changed afterward. All fixtures/study hashes verified. Ignored closed `UABS.PackageV2/artifacts/calibration-*` DBs/manifests intentionally retained. No benchmark still running; do not blindly repeat completed runs.
- Final audits pass: three assemblies zero Unity, startup output no V2/SQLite, canonical DDL equality, five docs/73 links/balanced fences/four syntax-reviewed Mermaid blocks, protected paths, fixture hashes and staged/unstaged/new-file whitespace/conflicts. Removed only newly generated regression fixture log; no incomplete DB/build sidecars. No desktop use.
- **Worktree:** index empty; four `UABS.PackageV2/Search` files, modified `V2SearchTests.cs`, new `V2SearchCalibration.cs`, Gate3 report and this handoff (eight files). No user changes discarded; no stage/commit/reset/clean/branch operation. Safe to review/commit.
- **Exact next step:** review [Gate3B report and proposed budgets](package-format-v2-gate-03-search.md#provisional-reference-budgets--approval-required), approve/revise reference envelope; decide outstanding production all-kind/multi-source, physical-cold I/O/temp-disk/RSS and RID/fault evidence. Budgets not user-approved; no universal production acceptance claim. No new Unity-exclusive decisions. Do not start Gate4/5 automatically.

### V2 Gate 3B checkpoint

- Baseline clean at `53c7bea`: Gate 3 is committed. Preserve historical 100k/1M first-page medians (Path24/23ms, exact83/485ms, CJK150/1312ms, broad215/1962ms), not guarantees. Schema/production/legacy/real packages remain unchanged.
- Added opt-in calibration helper only; reusable **owned synthetic** corpora retain closed DBs plus SHA-256 manifest for identical before/after/fresh-process reads. No production optimization yet. Existing Gate 3 benchmark workload definitions, materialization, fields, sort and page100 remain unchanged; Class+text and many-package/deep probes are explicitly additional workloads.
- Helper build passed with zero warnings/errors. Prepared owned, hash-manifested 100k/eight-package study at `UABS.PackageV2/artifacts/calibration-9b3505cac86d49219c9c8f3e2c3f6516`. Do not delete/recreate or mutate this before completing identical-data comparisons.
- Profile: first broad page272096 predicate/224976 sort callbacks,129734736B allocation,17 connections,100 emitted/101 buffered. Same-ID controlled source-sort variant reduced28095→9349 comparisons and9814424→4118712B; single-sample timing not a speedup claim. Also identified redundant no-exclude and duplicate category predicates in current SQL.
- **Three fresh-process baseline runs completed**, all counts pass:20 measured first/next pairs after two warmups, ten workloads (existing nine unchanged + Class/text). Complete raw logs preserved in scratch `gate3b-baseline100k-p1..p3.jsonl`; do not rerun them blindly.
- Implemented isolated candidate optimizations: capture exact constant source-sort values on admission; omit only proven constant local sort levels and fold their global keyset comparisons; eliminate no-exclude UDF calls and duplicate asset category tests. Package-row NULLs disable source folding, mixed sources retain differing levels. Added regression cases for all sorts/directions/mixed sources and 8/32/128 selected packages.
- Narrow resource correction: configurable SQLite value/record length cap1MiB and retained UTF-16 candidate/emitted-memo text budget16MiB, returning BudgetExceeded rather than truncating. **Optimized Debug build0 warnings/errors and54/54 focused entries pass** (53 functional/one inactive benchmark), including all new mixed-source/scope/guard cases. Baseline binary was used for all completed baseline runs.
- Latest helper rebuild passes0 warnings/errors. Three optimized100k process runs completed20 repeats each/allten workloads/allcounts match immutable data hashes. Median first-page before→after: exact73.60→55.08, path15.89→16.26, ASCII63.09→48.55, CJK95.46→67.72, mixed140.55→92.78, excludes209.36→153.43, memo94.45→63.05, broad137.71→92.00,textsort123.09→95.99,class97.25→68.42ms. Broad pair churn303.10→80.41MB; optimized profile101048 predicate/74992 sort callbacks,39709016B. Logs `gate3b-after100k-p1..p3.jsonl` preserve raw results.
- Dataset preparation and **all18 additional fresh-process runs completed successfully**:10k20repeats,1M3repeats/allten cases; equal8192-row8/32/128 mixed-hit scopes20repeats plus exact exhaustion/order/membership through42–43pages; separate2048-row8Ki-character memo study20repeats. All independent counts and input hashes pass. Raw logs `gate3b-<study>-p1..p3.jsonl`; studies/hashes `gate3b-studies.jsonl`. Do not rerun completed data preparation/calibration blindly.
- Additional diagnostics completed: cancellation3.9–6.5ms; steady live-managed0.62–1.35MB while repeatedly retaining one100-row page; native allocation0 after close. SQLite warm controlled query264 cache hits/0misses; sparse128 profile352 connections/96rows/0sortcallbacks exposes connection overhead, not unbounded concurrency.
- **Measured unload correction:** previous1M invalidated page took830ms to finish because other selected workers continued. Linked each page/count operation to all captured registration unload tokens; peers and queued work now cancel together. SQL plans/workloads unchanged. Final rebuilding, focused/full tests and targeted calibration/resource verification completed; see final status above. VSTest restriction unchanged.

### V2 Gate 3 implementation checkpoint

- Started clean at `1af9383`; index empty. Gate 2A is committed/preserved. Only isolated V2 Search, disposable tests and docs authorized; schema, V1 Search/UI/persistence and real/legacy data remain unchanged.
- Added Search contracts/session/registration, conservative .NET predicates, query-time OrdinalIgnoreCase-then-Ordinal sorting, mixed-direction/null keysets and bounded top-K federation. No long-lived database handles or full-result hydration; emitted memo contents load only after global selection. Registration unload drains worker leases; captured revisions are checked before publication.
- **Checkpoint 1 verified:** Debug storage/test/startup dependency build and rebuilt external runner pass with zero warnings/errors. **34/34 standalone Search checks pass**, covering V1 differential matching, all seven sort fields in both directions, one-row global keysets, unknown classes/null names, exact identity, source raw evidence, partial/count states, revisions/generations/unload, cancellation, budgets and recoverable failures. Fixed nullable type inference and missing CTE aliases for non-Package-only queries. These are not VSTest results.
- **Checkpoint 2 verified:** latest Debug build is zero-warning; **41/41 focused entries pass** (40 functional plus inactive opt-in benchmark). Added deterministic queued/count/sort cancellation, unload draining, NULL sort values and ambiguous identical-row tie cases. Foreign-session cursors return StaleCursor. Per-query global top-K metrics remain bounded. Removed constant per-package SQL sort comparisons and per-row predicate closures; all focused checks pass afterward.
- Current-source 10k and 100k/eight-package benchmarks completed (20 measured repeats after two warmups, independent counts pass): 100k first-page exact/path/ASCII/CJK/mixed/include-exclude/memo/broad/text-sort medians **82.83/24.24/98.66/149.86/220.51/332.38/148.10/215.32/191.24 ms**. Maximum buffer 101 candidates/two workers; managed allocation churn is substantial despite bounded retained rows. Final-source 10k medians **29.50/20.99/30.59/34.10/48.09/53.68/39.94/44.00/43.52 ms**. Earlier pre-optimization 10k values are superseded, not a speedup comparison.
- **1M/eight-package sample completed:** exact/pathid/cjk2/broad only, two warmups plus three measured repeats, independent counts pass. First/next medians **484.55/493.52**, **23.00/no next**, **1311.70/1457.67**, **1961.96/2426.78 ms**. Max buffer101/workers2/process lifetime peak~78.0 MB; CJK/broad managed churn1.80/2.90GB per page pair. Setup122.388s, admission3.320s,204,013,568B. Do not rerun completed measurements blindly. VSTest transport was not retried. No storage scale benchmarks were rerun; these are new Search fixtures only.
- **Final verified checkpoint:** sequential Debug solution (including actual startup) build **0 warnings/errors**; full standard in-process xUnit **453 passed, 0 failed/skipped, 19.58s**. Focused **41/41** includes40 functional/one inactive benchmark. These are not VSTest results; known transport restriction not retried/bypassed. Source is verified buildable and production behavior remains intact; no desktop interaction.
- Four Search implementation files, `V2SearchTests.cs`, [Gate 3 report](package-format-v2-gate-03-search.md), index and this handoff are the only changes (eight files, index empty). Canonical DDL equality, three-assembly zero-Unity/production-output audit, genuine fixture hashes, protected paths, staged/unstaged/new-file whitespace/conflict and **5-doc/71-link** checks pass; four Mermaid blocks syntax-reviewed, not rendered. Removed only newly generated full-test log; disposable artifact root empty. No commits/staging/reset/cleanup of existing work.
- **Next exact task:** Gate 3 performance calibration/acceptance, not Gate 4. Review proposed budgets in the report, profile scan/collation allocation churn, then complete three-process, 32/128-package, long-memo/skew, late-page and native/temp-disk resource measurements under approved thresholds. Numeric budgets are unapproved; current 10k/100k/1M samples are not full acceptance. The implementation is a safe checkpoint, but do not mark Gate 3 performance accepted. No benchmark is still running and no completed measurement needs blind rerunning.

### V2 Gate 2A checkpoint

- Started at clean `5505cbb`; index empty. Normal Avalonia write access granted. Preserve the committed Gate 2 implementation and all subsequent work; no production/legacy/package-data edits.
- User explicitly approved the pre-release V2.0 amendment on 2026-10-20: nullable `dependency.raw_virtual_path`, exact original serialized spelling in `raw_path`, unchanged CAB/GUID/type/order. Keep manifest 2.0 / user_version 20000; structural admission rejects earlier missing-column files, with no migration.
- Schema/design and builder mapping updated; focused regression cases added for independent/NULL/empty/Unicode paths, normalized-path non-substitution, order/evidence and old-schema rejection. Generic reader pages and canonical structural validation already include the new column without a separate DTO/API.
- **Final verified state:** Debug storage/test/startup and solution builds pass with **0 warnings/errors**. Focused standalone **49/49**; full in-process xUnit **412 run, 0 failed/skipped, 12.46 s**. These are not VSTest results; the previously blocked transport was not repeatedly retried or bypassed. No benchmark reruns.
- **Files changed:** `UABS.PackageV2/{Schema.sql,PackageBuilder.cs}`, `UABS.UnityAssets.Tests/V2StorageTests.cs`, design, storage report and this handoff (six tracked unstaged files, empty index). No reader/DTO branch required: generic dependency pages expose the new column, canonical structural signatures enforce it. Original/virtual NULL/empty/Unicode/spelling, CAB/GUID/type/order, missing-original rejection and old-schema read-only rejection all pass.
- Canonical DDL equality, three-assembly zero-Unity/production-isolation audit, genuine fixture hashes, protected-path scope, staged/unstaged whitespace/conflict and documentation checks pass: **3 docs / 24 local links/anchors**, balanced fences, three syntax-reviewed Mermaid blocks (not rendered). Removed only the newly generated test log; disposable artifacts are empty. Repository is verified buildable; nothing staged/committed.
- **Next exact action:** review/accept isolated Gate 2/2A. No unfinished Gate 2A implementation. Genuine multi-file/duplicate-CAB/version identity and native/filesystem faults remain prerequisites for production reliance; VSTest transport still needs an authorized functioning environment. Historical scale measurements below predate this schema amendment. Obtain a separate explicit Gate 3 request before Search work; no production integration, memo adapters, migration or Stage 7.

### V2 Gate 2 recovery checkpoint (historical; Gate 2A above supersedes schema status)

- **Final verified checkpoint:** implementation, builds, scale measurements and documentation are complete for isolated Gate 2. The [storage report](package-format-v2-gate-02-storage.md) is authoritative for detailed evidence/limitations. Worktree remains unstaged/uncommitted at `921de5f`; prior files preserved, no V1/Gate 1/approved DDL or legacy/package-data changes.
- Final Debug storage/test/startup builds: **0 warnings/errors**. Final standalone focused harness **41/41**; standard in-process xUnit executor ran the **full assembly: 404 passed, 0 failed/skipped, 12.61 s**, including zero-Unity and headless regression tests. These are **not VSTest results**. Initial VSTest 33/33 is historical; exactly one resume-focused VSTest attempt stalled without results and was cancelled, with no retries or permission bypass. Normal VSTest transport remains unavailable.
- Recovered all three-scale medians; no benchmark rerun: **10k 0.803 s / 1,806,336 B; 100k 8.049 s / 18,083,840 B; 1M 80.975 s / 185,876,480 B**. All exact streamed read-back counts pass; peak process lifetime observed ~50.6/53.0/55.7 MB, not per-extraction or retained allocation. No benchmark is active.
- **Acceptance/next exact action:** safe, buildable limited checkpoint; review/approve the dependency schema/adapter amendment before claiming general input support. Proposed minimum is one nullable `raw_virtual_path` plus exact original spelling in existing `raw_path`, with explicit version/admission decision. Until approval, keep current rejection of distinct original/virtual paths. Obtain genuine multi-file/duplicate-CAB fixtures before production identity reliance. Gate 3 still needs its own request; no integration/migration/Stage 7 work.
- Added library files `UABS.PackageV2/{UABS.PackageV2.csproj,.gitignore,Schema.sql,StorageContracts.cs,StorageSql.cs,PackageBuilder.cs,PackageReader.cs}`, `V2StorageTests.cs`, solution/test reference and report/index/handoff only. App has no V2 dependency/binaries; schema equality/reference audit pass. Scratch harness remains outside Git at `C:\Users\kolynlin\Documents\Codex\2026-10-16\files-pasted-by-the-user-i\v2-storage-verification` (`Verification.dll` arguments: none=focused, `full`=xUnit assembly; do not rerun completed benchmarks).
- Final Debug solution build also passes with zero warnings/errors. **3 docs / 54 links/anchors**, fences/one reviewed Mermaid block, whitespace/conflict and protected-file checks pass; index remains empty. Removed only the newly test-generated `uabs_log.txt`; fixture artifacts are clean, no benchmark active. Current work is verified buildable, with the explicitly documented schema/input and VSTest-route limitations above.

Earlier recovery checkpoints (historical; superseded by the verified state above):

- Resume verification: existing work preserved at `921de5f`. **Three 1M benchmark runs completed**, not rerun: median build **80,974.98 ms**, ingestion 68,618.73 ms, seal 12,286.14 ms, **185,876,480 bytes**, admission 2,653.48 ms, first/next 100 rows 1.01/0.55 ms, streamed full read 3,155.40 ms; exact 1M counts passed. No active benchmark needs continuation.
- Storage, test-project and actual startup **Debug builds now pass with 0 warnings/errors** after renewing the scoped Avalonia filesystem grant. The first resume build's denied `obj/bin` writes were environmental, not a source compile failure; no target substitution or security setting change.
- Verified dependency mismatch from vendored `AssetsFileExternal.Read/Write` IL: raw virtual path and raw declared/original path are independent serialized strings; `PathName` may normalize built-in resource paths. Proposed minimum: separately approved nullable `raw_virtual_path` plus `raw_path` mapped to original serialized spelling; normalized locator need not be canonical. **No schema/adapter change approved or implemented; rejection remains.** Full detail/version options are in the report.
- One focused VSTest attempt was pending at this earlier recovery point; its final blocked result and completed full-xUnit verification are recorded above.

- Checkpoint 2: [Gate 2 storage report](package-format-v2-gate-02-storage.md) records APIs, unchanged embedded DDL, safety and current metrics. Expanded **40/40 standalone storage assertions pass**; Debug builds compile with zero warnings/errors. Initial VSTest 33/33 passed; later runner/testhost handshake timed out before execution, so no complete-suite pass is yet claimed. Network permission was not granted; no control bypass or process termination. Offline single-node restore works.
- Explicit permission granted to `UABS.PackageV2/artifacts` for disposable outputs after OS-temp publication was denied. `UABS_V2_TEST_ROOT` selects this ignored root. At that earlier checkpoint, 10k/100k medians were complete and 1M work was active; the final completion above supersedes that pending state. Never delete live staging/journals.
- Next exact steps: finish 1M measurements, complete full-suite verification through a normal in-process xUnit executor if VSTest transport stays unavailable, verify explicit Debug startup build, then final docs/whitespace/protected-path checks. No production application references the new library; only solution/tests/storage/docs changed. Schema mismatch for distinct external virtual/original paths remains an explicit rejection/decision, not an automatic schema change.

- Started clean at `921de5f`; no staged/unstaged/untracked work. Authorized scope is isolated storage/tests/docs only. V1 application, approved DDL, real data and Gate 1 implementation remain unchanged.
- Plan: separate storage library (SQLite provider confined to storage/tests, no app reference), canonical embedded SQL, provisional per-source ingestion and final semantic sealing, no-overwrite staged publication, read-only bounded metadata admission/API, deterministic disposable safety tests and measured 10k/100k workloads (1M if practical).
- Gate 1 real multi-logical-file/duplicate-CAB production identity acceptance remains open; Gate 2 synthetic round-trips cannot close it. No generation replacement/carry-forward, Search, memo/UI adapters, migration or Stage 7 work.
- Current baseline is prior verified Gate 1: 362 passing tests, zero-warning Debug builds (historical; fresh verification pending). Next exact step: implement/compile storage contracts and schema, then incremental builder/reader tests. No automatic staging or commits.
- Checkpoint 1: new `UABS.PackageV2` library compiles with zero warnings/errors; application has no reference to it. Embedded `Schema.sql` matches the approved fenced DDL. Initial focused run **33/33 passes** (one opt-in benchmark entry does no timing work unless enabled). Genuine bundle/standalone packages each round-trip 43 assets; synthetic gaps/duplicates, partial acknowledgement, rejected-source cleanup, no-overwrite races, fault boundaries, read-only admission/corruption/profile fallback and bounded pages pass.
- Provisional rows stay in private staging; a failed source retains diagnostics/source status but loses all its assets/types/files/declarations. Known Gate 1→DDL mismatch: distinct nonempty external virtual/original paths lack canonical columns, so builds reject lossless-unrepresentable inputs rather than change schema or discard evidence. Real multi-file identity gate remains open. Next: additional count/evidence/admission tests, isolated repeated 10k/100k measurements, final Debug/full-regression/audit/docs checks. Current source compiles; no production routing changed.

### V2 Gate 1 recovery checkpoint

- **Final checkpoint:** isolated Gate 1 implementation verified. See [Gate 1 report](package-format-v2-gate-01-extraction.md) for contracts, real/synthetic evidence, measured costs and limitations. Source files: `UABS.UnityAssets/Metadata/{V2MetadataContracts,V2ReadWindow,V2MetadataEnrichment,AssetsToolsV2MetadataExtractor}.cs`; tests: `V2MetadataExtractionTests.cs`, `V2MetadataFixture.cs` plus existing test-project parser reference. Report/index/handoff updated; no other production code or approved schema changed.
- Sequential relevant and startup Debug builds: **0 warnings/errors**; focused **22/22**, full **362/362** (baseline 340), including zero-Unity audit. Buildable checkpoint confirmed. Full suite explicitly used `--no-build --no-restore`; an earlier summary-less invocation and cancelled audit-only attempt are not counted as successful checks. Dedicated completed cost probe records three-run small-fixture medians in the report.
- **Acceptance boundary:** implementation complete, real multi-logical-file/duplicate-CAB identity verification remains open. Synthetic resource-gap/duplicate-name/CAB cases do not replace genuine fixtures. Next exact task: obtain read-only genuine fixtures, validate occurrence identities, then seek separate Gate 2 builder/reader authorization. Do not start Gate 2, production integration, migration or Stage 7 automatically.
- Source/revision UUIDs remain caller-assigned; before/after full SHA-256/size/timestamp gates final eligibility. All streamed rows are provisional and future consumers must discard on failure/cancellation/mutation. Bounded callback batches do not bound the parser's full metadata allocations; name/dependency enrichment is optional, bounded and never full payload decode. Test fixtures/source hashes are unchanged; V1 and memo paths remain untouched.
- Git remains unstaged/uncommitted; only scoped source/tests/docs changes, with no user processes killed or desktop interaction. Final checks pass: **3 documents / 53 local links/anchors**, balanced fences / one syntax-reviewed Mermaid block, staged/unstaged/new-file whitespace/conflict and protected-path scope checks. Final original-path/full-path distinction is covered by the repeated **22/22 focused**, **362/362 full**, and zero-warning Debug builds; no source edit remains unverified.

- Earlier incremental checkpoints follow:
- Started from clean `9f39b6d`; index empty. Gate 1 only is authorized: new neutral extraction contracts/adapter/tests, no production composition, database, Search, memo or schema changes.
- Baseline inspection confirms the old session factory substitutes display names and loses original logical ordinals. It will remain unchanged; the new adapter must retain raw nullable names and original directory/table occurrence identities, including duplicates.
- Available committed real fixtures: `aigirl.ab` and `aigirl.assets`, previously one logical file each. Real multi-logical-file/duplicate-CAB validation is still open; synthetic tests must not be reported as genuine fixture evidence.
- Build/tests pending for this checkpoint. Next: establish baseline, implement isolated contracts and bounded extraction, then focused tests and Debug verification. Original sources, packages and legacy Unity remain read-only; no staging/commit or desktop control.
- Checkpoint 1: baseline **340/340** passes; isolated contracts, non-owning bounded streams, metadata enrichment and extractor compile. Initial **9/9 focused tests** pass for both 43-asset real fixtures, class retention, verified fingerprints, bounded batches, name budgets, structural-only mode, caller IDs, cancellation and callback failure. V1 source/composition is unchanged. Next: deterministic disposable multi-file/duplicate/malformed metadata fixtures and dependency/GUID coverage, then final builds/full suite/audit/docs. No claim of real multi-file validation yet.
- Checkpoint 2: **22/22 focused tests pass**. Disposable synthetic byte fixtures validate ordinals 1/3 across resource gaps, duplicate/empty logical names/CABs, duplicate/signed/large Path IDs, unknown classes, empty versus absent/failed names, GUID encoding, ordered externals, corrupt siblings and a 10k-row/31-record batch sink. Bounded dependency walker tests preserve empty/duplicate strings and reject excessive arrays. Controlled timestamp evidence validates post-parse mutation rejection without modifying fixtures. Fixed new-adapter malformed-name and invalid-header handling; V1 remains untouched. Relevant/test Debug builds compile through focused tests. Next: final Debug builds, full suite/audit, measured-cost report and doc/Git checks; real multi-file validation remains incomplete.

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
